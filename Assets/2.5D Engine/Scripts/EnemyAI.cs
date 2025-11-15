/*
 * DÜŞMAN YAPAY ZEKASI (ENEMY AI) - DATA-DRIVEN MOTOR (v3.1)
 *
 * * DEĞİŞİKLİKLER (v3.1 - Yol Takibi Düzeltmesi):
 * - 'HandleFollowPathMovement()' metodu güncellendi.
 * - Artık 'targetWaypoint.position'ı doğrudan hedef almıyor.
 * - 'targetPositionOnGround' adında geçici bir Vector3 oluşturuluyor.
 * - Bu yeni vektör, waypoint'in X ve Z'sini, ancak düşmanın KENDİ Y
 * pozisyonunu alır (transform.position.y).
 * - 'MoveTowards' ve 'Distance' hesaplamaları artık bu 'targetPositionOnGround'
 * vektörünü kullanır.
 * - BU DÜZELTME, düşmanın Y ekseninde (havada) olan waypoint'lere
 * takılıp kalmasını engeller ve hareketi XZ düzlemine kilitler.
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
        [Tooltip("Yön değiştirdiğinde dönecek olan Sprite Renderer.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        // --- Motorun Anlık (Runtime) Verileri ---
        private Transform chaseTarget;          // 'ChasePlayer' modu için hedef (Oyuncu)
        private Transform[] currentPathWaypoints; // 'FollowPath' modu için hedef yol
        private int currentWaypointIndex = 0;   // Yolda kaçıncı noktada olduğu
        
        private void Awake()
        {
            if (enemyData == null)
            {
                Debug.LogError($"'{gameObject.name}' üzerinde 'EnemyData' asset'i atanmamış! " +
                               "EnemyAI çalışmayacak.", this);
                this.enabled = false; 
            }
        }
        
        /// <summary>
        /// YENİ METOT: Bu düşmanın motorunu başlatır.
        /// 'WaveManager' tarafından spawn edildikten hemen sonra çağrılır.
        /// </summary>
        public void Initialize(Transform targetToChase, Transform[] path)
        {
            this.chaseTarget = targetToChase;
            this.currentPathWaypoints = path;
            this.currentWaypointIndex = 0; // Her spawn'da sıfırla
            
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
        /// 'Update' artık 'enemyData'dan okunan veriye göre bir yönlendiricidir.
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

        /// <summary>
        /// MOD 1: Oyuncuyu Takip Etme
        /// </summary>
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

        /// <summary>
        /// MOD 2: Yolu Takip Etme (v3.1 Y-Ekseni Düzeltmesi ile)
        /// </summary>
        private void HandleFollowPathMovement()
        {
            if (currentPathWaypoints == null || currentPathWaypoints.Length == 0)
            {
                return; // Takip edilecek yol yoksa dur.
            }
            
            // 1. Hedef waypoint'i belirle
            if (currentWaypointIndex >= currentPathWaypoints.Length)
            {
                if (enemyData.loopPath)
                {
                    currentWaypointIndex = 0; // Başa dön
                }
                else
                {
                    return; // Yol bittiyse dur
                }
            }
            
            Transform targetWaypoint = currentPathWaypoints[currentWaypointIndex];
            if (targetWaypoint == null) return;
            
            // --- DEĞİŞİKLİK BAŞLANGICI (v3.1 - Y Ekseni Düzeltmesi) ---
            
            // 2. Hedef pozisyonu al, ANCAK Y eksenini (yüksekliği)
            //    düşmanın kendi Y ekseni olarak ayarla.
            //    Bu, düşmanın havada bir noktaya ulaşmaya çalışmasını engeller.
            Vector3 targetPositionOnGround = new Vector3(
                targetWaypoint.position.x, 
                transform.position.y, // Düşmanın kendi Y yüksekliğini kullan
                targetWaypoint.position.z
            );

            // 3. Sprite yönünü bu XZ hedefli pozisyona göre ayarla
            if (targetPositionOnGround.x > transform.position.x)
                spriteRenderer.flipX = false;
            else
                spriteRenderer.flipX = true;
            
            // 4. Yerdeki hedef pozisyona doğru hareket et
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPositionOnGround, // Düzeltilmiş hedefi kullan
                Time.deltaTime * enemyData.speed
            );

            // 5. Hedefe ulaşıp ulaşmadığımızı KONTROL EDERKEN de
            //    Y eksenini görmezden gelmeliyiz. (Mesafe artık 0.1f'in altına inebilir)
            if (Vector3.Distance(transform.position, targetPositionOnGround) < 0.1f)
            {
                currentWaypointIndex++; // Bir sonraki noktaya geç
            }
            // --- DEĞİŞİKLİK SONU ---
        }

        /// <summary>
        /// MOD 3: Sabit Yönde İlerleme
        /// </summary>
        private void HandleFixedDirectionMovement()
        {
            if (enemyData.fixedDirection.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (enemyData.fixedDirection.x < -0.01f)
                spriteRenderer.flipX = true;
            
            transform.position += enemyData.fixedDirection.normalized * enemyData.speed * Time.deltaTime;
        }
        
        /// <summary>
        /// Çarpışma mantığı - Hasarı 'enemyData'dan alıyor
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
        /// 'HealthSystem'in can verisini alması için.
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