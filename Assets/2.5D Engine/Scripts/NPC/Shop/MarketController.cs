/*
 * MARKET KONTROLCÜSÜ
 * GÖREVİ:
 * - Belirli aralıklarla (sellInterval) elindeki kaynağı satar (yok eder).
 * - Stok azaldığında (maxStorageCapacity'den azsa) Silo'dan kaynak ister.
 * - Kendi NPC'lerini Silo'ya gönderir, kaynak aldırır ve geri getirir.
 * - Tamamen 'NpcPooler' ve 'SiloController' ile entegre çalışır.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MarketController : MonoBehaviour
{
    [Header("Veri Kaynağı")]
    [SerializeField] private MarketData marketData;

    [Header("Kaynak Kaynağı")]
    [Tooltip("Bu marketin mal çekeceği Silo.")]
    [SerializeField] private SiloController targetSilo;
    
    [Tooltip("Market ile Silo arasındaki yol (Opsiyonel).")]
    [SerializeField] private NpcPath optionalPath;

    [Header("Konumlandırma")]
    [SerializeField] private Transform spawnPoint; // Marketin kapısı

    [Header("Stok Durumu")]
    [SerializeField] private int currentStock = 0;
    [SerializeField] private int currentActiveWorkers = 0;

    // Yönetilen NPC'ler
    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();

    private void Start()
    {
        if (marketData == null || targetSilo == null)
        {
            Debug.LogError($"Market ({gameObject.name}): Data veya Hedef Silo eksik!", this);
            return;
        }

        // 1. Satış Döngüsünü Başlat
        StartCoroutine(SalesRoutine());

        // 2. Lojistik (Stok Kontrol) Döngüsünü Başlat
        StartCoroutine(LogisticsRoutine());
    }

    /// <summary>
    /// Belirli aralıklarla stoktan ürün eksiltir (Satış Simülasyonu).
    /// </summary>
    private IEnumerator SalesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(marketData.sellInterval);

            if (currentStock >= marketData.sellAmount)
            {
                currentStock -= marketData.sellAmount;
                // Debug.Log($"Market: {marketData.sellAmount} adet {marketData.resourceToSell.name} satıldı. Kalan: {currentStock}");
            }
            else
            {
                // Debug.Log("Market: Stok yok, satış yapılamadı!");
            }
        }
    }

    /// <summary>
    /// Stok durumunu kontrol eder ve işçi çıkarır/emekli eder.
    /// </summary>
    private IEnumerator LogisticsRoutine()
    {
        while (true)
        {
            ManageWorkforce();
            yield return new WaitForSeconds(1.0f); // Her saniye kontrol et
        }
    }

    private void ManageWorkforce()
    {
        // Ne kadar boş yerimiz var?
        int spaceAvailable = marketData.maxStorageCapacity - currentStock;

        // Bu boşluğu doldurmak için kaç işçi lazım?
        int workerCapacity = marketData.npcDataToSpawn.maxCarryCapacity;
        int neededWorkers = Mathf.CeilToInt((float)spaceAvailable / workerCapacity);
        
        // Nüfus sınırını aşma
        neededWorkers = Mathf.Clamp(neededWorkers, 0, marketData.populationCount);

        // Eğer stok tamamen doluysa işçiye gerek yok
        if (spaceAvailable <= 0) neededWorkers = 0;

        int workersToSpawn = neededWorkers - activeWorkers.Count;

        if (workersToSpawn > 0)
        {
            StartCoroutine(SpawnBatch(workersToSpawn));
        }
        else if (workersToSpawn < 0 && spaceAvailable <= 0) 
        {
            // Fazla işçi varsa ve depo dolduysa, dönenleri emekli et (HandleWorkerReturnedHome'da yapılır)
        }
    }

    private IEnumerator SpawnBatch(int count)
    {
        string poolTag = marketData.genericNpcPrefab.name;
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            FriendlyNpcAI npc = NpcPooler.Instance.SpawnFromPool(poolTag, pos, Quaternion.identity);

            if (npc != null)
            {
                activeWorkers.Add(npc);
                currentActiveWorkers = activeWorkers.Count;

                npc.OnArrivedAtWork += HandleWorkerArrivedAtSilo;
                npc.OnArrivedAtHome += HandleWorkerReturnedToMarket;

                // İşe Gönder (Ev = Market, İş = Silo)
                SendWorkerToSilo(npc);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void SendWorkerToSilo(FriendlyNpcAI npc)
    {
        Transform myHome = (spawnPoint != null) ? spawnPoint : transform;
        Transform siloDest = targetSilo.GetSpawnPoint();

        npc.Activate(marketData.npcDataToSpawn, myHome, siloDest, optionalPath);
    }

    /// <summary>
    /// NPC Silo'ya vardığında.
    /// </summary>
    private void HandleWorkerArrivedAtSilo(FriendlyNpcAI npc)
    {
        // Kapasitesi kadar kaynak iste
        int capacity = npc.GetNpcData().maxCarryCapacity;
        
        // Silo'dan kaynağı çekmeye çalış
        int collected = targetSilo.TakeResource(marketData.resourceToSell, capacity);

        // Eve (Market) dön
        npc.ReturnHome(collected, marketData.resourceToSell);
    }

    /// <summary>
    /// NPC Market'e döndüğünde.
    /// </summary>
    private void HandleWorkerReturnedToMarket(FriendlyNpcAI npc, int amount, ResourceData resource)
    {
        if (amount > 0)
        {
            currentStock += amount;
            // Kapasiteyi aşma (Güvenlik)
            if (currentStock > marketData.maxStorageCapacity) currentStock = marketData.maxStorageCapacity;
            
            Debug.Log($"Market: +{amount} {resource.name} geldi. Stok: {currentStock}/{marketData.maxStorageCapacity}");
        }
        else
        {
            Debug.Log("Market: Silo'da kaynak yok, işçi boş döndü.");
        }

        // İşçi durumu değerlendirmesi
        int spaceAvailable = marketData.maxStorageCapacity - currentStock;
        
        // Depo dolduysa işçiyi emekli et
        if (spaceAvailable <= 0)
        {
            RetireWorker(npc);
        }
        else
        {
            StartCoroutine(RestAndRestart(npc));
        }
    }

    private void RetireWorker(FriendlyNpcAI npc)
    {
        npc.OnArrivedAtWork -= HandleWorkerArrivedAtSilo;
        npc.OnArrivedAtHome -= HandleWorkerReturnedToMarket;
        activeWorkers.Remove(npc);
        currentActiveWorkers = activeWorkers.Count;

        string poolTag = marketData.genericNpcPrefab.name;
        NpcPooler.Instance.ReturnToPool(poolTag, npc);
    }

    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(marketData.restDuration);
        if (npc.gameObject.activeInHierarchy)
        {
            SendWorkerToSilo(npc);
        }
    }
    
    // Pooler için veri erişimi
    public MarketData GetMarketData() { return marketData; }
}