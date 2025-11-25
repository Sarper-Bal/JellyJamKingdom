using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [System.Serializable]
    public struct ResourceCost
    {
        public ResourceData resource;
        public int amount;
    }

    [CreateAssetMenu(fileName = "NewNpcHousingData", menuName = "Engine 2.5D/NPC/Housing Data")]
    public class NpcHousingData : ScriptableObject
    {
        [Header("Görsellik & Seviye")]
        public string buildingName;
        
        // --- DEĞİŞİKLİK: Sprite yerine İndeks ---
        [Tooltip("BuildingVisualController listesindeki kaçıncı modeli açacak? (0, 1, 2...)")]
        public int visualIndex; 
        // public Sprite buildingSprite; // <-- BU SİLİNDİ
        // ----------------------------------------

        [Header("Upgrade Bağlantıları")]
        public NpcHousingData nextLevelData;
        public List<ResourceCost> upgradeCosts;

        [Header("İşçi Ayarları")]
        public GameObject genericNpcPrefab;
        public FriendlyNpcData npcDataToSpawn;
        public int populationCount = 3;
        public float spawnInterval = 1.5f;
        public float restDuration = 3.0f;

        [Header("Üretim Ayarları")]
        public ResourceData producedResource; 
        public bool requiresConversion = false;
        [Min(1)] public int conversionRate = 3; 
        public float conversionTime = 2.0f; 
    }
}