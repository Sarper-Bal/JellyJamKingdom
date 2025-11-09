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
    public class PlayerController : MonoBehaviour
    {
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        [Header("Stats Data")]
        [Tooltip("Karakterin tüm temel stat'larının (hız, can, vb.) çekildiği veri objesi.")]
        [SerializeField] private PlayerStatsData statsData; // YENİ: Inspector'dan sürüklenecek

        // ESKİ STAT'LAR KALDIRILDI
        // [SerializeField] private float moveSpeed = 5f;
        // [SerializeField] private float rollForce = 8f;
        // [SerializeField] private float rollCooldown = 1f;
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
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            // Başlangıçta hangi saldırı tipi seçildiyse onu aktif et, diğerini kapat.
            if (attackType == AttackType.SwordSlash)
                GetComponent<SwordAttack>().enabled = true;
            else
                GetComponent<ProjectileShooter>().enabled = true;
                
            // GÜVENLİK ÖNLEMİ: Eğer statsData atanmamışsa konsola hata bas.
            if(statsData == null)
            {
                Debug.LogError("PlayerController üzerinde 'StatsData' referansı atanmamış! Lütfen 'DefaultPlayerStats' asset'ini sürükleyin.");
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
            // GÜVENLİK ÖNLEMİ: statsData yoksa hareketi engelle.
            if (statsData == null) return; 

            // --- DEĞİŞİKLİK: 'moveSpeed' yerine 'statsData.moveSpeed' kullan ---
            Vector3 movement = new Vector3(inputDirection.x, 0f, inputDirection.y) * statsData.moveSpeed * Time.fixedDeltaTime;
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
            if (statsData == null) return;
            
            // --- DEĞİŞİKLİK: 'rollCooldown' yerine 'statsData.rollCooldown' kullan ---
            if(Time.time > lastRollTime + statsData.rollCooldown && !isRolling)
            {
                StartCoroutine(PerformRoll());
            }
        }

        private IEnumerator PerformRoll()
        {
            // GÜVENLİK ÖNLEMİ
            if (statsData == null)
            {
                isRolling = false;
                yield break;
            }
                
            isRolling = true;
            // --- DEĞİŞİKLİK: 'rollCooldown' yerine 'statsData.rollCooldown' kullan ---
            lastRollTime = Time.time; // Cooldown'u başlat
            
            Vector3 rollDir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;

            // Eğer oyuncu duruyorsa, baktığı yöne doğru takla at.
            if (rollDir == Vector3.zero)
            {
                rollDir = new Vector3(transform.localScale.x, 0, 0);
            }

            float rollDuration = 0.3f; // Takla süresi (sabit kalabilir)
            
            // --- DEĞİŞİKLİK: 'rollForce' yerine 'statsData.rollForce' kullan ---
            rb.DOMove(rb.position + rollDir * statsData.rollForce, rollDuration).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(rollDuration);

            isRolling = false;
        }
    }
}