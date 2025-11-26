/*
 * SIMPLE MARKET CONTROLLER - FINAL (Delayed Rejection)
 * YENİ ÖZELLİKLER:
 * 1. [DELAY] Yasaklı ürün isteyen müşteri hemen gitmez, 'rejectionDuration' kadar bekler.
 * 2. [VISUAL] Beklerken başındaki ikon kızarır ve sallanır.
 * 3. [LOGIC] +0 yazısı anında çıkar, kuyruk süre bitince ilerler.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; 
using IndianOceanAssets.Engine2_5D; 

public class SimpleMarketController : MonoBehaviour, ISaveable, IResourceProvider
{
    [Header("--- VERİ KAYNAĞI ---")]
    [SerializeField] private SimpleMarketData marketData; 

    [Header("--- GÖRSEL & ÖDEME ---")]
    [SerializeField] private BuildingVisualController visualController;
    [SerializeField] private List<GameObject> paymentSources;

    [Header("--- EFEKTLER ---")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform textSpawnPoint;
    [SerializeField] private Color earningsTextColor = Color.yellow;
    
    // --- YENİ: REDDETME SÜRESİ ---
    [Tooltip("Yasaklı ürün isteyen müşterinin ne kadar süre bekleyip gideceği.")]
    [SerializeField] private float rejectionDuration = 1.5f;
    // -----------------------------

    [Header("--- YÖNETİM ---")]
    [SerializeField] private List<ResourceData> blockedResources = new List<ResourceData>(); 

    [Header("--- MODLAR ---")]
    [SerializeField] private bool keepWorkerActive = true;
    [SerializeField] private bool smartWaitMode = true; 

    [Header("--- SAHNE REFERANSLARI ---")]
    [SerializeField] private Transform[] queueSpots;
    [SerializeField] private Transform workerSpawnPoint;
    [SerializeField] private NpcPath workerPath;
    [SerializeField] private SiloController targetSilo;
    
    [Header("--- KASA ---")]
    [SerializeField] private int accumulatedCurrency = 0;

    // Runtime
    private SimpleCustomer[] currentCustomers;
    private bool isWorkerBusy = false;
    private List<ResourceData> possibleRequests;
    private bool isRunning = false; 
    private FriendlyNpcAI localWorker; 
    private bool isDataRestored = false;
    private List<IResourceProvider> _cachedProviders;
    
    // --- YENİ: REDDETME KİLİDİ ---
    private bool isProcessingRejection = false; 
    private bool isPoolRegistered = false;

    private IEnumerator Start()
    {
        InitializePaymentSources();

        if (isDataRestored) 
        {
            if (!isRunning) StartMarketLoop();
            yield break;
        }

        UpdateVisuals(false);
        InitializeMarket();
        StartMarketLoop();
    }

    private void InitializePaymentSources()
    {
        _cachedProviders = new List<IResourceProvider>();
        if (paymentSources != null)
        {
            foreach (var obj in paymentSources)
            {
                if (obj != null)
                {
                    var provider = obj.GetComponent<IResourceProvider>();
                    if (provider != null) _cachedProviders.Add(provider);
                }
            }
        }
        if (!_cachedProviders.Contains(this)) _cachedProviders.Add(this);
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
        
        if (!isPoolRegistered && CustomerPooler.Instance != null && marketData.customerPrefab != null)
        {
            SimpleCustomer scPrefab = marketData.customerPrefab.GetComponent<SimpleCustomer>();
            if (scPrefab != null)
            {
                CustomerPooler.Instance.RegisterPool(scPrefab, queueSpots.Length + 2);
                isPoolRegistered = true; 
            }
        }

        CreateOrUpdateWorker();
    }

    private void CreateOrUpdateWorker()
    {
        if (marketData.workerPrefab == null) return;

        if (localWorker != null)
        {
            Destroy(localWorker.gameObject);
            localWorker = null;
        }

        Vector3 spawnPos = (workerSpawnPoint != null) ? workerSpawnPoint.position : transform.position;
        GameObject workerObj = Instantiate(marketData.workerPrefab.gameObject, spawnPos, Quaternion.identity, transform);
        localWorker = workerObj.GetComponent<FriendlyNpcAI>();
        
        if (localWorker != null)
        {
            workerObj.SetActive(false); 
            localWorker.OnArrivedAtWork += OnWorkerArrivedAtSilo;
            localWorker.OnArrivedAtHome += OnWorkerReturnedToShop;
        }
    }

    public void ToggleProductSales(ResourceData resource, bool stopSelling)
    {
        if (stopSelling) { if (!blockedResources.Contains(resource)) blockedResources.Add(resource); }
        else { if (blockedResources.Contains(resource)) blockedResources.Remove(resource); }
    }

    // --- CORE LOGIC ---
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
            ResourceData randomResource = possibleRequests[Random.Range(0, possibleRequests.Count)];
            newCustomer.Initialize(randomResource);
            currentCustomers[index] = newCustomer;

            if (index == 0) newCustomer.ShowRequestBubble();
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

                if (i == 0) currentCustomers[0].ShowRequestBubble();
            }
        }
    }

    private void ManageWorkerLogic() 
    {
        // Eğer işçi meşgulse VEYA şu an birini reddediyorsak işlem yapma
        if (isWorkerBusy || isProcessingRejection || currentCustomers[0] == null || targetSilo == null || localWorker == null) return;
        
        ResourceData requestedRes = currentCustomers[0].RequestedResource;

        // --- YASAKLI ÜRÜN KONTROLÜ ---
        if (blockedResources.Contains(requestedRes))
        {
            // Gecikmeli reddetme sürecini başlat
            StartCoroutine(ProcessRejectionRoutine(currentCustomers[0], requestedRes));
            return;
        }
        // -----------------------------

        if (smartWaitMode && targetSilo.GetStoredAmount(requestedRes) < 1) return; 
        
        StartCoroutine(DispatchWorker());
    }

    // --- YENİ: GECİKMELİ REDDETME COROUTINE ---
    private IEnumerator ProcessRejectionRoutine(SimpleCustomer customer, ResourceData resource)
    {
        isProcessingRejection = true; // Sistemi kilitle

        // 1. Görsel Tepki (Kızarma / Sallanma)
        customer.PlayRejectionAnim();

        // 2. +0 Yazısı Çıkar
        CalculateEarnings(resource, 0); 

        // 3. Bekle
        yield return new WaitForSeconds(rejectionDuration);

        // 4. Gönder
        customer.LeaveHappy();
        currentCustomers[0] = null;

        isProcessingRejection = false; // Kilidi aç
    }
    // ------------------------------------------

    private IEnumerator DispatchWorker() 
    {
        isWorkerBusy = true;
        if (!localWorker.gameObject.activeInHierarchy)
        {
            Vector3 spawnPos = (workerSpawnPoint != null) ? workerSpawnPoint.position : transform.position;
            localWorker.transform.position = spawnPos;
            localWorker.gameObject.SetActive(true);
            if (localWorker is IPooledNpc p) p.OnNpcSpawned();
        }
        Transform homePoint = (workerSpawnPoint != null) ? workerSpawnPoint : transform;
        localWorker.Activate(marketData.workerData, homePoint, targetSilo.GetSpawnPoint(), workerPath);
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
        isWorkerBusy = false;
        if (!keepWorkerActive) {
            npc.gameObject.SetActive(false);
        }
        if (amount > 0 && currentCustomers[0] != null) {
            CalculateEarnings(resource, amount);
            currentCustomers[0].LeaveHappy(); 
            currentCustomers[0] = null; 
        }
    }
    
    private void CalculateEarnings(ResourceData soldItem, int quantity) 
    {
        if (marketData.currencyResource == null) return;
        
        int price = marketData.GetPriceFor(soldItem);
        int totalEarned = price * quantity;

        if (totalEarned > 0)
        {
            accumulatedCurrency += totalEarned;
            Debug.Log($"KAZANÇ: {quantity}x {soldItem.resourceName} -> {totalEarned}");
        }

        if (floatingTextPrefab != null)
        {
            Vector3 spawnPos = (textSpawnPoint != null) ? textSpawnPoint.position : transform.position + Vector3.up * 2.0f;
            GameObject popup = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
            var ft = popup.GetComponent<FloatingText>();
            
            if (ft != null)
            {
                // 0 ise kırmızı, kazanç ise seçili renk
                Color textColor = (totalEarned > 0) ? earningsTextColor : Color.red; 
                ft.Init("+" + totalEarned, textColor); 
            }
        }
    }

    // --- UPGRADE ---
    [ContextMenu("Upgrade Market")]
    public void TryUpgrade()
    {
        if (marketData == null || marketData.nextLevelData == null) return;
        if (_cachedProviders == null || _cachedProviders.Count == 0) InitializePaymentSources();

        foreach (var cost in marketData.upgradeCosts)
        {
            int totalAvailable = 0;
            foreach (var provider in _cachedProviders) totalAvailable += provider.GetStoredAmount(cost.resource);
            if (totalAvailable < cost.amount) { Debug.Log("Yetersiz Kaynak"); return; }
        }

        foreach (var cost in marketData.upgradeCosts)
        {
            int remaining = cost.amount;
            foreach (var provider in _cachedProviders)
            {
                if (remaining <= 0) break;
                int taken = provider.TakeResource(cost.resource, remaining);
                remaining -= taken;
            }
        }

        ApplyUpgradeData(marketData.nextLevelData);
        Debug.Log($"<color=green>MARKET UPGRADE!</color> Yeni Seviye: {marketData.buildingName}");

        if (FindObjectOfType<AutoSaveManager>() is AutoSaveManager autoSave)
            autoSave.TriggerAutoSave("Market Yükseltildi");
    }

    private void ApplyUpgradeData(SimpleMarketData newData)
    {
        this.marketData = newData;
        UpdateVisuals(true);
        InitializeMarket(); 
    }

    private void UpdateVisuals(bool animate)
    {
        if (visualController != null && marketData != null)
        {
            visualController.SetVisualIndex(marketData.visualIndex, animate);
        }
    }

    // --- IResourceProvider ---
    public int GetStoredAmount(ResourceData resource) { if (marketData != null && marketData.currencyResource == resource) return accumulatedCurrency; return 0; }
    public int TakeResource(ResourceData resource, int amountToTake) { if (marketData != null && marketData.currencyResource == resource) { int actual = Mathf.Min(accumulatedCurrency, amountToTake); accumulatedCurrency -= actual; return actual; } return 0; }

    // --- SAVE SYSTEM ---
    [System.Serializable]
    public class MarketSaveData
    {
        public string levelDataName; 
        public int savedWalletAmount;
        public List<string> blockedResourceIDs;
    }

    public object CaptureState()
    {
        MarketSaveData data = new MarketSaveData
        {
            levelDataName = (marketData != null) ? marketData.name : "",
            savedWalletAmount = this.accumulatedCurrency,
            blockedResourceIDs = new List<string>()
        };
        foreach (var res in blockedResources) if (res != null) data.blockedResourceIDs.Add(res.name);
        return data;
    }

    public void RestoreState(object state)
    {
        string jsonString = state as string;
        if (!string.IsNullOrEmpty(jsonString))
        {
            MarketSaveData data = JsonUtility.FromJson<MarketSaveData>(jsonString);
            if (data != null)
            {
                if (!string.IsNullOrEmpty(data.levelDataName))
                {
                    SimpleMarketData loadedLevel = Resources.Load<SimpleMarketData>(data.levelDataName);
                    if (loadedLevel == null)
                    {
                        var all = Resources.LoadAll<SimpleMarketData>("");
                        foreach(var d in all) if(d.name == data.levelDataName) { loadedLevel = d; break; }
                    }
                    if (loadedLevel != null) { this.marketData = loadedLevel; UpdateVisuals(false); InitializeMarket(); }
                }

                this.accumulatedCurrency = data.savedWalletAmount;

                blockedResources.Clear();
                if (data.blockedResourceIDs != null)
                {
                    foreach (string resName in data.blockedResourceIDs)
                    {
                        ResourceData res = Resources.Load<ResourceData>(resName);
                        if (res == null)
                        {
                             var allRes = Resources.LoadAll<ResourceData>("");
                             foreach(var r in allRes) if(r.name == resName) { res = r; break; }
                        }
                        if (res != null) blockedResources.Add(res);
                    }
                }
                
                isDataRestored = true;
                if (!isRunning) StartMarketLoop();
            }
        }
    }
}