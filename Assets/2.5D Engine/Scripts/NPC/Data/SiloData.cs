using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [CreateAssetMenu(fileName = "NewSiloData", menuName = "Engine 2.5D/NPC/Silo Data")]
    public class SiloData : ScriptableObject
    {
        // --- GÖRSELLİK VE UPGRADE ---
        [Header("Görsellik & Seviye")]
        [Tooltip("Silonun oyun içindeki adı (Örn: 'Taş Deposu Sv.1').")]
        public string buildingName;

        [Tooltip("Bu seviyedeki silonun görünümü.")]
        public Sprite buildingSprite;

        [Header("Upgrade Bağlantıları")]
        [Tooltip("Bir sonraki seviyenin datası.")]
        public SiloData nextLevelData;

        [Tooltip("Yükseltme maliyeti.")]
        // Not: ResourceCost yapısı namespace içinde tanımlı olduğu için buradan erişebiliriz.
        public List<ResourceCost> upgradeCosts;

        // --- MEVCUT AYARLAR ---
        [Header("İşçi Ayarları")]
        public GameObject genericNpcPrefab;
        public FriendlyNpcData npcDataToSpawn;
        
        [Tooltip("Bu silonun çalıştırabileceği maksimum işçi sayısı.")]
        public int populationCount = 2; // Seviye arttıkça bunu artırın
        
        public float restDuration = 2.0f;
    }
}