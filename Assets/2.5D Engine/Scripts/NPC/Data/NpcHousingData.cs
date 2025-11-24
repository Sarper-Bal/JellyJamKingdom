using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    // Upgrade maliyeti için yardımcı yapı
    [System.Serializable]
    public struct ResourceCost
    {
        public ResourceData resource;
        public int amount;
    }

    [CreateAssetMenu(fileName = "NewNpcHousingData", menuName = "Engine 2.5D/NPC/Housing Data")]
    public class NpcHousingData : ScriptableObject
    {
        // --- 1. BÖLÜM: GÖRSELLİK VE UPGRADE (Hatanın Çözümü Burası) ---
        [Header("Görsellik & Seviye")]
        [Tooltip("Binanın oyun içindeki adı (Örn: 'Oduncu Evi Sv.1').")]
        public string buildingName; // <-- HATA VEREN EKSİK DEĞİŞKEN BU
        
        [Tooltip("Bu seviyedeki binanın görünümü (Sprite). Upgrade olunca bu değişir.")]
        public Sprite buildingSprite;

        [Header("Upgrade Bağlantıları")]
        [Tooltip("Bir sonraki seviyenin datası. Boş ise bu son seviyedir.")]
        public NpcHousingData nextLevelData;

        [Tooltip("Bir sonraki seviyeye geçmek için gereken kaynaklar.")]
        public List<ResourceCost> upgradeCosts;

        // --- 2. BÖLÜM: İŞÇİ VE SPAWN ---
        [Header("İşçi Ayarları")]
        public GameObject genericNpcPrefab;
        public FriendlyNpcData npcDataToSpawn;
        public int populationCount = 3;
        public float spawnInterval = 1.5f;
        public float restDuration = 3.0f;

        // --- 3. BÖLÜM: ÜRETİM VE EKONOMİ ---
        [Header("Üretim Ayarları")]
        public ResourceData producedResource; 
        
        [Tooltip("Eğer işaretliyse, hammaddeyi işleyip ürüne dönüştürür.")]
        public bool requiresConversion = false;
        
        [Tooltip("1 ürün için kaç hammadde lazım?")]
        [Min(1)] 
        public int conversionRate = 3; 
        
        [Tooltip("Üretim süresi (saniye).")]
        public float conversionTime = 2.0f; 
    }
}