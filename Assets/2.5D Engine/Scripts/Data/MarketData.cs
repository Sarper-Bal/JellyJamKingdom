/*
 * MARKET VERİSİ - v2.0 (Stoksuz Tedarik)
 * * DEĞİŞİKLİKLER:
 * - 'maxStorageCapacity', 'sellInterval' ve 'sellAmount' SİLİNDİ.
 * - Artık sadece lojistik ve NPC verilerini tutar.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewMarketData", menuName = "Stats/Market Data")]
public class MarketData : ScriptableObject
{
    [Header("Spawn Ayarları")]
    public GameObject genericNpcPrefab;
    public FriendlyNpcData npcDataToSpawn;
    public int populationCount = 3;
    public float spawnInterval = 1.5f;

    [Header("Davranış")]
    [Tooltip("NPC'lerin dinlenme süresi.")]
    public float restDuration = 2.0f;
    
    // Satılan ürün bilgisi (MarketController bu bilgiyi kullanmayacak, ama müşteri data'sında lazım)
    // [Tooltip("Bu marketin sattığı (ve Silo'dan isteyeceği) kaynak.")]
    // public ResourceData resourceToSell; // <-- KALDIRILDI
}