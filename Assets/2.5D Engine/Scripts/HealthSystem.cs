using UnityEngine;
using UnityEngine.SceneManagement;
namespace IndianOceanAssets.Engine2_5D
{
    // Handles health, damage, and death for entities
    public class HealthSystem : MonoBehaviour
    {
        [Range(1, 100)]
        [SerializeField]
        private int maxHealth = 100; // Maximum health
        private int health = 100; // Current health

        [SerializeField]
        private GameObject deathEffect; // Effect prefab on death

        [SerializeField]
        private bool isPlayer; // Is this the player?

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
            Instantiate(deathEffect, transform.position + new Vector3(0f, .5f, 0f), Quaternion.identity);
            Destroy(gameObject);
        }
    }
}