/*
 * MARKET KONTROLCÜSÜ - v3.0 (Talep Üzerine Tek Sipariş)
 * * GÖREVİ:
 * - Müşteri geldiğinde NPC'yi Silo'ya gönderir.
 * - NPC geri döndüğünde, bekleyen müşteriye hizmet verir.
 *
 * * DEĞİŞİKLİKLER (v3.0):
 * - 'SalesRoutine', 'LogisticsRoutine' ve 'currentStock' SİLİNDİ.
 * - YENİ HAFIZA: 'workerAssignments' Sözlüğü, hangi NPC'nin hangi müşteriye
 * hizmet verdiğini tutar.
 * - 'HandleWorkerArrivedAtSilo' artık Silo'dan müşterinin istediği ürünü çeker.
 * - 'HandleWorkerReturnedToMarket' ürünü müşteriye teslim eder ve müşteriyi gönderir.
 * - 'TryBuyItem' metodu kaldırıldı, çünkü 'AttendToCustomer' ile kuyruk yönetimi yapılıyor.
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
    [Tooltip("Market NPC'lerinin doğacağı yer ve Müşterinin duracağı yer.")]
    [SerializeField] private Transform spawnPoint; 

    // --- DEĞİŞİKLİK BAŞLANGICI (v3.0) ---
    // Stok takibi kaldırıldı.
    // [SerializeField] private int currentStock = 0; // SİLİNDİ
    // --- DEĞİŞİKLİK SONU ---
    
    [Header("Lojistik Takibi")]
    [SerializeField] private int currentActiveWorkers = 0;

    // NPC Kuyrukları
    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();
    private Queue<FriendlyNpcAI> idleWorkers = new Queue<FriendlyNpcAI>(); 
    private Queue<CustomerAI> customerQueue = new Queue<CustomerAI>();

    // Hangi işçi hangi müşteriye hizmet veriyor? (Görev fişi)
    private Dictionary<FriendlyNpcAI, CustomerAI> workerCustomerAssignments = 
        new Dictionary<FriendlyNpcAI, CustomerAI>();


    private void Start()
    {
        if (marketData == null || targetSilo == null) return;
        
        // Satış ve Stok rutini kaldırıldı.
        // StartCoroutine(SalesRoutine()); 
        StartCoroutine(SpawnBatch(marketData.populationCount)); // Market NPC'lerini başlat
        
        // Kuyruk kontrolü hemen başlasın.
        StartCoroutine(LogisticsRoutine());
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
            CustomerAI customer = customerQueue.Dequeue(); // Müşteriyi al
            FriendlyNpcAI worker = idleWorkers.Dequeue(); // NPC'yi al
            
            // 2. Görev Fişini Oluştur
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
                
                idleWorkers.Enqueue(npc); // Başlangıçta boşta
                
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
        // NPC'nin 'Activate' metodu, Target'ı Silo olarak biliyor.
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
            // Debug.LogWarning("Market: Dönen NPC'nin görev fişi yok.");
            StartCoroutine(RestAndRestart(npc));
            return;
        }

        // 2. Teslimat
        if (amount >= customer.data.purchaseAmount)
        {
            // A) Başarılı Teslimat: Müşterinin istediği kadarını NPC getirdi.
            Debug.Log($"Market: {customer.name} {resource.resourceName}'i satın aldı. Teslimat başarılı.");
            
            // 3. Müşteriyi gönder
            customer.LeaveShop(); 
            
            // 4. Müşterinin görev fişini sil
            workerCustomerAssignments.Remove(npc);
        }
        else
        {
            // B) Başarısız Teslimat: Stokta yoktu, NPC az getirdi veya eli boş döndü.
            Debug.Log($"Market: Ürün ({resource.name}) yetmedi! Müşteri üzgün ayrılıyor.");
            customer.LeaveShop(); 
            workerCustomerAssignments.Remove(npc);
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
    
    public Transform GetInteractionPoint()
    {
        return (spawnPoint != null) ? spawnPoint : transform;
    }
    public MarketData GetMarketData() { return marketData; }
}