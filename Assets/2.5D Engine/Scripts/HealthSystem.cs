/*
 * SAĞLIK SİSTEMİ (HEALTH SYSTEM) - DATA-DRIVEN GÜNCELLEME (v2.0)
 *
 * DEĞİŞİKLİKLER:
 * - Inspector'daki 'maxHealth' değişkeni SİLİNDİ.
 * - 'playerStatsComponent' yanına 'enemyAI' referansı eklendi.
 * - 'Start()' metodu, 'isPlayer' DEĞİLSE, 'GetComponent<EnemyAI>()'
 * yapacak şekilde güncellendi.
 * - 'InitializeHealth()' metodu güncellendi:
 * - 'isPlayer' ise canı 'PlayerStats'tan alır (ESKİSİ GİBİ).
 * - 'isPlayer' DEĞİLSE, 'enemyAI.GetMaxHealthFromData()' metodunu
 * çağırarak canı 'EnemyData' (ScriptableObject) üzerinden alır.
 *
 * BU YAPI SAYESİNDE:
 * - Player'ın can sistemi HİÇBİR ŞEKİLDE ETKİLENMEZ.
 * - Düşmanların canı artık prefab'a gömülü değil, 'EnemyData'
 * asset'i üzerinden merkezi olarak yönetilir.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.Engine2_5D
{
    // Handles health, damage, and death for entities
    public class HealthSystem : MonoBehaviour, IPooledObject
    {
        [Header("Stats Data")]
        // Player ise, PlayerStats component'ine referans
        private PlayerStats playerStatsComponent;
        
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // Enemy ise, EnemyAI component'ine referans
        private EnemyAI enemyAIComponent;
        
        // Inspector'daki 'maxHealth' değişkeni SİLİNDİ, çünkü bu veri
        // artık 'EnemyData' veya 'PlayerStatsData'dan gelecek.
        // [SerializeField] private int maxHealth = 100; // <-- SİLİNDİ
        // --- DEĞİŞİKLİK SONU ---

        // Mevcut can.
        private int health; 
        
        // Bu varlığın (oyuncu veya düşman) o anki maksimum canı.
        private int currentMaxHealth; 
        
        [Header("Effects & Settings")]
        [SerializeField]
        [Tooltip("Bu varlık öldüğünde (Die) spawn olacak efekt prefab'ı. " +
                 "Düşmanlar için, bu prefab'ın adı ObjectPooler'daki 'tag' olarak kullanılacaktır.")]
        private GameObject deathEffect; // Effect prefab on death

        [SerializeField]
        private bool isPlayer; // Is this the player?

        public string PoolTag { get; set; }

        // Bu fonksiyon, obje havuzdan her "spawn" olduğunda ObjectPooler tarafından çağrılır.
        public void OnObjectSpawn()
        {
            // Obje havuzdan çıktığında canını doldur
            InitializeHealth();
        }

        // 'Start' yerine 'Awake' kullanmak, referansların daha hızlı
        // alınmasını garantiler (Özellikle 'WaveManager' için)
        private void Awake() // 'Start'tan 'Awake'e değiştirildi
        {
            if (isPlayer)
            {
                playerStatsComponent = GetComponent<PlayerStats>();
                if (playerStatsComponent == null)
                {
                    Debug.LogError("HealthSystem 'isPlayer' olarak işaretli ancak PlayerStats component'i bulunamadı!");
                }
            }
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            else
            {
                // Eğer bu bir düşmansa, EnemyAI component'ini bul
                enemyAIComponent = GetComponent<EnemyAI>();
                if (enemyAIComponent == null)
                {
                    Debug.LogError("HealthSystem 'isPlayer' DEĞİL olarak işaretli ancak EnemyAI component'i bulunamadı!");
                }
            }
            // --- DEĞİŞİKLİK SONU ---
            
            InitializeHealth();

            if (isPlayer && playerStatsComponent != null)
            {
                playerStatsComponent.OnStatsChanged += HandlePlayerStatsChanged;
            }
        }
        
        // Start metodu Awake'e taşındı
        // private void Start() { ... }

        private void OnDestroy()
        {
            if (isPlayer && playerStatsComponent != null)
            {
                playerStatsComponent.OnStatsChanged -= HandlePlayerStatsChanged;
            }
        }

        private void HandlePlayerStatsChanged()
        {
            int oldMaxHealth = currentMaxHealth;
            currentMaxHealth = playerStatsComponent.CurrentMaxHealth;

            if (health > currentMaxHealth)
            {
                health = currentMaxHealth;
            }

            if (isPlayer)
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);
        }


        /// <summary>
        /// Hem Start/Awake hem de OnObjectSpawn için ortak can başlatma fonksiyonu.
        /// </summary>
        private void InitializeHealth()
        {
            if (isPlayer && playerStatsComponent != null)
            {
                // OYUNCU: Canı PlayerStats component'inden al.
                currentMaxHealth = playerStatsComponent.CurrentMaxHealth;
            }
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            else if (!isPlayer && enemyAIComponent != null)
            {
                // DÜŞMAN: Canı EnemyAI -> EnemyData (ScriptableObject) üzerinden al.
                currentMaxHealth = enemyAIComponent.GetMaxHealthFromData();
            }
            else if (!isPlayer)
            {
                // GÜVENLİK: Düşman ama 'EnemyAI' bulunamadıysa, 1 can ver.
                Debug.LogError("Düşman canı 'EnemyData'dan okunamadı. Varsayılan can 1 olarak ayarlandı.", this);
                currentMaxHealth = 1;
            }
            // --- DEĞİŞİKLİK SONU ---

            // Canı doldur.
            health = currentMaxHealth;

            // Eğer oyuncuysa UI'ı da güncelle.
            if (isPlayer)
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);
        }

        /// <summary>
        /// Bu varlığa hasar uygular.
        /// </summary>
        public void Damage(int damageAmount)
        {
            health -= damageAmount;

            if (isPlayer)
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);

            if (health <= 0)
            {
                if (isPlayer)
                    HealthUI.Instance.ReloadScene();
                
                Die();
            }
        }

        /// <summary>
        /// Ölüm mantığını ve efektlerini yönetir.
        /// </summary>
        public void Die()
        {
            if (!isPlayer)
            {
                if (deathEffect != null)
                {
                    ObjectPooler.Instance.SpawnFromPool(
                        deathEffect.name,
                        transform.position + new Vector3(0f, .5f, 0f), 
                        Quaternion.identity);
                }
                
                // Kendini havuza geri gönder
                ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
            }
            else 
            {
                // Oyuncu ise yok et (Eski sistem)
                if (deathEffect != null)
                {
                    Instantiate(deathEffect, transform.position + new Vector3(0f, .5f, 0f), Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
        
        public GameObject GetDeathEffectPrefab()
        {
            return deathEffect;
        }
    }
}