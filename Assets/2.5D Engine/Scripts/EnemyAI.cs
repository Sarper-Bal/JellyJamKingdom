/*
 * DÜŞMAN YAPAY ZEKASI (ENEMY AI) - DATA-DRIVEN MOTOR (v3.2)
 *
 * * DEĞİŞİKLİKLER (v3.2 - Görsel Entegrasyonu):
 * - 'Awake()' metodu artık 'InitializeVisuals()' adında yeni bir
 * fonksiyonu çağırıyor.
 * - 'InitializeVisuals()' (YENİ METOT): 'spriteRenderer' referansını
 * kontrol eder ve 'enemyData' içindeki 'characterSprite'ı atar.
 * - Bu, tıpkı 'PlayerStats' component'i gibi, görsellerin de
 * data-driven olmasını sağlar.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    [RequireComponent(typeof(HealthSystem))] 
    public class EnemyAI : MonoBehaviour
    {
        [Header("Veri Kaynağı (ZORUNLU)")]
        [Tooltip("Bu düşmanın tüm statlarını ve davranışlarını belirleyen " +
                 "ScriptableObject verisi.")]
        [SerializeField] private EnemyData enemyData;

        [Header("Bileşen Referansları")]
        [Tooltip("Yön değiştirdiğinde dönecek ve 'EnemyData'dan sprite alacak " +
                 "Renderer. (Genellikle GFX alt objesindedir)")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        // --- Motorun Anlık (Runtime) Verileri ---
        private Transform chaseTarget;          
        private Transform[] currentPathWaypoints; 
        private int currentWaypointIndex = 0;   
        
        private void Awake()
        {
            if (enemyData == null)
            {
                Debug.LogError($"'{gameObject.name}' üzerinde 'EnemyData' asset'i atanmamış! " +
                               "EnemyAI çalışmayacak.", this);
                this.enabled = false; 
                return; // 'enemyData' yoksa devam etme
            }
            
            // --- DEĞİŞİKLİK BAŞLANGICI (v3.2) ---
            // 'enemyData' bulunduğuna göre, görselleri ata.
            // Bu, havuzdan çıksa bile sadece bir kez (veya prefab güncellenirse)
            // çalışır ve sprite'ı ayarlar.
            InitializeVisuals();
            // --- DEĞİŞİKLİK SONU ---
        }
        
        // --- DEĞİŞİKLİK BAŞLANGICI (v3.2 - YENİ METOT) ---
        /// <summary>
        /// 'EnemyData'dan okunan 'characterSprite'ı 'spriteRenderer'a atar.
        /// Tıpkı 'PlayerStats'taki 'InitializeVisuals' gibi çalışır.
        /// </summary>
        private void InitializeVisuals()
        {
            // 1. Inspector'da bir SpriteRenderer atanmış mı?
            if (spriteRenderer == null)
            {
                // Atanmamış. Bu bir hata değil, belki bu AI'ın
                // sprite'ı yoktur. Uyarı verip geç.
                Debug.LogWarning($"'{gameObject.name}' üzerindeki EnemyAI'a bir 'Sprite Renderer' " +
                                 "atanmamış. Görsel ayarlanmayacak.", this);
                return;
            }

            // 2. 'EnemyData' asset'inde bir sprite tanımlanmış mı?
            if (enemyData.characterSprite != null)
            {
                // Evet, sprite'ı ata.
                spriteRenderer.sprite = enemyData.characterSprite;
            }
            else
            {
                // Renderer var ama data'da sprite yok.
                Debug.LogWarning($"'{enemyData.name}' asset'inde 'Character Sprite' alanı boş. " +
                                 $"'{spriteRenderer.name}' üzerindeki mevcut sprite korunacak.", this);
            }
        }
        // --- DEĞİŞİKLİK SONU ---
        
        /// <summary>
        /// Bu düşmanın motorunu başlatır ('WaveManager' tarafından çağrılır).
        /// </summary>
        public void Initialize(Transform targetToChase, Transform[] path)
        {
            this.chaseTarget = targetToChase;
            this.currentPathWaypoints = path;
            this.currentWaypointIndex = 0; 
            
            if (enemyData.movementType == MovementType.ChasePlayer && this.chaseTarget == null)
            {
                Debug.LogWarning("EnemyAI: 'ChasePlayer' modunda ancak 'targetToChase' (Player) null geldi.", this);
            }
            
            if (enemyData.movementType == MovementType.FollowPath && (this.currentPathWaypoints == null || this.currentPathWaypoints.Length == 0))
            {
                Debug.LogWarning("EnemyAI: 'FollowPath' modunda ancak 'path' (Waypoints) boş veya null geldi. " +
                                 "Düşman hareket etmeyecek.", this);
            }
        }
        
        /// <summary>
        /// 'Update' yönlendiricisi (Değişiklik yok)
        /// </summary>
        public void Update()
        {
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
            if (currentPathWaypoints == null || currentPathWaypoints.Length == 0)
            {
                return; 
            }
            
            if (currentWaypointIndex >= currentPathWaypoints.Length)
            {
                if (enemyData.loopPath) { currentWaypointIndex = 0; }
                else { return; }
            }
            
            Transform targetWaypoint = currentPathWaypoints[currentWaypointIndex];
            if (targetWaypoint == null) return;
            
            // 2.5D (Y-Ekseni) düzeltmesi
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

        /// <summary>
        /// Çarpışma mantığı (Değişiklik yok)
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            if (enemyData == null) return; 
            
            if (collision.collider.CompareTag("Player"))
            {
                collision.collider.GetComponent<HealthSystem>().Damage(enemyData.damageAmount);
                GetComponent<HealthSystem>().Die();
            }
        }
        
        /// <summary>
        /// 'HealthSystem'in can verisini alması için (Değişiklik yok)
        /// </summary>
        public int GetMaxHealthFromData()
        {
            if (enemyData != null)
            {
                return enemyData.maxHealth;
            }
            Debug.LogError("EnemyData atanmadığı için can 1 olarak ayarlandı!", this);
            return 1;
        }
    }
}