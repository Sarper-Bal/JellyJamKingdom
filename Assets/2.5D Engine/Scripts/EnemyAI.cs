/*
 * DÜŞMAN YAPAY ZEKASI (ENEMY AI) - DATA-DRIVEN MOTOR (v3.3)
 *
 * * DEĞİŞİKLİKLER (v3.3 - Scale Entegrasyonu):
 * - 'InitializeVisuals()' metodu güncellendi.
 * - Artık 'enemyData'dan 'scale' verisini okuyor ve bunu
 * doğrudan bu objenin 'transform.localScale'ine atıyor.
 * - Bu, düşmanın boyutunun da data-driven olmasını sağlar.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    [RequireComponent(typeof(HealthSystem))] 
    public class EnemyAI : MonoBehaviour
    {
        [Header("Veri Kaynağı (ZORUNLU)")]
        [SerializeField] private EnemyData enemyData;

        [Header("Bileşen Referansları")]
        [Tooltip("Görseli (Sprite) ayarlamak için GFX objesinin SpriteRenderer'ı.")]
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
                return;
            }
            
            // Görselleri ve boyutu ayarla.
            // Bu 'Awake'te yapılır, çünkü bu ayarlar spawn/despawn
            // sırasında (OnEnable/OnDisable) sürekli değişmeyecek.
            InitializeVisuals();
        }
        
        /// <summary>
        /// 'EnemyData'dan görselleri ve boyutu okuyup ayarlar.
        /// </summary>
        private void InitializeVisuals()
        {
            // 1. Sprite Ayarı (v3.2'den)
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"'{gameObject.name}' üzerindeki EnemyAI'a 'Sprite Renderer' " +
                                 "atanmamış. Görsel ayarlanmayacak.", this);
            }
            else
            {
                if (enemyData.characterSprite != null)
                {
                    spriteRenderer.sprite = enemyData.characterSprite;
                }
                else
                {
                    Debug.LogWarning($"'{enemyData.name}' asset'inde 'Character Sprite' alanı boş. " +
                                     $"Mevcut sprite korunacak.", this);
                }
            }
            
            // --- DEĞİŞİKLİK BAŞLANGICI (v3.3) ---
            // 2. Scale (Boyut) Ayarı
            // 'enemyData.scale' (1, 1, 1) değilse, prefab'ın ana scale'ini ez.
            if (enemyData.scale != Vector3.one && enemyData.scale != Vector3.zero)
            {
                // İsteğiniz üzerine GFX'in değil, doğrudan bu component'in
                // bağlı olduğu 'transform'un scale'ini değiştiriyoruz.
                transform.localScale = enemyData.scale;
            }
            // (Eğer data'da (1,1,1) ise, prefab'ın orijinal
            // ayarını korumak için hiçbir şey yapmayız, bu da optimize bir yoldur)
            // --- DEĞİŞİKLİK SONU ---
        }
        
        /// <summary>
        /// Bu düşmanın motorunu başlatır ('WaveManager' tarafından çağrılır).
        /// </summary>
        public void Initialize(Transform targetToChase, Transform[] path)
        {
            this.chaseTarget = targetToChase;
            this.currentPathWaypoints = path;
            this.currentWaypointIndex = 0; 
            
            // Uyarı kontrolleri (Değişiklik yok)
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

        // --- HAREKET VE DİĞER METOTLAR (v3.1 - Değişiklik yok) ---
        #region Movement, Collision, and Data Accessors (No Change)
        
        private void HandleChasePlayerMovement()
        {
            if (chaseTarget)
            {
                // Not: Sprite flip'leri 'transform.localScale'i ezmez,
                // sadece X yönünü değiştirir. Bu yüzden scale
                // sistemimizle uyumlu çalışır.
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
        
        void OnCollisionEnter(Collision collision)
        {
            if (enemyData == null) return; 
            if (collision.collider.CompareTag("Player"))
            {
                collision.collider.GetComponent<HealthSystem>().Damage(enemyData.damageAmount);
                GetComponent<HealthSystem>().Die();
            }
        }
        
        public int GetMaxHealthFromData()
        {
            if (enemyData != null) { return enemyData.maxHealth; }
            Debug.LogError("EnemyData atanmadığı için can 1 olarak ayarlandı!", this);
            return 1;
        }
        
        #endregion
    }
}