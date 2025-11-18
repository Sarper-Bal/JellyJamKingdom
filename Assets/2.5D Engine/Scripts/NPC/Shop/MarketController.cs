/*
 * MARKET KONTROLCÜSÜ - v2.2 (Hata Düzeltmesi)
 * * DEĞİŞİKLİKLER:
 * - HATA DÜZELTMESİ (CS1061): 'FriendlyNpcAI'daki 'OnArrivedAtShop' event'i
 * 'OnArrivedAtWork' olarak düzeltildi (Line 131 ve 132).
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MarketController : MonoBehaviour
{
    [Header("Veri Kaynağı")]
    [SerializeField] private MarketData marketData;

    [Header("Kaynak Kaynağı")]
    [SerializeField] private SiloController targetSilo;
    [SerializeField] private NpcPath optionalPath;

    [Header("Konumlandırma")]
    [SerializeField] private Transform spawnPoint; 

    [Header("Stok Durumu")]
    [SerializeField] private int currentStock = 0;
    [SerializeField] private int currentActiveWorkers = 0;

    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();
    private Queue<FriendlyNpcAI> idleWorkers = new Queue<FriendlyNpcAI>(); 
    private Queue<CustomerAI> customerQueue = new Queue<CustomerAI>();

    private void Start()
    {
        if (marketData == null || targetSilo == null) return;
        StartCoroutine(SalesRoutine()); 
        StartCoroutine(LogisticsRoutine());
        StartCoroutine(SpawnBatch(marketData.populationCount)); 
    }

    private IEnumerator SalesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(marketData.sellInterval);
            if (currentStock >= marketData.sellAmount) currentStock -= marketData.sellAmount;
        }
    }

    private IEnumerator LogisticsRoutine()
    {
        while (true)
        {
            ProcessQueue();
            yield return new WaitForSeconds(1.0f); 
        }
    }
    
    private void SendWorkerToSilo(FriendlyNpcAI npc)
    {
        Transform myHome = (spawnPoint != null) ? spawnPoint : transform;
        Transform siloDest = targetSilo.GetSpawnPoint();
        npc.Activate(marketData.npcDataToSpawn, myHome, siloDest, optionalPath);
    }

    private void HandleWorkerArrivedAtSilo(FriendlyNpcAI npc)
    {
        int capacity = npc.GetNpcData().maxCarryCapacity;
        int collected = targetSilo.TakeResource(marketData.resourceToSell, capacity);
        npc.ReturnHome(collected, marketData.resourceToSell);
    }

    private void HandleWorkerReturnedToMarket(FriendlyNpcAI npc, int amount, ResourceData resource)
    {
        if (customerQueue.Count > 0)
        {
            CustomerAI customer = customerQueue.Dequeue();
            
            if (amount >= customer.data.purchaseAmount)
            {
                amount -= customer.data.purchaseAmount;
                customer.LeaveShop();
            }
            else
            {
                customer.LeaveShop(); 
            }
        }
        
        StartCoroutine(RestAndRestart(npc));
    }
    
    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(marketData.restDuration);
        idleWorkers.Enqueue(npc);
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        // Müşteri ve Boşta NPC var mı?
        if (customerQueue.Count > 0 && idleWorkers.Count > 0)
        {
            FriendlyNpcAI worker = idleWorkers.Dequeue(); 
            SendWorkerToSilo(worker);
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
                
                idleWorkers.Enqueue(npc);
                
                // --- HATA DÜZELTMESİ (CS1061) ---
                npc.OnArrivedAtWork -= HandleWorkerArrivedAtSilo; 
                npc.OnArrivedAtHome -= HandleWorkerReturnedToMarket;
                npc.OnArrivedAtWork += HandleWorkerArrivedAtSilo; // <-- DÜZELTİLDİ
                npc.OnArrivedAtHome += HandleWorkerReturnedToMarket;
                // ----------------------------------
            }
            yield return new WaitForSeconds(marketData.spawnInterval);
        }
    }

    // --- MÜŞTERİ SİSTEMİ METOTLARI ---

    /// <summary>
    /// Müşteri Yöneticisi tarafından çağrılır. Müşteriyi hizmet için kuyruğa alır.
    /// </summary>
    public void AttendToCustomer(CustomerAI customer)
    {
        customerQueue.Enqueue(customer);
        ProcessQueue();
    }
    
    public Transform GetInteractionPoint()
    {
        return (spawnPoint != null) ? spawnPoint : transform;
    }
    public MarketData GetMarketData() { return marketData; }
}