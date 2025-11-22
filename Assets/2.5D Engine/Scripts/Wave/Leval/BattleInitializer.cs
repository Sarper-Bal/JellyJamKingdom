using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [DefaultExecutionOrder(-1)] // WaveController'dan hemen önce çalışmalı
    public class BattleInitializer : MonoBehaviour
    {
        [Header("Sahne Referansları")]
        [SerializeField] private Transform heroSpawnPoint;
        [SerializeField] private GameObject heroPrefab;
        [SerializeField] private WaveSequenceController waveController;

        [Header("Kuleler")]
        [Tooltip("Sahnede yerleştirdiğin kuleleri buraya sürükle. LevelData'daki liste sırasıyla eşleşecek.")]
        [SerializeField] private List<PlayerStats> sceneTowers; 

        private void Start()
        {
            // 1. GameManager'dan Veriyi Al
            LevelData data = null;
            
            if (GameManager.Instance != null)
            {
                data = GameManager.Instance.PendingLevelData;
            }

            if (data == null)
            {
                Debug.LogWarning("BattleInitializer: GameManager verisi yok! (Test için sahneden başlatılmış olabilir).");
                // Test modundaysak varsayılan bir şey yapabilir veya durabiliriz.
                return; 
            }

            InitializeBattle(data);
        }

        private void InitializeBattle(LevelData data)
        {
            Debug.Log($"<color=green>BattleInitializer: {data.name} verisiyle kurulum yapılıyor...</color>");

            // A. Hero Spawn & Setup
            if (heroPrefab != null && heroSpawnPoint != null)
            {
                GameObject hero = Instantiate(heroPrefab, heroSpawnPoint.position, Quaternion.identity);
                
                // Eğer PlayerStats scriptinde Initialize metodu varsa çağır
                // Yoksa, statları otomatik alıyordur (PlayerStats yapına göre burası değişebilir)
                /* * ÖNEMLİ: PlayerStats scriptine 'public void Initialize(PlayerStatsData data)' 
                 * metodu eklemen gerekebilir. Eğer yoksa şimdilik prefab'ın kendi statlarını kullanır.
                 */
            }

            // B. Kule Setup (Sahnedeki kulelere veri enjekte et)
            for (int i = 0; i < sceneTowers.Count; i++)
            {
                // Eğer Data listesinde bu kule için veri varsa
                if (data.towerStats != null && i < data.towerStats.Count)
                {
                    PlayerStatsData towerData = data.towerStats[i];
                    if (sceneTowers[i] != null)
                    {
                        // Kulenin statlarını güncelle (Eğer PlayerStats destekliyorsa)
                        // sceneTowers[i].Initialize(towerData); 
                        Debug.Log($"Kule {i+1} statları güncellendi: {towerData.name}");
                    }
                }
            }

            // C. Wave Setup & Start (KRİTİK NOKTA)
            if (waveController != null && data.levelWaves != null)
            {
                // WaveController'a "Hazır ol, bu listeyi oynatacağız" diyoruz
                waveController.InitializeFromExternal(data.levelWaves);
            }
            else
            {
                Debug.LogError("BattleInitializer: WaveController veya LevelWaves verisi eksik!");
            }
        }
    }
}