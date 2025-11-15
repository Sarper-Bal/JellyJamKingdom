using UnityEngine;
using System.Collections;
using DG.Tweening; // DOTween kütüphanesi (Roll için kullanılıyor)

namespace IndianOceanAssets.Engine2_5D
{
    // Gerekli bileşenleri 'Player' objesine otomatik ekler.
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(SwordAttack))]
    [RequireComponent(typeof(ProjectileShooter))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerStats))] 
    
    public class PlayerController : MonoBehaviour, IAttackStateProvider
    {
        [Header("Stats Data")]
        private PlayerStats playerStats;

        [Header("Attack Settings")]
        [SerializeField] private AttackType attackType;
        private enum AttackType
        {
            SwordSlash, ProjectileShoot
        }
        
        // Gerekli bileşen referansları
        private Rigidbody rb;
        private Animator animator;
        private PlayerInputHandler inputHandler;
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Kamera Referansı) ---
        // Kameranın mevcut modunu sorgulamak için referans
        private CameraFollow cameraFollow;
        // --- DEĞİŞİKLİK SONU ---

        // Dahili durum değişkenleri
        private Vector2 inputDirection;
        private float lastRollTime;
        private bool isRolling;
        public bool IsMoving { get; private set; }


        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            inputHandler = GetComponent<PlayerInputHandler>();
            playerStats = GetComponent<PlayerStats>();
            
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            if (attackType == AttackType.SwordSlash)
                GetComponent<SwordAttack>().enabled = true;
            else
                GetComponent<ProjectileShooter>().enabled = true;
                
            if(playerStats == null)
            {
                Debug.LogError("PlayerController üzerinde 'PlayerStats' component'i bulunamadı!");
            }
            
            // --- DEĞİŞİKLİK BAŞLANGICI (Kamera Referansı Alma) ---
            // CameraFollow Singleton'ına eriş
            cameraFollow = CameraFollow.Instance;
            if (cameraFollow == null)
            {
                Debug.LogError("PlayerController: Sahnede 'CameraFollow' component'i (Instance) bulunamadı! " +
                               "Kamera modu kontrolleri çalışmayacak.");
            }
            // --- DEĞİŞİKLİK SONU ---
        }

        private void Update()
        {
            // --- DEĞİŞİKLİK BAŞLANGICI (Kamera Modu Kontrolü) ---
            // Kamerayı (Singleton) kontrol et.
            // Eğer kamera 'FreeMove' modundaysa, oyuncu input'unu sıfırla (hareket etme).
            if (cameraFollow != null && cameraFollow.CurrentMode == CameraFollow.CameraMode.FreeMove)
            {
                inputDirection = Vector2.zero;
            }
            else
            {
                // Kamera serbest modda değilse, normal oyuncu input'unu al
                inputDirection = inputHandler.MoveInput;
            }
            // --- DEĞİŞİKLİK SONU ---
            
            // IsMoving bayrağını güncelle.
            IsMoving = inputDirection != Vector2.zero;

            if (!isRolling)
            {
                AnimateMovement();
                FlipCharacter();
            }
        }

        private void FixedUpdate()
        {
            if (!isRolling)
                Move();
        }
        
        public bool CanAttack()
        {
            // --- DEĞİŞİKLİK (Güvenlik Kontrolü) ---
            // Eğer kamera serbest dolaşım modundaysa, oyuncu saldıramaz.
            if (cameraFollow != null && cameraFollow.CurrentMode == CameraFollow.CameraMode.FreeMove)
            {
                return false;
            }
            // --- DEĞİŞİKLİK SONU ---
            
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

            // Not: 'inputDirection' zaten Update() içinde kamera moduna
            // göre sıfırlandığı için bu metot 'FreeMove' modunda
            // otomatik olarak (0,0,0) hareket uygulayacaktır (yani hareket etmeyecektir).
            Vector3 movement = new Vector3(inputDirection.x, 0f, inputDirection.y) * playerStats.CurrentMoveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }

        private void AnimateMovement()
        {
            animator.SetBool("Run", IsMoving);
        }

        private void FlipCharacter()
        {
            if (Mathf.Abs(inputDirection.x) > 0.1f)
            {
                float newScaleX = Mathf.Sign(inputDirection.x);
                transform.localScale = new Vector3(newScaleX, 1, 1);
            }
        }

        public void AttemptRoll()
        {
            if (playerStats == null) return;
            
            // --- DEĞİŞİKLİK BAŞLANGICI (Kamera Modu Kontrolü) ---
            // Eğer kamera serbest dolaşım modundaysa, oyuncu takla atamaz.
            if (cameraFollow != null && cameraFollow.CurrentMode == CameraFollow.CameraMode.FreeMove)
            {
                return; // Takla atma
            }
            // --- DEĞİŞİKLİK SONU ---
            
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
            
            Vector3 rollDir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;

            if (rollDir == Vector3.zero)
            {
                rollDir = new Vector3(transform.localScale.x, 0, 0);
            }

            float rollDuration = 0.3f; 
            
            rb.DOMove(rb.position + rollDir * playerStats.CurrentRollForce, rollDuration).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(rollDuration);

            isRolling = false;
        }
    }
}