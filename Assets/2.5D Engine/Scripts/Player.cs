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
    
    // --- YENİ EKLENEN KISIM BAŞLANGICI ---
    // 'IAttackStateProvider' arayüzünü uyguladığımızı (implemente ettiğimizi) belirtiyoruz.
    public class PlayerController : MonoBehaviour, IAttackStateProvider
    // --- YENİ EKLENEN KISIM SONU ---
    {
        [Header("Stats Data")]
        // Player'ın anlık stat'larını yöneten component'e referans
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
            playerStats = GetComponent<PlayerStats>(); // Referansı Awake'te al
            
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
        }

        private void Update()
        {
            inputDirection = inputHandler.MoveInput;
            
            // IsMoving bayrağını güncelle.
            // (Bu, AutoAttack tarafından değil, animasyon ve Move() tarafından kullanılır)
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

        // --- YENİ EKLENEN FONKSİYON BAŞLANGICI ---
        /// <summary>
        /// IAttackStateProvider arayüzünden gelen zorunlu metot.
        /// AutoAttack'a saldırıp saldıramayacağını söyler.
        /// </summary>
        /// <returns>True (saldırabilir), False (saldıramaz)</returns>
        public bool CanAttack()
        {
            // PlayerStats'tan "hareketliyken ateş etme" ayarını oku
            bool canFireWhileMoving = playerStats.CurrentCanFireWhileMoving;

            // Eğer hareket ediyorsam VE hareketliyken ateş etme yeteneğim YOKSA,
            // saldıramam.
            if (IsMoving && !canFireWhileMoving)
            {
                return false; 
            }

            // (Gelecekte buraya 'isStunned' (sersemlemiş) gibi başka kontroller de ekleyebilirsin)
            // if (isStunned) return false;

            // Diğer tüm durumlarda (duruyorsam VEYA hareketliyken ateş edebiliyorsam)
            // saldırabilirim.
            return true;
        }
        // --- YENİ EKLENEN FONKSİYON SONU ---

        private void Move()
        {
            if (playerStats == null) return; 

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