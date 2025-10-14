using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
namespace IndianOceanAssets.Engine2_5D
{
    // Handles updating and displaying the health UI
    public class HealthUI : MonoBehaviour
    {
        [SerializeField] private Image healthFill; // UI image for health bar
        public static HealthUI Instance; // Singleton instance

        // Assigns singleton instance
        private void Awake()
        {
            Instance = this;
        }

        // Updates the health bar fill based on health values
        public void UpdateHealthBar(float maxHealth, float currentHealth)
        {
            healthFill.fillAmount = currentHealth / maxHealth;
        }

        // Starts coroutine to reload scene
        public void ReloadScene()
        {
            StartCoroutine(_ReloadScene());
        }

        // Waits and reloads current scene
        IEnumerator _ReloadScene()
        {
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}