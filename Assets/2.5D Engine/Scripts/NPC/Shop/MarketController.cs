/*
 * MARKET KONTROLCÜSÜ - v4.0 (Talep Üzerine Tek Sipariş)
 * * GÖREVİ:
 * - Stoksuz "Just-In-Time" tedarik yönetimi.
 * - Müşterinin siparişini (Görev Fişi) NPC'ye atar.
 *
 * * HATA DÜZELTMESİ (CS1061): 'OnArrivedAtShop' -> 'OnArrivedAtWork' düzeltmesi içerir.
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

    [Header("Lojistik Takibi")]
    [SerializeField] private int currentActiveWorkers = 0;

    // NPC Kuyrukları
    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();
    private Queue<FriendlyNpcAI> idleWorkers = new Queue<FriendlyNpcAI>(); 
    private Queue<CustomerAI> customerQueue = new Queue<CustomerAI>();

    // Hangi NPC hangi müşteriye hizmet veriyor? (Görev fişi)
    private Dictionary<FriendlyNpcAI, CustomerAI> workerCustomerAssignments = 
        new Dictionary<FriendlyNpcAI, CustomerAI>();


    private void Start()
    {
        if (marketData == null || targetSilo == null) return;
        
        // Satış Rutini kaldırıldı
        StartCoroutine(SpawnBatch(marketData.populationCount)); 
        StartCoroutine(LogisticsRoutine()); // Kuyruk kontrolü hemen başlasın.
    }
    
    private IEnumerator LogisticsRoutine()
    {
        while (true)
        {
            ProcessQueue();
            yield return new WaitForSeconds(0.5f); // Daha sık kontrol edelim
        }
    }
    
    // --- MÜŞTERİ YÖNETİMİ ---

    /// <summary>
    /// Müşteri Yöneticisi tarafından çağrılır. Müşteriyi hizmet için kuyruğa alır.
    /// </summary>
    public void AttendToCustomer(CustomerAI customer)
    {
        // NPC'ye ihtiyaç var mı? Önce kuyruğa ekle
        customerQueue.Enqueue(customer);
        ProcessQueue();
    }
    
    /// <summary>
    /// Kuyruktaki müşteriye hizmet vermek için boşta NPC atar.
    /// </summary>
    private void ProcessQueue()
    {
        // 1. Müşteri ve Boşta NPC var mı?
        if (customerQueue.Count > 0 && idleWorkers.Count > 0)
        {
            CustomerAI customer = customerQueue.Peek(); // Müşteriyi al (kuyruktan çıkarma)
            FriendlyNpcAI worker = idleWorkers.Dequeue(); // Boşta NPC'yi al
            
            // 2. Görev Fişini Oluştur (Bu NPC bu müşterinin işini yapıyor)
            workerCustomerAssignments.Add(worker, customer);
            
            // 3. NPC'yi Silo'ya gönder
            SendWorkerToSilo(worker, customer);
        }
    }
    
    private IEnumerator SpawnBatch(int count)
    {
        // ... (NPC Spawn Mantığı AYNEN KALIYOR) ...
        #region SpawnBatch
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
                
                npc.OnArrivedAtWork += HandleWorkerArrivedAtSilo;
                npc.OnArrivedAtHome += HandleWorkerReturnedToMarket;
            }
            yield return new WaitForSeconds(marketData.spawnInterval);
        }
        #endregion
    }
    
    // --- LOHİSTİK MANİPÜLASYON METOTLARI ---

    /// <summary>
    /// NPC'yi Silo'ya gönderir.
    /// </summary>
    private void SendWorkerToSilo(FriendlyNpcAI npc, CustomerAI customer)
    {
        Transform myHome = (spawnPoint != null) ? spawnPoint : transform;
        Transform siloDest = targetSilo.GetSpawnPoint();

        // NPC'ye, müşterinin istediği ürünü getirme komutu veriliyor.
        // NPC'nin 'Activate' metoduna Silo'yu hedef olarak veriyoruz.
        npc.Activate(marketData.npcDataToSpawn, myHome, siloDest, optionalPath);
    }

    private void HandleWorkerArrivedAtSilo(FriendlyNpcAI npc)
    {
        // 1. Görev fişini al (Bu NPC kime hizmet ediyor?)
        if (!workerCustomerAssignments.TryGetValue(npc, out CustomerAI customer)) return;
        
        // 2. Müşterinin istediği ürünü ve miktarı öğren
        ResourceData requestedResource = customer.data.resourceToBuy;
        int requestedAmount = customer.data.purchaseAmount;
        
        // 3. NPC'nin kapasitesine göre en az olanı çek
        int amountToPull = Mathf.Min(requestedAmount, npc.GetNpcData().maxCarryCapacity);
        
        // 4. Silo'dan kaynağı çek
        int collected = targetSilo.TakeResource(requestedResource, amountToPull);
        
        // 5. Markete dön
        npc.ReturnHome(collected, requestedResource);
    }

    private void HandleWorkerReturnedToMarket(FriendlyNpcAI npc, int amount, ResourceData resource)
    {
        // 1. Hangi müşteriye hizmet veriyordu?
        if (!workerCustomerAssignments.TryGetValue(npc, out CustomerAI customer))
        {
            // Fiş kaybolmuş (olmamalı), NPC'yi dinlenmeye gönder.
            StartCoroutine(RestAndRestart(npc));
            return;
        }
        
        // 2. Teslimat
        if (amount >= customer.data.purchaseAmount)
        {
            // A) BAŞARILI Teslimat: Müşteriye ürün teslim edildi.
            
            // 3. Müşteriye malı ver ve gönder
            customer.LeaveShop(); 
            customerQueue.Dequeue(); // Kuyruktan tamamen çıkar
            
            // 4. NPC'den fişi sil
            workerCustomerAssignments.Remove(npc);
            
            Debug.Log($"Market: {customer.data.resourceToBuy.resourceName} satıldı. Müşteri ayrılıyor.");
        }
        else
        {
            // B) BAŞARISIZ Teslimat: Stokta yoktu, NPC az getirdi veya eli boş döndü.
            // Bu durumda, Market NPC'si getirdiği ürünü (amount) iade etmeli.
            
            // 3. Silo'ya kalan ürünü iade et
            if (amount > 0)
            {
                 targetSilo.IncreaseCounter(resource, amount);
            }
            
            // 4. Müşteriyi gönder (satış yapılamadı)
            customer.LeaveShop(); 
            customerQueue.Dequeue();
            workerCustomerAssignments.Remove(npc);

            Debug.Log($"Market: {customer.data.resourceToBuy.resourceName} yetmedi! Müşteri üzgün ayrılıyor. {amount} ürün iade edildi.");
        }

        // 5. NPC'yi dinlenmeye gönder
        StartCoroutine(RestAndRestart(npc));
    }
    
    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(marketData.restDuration);
        
        idleWorkers.Enqueue(npc);
        ProcessQueue();
    }
    
    // --- MÜŞTERİ SİSTEMİ İÇİN ERİŞİM METOTLARI ---
    public Transform GetInteractionPoint()
    {
        return (spawnPoint != null) ? spawnPoint : transform;
    }
    public MarketData GetMarketData() { return marketData; }
}