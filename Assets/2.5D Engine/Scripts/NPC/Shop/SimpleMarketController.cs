using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // GetPriceFor metodu Data içinde olduğu için buradaki Linq kullanımı azaldı

public class SimpleMarketController : MonoBehaviour
{
    [Header("--- VERİ KAYNAĞI (Data Source) ---")]
    [Tooltip("Marketin tüm ayarlarını (Fiyatlar, Prefablar vb.) içeren veri dosyası.")]
    [SerializeField] private SimpleMarketData marketData; // <-- BÜTÜN GÜÇ BURADA!

    [Header("--- MODLAR (Modes) ---")]
    [SerializeField] private bool keepWorkerActive = true;
    [SerializeField] private bool smartWaitMode = true; 

    [Header("--- SAHNE REFERANSLARI (Scene Refs) ---")]
    [Tooltip("Sıra noktaları (Scene'deki objeler).")]
    [SerializeField] private Transform[] queueSpots;
    
    [SerializeField] private Transform workerSpawnPoint;
    [SerializeField] private NpcPath workerPath;

    [SerializeField] private SiloController targetSilo;
    
    [Header("--- KASA (Wallet) ---")]
    [SerializeField] private int accumulatedCurrency = 0;

    // --- Runtime Değişkenleri ---
    private SimpleCustomer[] currentCustomers;
    private bool isWorkerBusy = false;
    private FriendlyNpcAI permanentWorker; 
    private List<ResourceData> possibleRequests; // Data'dan otomatik çekilecek

    private IEnumerator Start()
    {
        // 1. Önce Pooler'ın hazır olmasını bekle
        yield return new WaitUntil(() => NpcPooler.Instance != null);
        
        // 2. Veri Kontrolü (Data-Driven Güvenlik)
        if (marketData == null)
        {
            Debug.LogError($"HATA: '{name}' marketine 'SimpleMarketData' atanmamış! Çalışamıyor.");
            yield break;
        }
        if (queueSpots == null || queueSpots.Length == 0)
        {
             Debug.LogError($"HATA: '{name}' kuyruk noktaları (Queue Spots) atanmamış!");
             yield break;
        }

        // 3. Satılabilir ürünleri Data'dan çek (Otomatik)
        possibleRequests = marketData.GetSellableResources();
        if (possibleRequests.Count == 0)
        {
            Debug.LogWarning($"UYARI: '{marketData.name}' fiyat listesi boş! Müşteriler ne isteyeceğini bilemez.");
        }

        // 4. Müşteri Havuzunu Ayarla
        currentCustomers = new SimpleCustomer[queueSpots.Length];
        if (CustomerPooler.Instance != null && marketData.customerPrefab != null)
        {
            CustomerPooler.Instance.RegisterPool(marketData.customerPrefab, queueSpots.Length + 2);
        }

        // 5. İşçi Havuzu Rezervasyonu
        if (NpcPooler.Instance != null && marketData.workerPrefab != null)
        {
            NpcPooler.Instance.CreatePool(marketData.workerPoolTag, marketData.workerPrefab.gameObject, 1);
        }
        
        Debug.Log($"SimpleMarket: '{name}' ('{marketData.name}') verisiyle başlatıldı.");

        // 6. Döngüleri Başlat
        StartCoroutine(SpawnRoutine());
        StartCoroutine(LogicRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            TrySpawnCustomer();
            // Süreyi Data'dan al
            yield return new WaitForSeconds(marketData.customerSpawnInterval);
        }
    }

    private IEnumerator LogicRoutine()
    {
        while (true)
        {
            ShiftQueue();
            ManageWorkerLogic(); 
            yield return new WaitForSeconds(0.5f);
        }
    }

    // --- MÜŞTERİ YÖNETİMİ ---
    private void TrySpawnCustomer()
    {
        int lastIndex = queueSpots.Length - 1;
        if (currentCustomers[lastIndex] == null) SpawnCustomerAtSlot(lastIndex);
    }

    private void SpawnCustomerAtSlot(int index)
    {
        if (possibleRequests == null || possibleRequests.Count == 0) return;
        if (CustomerPooler.Instance == null) return;

        SimpleCustomer newCustomer = CustomerPooler.Instance.GetCustomer(queueSpots[index].position, Quaternion.identity);

        if (newCustomer != null)
        {
            // Listeden rastgele seç
            ResourceData randomResource = possibleRequests[Random.Range(0, possibleRequests.Count)];
            newCustomer.Initialize(randomResource);
            currentCustomers[index] = newCustomer;
        }
    }

    private void ShiftQueue()
    {
        for (int i = 0; i < queueSpots.Length - 1; i++)
        {
            if (currentCustomers[i] == null && currentCustomers[i + 1] != null)
            {
                currentCustomers[i] = currentCustomers[i + 1];
                currentCustomers[i + 1] = null; 
                currentCustomers[i].MoveToSpot(queueSpots[i].position);
            }
        }
    }

    // --- İŞÇİ MANTIĞI ---
    private void ManageWorkerLogic()
    {
        if (isWorkerBusy || currentCustomers[0] == null || targetSilo == null) return;

        ResourceData requestedRes = currentCustomers[0].RequestedResource;
        if (smartWaitMode)
        {
            if (targetSilo.GetStoredAmount(requestedRes) < 1) return; 
        }

        StartCoroutine(DispatchWorker());
    }

    private IEnumerator DispatchWorker()
    {
        isWorkerBusy = true;

        if (permanentWorker == null)
        {
            Vector3 spawnPos = (workerSpawnPoint != null) ? workerSpawnPoint.position : transform.position;
            Quaternion spawnRot = (workerSpawnPoint != null) ? workerSpawnPoint.rotation : Quaternion.identity;

            // Data'daki Tag'i kullan
            permanentWorker = NpcPooler.Instance.SpawnFromPool(marketData.workerPoolTag, spawnPos, spawnRot);
            
            if (permanentWorker == null)
            {
                Debug.LogWarning($"SimpleMarket: '{marketData.workerPoolTag}' havuzu boş! Acil durum.");
                // Acil durum yaratımı (Data'daki prefab ile)
                NpcPooler.Instance.CreatePool(marketData.workerPoolTag, marketData.workerPrefab.gameObject, 1);
                permanentWorker = NpcPooler.Instance.SpawnFromPool(marketData.workerPoolTag, spawnPos, spawnRot);
                
                if (permanentWorker == null) { isWorkerBusy = false; yield break; }
            }
        }
        else
        {
            if (!permanentWorker.gameObject.activeInHierarchy) permanentWorker.gameObject.SetActive(true);
        }

        permanentWorker.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
        permanentWorker.OnArrivedAtHome -= OnWorkerReturnedToShop;
        permanentWorker.OnArrivedAtWork += OnWorkerArrivedAtSilo;
        permanentWorker.OnArrivedAtHome += OnWorkerReturnedToShop;

        Transform homePoint = (workerSpawnPoint != null) ? workerSpawnPoint : transform;
        
        // Data'daki workerData'yı kullan
        permanentWorker.Activate(marketData.workerData, homePoint, targetSilo.GetSpawnPoint(), workerPath);
        yield return null;
    }

    private void OnWorkerArrivedAtSilo(FriendlyNpcAI npc)
    {
        if (currentCustomers[0] == null) { npc.ReturnHome(0, null); return; }

        ResourceData requested = currentCustomers[0].RequestedResource;
        int taken = targetSilo.TakeResource(requested, 1);
        npc.ReturnHome(taken, requested);
    }

    private void OnWorkerReturnedToShop(FriendlyNpcAI npc, int amount, ResourceData resource)
    {
        npc.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
        npc.OnArrivedAtHome -= OnWorkerReturnedToShop;
        isWorkerBusy = false;

        if (!keepWorkerActive)
        {
            NpcPooler.Instance.ReturnToPool(marketData.workerPoolTag, npc);
            permanentWorker = null; 
        }

        if (amount > 0 && currentCustomers[0] != null)
        {
            // --- DATA-DRIVEN KAZANÇ HESAPLAMA ---
            CalculateEarnings(resource, amount);
            
            currentCustomers[0].LeaveHappy();
            currentCustomers[0] = null; 
        }
    }

    private void CalculateEarnings(ResourceData soldItem, int quantity)
    {
        if (marketData.currencyResource == null) return;

        // Fiyatı Data'dan sor
        int price = marketData.GetPriceFor(soldItem);
        
        if (price > 0)
        {
            int totalEarned = price * quantity;
            accumulatedCurrency += totalEarned;
            
            Debug.Log($"KAZANÇ: {quantity}x {soldItem.resourceName} -> {totalEarned} {marketData.currencyResource.resourceName}. Toplam: {accumulatedCurrency}");
        }
        else
        {
            Debug.LogWarning($"Market: '{soldItem.resourceName}' Data dosyasında fiyatlandırılmamış! (0 Coin)");
        }
    }
}