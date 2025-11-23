using UnityEngine;
using System.Collections;
using DG.Tweening; // DOTween kütüphanesi (Roll animasyonları için)

namespace IndianOceanAssets.Engine2_5D
{
    // [Modülerlik] Gerekli bileşenlerin otomatik eklenmesi, manuel hata yapma riskini azaltır.
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(SwordAttack))]
    [RequireComponent(typeof(ProjectileShooter))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerStats))] 
    
    public class PlayerController : MonoBehaviour, IAttackStateProvider
    {
        #region Variables & Configuration

        [Header("Stats Data")]
        // Oyuncunun hız, can vb. verilerini tutan ScriptableObject referansı veya komponenti
        private PlayerStats playerStats;

        [Header("Attack Settings")]
        [Tooltip("Oyuncunun saldırı türünü belirler.")]
        [SerializeField] private AttackType attackType;
        
        private enum AttackType
        {
            SwordSlash,      // Kılıç Saldırısı
            ProjectileShoot  // Menzilli Saldırı
        }
        
        // --- Component References (Bileşen Referansları) ---
        private Rigidbody rb;
        private Animator animator;
        private PlayerInputHandler inputHandler;
        
        // [Optimizasyon] Kamera referansını her karede Singleton üzerinden çağırmak yerine
        // Start metodunda bir kez alıp burada saklayacağız (Caching).
        private CameraFollow cachedCameraFollow; 

        // --- State Variables (Durum Değişkenleri) ---
        private Vector2 inputDirection;
        private float lastRollTime;
        private bool isRolling;
        
        // Property: Dışarıdan sadece okunabilir, içeriden değiştirilebilir.
        public bool IsMoving { get; private set; }

        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
            // Bileşenleri önbelleğe alıyoruz (Caching components)
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            inputHandler = GetComponent<PlayerInputHandler>();
            playerStats = GetComponent<PlayerStats>();
            
            // [Mobil Optimizasyon] Mobil cihazlarda pil tasarrufu ve stabilite için 
            // FPS'i 60'a sabitliyoruz.
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            InitializeAttackComponent();
            InitializeStats();
            InitializeCameraReference();
        }

        private void Update()
        {
            HandleInputProcessing();
            UpdateMovementStatus();

            // Eğer takla atmıyorsa normal hareket animasyonlarını ve yönünü güncelle
            if (!isRolling)
            {
                AnimateMovement();
                FlipCharacter();
            }
        }

        private void FixedUpdate()
        {
            // Fizik işlemleri her zaman FixedUpdate içinde yapılmalıdır.
            if (!isRolling)
            {
                Move();
            }
        }

        #endregion

        #region Initialization Methods

        // [Refactoring] Start metodunu temiz tutmak için başlatma kodlarını ayırdım.
        private void InitializeAttackComponent()
        {
            // Seçilen saldırı türüne göre ilgili komponenti aktif et, diğerini pasif bırakabilirdik 
            // ama burada sadece seçileni açıyoruz.
            if (attackType == AttackType.SwordSlash)
                GetComponent<SwordAttack>().enabled = true;
            else
                GetComponent<ProjectileShooter>().enabled = true;
        }

        private void InitializeStats()
        {
            if (playerStats == null)
            {
                Debug.LogError("PlayerController: 'PlayerStats' component is missing!");
            }
        }

        private void InitializeCameraReference()
        {
            // [Güvenlik] Singleton örneğine erişmeye çalışıyoruz.
            if (CameraFollow.Instance != null)
            {
                cachedCameraFollow = CameraFollow.Instance;
            }
            else
            {
                // Kritik hata değil ama uyarı veriyoruz, çünkü kamera kontrolleri çalışmayacak.
                Debug.LogWarning("PlayerController: CameraFollow Instance not found! Camera mode checks will be skipped.");
            }
        }

        #endregion

        #region Logic & Movement

        private void HandleInputProcessing()
        {
            // [Modifikasyon] Kamera referansı önbellekten (cached) kontrol ediliyor.
            // Eğer kamera "FreeMove" (Serbest Dolaşım) modundaysa oyuncu hareketini kısıtlıyoruz.
            if (cachedCameraFollow != null && cachedCameraFollow.CurrentMode == CameraFollow.CameraMode.FreeMove)
            {
                inputDirection = Vector2.zero; // Girdiyi sıfırla
            }
            else
            {
                // Normal oyun modunda input handler'dan veriyi al
                inputDirection = inputHandler.MoveInput;
            }
        }

        private void UpdateMovementStatus()
        {
            IsMoving = inputDirection != Vector2.zero;
        }

        /// <summary>
        /// IAttackStateProvider arayüzünden gelen metot.
        /// AutoAttack sistemi bu metoda sorarak ateş edip etmeyeceğine karar verir.
        /// </summary>
        public bool CanAttack()
        {
            // 1. Kamera modu kontrolü (Serbest modda saldırı yok)
            if (cachedCameraFollow != null && cachedCameraFollow.CurrentMode == CameraFollow.CameraMode.FreeMove)
            {
                return false;
            }
            
            // 2. Hareket ederken ateş etme yeteneği kontrolü (Stats verisinden)
            bool canFireWhileMoving = playerStats.CurrentCanFireWhileMoving;
            
            if (IsMoving && !canFireWhileMoving)
            {
                return false; 
            }
            
            return true;
        }

        private void Move()
        {
            if (playerStats == null) return; 

            // Y eksenindeki girdiyi Z eksenine (derinlik) çeviriyoruz. 2.5D yapı gereği.
            Vector3 movement = new Vector3(inputDirection.x, 0f, inputDirection.y) * playerStats.CurrentMoveSpeed * Time.fixedDeltaTime;
            
            // Rigidbody.MovePosition, Transform.Translate'e göre fizik motoruyla daha uyumludur.
            // Objelerin içinden geçmeyi engeller.
            rb.MovePosition(rb.position + movement);
        }

        private void AnimateMovement()
        {
            if(animator != null)
                animator.SetBool("Run", IsMoving);
        }

        private void FlipCharacter()
        {
            // Çok küçük değerlerde titremeyi (jitter) önlemek için eşik değeri (0.1f) kullanıyoruz.
            if (Mathf.Abs(inputDirection.x) > 0.1f)
            {
                float newScaleX = Mathf.Sign(inputDirection.x);
                // Sadece X eksenini ters çevir, Y ve Z sabit kalsın.
                transform.localScale = new Vector3(newScaleX, 1, 1);
            }
        }

        #endregion

        #region Actions (Roll)

        public void AttemptRoll()
        {
            if (playerStats == null) return;
            
            // [Modifikasyon] Kamera serbest modda ise takla atılamaz.
            if (cachedCameraFollow != null && cachedCameraFollow.CurrentMode == CameraFollow.CameraMode.FreeMove)
            {
                return;
            }
            
            // Cooldown kontrolü ve şu an takla atıyor mu kontrolü
            if(Time.time > lastRollTime + playerStats.CurrentRollCooldown && !isRolling)
            {
                StartCoroutine(PerformRoll());
            }
        }

        private IEnumerator PerformRoll()
        {
            if (playerStats == null)
            {
                isRolling = false;
                yield break;
            }
                
            isRolling = true;
            lastRollTime = Time.time; 
            
            // Hareket yönüne doğru, eğer hareket yoksa baktığı yöne doğru atıl
            Vector3 rollDir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;

            if (rollDir == Vector3.zero)
            {
                rollDir = new Vector3(transform.localScale.x, 0, 0);
            }

            float rollDuration = 0.3f; 
            
            // [DOTween] Yumuşak bir geçiş hareketi (Ease.OutQuad)
            rb.DOMove(rb.position + rollDir * playerStats.CurrentRollForce, rollDuration).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(rollDuration);

            isRolling = false;
        }

        #endregion
    }
}