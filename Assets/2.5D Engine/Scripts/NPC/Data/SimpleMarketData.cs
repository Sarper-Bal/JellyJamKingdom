using UnityEngine;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D; // ResourceCost ve ResourceData için

[CreateAssetMenu(fileName = "NewSimpleMarketData", menuName = "Economy/Simple Market Data")]
public class SimpleMarketData : ScriptableObject
{
    // --- YENİ: LEVEL & UPGRADE SİSTEMİ ---
    [Header("--- SEVİYE & UPGRADE ---")]
    [Tooltip("Marketin bu seviyedeki adı (Örn: Pazar Yeri Sv.2)")]
    public string buildingName;

    [Tooltip("Bir sonraki seviyenin datası. Boşsa son seviyedir.")]
    public SimpleMarketData nextLevelData;

    [Tooltip("Yükseltme için gereken kaynaklar.")]
    public List<ResourceCost> upgradeCosts;
    // -------------------------------------

    [Header("--- GENEL AYARLAR ---")]
    [Tooltip("Yeni müşteri gelme sıklığı (saniye).")]
    public float customerSpawnInterval = 2.5f;

    [Header("--- EKONOMİ ---")]
    [Tooltip("Bu market satış karşılığında ne kazanacak? (Örn: Coin)")]
    public ResourceData currencyResource;

    [System.Serializable]
    public struct TradeItem
    {
        public ResourceData itemToSell; 
        public int pricePerUnit;        
    }

    [Tooltip("Bu markette satılan ürünler ve fiyat listesi.")]
    public List<TradeItem> priceList;

    [Header("--- PREFABLAR & İŞÇİ ---")]
    [Tooltip("Müşteri Prefabı.")]
    public SimpleCustomer customerPrefab;

    [Tooltip("İşçi Prefabı.")]
    public FriendlyNpcAI workerPrefab;

    [Tooltip("İşçinin Hız ve Taşıma verileri.")]
    public FriendlyNpcData workerData;
    
    [Tooltip("İşçi Havuz Etiketi.")]
    public string workerPoolTag = "NPC";

    // --- YARDIMCI METOTLAR ---
    public List<ResourceData> GetSellableResources()
    {
        List<ResourceData> list = new List<ResourceData>();
        if (priceList != null)
        {
            foreach (var item in priceList)
            {
                if (item.itemToSell != null) list.Add(item.itemToSell);
            }
        }
        return list;
    }
    
    public int GetPriceFor(ResourceData resource)
    {
        if (priceList == null) return 0;
        foreach (var item in priceList)
        {
            if (item.itemToSell == resource) return item.pricePerUnit;
        }
        return 0; 
    }
}