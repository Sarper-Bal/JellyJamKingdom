/*
 * SAĞLIK SİSTEMİ (HEALTH SYSTEM) - v2.2 (Hibrit Refactor)
 *
 * DEĞİŞİKLİKLER:
 * - 'deathEffect' (GameObject) alanı Inspector'a geri eklendi.
 * - YENİ TOOLTIP: Bu alanın artık SADECE 'isPlayer' true ise
 * kullanıldığı belirtildi.
 * - 'Die()' metodu GÜNCELLENDİ:
 * - EĞER 'isPlayer' FALSE (Düşman) ise:
 * - Efekti 'enemyAIComponent.GetDeathEffectFromData()' (yani EnemyData)
 * üzerinden alır.
 * - Efekti 'ObjectPooler.SpawnFromPool' ile havuzdan çağırır.
 * - Kendini 'ObjectPooler.ReturnToPool' ile havuza döndürür.
 * (Bu, 'otomatik Pool sistemimizin' düzgün çalışmasını sağlar)
 *
 * - EĞER 'isPlayer' TRUE (Oyuncu) ise:
 * - Efekti Inspector'daki 'deathEffect' alanından alır.
 * - Efekti 'Instantiate' ile yaratır (Oyuncu havuzlanmadığı için).
 * - Kendini 'Destroy(gameObject)' ile yok eder.
 *
 * BU YAPI, Player prefab'ını bozmadan, Enemy sistemini data-driven
 * tutmamızı sağlar.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.Engine2_5D
{
    public class HealthSystem : MonoBehaviour, IPooledObject
    {
        [Header("Stats Data")]
        private PlayerStats playerStatsComponent;
        private EnemyAI enemyAIComponent; 

        [Header("Effects & Settings")]
        [SerializeField]
        private bool isPlayer;
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Hibrit Alan) ---
        [Tooltip("EĞER 'isPlayer' TRUE (Oyuncu) ise, öldüğünde spawn olacak efekt. " +
                 "(Düşmanlar bu alanı KULLANMAZ, efekti EnemyData'dan alır)")]
        [SerializeField]
        private GameObject deathEffect; // <-- GERİ EKLENDİ (Sadece Oyuncu için)
        // --- DEĞİŞİKLİK SONU ---

        private int health; 
        private int currentMaxHealth; 
        
        public string PoolTag { get; set; }

        public void OnObjectSpawn()
        {
            // Oyuncuysak canı doldur; düşmansak EnemyAI'dan komut bekle
            if (isPlayer)
            {
                InitializeHealth();
            }
        }

        private void Awake()
        {
            if (isPlayer)
            {
                playerStatsComponent = GetComponent<PlayerStats>();
                if (playerStatsComponent == null)
                {
                    Debug.LogError("HealthSystem 'isPlayer' olarak işaretli ancak PlayerStats component'i bulunamadı!");
                }
                InitializeHealth(); 
                playerStatsComponent.OnStatsChanged += HandlePlayerStatsChanged;
            }
            else
            {
                // Düşman ise, 'EnemyAI' referansını al
                enemyAIComponent = GetComponent<EnemyAI>();
                if (enemyAIComponent == null)
                {
                    Debug.LogError("HealthSystem 'isPlayer' DEĞİL olarak işaretli ancak EnemyAI component'i bulunamadı!");
                }
            }
        }

        private void OnDestroy()
        {
            if (isPlayer && playerStatsComponent != null)
            {
                playerStatsComponent.OnStatsChanged -= HandlePlayerStatsChanged;
            }
        }

        private void HandlePlayerStatsChanged()
        {
            // ... (Değişiklik yok)
            int oldMaxHealth = currentMaxHealth;
            currentMaxHealth = playerStatsComponent.CurrentMaxHealth;
            if (health > currentMaxHealth) { health = currentMaxHealth; }
            if (isPlayer)
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);
        }

        /// <summary>
        /// Sadece OYUNCU için can başlatma fonksiyonu.
        /// </summary>
        private void InitializeHealth()
        {
            if (!isPlayer || playerStatsComponent == null) return;
            currentMaxHealth = playerStatsComponent.CurrentMaxHealth;
            health = currentMaxHealth;
            if (isPlayer)
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);
        }
        
        /// <summary>
        /// DÜŞMANLAR için 'EnemyAI' tarafından çağrılır.
        /// </summary>
        public void InitializeFromData(int newMaxHealth)
        {
            if (isPlayer) return; 
            currentMaxHealth = newMaxHealth;
            health = currentMaxHealth;
        }

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

        // --- DEĞİŞİKLİK BAŞLANGICI (Hibrit Die Metodu) ---
        /// <summary>
        /// Ölüm mantığını ve efektlerini yönetir.
        /// 'isPlayer' durumuna göre farklı çalışır.
        /// </summary>
        public void Die()
        {
            if (!isPlayer)
            {
                // --- DÜŞMAN İÇİN DATA-DRIVEN YOL ---
                
                // 1. 'EnemyAI' component'i geçerli mi?
                if (enemyAIComponent != null)
                {
                    // 2. 'EnemyData'dan ölüm efektini iste
                    GameObject effectPrefab = enemyAIComponent.GetDeathEffectFromData();
                    if (effectPrefab != null)
                    {
                        // 3. Efekti prefab adını 'tag' olarak kullanarak HAVUZDAN ÇAĞIR
                        ObjectPooler.Instance.SpawnFromPool(
                            effectPrefab.name, 
                            transform.position + new Vector3(0f, .5f, 0f), 
                            Quaternion.identity);
                    }
                }

                // 4. Kendini HAVUZA GERİ GÖNDER
                ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
            }
            else 
            {
                // --- OYUNCU İÇİN ESKİ (PREFAB) YOL ---
                
                // 1. Inspector'daki 'deathEffect' alanını kontrol et
                if (deathEffect != null)
                {
                    // 2. Efekti 'Instantiate' ile YARAT (Havuzlama yok)
                    Instantiate(deathEffect, transform.position + new Vector3(0f, .5f, 0f), Quaternion.identity);
                }
                
                // 3. Oyuncu objesini YOK ET
                Destroy(gameObject);
            }
        }
        // --- DEĞİŞİKLİK SONU ---
    }
}