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
        
        [Tooltip("EĞER 'isPlayer' TRUE (Oyuncu) ise, öldüğünde spawn olacak efekt.")]
        [SerializeField]
        private GameObject deathEffect; 

        private int health; 
        private int currentMaxHealth; 
        
        public string PoolTag { get; set; }

        public void OnObjectSpawn()
        {
            // Havuzdan çıkarken (Düşmanlar için)
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
                
                // --- DEĞİŞİKLİK: InitializeHealth BURADAN KALDIRILDI ---
                // Burası çok erkendi, HealthUI daha hazır olmamış olabilir.
                
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

        // --- DEĞİŞİKLİK: Start Metodu Eklendi ---
        private void Start()
        {
            // Oyuncu can barı başlatmasını burada yapıyoruz.
            // Çünkü Start çalıştığında tüm objelerin 'Awake'i bitmiştir ve HealthUI.Instance hazırdır.
            if (isPlayer)
            {
                InitializeHealth();
            }
        }
        // ----------------------------------------

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
            if (health > currentMaxHealth) { health = currentMaxHealth; }
            if (isPlayer && HealthUI.Instance != null) // Ekstra güvenlik
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
            
            // HealthUI'ın hazır olduğundan emin olduğumuz yer (Start)
            if (isPlayer && HealthUI.Instance != null)
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
            if (isPlayer && HealthUI.Instance != null)
                HealthUI.Instance.UpdateHealthBar(currentMaxHealth, health);

            if (health <= 0)
            {
                if (isPlayer && HealthUI.Instance != null)
                    HealthUI.Instance.ReloadScene();
                Die();
            }
        }

        public void Die()
        {
           if (!isPlayer)
            {
                // --- YENİ: WAVE MANAGER'A HABER VER ---
                if (WaveManager.Instance != null)
                {
                    WaveManager.Instance.OnEnemyKilled();
                }
                // --------------------------------------

                // --- DÜŞMAN İÇİN DATA-DRIVEN YOL ---
                if (enemyAIComponent != null)
                {
                    GameObject effectPrefab = enemyAIComponent.GetDeathEffectFromData();
                    if (effectPrefab != null)
                    {
                        ObjectPooler.Instance.SpawnFromPool(
                            effectPrefab.name, 
                            transform.position + new Vector3(0f, .5f, 0f), 
                            Quaternion.identity);
                    }
                }
                ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
            }
            else 
            {
                // --- OYUNCU İÇİN ---
                if (deathEffect != null)
                {
                    Instantiate(deathEffect, transform.position + new Vector3(0f, .5f, 0f), Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
    }
}