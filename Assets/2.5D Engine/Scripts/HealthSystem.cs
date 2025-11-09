using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.Engine2_5D
{
    // Handles health, damage, and death for entities
    public class HealthSystem : MonoBehaviour, IPooledObject
    {
        // --- DEĞİŞİKLİK BAŞLANGICI ---

        [Header("Stats Data")]
        [Tooltip("EĞER BU OYUNCU İSE, can verisini bu ScriptableObject'ten çeker.")]
        [SerializeField] private PlayerStatsData playerStats;

        [Header("Fallback Stats (Düşmanlar/Oyuncu-Dışı Varlıklar İçin)")]
        [Tooltip("EĞER 'playerStats' atanmamışsa veya bu bir 'isPlayer' değilse, bu can değeri kullanılır.")]
        [Range(1, 1000)]
        [SerializeField] private int maxHealth = 100; // ESKİ DEĞİŞKENİ KORUYORUZ (Düşmanlar için)

        // Mevcut can.
        private int health; 
        
        // Bu varlığın (oyuncu veya düşman) o anki maksimum canı.
        // (Power-up alınca artabilir)
        private int currentMaxHealth; 
        
        // --- DEĞİŞİKLİK SONU ---


        [Header("Effects & Settings")]
        [SerializeField]
        private GameObject deathEffect; // Effect prefab on death

        [SerializeField]
        private bool isPlayer; // Is this the player?

        public string PoolTag { get; set; }

        // Bu fonksiyon, obje havuzdan her "spawn" olduğunda ObjectPooler tarafından çağrılır.
        public void OnObjectSpawn()
        {
            // --- DEĞİŞİKLİK: Canı doğru kaynaktan ayarla ---
            InitializeHealth();
        }

        // Initializes health
        private void Start()
        {
            // --- DEĞİŞİKLİK: Canı doğru kaynaktan ayarla ---
            InitializeHealth();
        }

        // YENİ: Hem Start hem de OnObjectSpawn için ortak can başlatma fonksiyonu.
        private void InitializeHealth()
        {
            if (isPlayer && playerStats != null)
            {
                // Eğer bu OYUNCU ise ve 'playerStats' atanmışsa, canı oradan al.
                currentMaxHealth = playerStats.maxHealth;
            }
            else
            {
                // Eğer bu DÜŞMAN ise veya 'playerStats' atanmamışsa,
                // Inspector'daki 'maxHealth' değerini kullan (eski sistem).
                // Bu, düşmanların bozulmamasını sağlar.
                currentMaxHealth = this.maxHealth;
            }

            // Canı doldur.
            health = currentMaxHealth;

            // Eğer oyuncuysa UI'ı da güncelle.
            if (isPlayer)
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);
        }

        // Applies damage and checks for death
        public void Damage(int damageAmount)
        {
            health -= damageAmount;

            // Update UI if player
            if (isPlayer)
                // --- DEĞİŞİKLİK: 'maxHealth' yerine 'currentMaxHealth' kullan ---
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);

            // If dead, reload scene if player, then die
            if (health <= 0)
            {
                if (isPlayer)
                    HealthUI.Instance.ReloadScene();
                Die();
            }
        }

        // Handles death logic and effects
        public void Die()
        {
            // Eğer bu bir oyuncu değilse (yani bir düşmansa)
            if (!isPlayer)
            {
                // Ölüm efektini havuzdan çağır.
                ObjectPooler.Instance.SpawnFromPool("enemyDeath", transform.position + new Vector3(0f, .5f, 0f), Quaternion.identity);

                // Kendini yok etmek yerine, kendi etiketiyle havuza geri dön.
                ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
            }
            else // Eğer bu oyuncuysa, eski sistem gibi yok et (şimdilik).
            {
                Instantiate(deathEffect, transform.position + new Vector3(0f, .5f, 0f), Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}