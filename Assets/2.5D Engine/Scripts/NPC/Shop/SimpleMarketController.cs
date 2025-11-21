using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; 

public class SimpleMarketController : MonoBehaviour
{
    [Header("--- VERİ KAYNAĞI (Data Source) ---")]
    [SerializeField] private SimpleMarketData marketData; 

    [Header("--- MODLAR (Modes) ---")]
    [SerializeField] private bool keepWorkerActive = true;
    [SerializeField] private bool smartWaitMode = true; 

    [Header("--- SAHNE REFERANSLARI ---")]
    [SerializeField] private Transform[] queueSpots;
    [SerializeField] private Transform workerSpawnPoint;
    [SerializeField] private NpcPath workerPath;
    [SerializeField] private SiloController targetSilo;
    
    [Header("--- KASA (Wallet) ---")]
    [SerializeField] private int accumulatedCurrency = 0;

    // Runtime
    private SimpleCustomer[] currentCustomers;
    private bool isWorkerBusy = false;
    private FriendlyNpcAI permanentWorker; 
    private List<ResourceData> possibleRequests;
    private bool isRunning = false; // Market çalışıyor mu?

    private IEnumerator Start()
    {
        // 1. Pooler'ı bekle
        yield return new WaitUntil(() => NpcPooler.Instance != null);
        
        // 2. Kurulumları yap (Init) ama BAŞLATMA
        InitializeMarket();

        // 3. EconomyManager'a Abone Ol
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyStart += StartMarketLoop;
            EconomyManager.Instance.OnEconomyStop += StopMarketLoop;

            // Eğer yönetici zaten çalışıyorsa biz de başlayalım
            if (EconomyManager.Instance.IsSystemActive)
            {
                StartMarketLoop();
            }
        }
        else
        {
            // Yönetici yoksa eski usül otomatik başla (Güvenlik)
            Debug.LogWarning($"SimpleMarket ({name}): EconomyManager bulunamadı, otomatik başlatılıyor.");
            StartMarketLoop();
        }
    }

    private void OnDestroy()
    {
        // Abonelikten çık (Memory Leak önlemi)
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyStart -= StartMarketLoop;
            EconomyManager.Instance.OnEconomyStop -= StopMarketLoop;
        }
    }

    // --- YENİ: BAŞLAT/DURDUR KONTROLLERİ ---

    public void StartMarketLoop()
    {
        if (isRunning) return;
        isRunning = true;
        StartCoroutine(SpawnRoutine());
        StartCoroutine(LogicRoutine());
    }

    public void StopMarketLoop()
    {
        isRunning = false;
        StopAllCoroutines(); // Döngüleri durdur
    }

    // --- INIT VE LOGIC (ESKİ KODLARIN DÜZENLENMİŞ HALİ) ---

    private void InitializeMarket()
    {
        if (marketData == null || queueSpots == null || queueSpots.Length == 0) return;

        possibleRequests = marketData.GetSellableResources();
        
        // Müşteri Havuzu
        currentCustomers = new SimpleCustomer[queueSpots.Length];
        if (CustomerPooler.Instance != null && marketData.customerPrefab != null)
            CustomerPooler.Instance.RegisterPool(marketData.customerPrefab, queueSpots.Length + 2);

        // İşçi Havuzu
        if (NpcPooler.Instance != null && marketData.workerPrefab != null)
            NpcPooler.Instance.CreatePool(marketData.workerPoolTag, marketData.workerPrefab.gameObject, 1);
    }

    private IEnumerator SpawnRoutine()
    {
        while (isRunning)
        {
            TrySpawnCustomer();
            yield return new WaitForSeconds(marketData.customerSpawnInterval);
        }
    }

    private IEnumerator LogicRoutine()
    {
        while (isRunning)
        {
            ShiftQueue();
            ManageWorkerLogic(); 
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ... (TrySpawnCustomer, SpawnCustomerAtSlot, ShiftQueue, ManageWorkerLogic, DispatchWorker AYNI) ...
    // Kod tekrarı olmaması için alttaki lojistik metodları aynen koruyoruz.
    // Sadece class'ın geri kalanının çalıştığını varsayıyoruz.
    
    #region Core Logic (Unchanged)
    private void TrySpawnCustomer() {
        int lastIndex = queueSpots.Length - 1;
        if (currentCustomers[lastIndex] == null) SpawnCustomerAtSlot(lastIndex);
    }
    private void SpawnCustomerAtSlot(int index) {
        if (possibleRequests == null || possibleRequests.Count == 0) return;
        if (CustomerPooler.Instance == null) return;
        SimpleCustomer newCustomer = CustomerPooler.Instance.GetCustomer(queueSpots[index].position, Quaternion.identity);
        if (newCustomer != null) {
            ResourceData randomResource = possibleRequests[Random.Range(0, possibleRequests.Count)];
            newCustomer.Initialize(randomResource);
            currentCustomers[index] = newCustomer;
        }
    }
    private void ShiftQueue() {
        for (int i = 0; i < queueSpots.Length - 1; i++) {
            if (currentCustomers[i] == null && currentCustomers[i + 1] != null) {
                currentCustomers[i] = currentCustomers[i + 1];
                currentCustomers[i + 1] = null; 
                currentCustomers[i].MoveToSpot(queueSpots[i].position);
            }
        }
    }
    private void ManageWorkerLogic() {
        if (isWorkerBusy || currentCustomers[0] == null || targetSilo == null) return;
        ResourceData requestedRes = currentCustomers[0].RequestedResource;
        if (smartWaitMode && targetSilo.GetStoredAmount(requestedRes) < 1) return; 
        StartCoroutine(DispatchWorker());
    }
    private IEnumerator DispatchWorker() {
        isWorkerBusy = true;
        if (permanentWorker == null) {
            Vector3 spawnPos = (workerSpawnPoint != null) ? workerSpawnPoint.position : transform.position;
            Quaternion spawnRot = (workerSpawnPoint != null) ? workerSpawnPoint.rotation : Quaternion.identity;
            permanentWorker = NpcPooler.Instance.SpawnFromPool(marketData.workerPoolTag, spawnPos, spawnRot);
            if (permanentWorker == null) {
                NpcPooler.Instance.CreatePool(marketData.workerPoolTag, marketData.workerPrefab.gameObject, 1);
                permanentWorker = NpcPooler.Instance.SpawnFromPool(marketData.workerPoolTag, spawnPos, spawnRot);
                if (permanentWorker == null) { isWorkerBusy = false; yield break; }
            }
        } else {
            if (!permanentWorker.gameObject.activeInHierarchy) permanentWorker.gameObject.SetActive(true);
        }
        permanentWorker.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
        permanentWorker.OnArrivedAtHome -= OnWorkerReturnedToShop;
        permanentWorker.OnArrivedAtWork += OnWorkerArrivedAtSilo;
        permanentWorker.OnArrivedAtHome += OnWorkerReturnedToShop;
        Transform homePoint = (workerSpawnPoint != null) ? workerSpawnPoint : transform;
        permanentWorker.Activate(marketData.workerData, homePoint, targetSilo.GetSpawnPoint(), workerPath);
        yield return null;
    }
    private void OnWorkerArrivedAtSilo(FriendlyNpcAI npc) {
        if (currentCustomers[0] == null) { npc.ReturnHome(0, null); return; }
        ResourceData requested = currentCustomers[0].RequestedResource;
        int taken = targetSilo.TakeResource(requested, 1);
        npc.ReturnHome(taken, requested);
    }
    private void OnWorkerReturnedToShop(FriendlyNpcAI npc, int amount, ResourceData resource) {
        npc.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
        npc.OnArrivedAtHome -= OnWorkerReturnedToShop;
        isWorkerBusy = false;
        if (!keepWorkerActive) {
            NpcPooler.Instance.ReturnToPool(marketData.workerPoolTag, npc);
            permanentWorker = null; 
        }
        if (amount > 0 && currentCustomers[0] != null) {
            CalculateEarnings(resource, amount);
            currentCustomers[0].LeaveHappy();
            currentCustomers[0] = null; 
        }
    }
    private void CalculateEarnings(ResourceData soldItem, int quantity) {
        if (marketData.currencyResource == null) return;
        int price = marketData.GetPriceFor(soldItem);
        if (price > 0) {
            int totalEarned = price * quantity;
            accumulatedCurrency += totalEarned;
            Debug.Log($"KAZANÇ: {quantity}x {soldItem.resourceName} -> {totalEarned}");
        }
    }
    #endregion
}