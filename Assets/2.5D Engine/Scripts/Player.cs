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
    [RequireComponent(typeof(PlayerStats))] // YENİ: PlayerStats component'ini zorunlu kıl
    public class PlayerController : MonoBehaviour
    {
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        [Header("Stats Data")]
        // [SerializeField] private PlayerStatsData statsData; // ESKİ: Kaldırıldı
        
        // YENİ: Player'ın anlık stat'larını yöneten component'e referans
        private PlayerStats playerStats;
        // --- DEĞİŞİKLİK SONU ---


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
            
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            // statsData referansı yerine PlayerStats component'ini al
            playerStats = GetComponent<PlayerStats>();
            // --- DEĞİŞİKLİK SONU ---
            
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            // Başlangıçta hangi saldırı tipi seçildiyse onu aktif et, diğerini kapat.
            if (attackType == AttackType.SwordSlash)
                GetComponent<SwordAttack>().enabled = true;
            else
                GetComponent<ProjectileShooter>().enabled = true;
                
            // GÜVENLİK ÖNLEMİ
            // --- DEĞİŞİKLİK: statsData yerine playerStats kontrolü ---
            if(playerStats == null)
            {
                Debug.LogError("PlayerController üzerinde 'PlayerStats' component'i bulunamadı!");
            }
        }

        private void Update()
        {
            // Input'u her frame oku.
            inputDirection = inputHandler.MoveInput;
            
            // Hareket girdisi varsa IsMoving true, yoksa false olacak.
            // (Bu değişken AutoAttack script'i tarafından kullanılıyor)
            IsMoving = inputDirection != Vector2.zero;

            if (!isRolling)
            {
                AnimateMovement();
                FlipCharacter();
            }

            // --- Gelecek Planı (Roll Mekaniği) ---
            // Buraya 'Roll' tuşuna basıldığında 'AttemptRoll()' fonksiyonunu
            // çağıran kodu (PlayerInputHandler'dan gelen event ile) ekleyeceğiz.
        }

        private void FixedUpdate()
        {
            // Fizik güncellemeleri FixedUpdate'te yapılır.
            if (!isRolling)
                Move();
        }

        private void Move()
        {
            // GÜVENLİK ÖNLEMİ
            if (playerStats == null) return; 

            // --- DEĞİŞİKLİK: 'statsData.moveSpeed' yerine 'playerStats.CurrentMoveSpeed' kullan ---
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

        // --- Gelecek Planı (Roll Mekaniği) ---
        // Bu fonksiyonu public yapıp, tuşa basıldığında çağıracağız.
        public void AttemptRoll()
        {
            // GÜVENLİK ÖNLEMİ
            if (playerStats == null) return;
            
            // --- DEĞİŞİKLİK: 'statsData.rollCooldown' yerine 'playerStats.CurrentRollCooldown' kullan ---
            if(Time.time > lastRollTime + playerStats.CurrentRollCooldown && !isRolling)
            {
                StartCoroutine(PerformRoll());
            }
        }

        private IEnumerator PerformRoll()
        {
            // GÜVENLİK ÖNLEMİ
            if (playerStats == null)
            {
                isRolling = false;
                yield break;
            }
                
            isRolling = true;
            // --- DEĞİŞİKLİK: 'statsData.rollCooldown' yerine 'playerStats.CurrentRollCooldown' kullan ---
            lastRollTime = Time.time; // Cooldown'u başlat
            
            Vector3 rollDir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;

            // Eğer oyuncu duruyorsa, baktığı yöne doğru takla at.
            if (rollDir == Vector3.zero)
            {
                rollDir = new Vector3(transform.localScale.x, 0, 0);
            }

            float rollDuration = 0.3f; // Takla süresi (sabit kalabilir)
            
            // --- DEĞİŞİKLİK: 'statsData.rollForce' yerine 'playerStats.CurrentRollForce' kullan ---
            rb.DOMove(rb.position + rollDir * playerStats.CurrentRollForce, rollDuration).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(rollDuration);

            isRolling = false;
        }
    }
}