/*
 * MARKET VERİSİ (SCRIPTABLE OBJECT)
 * GÖREVİ:
 * Market binasının ne satacağını ve nasıl çalışacağını belirler.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewMarketData", menuName = "Stats/Market Data")]
public class MarketData : ScriptableObject
{
    [Header("Spawn Ayarları")]
    [Tooltip("Marketin kullanacağı taşıyıcı NPC prefab'ı.")]
    public GameObject genericNpcPrefab;
    
    [Tooltip("Taşıyıcı NPC'lerin hızı/kapasitesi.")]
    public FriendlyNpcData npcDataToSpawn;

    [Tooltip("Markette çalışan toplam NPC sayısı.")]
    public int populationCount = 3;

    [Tooltip("NPC'lerin çıkış aralığı.")]
    public float spawnInterval = 1.5f;

    [Header("Ekonomi ve Satış")]
    [Tooltip("Bu marketin sattığı (ve Silo'dan isteyeceği) kaynak.")]
    public ResourceData resourceToSell;

    [Tooltip("Marketin maksimum stok kapasitesi.")]
    public int maxStorageCapacity = 50;

    [Tooltip("Kaç saniyede bir ürün satılacağı.")]
    public float sellInterval = 3.0f;

    [Tooltip("Her satışta kaç birim eksileceği.")]
    public int sellAmount = 1;

    [Header("Davranış")]
    [Tooltip("NPC'lerin dinlenme süresi.")]
    public float restDuration = 2.0f;
}