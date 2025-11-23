using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    public class AutoUpgradeSystem : MonoBehaviour
    {
        [Header("Debug / Test")]
        [Tooltip("Eğer oyunu direkt bu sahneden başlatırsan (GameManager yoksa), test için bu LevelData'yı kullanır.")]
        [SerializeField] private LevelData debugLevelData;

        // Kart havuzu artık private, çünkü veriyi GameManager'dan çekecek.
        private List<UpgradeData> currentCardPool;

        [Header("Referanslar")]
        private RoundManager roundManager;
        private PlayerStats playerStats;

        private void Start()
        {
            InitializeSystem();
            LoadCardPool();
        }

        private void InitializeSystem()
        {
            // 1. RoundManager Bağlantısı
            roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
            {
                roundManager.OnRoundEnded += HandleRoundEnded;
            }
            else
            {
                Debug.LogWarning("AutoUpgradeSystem: RoundManager bulunamadı!");
            }

            // 2. PlayerStats Bağlantısı
            var player = FindObjectOfType<PlayerController>();
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
        }

        /// <summary>
        /// Bölüme özel kartları yükleyen kritik fonksiyon.
        /// </summary>
        private void LoadCardPool()
        {
            // Öncelik 1: Gerçek Oyun Akışı (GameManager)
            if (GameManager.Instance != null && GameManager.Instance.PendingLevelData != null)
            {
                currentCardPool = GameManager.Instance.PendingLevelData.availableUpgrades;
                Debug.Log($"AutoUpgradeSystem: GameManager üzerinden {currentCardPool.Count} kart yüklendi.");
            }
            // Öncelik 2: Editör Testi (Debug Slotu)
            else if (debugLevelData != null)
            {
                currentCardPool = debugLevelData.availableUpgrades;
                Debug.LogWarning($"AutoUpgradeSystem: TEST MODU. Debug datasından {currentCardPool.Count} kart yüklendi.");
            }
            else
            {
                Debug.LogError("AutoUpgradeSystem: Kart yüklenemedi! Ne GameManager var ne de Debug Data atanmış.");
                currentCardPool = new List<UpgradeData>();
            }
        }

        private void OnDestroy()
        {
            if (roundManager != null) roundManager.OnRoundEnded -= HandleRoundEnded;
        }

        private void HandleRoundEnded()
        {
            ApplyRandomCard();
        }

        private void ApplyRandomCard()
        {
            if (playerStats == null || currentCardPool == null || currentCardPool.Count == 0)
            {
                Debug.Log("AutoUpgradeSystem: Kart verilemedi (Liste boş veya Oyuncu yok).");
                return;
            }

            // Havuzdan rastgele seç
            int randomIndex = Random.Range(0, currentCardPool.Count);
            UpgradeData selectedCard = currentCardPool[randomIndex];

            // Uygula
            playerStats.ApplyUpgrade(selectedCard);
        }
    }
}