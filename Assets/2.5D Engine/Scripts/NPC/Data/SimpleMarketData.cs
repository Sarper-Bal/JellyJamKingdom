using UnityEngine;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D; // ResourceCost ve ResourceData için

[CreateAssetMenu(fileName = "NewSimpleMarketData", menuName = "Economy/Simple Market Data")]
public class SimpleMarketData : ScriptableObject
{
    // --- LEVEL & UPGRADE ---
    [Header("--- SEVİYE & GÖRSEL ---")]
    [Tooltip("Marketin bu seviyedeki adı (Örn: Pazar Yeri Sv.2)")]
    public string buildingName;

    // --- DEĞİŞİKLİK: 3D Görsel İndeksi ---
    [Tooltip("BuildingVisualController listesindeki kaçıncı modeli açacak? (0, 1, 2...)")]
    public int visualIndex;
    // -------------------------------------

    [Header("Upgrade Bağlantıları")]
    [Tooltip("Bir sonraki seviyenin datası. Boşsa son seviyedir.")]
    public SimpleMarketData nextLevelData;

    [Tooltip("Yükseltme maliyeti.")]
    public List<ResourceCost> upgradeCosts;

    // --- MEVCUT AYARLAR ---
    [Header("--- GENEL AYARLAR ---")]
    public float customerSpawnInterval = 2.5f;

    [Header("--- EKONOMİ ---")]
    public ResourceData currencyResource;

    [System.Serializable]
    public struct TradeItem { public ResourceData itemToSell; public int pricePerUnit; }
    public List<TradeItem> priceList;

    [Header("--- PREFABLAR & İŞÇİ ---")]
    public SimpleCustomer customerPrefab;
    public FriendlyNpcAI workerPrefab;
    public FriendlyNpcData workerData;
    public string workerPoolTag = "NPC";

    public List<ResourceData> GetSellableResources() {
        List<ResourceData> list = new List<ResourceData>();
        if (priceList != null) foreach (var item in priceList) if (item.itemToSell != null) list.Add(item.itemToSell);
        return list;
    }
    public int GetPriceFor(ResourceData resource) {
        if (priceList == null) return 0;
        foreach (var item in priceList) if (item.itemToSell == resource) return item.pricePerUnit;
        return 0; 
    }
}