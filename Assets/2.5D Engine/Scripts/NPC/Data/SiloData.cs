using UnityEngine;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D; // ResourceCost yapısı için

namespace IndianOceanAssets.Engine2_5D
{
    [CreateAssetMenu(fileName = "NewSiloData", menuName = "Engine 2.5D/NPC/Silo Data")]
    public class SiloData : ScriptableObject
    {
        [Header("Görsellik & Seviye")]
        [Tooltip("Silonun oyun içindeki adı (Örn: 'Taş Deposu Sv.2').")]
        public string buildingName;

        // --- DEĞİŞİKLİK: Sprite yerine İndeks ---
        [Tooltip("BuildingVisualController listesindeki kaçıncı modeli açacak? (0, 1, 2...)")]
        public int visualIndex;
        // ----------------------------------------

        [Header("Upgrade Bağlantıları")]
        [Tooltip("Bir sonraki seviyenin datası. Boşsa son seviyedir.")]
        public SiloData nextLevelData;

        [Tooltip("Yükseltme maliyeti.")]
        public List<ResourceCost> upgradeCosts;

        [Header("İşçi Ayarları")]
        public GameObject genericNpcPrefab;
        public FriendlyNpcData npcDataToSpawn;
        
        [Tooltip("Bu silonun çalıştırabileceği maksimum işçi sayısı.")]
        public int populationCount = 2; 
        
        public float restDuration = 2.0f;
    }
}