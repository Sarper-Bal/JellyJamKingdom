using UnityEngine;
using System.Collections.Generic;

// Bu satır sayesinde Project penceresinde Sağ Tık -> Create -> Economy -> Simple Market Data diyebileceksin.
[CreateAssetMenu(fileName = "NewSimpleMarketData", menuName = "Economy/Simple Market Data")]
public class SimpleMarketData : ScriptableObject
{
    [Header("--- GENEL AYARLAR (General) ---")]
    [Tooltip("Yeni müşteri gelme sıklığı (saniye).")]
    public float customerSpawnInterval = 2.5f;

    [Header("--- EKONOMİ (Economy) ---")]
    [Tooltip("Bu market satış karşılığında ne kazanacak? (Örn: Coin)")]
    public ResourceData currencyResource;

    [System.Serializable]
    public struct TradeItem
    {
        public ResourceData itemToSell; // Satılan Ürün (Örn: Stone)
        public int pricePerUnit;        // Fiyatı (Örn: 2 Coin)
    }

    [Tooltip("Bu markette satılan ürünler ve fiyat listesi.")]
    public List<TradeItem> priceList;

    [Header("--- PREFABLAR & İŞÇİ (Prefabs & Worker) ---")]
    [Tooltip("Müşteri Prefabı.")]
    public SimpleCustomer customerPrefab;

    [Tooltip("İşçi Prefabı (NpcPooler'a tanıtılacak).")]
    public FriendlyNpcAI workerPrefab;

    [Tooltip("İşçinin Hız ve Taşıma verileri.")]
    public FriendlyNpcData workerData;
    
    [Tooltip("İşçi Havuz Etiketi.")]
    public string workerPoolTag = "NPC";

    /// <summary>
    /// Marketin satabileceği ürünlerin listesini (ResourceData olarak) döndürür.
    /// </summary>
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
    
    /// <summary>
    /// Verilen ürünün fiyatını bulur. Listede yoksa 0 döner.
    /// </summary>
    public int GetPriceFor(ResourceData resource)
    {
        if (priceList == null) return 0;
        foreach (var item in priceList)
        {
            if (item.itemToSell == resource) return item.pricePerUnit;
        }
        return 0; // Listede yoksa bedava veya satılamaz
    }
}