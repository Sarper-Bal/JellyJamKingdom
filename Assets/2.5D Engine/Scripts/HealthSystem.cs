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

        [Header("Fallback Stats (Düşmanlar/Oyuncu-Dışı Varlıklar İçin)")]
        [Tooltip("EĞER 'isPlayer' değilse (yani DÜŞMAN ise), bu can değeri kullanılır.")]
        [Range(1, 1000)]
        [SerializeField] private int maxHealth = 100; // Düşmanlar için bu değişken korunuyor.

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
            InitializeHealth();
        }

        // Initializes health
        private void Start()
        {
            if (isPlayer)
            {
                playerStatsComponent = GetComponent<PlayerStats>();
                if (playerStatsComponent == null)
                {
                    Debug.LogError("HealthSystem 'isPlayer' olarak işaretli ancak PlayerStats component'i bulunamadı!");
                }
            }
            
            InitializeHealth();

            if (isPlayer && playerStatsComponent != null)
            {
                // PlayerStats'taki (örn: max can bonusu) değişiklikleri dinle
                playerStatsComponent.OnStatsChanged += HandlePlayerStatsChanged;
            }
        }

        private void OnDestroy()
        {
            // Obje yok olduğunda event dinlemeyi bırak (Memory leak önlemi)
            if (isPlayer && playerStatsComponent != null)
            {
                playerStatsComponent.OnStatsChanged -= HandlePlayerStatsChanged;
            }
        }

        /// <summary>
        /// PlayerStats'tan gelen "stats değişti" event'ini dinler.
        /// </summary>
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
        /// Hem Start hem de OnObjectSpawn için ortak can başlatma fonksiyonu.
        /// </summary>
        private void InitializeHealth()
        {
            if (isPlayer && playerStatsComponent != null)
            {
                // OYUNCU: Canı PlayerStats component'inden al.
                currentMaxHealth = playerStatsComponent.CurrentMaxHealth;
            }
            else
            {
                // DÜŞMAN: Canı Inspector'daki 'maxHealth' değerinden al.
                currentMaxHealth = this.maxHealth;
            }

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
                    HealthUI.Instance.ReloadScene(); // Şimdilik sahneyi yeniden başlat
                
                Die();
            }
        }

        /// <summary>
        /// Ölüm mantığını ve efektlerini yönetir.
        /// </summary>
        public void Die()
        {
            // --- DEĞİŞİKLİK BAŞLANGICI (Akıllı Ölüm Efekti) ---
            if (!isPlayer)
            {
                // Eğer bu bir DÜŞMAN ise:
                
                // 1. Ölüm efekti bu prefab'a atanmış mı?
                if (deathEffect != null)
                {
                    // 2. Efektin prefab adını 'tag' olarak kullanarak havuzdan çağır.
                    //    (Örn: 'Enemy Death Effect.prefab' -> "Enemy Death Effect" tag'i)
                    ObjectPooler.Instance.SpawnFromPool(
                        deathEffect.name, // "enemyDeath" (sabit) yerine prefab'ın adını kullan
                        transform.position + new Vector3(0f, .5f, 0f), 
                        Quaternion.identity);
                }

                // 3. Kendini yok etmek yerine, kendi etiketiyle havuza geri dön.
                //    (PoolTag, 'SpawnBurst' içinde 'enemyPrefab.name' olarak atanmıştı)
                ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
            }
            // --- DEĞİŞİKLİK SONU ---
            else 
            {
                // Eğer bu OYUNCU ise, eski sistem gibi yok et (şimdilik).
                if (deathEffect != null)
                {
                    Instantiate(deathEffect, transform.position + new Vector3(0f, .5f, 0f), Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
        
        // --- YENİ FONKSİYON BAŞLANGICI ---
        /// <summary>
        /// WaveManager'ın havuz hesaplaması yapabilmesi için bu prefab'a atanan
        /// ölüm efekti prefab'ını döndürür.
        /// </summary>
        /// <returns>Inspector'da atanan 'deathEffect' prefab'ı.</returns>
        public GameObject GetDeathEffectPrefab()
        {
            return deathEffect;
        }
        // --- YENİ FONKSİYON SONU ---
    }
}