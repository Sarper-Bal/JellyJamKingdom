using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    public class AutoUpgradeSystem : MonoBehaviour
    {
        [Header("Ayarlar")]
        [Tooltip("Bu sahnede çıkabilecek tüm kartları buraya sürükle.")]
        [SerializeField] private List<UpgradeData> potentialUpgrades;

        [Header("Referanslar")]
        [SerializeField] private RoundManager roundManager;
        private PlayerStats playerStats;

        private void Start()
        {
            // 1. RoundManager'ı bul ve abone ol
            if (roundManager == null) roundManager = FindObjectOfType<RoundManager>();
            
            if (roundManager != null)
            {
                roundManager.OnRoundEnded += HandleRoundEnded;
            }
            else
            {
                Debug.LogWarning("AutoUpgradeSystem: RoundManager bulunamadı! Otomatik upgrade çalışmayacak.");
            }

            // 2. Oyuncuyu bul
            var player = FindObjectOfType<PlayerController>();
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
        }

        private void OnDestroy()
        {
            // Abonelikten çık (Hata önlemek için)
            if (roundManager != null) roundManager.OnRoundEnded -= HandleRoundEnded;
        }

        // Tur bittiğinde RoundManager bunu otomatik çağırır
        private void HandleRoundEnded()
        {
            ApplyRandomCard();
        }

        private void ApplyRandomCard()
        {
            if (playerStats == null || potentialUpgrades == null || potentialUpgrades.Count == 0)
            {
                Debug.Log("AutoUpgradeSystem: Kart verilemedi (Oyuncu yok veya Kart listesi boş).");
                return;
            }

            // Rastgele seçim
            int randomIndex = Random.Range(0, potentialUpgrades.Count);
            UpgradeData selectedCard = potentialUpgrades[randomIndex];

            // Oyuncuya uygula
            playerStats.ApplyUpgrade(selectedCard);
        }
    }
}