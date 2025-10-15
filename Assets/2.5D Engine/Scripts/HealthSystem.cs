using UnityEngine;
using UnityEngine.SceneManagement;
namespace IndianOceanAssets.Engine2_5D
{
    // Handles health, damage, and death for entities
    public class HealthSystem : MonoBehaviour, IPooledObject
    {
        [Range(1, 100)]
        [SerializeField]
        private int maxHealth = 100; // Maximum health
        private int health = 100; // Current health



        [SerializeField]
        private GameObject deathEffect; // Effect prefab on death

        [SerializeField]
        private bool isPlayer; // Is this the player?

        public string PoolTag { get; set; }

        // Bu fonksiyon, obje havuzdan her "spawn" olduğunda ObjectPooler tarafından çağrılır.
        public void OnObjectSpawn()
        {
            // Obje yeniden kullanıldığında canını tekrar maksimuma doldur.
            health = maxHealth;
        }
        // YENİ FONKSİYON: Bu HealthSystem'i belirli bir can değeriyle başlatır.
        public void Initialize(int healthValue)
        {
            maxHealth = healthValue;
            health = maxHealth;
        }

        // Initializes health
        private void Start()
        {
            health = maxHealth;
        }

        // Applies damage and checks for death
        public void Damage(int damageAmount)
        {
            health -= damageAmount;

            // Update UI if player
            if (isPlayer)
                HealthUI.Instance.UpdateHealthBar(maxHealth, health);

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
                // PoolTag, havuzdan spawn olurken ObjectPooler tarafından atanır.
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