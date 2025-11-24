/*
 * SIMPLE MARKET CONTROLLER - TAM BAĞIMSIZ (FIXED)
 * HATA DÜZELTMESİ:
 * - 'NpcPooler' referansları tamamen temizlendi.
 * - İşçi (Worker) artık yerel olarak (Instantiate) üretilip 'localWorker' değişkeninde saklanıyor.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IndianOceanAssets.Engine2_5D; // Save System ve IResourceProvider için

public class SimpleMarketController : MonoBehaviour, ISaveable, IResourceProvider
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
    private List<ResourceData> possibleRequests;
    private bool isRunning = false; 
    
    // --- YEREL İŞÇİ YÖNETİMİ ---
    private FriendlyNpcAI localWorker; 
    // ---------------------------

    private void Start()
    {
        // Artık NpcPooler beklemiyoruz.
        InitializeMarket();
        StartMarketLoop();
    }

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
        StopAllCoroutines(); 
        if (localWorker != null) localWorker.gameObject.SetActive(false);
    }

    private void InitializeMarket()
    {
        if (marketData == null || queueSpots == null || queueSpots.Length == 0) return;

        possibleRequests = marketData.GetSellableResources();
        currentCustomers = new SimpleCustomer[queueSpots.Length];
        
        // Müşteri Havuzu (CustomerPooler hala varsa kullanılır, yoksa hata vermesin diye null check)
        if (CustomerPooler.Instance != null && marketData.customerPrefab != null)
            CustomerPooler.Instance.RegisterPool(marketData.customerPrefab, queueSpots.Length + 2);

        // --- YEREL İŞÇİYİ OLUŞTUR ---
        if (localWorker == null && marketData.workerPrefab != null)
        {
            Vector3 spawnPos = (workerSpawnPoint != null) ? workerSpawnPoint.position : transform.position;
            
            // İşçiyi Market'in çocuğu olarak yarat
            GameObject workerObj = Instantiate(marketData.workerPrefab.gameObject, spawnPos, Quaternion.identity, transform);
            localWorker = workerObj.GetComponent<FriendlyNpcAI>();
            
            if (localWorker != null)
            {
                workerObj.SetActive(false); // Pasif bekle
                
                // Eventleri bağla
                localWorker.OnArrivedAtWork += OnWorkerArrivedAtSilo;
                localWorker.OnArrivedAtHome += OnWorkerReturnedToShop;
            }
        }
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
    
    #region Core Logic
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
        if (isWorkerBusy || currentCustomers[0] == null || targetSilo == null || localWorker == null) return;
        ResourceData requestedRes = currentCustomers[0].RequestedResource;
        if (smartWaitMode && targetSilo.GetStoredAmount(requestedRes) < 1) return; 
        StartCoroutine(DispatchWorker());
    }

    private IEnumerator DispatchWorker() {
        isWorkerBusy = true;
        
        // İşçiyi aktifleştir (Eğer pasifse)
        if (!localWorker.gameObject.activeInHierarchy)
        {
            Vector3 spawnPos = (workerSpawnPoint != null) ? workerSpawnPoint.position : transform.position;
            localWorker.transform.position = spawnPos;
            localWorker.gameObject.SetActive(true);
            
            // Reset (IPooledNpc arayüzü varsa)
            if (localWorker is IPooledNpc p) p.OnNpcSpawned();
        }

        // Göreve gönder
        Transform homePoint = (workerSpawnPoint != null) ? workerSpawnPoint : transform;
        localWorker.Activate(marketData.workerData, homePoint, targetSilo.GetSpawnPoint(), workerPath);
        
        yield return null;
    }

    private void OnWorkerArrivedAtSilo(FriendlyNpcAI npc) {
        if (currentCustomers[0] == null) { npc.ReturnHome(0, null); return; }
        ResourceData requested = currentCustomers[0].RequestedResource;
        int taken = targetSilo.TakeResource(requested, 1);
        npc.ReturnHome(taken, requested);
    }

    private void OnWorkerReturnedToShop(FriendlyNpcAI npc, int amount, ResourceData resource) {
        isWorkerBusy = false;
        
        // İş bitince pasif yap (Eğer sürekli aktif kalması istenmiyorsa)
        if (!keepWorkerActive) {
            npc.gameObject.SetActive(false);
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

    // --- IResourceProvider IMPLEMENTATION ---
    public int GetStoredAmount(ResourceData resource)
    {
        if (marketData != null && marketData.currencyResource == resource) return accumulatedCurrency;
        return 0;
    }

    public int TakeResource(ResourceData resource, int amountToTake)
    {
        if (marketData != null && marketData.currencyResource == resource)
        {
            int actual = Mathf.Min(accumulatedCurrency, amountToTake);
            accumulatedCurrency -= actual;
            return actual;
        }
        return 0;
    }

    // --- SAVE SYSTEM INTEGRATION ---
    public object CaptureState()
    {
        return new MarketSaveData { savedWalletAmount = this.accumulatedCurrency };
    }

    public void RestoreState(object state)
    {
        string jsonString = state as string;
        if (!string.IsNullOrEmpty(jsonString))
        {
            MarketSaveData data = JsonUtility.FromJson<MarketSaveData>(jsonString);
            if (data != null) this.accumulatedCurrency = data.savedWalletAmount;
        }
    }
}