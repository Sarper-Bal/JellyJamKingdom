using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.Engine2_5D
{
    // Handles health, damage, and death for entities
    public class HealthSystem : MonoBehaviour, IPooledObject
    {
        // --- DEĞİŞİKLİK BAŞLANGICI ---

        [Header("Stats Data")]
        // EĞER BU OYUNCU İSE, can verisini bu ScriptableObject'ten çeker.
        // [SerializeField] private PlayerStatsData playerStats; // ESKİ: Kaldırıldı.
        
        // YENİ: PlayerStats component'ine referans. Sadece oyuncu ise kullanılır.
        private PlayerStats playerStatsComponent;

        // --- DEĞİŞİKLİK SONU ---


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
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            // PlayerStats component'ini bul (eğer oyuncuysa)
            if (isPlayer)
            {
                playerStatsComponent = GetComponent<PlayerStats>();
                if (playerStatsComponent == null)
                {
                    Debug.LogError("HealthSystem 'isPlayer' olarak işaretli ancak PlayerStats component'i bulunamadı!");
                }
            }
            
            // Canı ayarla
            InitializeHealth();

            // YENİ: PlayerStats'taki değişiklikleri dinle
            if (isPlayer && playerStatsComponent != null)
            {
                playerStatsComponent.OnStatsChanged += HandlePlayerStatsChanged;
            }
            // --- DEĞİŞİKLİK SONU ---
        }

        // --- YENİ: Event aboneliğini iptal et ---
        private void OnDestroy()
        {
            // Obje yok olduğunda event dinlemeyi bırak (Memory leak önlemi)
            if (isPlayer && playerStatsComponent != null)
            {
                playerStatsComponent.OnStatsChanged -= HandlePlayerStatsChanged;
            }
        }

        // --- YENİ FONKSİYON ---
        // PlayerStats'tan gelen "stats değişti" event'ini dinler.
        private void HandlePlayerStatsChanged()
        {
            // Canımızı yeni stat'a göre güncelle
            int oldMaxHealth = currentMaxHealth;
            currentMaxHealth = playerStatsComponent.CurrentMaxHealth;

            // Eğer canımız yeni maksimumu aşıyorsa, onu kırp.
            // (Örn: Max can 100->80'e düşerse, mevcut can da 80 olmalı)
            if (health > currentMaxHealth)
            {
                health = currentMaxHealth;
            }
            // Eğer max can artarsa mevcut canı da arttırabiliriz (opsiyonel)
            // else if (currentMaxHealth > oldMaxHealth)
            // {
            //     health += currentMaxHealth - oldMaxHealth; // Aradaki fark kadar iyileştir
            // }

            // UI'ı yeni maksimum cana göre güncelle
            if (isPlayer)
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);
        }


        // Hem Start hem de OnObjectSpawn için ortak can başlatma fonksiyonu.
        private void InitializeHealth()
        {
            // --- DEĞİŞİKLİK BAŞLANGICI: Canı PlayerStats'tan al ---
            if (isPlayer && playerStatsComponent != null)
            {
                // Eğer bu OYUNCU ise, canı ScriptableObject yerine PlayerStats component'inden al.
                currentMaxHealth = playerStatsComponent.CurrentMaxHealth;
            }
            // --- DEĞİŞİKLİK SONU ---
            else
            {
                // EĞER BU DÜŞMAN İSE veya 'playerStatsComponent' atanmamışsa,
                // Inspector'daki 'maxHealth' değerini kullan (eski sistem).
                // BU SAYEDE DÜŞMANLARIN SAĞLIK SİSTEMİ HİÇBİR ŞEKİLDE ETKİLENMEZ.
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