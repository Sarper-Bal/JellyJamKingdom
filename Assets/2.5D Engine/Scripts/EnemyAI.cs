/*
 * DÜŞMAN YAPAY ZEKASI (ENEMY AI) - DATA-DRIVEN MOTOR (v4.0)
 *
 * GÖREVİ:
 * Bu script artık bir "MOTOR"dur. Veriyi Inspector'dan ALMAZ.
 * 'WaveManager' tarafından 'Initialize' metodu çağrıldığında
 * ilgili 'EnemyData' (statlar) atanır ve motor çalışır.
 *
 * * DEĞİŞİKLİKLER (v4.0):
 * - '[SerializeField] private EnemyData enemyData' alanı,
 * 'private EnemyData enemyData' olarak değiştirildi (Artık Inspector'dan atanmıyor).
 * - 'Initialize()' metodunun imzası DEĞİŞTİ. Artık ilk parametre
 * olarak 'EnemyData' alıyor.
 * - 'Awake()' metodu 'HealthSystem' referansını alacak şekilde güncellendi.
 * - 'Initialize()' metodu artık 'InitializeVisuals()'ı çağırıyor VE
 * 'healthSystem.InitializeFromData()'yı tetikliyor.
 * - 'GetMaxHealthFromData()' SİLİNDİ.
 * - 'GetDeathEffectFromData()' EKLENDİ (HealthSystem'in kullanması için).
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    [RequireComponent(typeof(HealthSystem))] 
    public class EnemyAI : MonoBehaviour
    {
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // 'SerializeField' kaldırıldı. Bu veri artık 'Initialize'
        // metodu ile dışarıdan (WaveManager'dan) yüklenecek (DI - Dependency Injection).
        private EnemyData enemyData;
        // --- DEĞİŞİKLİK SONU ---

        [Header("Bileşen Referansları")]
        [Tooltip("Görseli (Sprite) ayarlamak için GFX objesinin SpriteRenderer'ı.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        // --- Gerekli Component Referansları ---
        private HealthSystem healthSystem;

        // --- Motorun Anlık (Runtime) Verileri ---
        private Transform chaseTarget;          
        private Transform[] currentPathWaypoints; 
        private int currentWaypointIndex = 0;   
        
        private void Awake()
        {
            // Gerekli sistemleri bul ve sakla
            healthSystem = GetComponent<HealthSystem>();
            if (spriteRenderer == null)
            {
                 Debug.LogWarning($"'{gameObject.name}' üzerinde 'Sprite Renderer' atanmamış. " +
                                  "Görsel ayarları çalışmayacak.", this);
            }
        }
        
        /// <summary>
        /// Düşman görsellerini ve boyutunu 'EnemyData'ya göre ayarlar.
        /// Bu metot, 'Initialize' içinden çağrılır.
        /// </summary>
        private void InitializeVisuals()
        {
            // 1. Sprite Ayarı
            if (spriteRenderer != null && enemyData.characterSprite != null)
            {
                spriteRenderer.sprite = enemyData.characterSprite;
            }

            // 2. Scale (Boyut) Ayarı
            if (enemyData.scale != Vector3.one && enemyData.scale != Vector3.zero)
            {
                transform.localScale = enemyData.scale;
            }
            else
            {
                // Eğer data'da (1,1,1) ise veya atanmamışsa,
                // prefab'ın orijinal scale'ini kullan (sıfırlama)
                transform.localScale = Vector3.one; 
            }
        }
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Yeni Initialize Metodu) ---
        /// <summary>
        /// YENİ METOT: Bu düşmanın motorunu başlatır.
        /// 'WaveManager' tarafından spawn edildikten hemen sonra çağrılır.
        /// </summary>
        /// <param name="data">Düşmanın tüm statlarını içeren ScriptableObject</param>
        /// <param name="targetToChase">'ChasePlayer' modu için hedef (genellikle oyuncu)</param>
        /// <param name="path">'FollowPath' modu için takip edilecek yol dizisi</param>
        public void Initialize(EnemyData data, Transform targetToChase, Transform[] path)
        {
            // 1. Veriyi (Statları) al
            this.enemyData = data;
            if (this.enemyData == null)
            {
                Debug.LogError("EnemyAI.Initialize() 'EnemyData' null olarak çağrıldı. Düşman çalışamaz.", this);
                gameObject.SetActive(false); // Havuza anında geri dön
                return;
            }

            // 2. Veriye göre görselleri ayarla (Sprite, Scale)
            InitializeVisuals();
            
            // 3. Veriye göre canı ayarla (HealthSystem'i tetikle)
            healthSystem.InitializeFromData(this.enemyData.maxHealth);

            // 4. Davranışsal hedefleri ayarla
            this.chaseTarget = targetToChase;
            this.currentPathWaypoints = path;
            this.currentWaypointIndex = 0; // Her spawn'da sıfırla
            
            // 5. Hata Kontrolleri
            if (enemyData.movementType == MovementType.ChasePlayer && this.chaseTarget == null)
            {
                Debug.LogWarning("EnemyAI: 'ChasePlayer' modunda ancak 'targetToChase' (Player) null geldi.", this);
            }
            if (enemyData.movementType == MovementType.FollowPath && (this.currentPathWaypoints == null || this.currentPathWaypoints.Length == 0))
            {
                Debug.LogWarning($"EnemyAI ({enemyData.name}): 'FollowPath' modunda ancak 'path' (Waypoints) " +
                                 "boş veya null geldi. 'Initialize' çağrısını kontrol edin.", this);
            }
        }
        // --- DEĞİŞİKLİK SONU ---
        
        
        public void Update()
        {
            // 'enemyData' atanmadıysa (Initialize edilmediyse) çalışma.
            if (enemyData == null) return;
            
            switch (enemyData.movementType)
            {
                case MovementType.ChasePlayer:
                    HandleChasePlayerMovement();
                    break;
                case MovementType.FollowPath:
                    HandleFollowPathMovement();
                    break;
                case MovementType.FixedDirection:
                    HandleFixedDirectionMovement();
                    break;
            }
        }

        // --- HAREKET METOTLARI (v3.1 - Değişiklik yok) ---
        #region Movement Handlers (No Change)
        
        private void HandleChasePlayerMovement()
        {
            if (chaseTarget)
            {
                if (chaseTarget.position.x > transform.position.x)
                    spriteRenderer.flipX = false;
                else
                    spriteRenderer.flipX = true;

                transform.position = Vector3.MoveTowards(
                    transform.position, 
                    chaseTarget.position, 
                    Time.deltaTime * enemyData.speed
                );
            }
        }
        
        private void HandleFollowPathMovement()
        {
            if (currentPathWaypoints == null || currentPathWaypoints.Length == 0) return; 
            if (currentWaypointIndex >= currentPathWaypoints.Length)
            {
                if (enemyData.loopPath) { currentWaypointIndex = 0; }
                else { return; }
            }
            Transform targetWaypoint = currentPathWaypoints[currentWaypointIndex];
            if (targetWaypoint == null) return;
            
            Vector3 targetPositionOnGround = new Vector3(
                targetWaypoint.position.x, 
                transform.position.y, 
                targetWaypoint.position.z
            );

            if (targetPositionOnGround.x > transform.position.x)
                spriteRenderer.flipX = false;
            else
                spriteRenderer.flipX = true;
            
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPositionOnGround, 
                Time.deltaTime * enemyData.speed
            );

            if (Vector3.Distance(transform.position, targetPositionOnGround) < 0.1f)
            {
                currentWaypointIndex++; 
            }
        }

        private void HandleFixedDirectionMovement()
        {
            if (enemyData.fixedDirection.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (enemyData.fixedDirection.x < -0.01f)
                spriteRenderer.flipX = true;
            
            transform.position += enemyData.fixedDirection.normalized * enemyData.speed * Time.deltaTime;
        }
        
        #endregion
        
        void OnCollisionEnter(Collision collision)
        {
            if (enemyData == null) return; 
            if (collision.collider.CompareTag("Player"))
            {
                collision.collider.GetComponent<HealthSystem>().Damage(enemyData.damageAmount);
                GetComponent<HealthSystem>().Die();
            }
        }
        
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        /// <summary>
        /// 'HealthSystem' tarafından 'Die' anında çağrılır.
        /// 'EnemyData'da tanımlanan ölüm efektini döndürür.
        /// </summary>
        public GameObject GetDeathEffectFromData()
        {
            return (enemyData != null) ? enemyData.deathEffectPrefab : null;
        }
        
        // 'GetMaxHealthFromData' SİLİNDİ, çünkü 'InitializeFromData'
        // metodu artık 'HealthSystem'de bu işi yapıyor.
        // --- DEĞİŞİKLİK SONU ---
    }
}