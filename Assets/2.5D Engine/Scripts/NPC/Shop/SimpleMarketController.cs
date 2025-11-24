/*
 * SIMPLE MARKET CONTROLLER - FINAL (3D Upgrade + Queue Preserved)
 * ÖZELLİKLER:
 * 1. Mevcut Müşteri Kuyruğu ve Pool sistemi korundu.
 * 2. 3D uyumlu (SpriteRenderer kaldırıldı).
 * 3. Upgrade sistemi (Çoklu Kaynak Ödemeli) eklendi.
 * 4. Save sistemi (Seviye ve Para) entegre edildi.
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

    [Header("--- UPGRADE & ÖDEME ---")]
    [Tooltip("Upgrade maliyetinin tahsil edileceği kaynaklar (Silo, Kendisi vb.).")]
    [SerializeField] private List<GameObject> paymentSources;

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
    
    // Yerel İşçi ve Save Kontrolü
    private FriendlyNpcAI localWorker; 
    private bool isDataRestored = false;
    private List<IResourceProvider> _cachedProviders;

    private IEnumerator Start()
    {
        InitializePaymentSources();

        // Save'den veri geldiyse akışı bozma
        if (isDataRestored) 
        {
            if (!isRunning) StartMarketLoop();
            yield break;
        }

        // İlk Başlangıç
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
        // Kendi kasasını da kullanabilsin
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
        
        // --- MÜŞTERİ HAVUZU (MEVCUT SİSTEM KORUNDU) ---
        if (CustomerPooler.Instance != null && marketData.customerPrefab != null)
        {
            // Prefab'i component olarak al (GameObject gelse bile çalışır)
            SimpleCustomer scPrefab = marketData.customerPrefab.GetComponent<SimpleCustomer>();
            if (scPrefab != null)
            {
                CustomerPooler.Instance.RegisterPool(scPrefab, queueSpots.Length + 2);
            }
        }

        // İşçiyi Yarat
        CreateOrUpdateWorker();
    }

    private void CreateOrUpdateWorker()
    {
        if (marketData.workerPrefab == null) return;

        // Eski işçiyi temizle (Upgrade durumunda)
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

    // --- UPGRADE SİSTEMİ (3D Uyumlu) ---

    [ContextMenu("Upgrade Market")]
    public void TryUpgrade()
    {
        if (marketData == null || marketData.nextLevelData == null) return;
        if (_cachedProviders == null || _cachedProviders.Count == 0) InitializePaymentSources();

        // 1. Kaynak Kontrolü
        foreach (var cost in marketData.upgradeCosts)
        {
            int totalAvailable = 0;
            foreach (var provider in _cachedProviders) totalAvailable += provider.GetStoredAmount(cost.resource);
            
            if (totalAvailable < cost.amount)
            {
                Debug.Log($"Yetersiz Kaynak: {cost.resource.name}");
                return;
            }
        }

        // 2. Ödeme Alma
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

        // 3. Uygulama
        ApplyUpgradeData(marketData.nextLevelData);
        Debug.Log($"<color=green>MARKET UPGRADE!</color> Yeni Seviye: {marketData.buildingName}");

        if (FindObjectOfType<AutoSaveManager>() is AutoSaveManager autoSave)
            autoSave.TriggerAutoSave("Market Yükseltildi");
    }

    private void ApplyUpgradeData(SimpleMarketData newData)
    {
        this.marketData = newData;
        
        // Görsel güncelleme (3D Mesh Swap vb.) buraya eklenebilir.
        // Şu an 3D olduğu için SpriteRenderer kullanılmıyor.
        
        // Yeni işçi özellikleri veya müşteri tipleri için marketi yenile
        InitializeMarket(); 
    }

    // --- MEVCUT KUYRUK MANTIĞI (Aynen Korundu) ---
    
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

    private void ManageWorkerLogic() 
    {
        if (isWorkerBusy || currentCustomers[0] == null || targetSilo == null || localWorker == null) return;
        ResourceData requestedRes = currentCustomers[0].RequestedResource;
        if (smartWaitMode && targetSilo.GetStoredAmount(requestedRes) < 1) return; 
        StartCoroutine(DispatchWorker());
    }

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
        if (price > 0) {
            int totalEarned = price * quantity;
            accumulatedCurrency += totalEarned;
            Debug.Log($"KAZANÇ: {quantity}x {soldItem.resourceName} -> {totalEarned}");
        }
    }

    // --- IResourceProvider (Para Sağlayıcı) ---
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

    // --- SAVE SYSTEM (Seviye ve Para) ---
    [System.Serializable]
    public class MarketSaveData
    {
        public string levelDataName; 
        public int savedWalletAmount; 
    }

    public object CaptureState()
    {
        return new MarketSaveData 
        { 
            levelDataName = (marketData != null) ? marketData.name : "",
            savedWalletAmount = this.accumulatedCurrency 
        };
    }

    public void RestoreState(object state)
    {
        string jsonString = state as string;
        if (!string.IsNullOrEmpty(jsonString))
        {
            MarketSaveData data = JsonUtility.FromJson<MarketSaveData>(jsonString);
            if (data != null)
            {
                // 1. Seviyeyi Yükle
                if (!string.IsNullOrEmpty(data.levelDataName))
                {
                    SimpleMarketData loadedLevel = Resources.Load<SimpleMarketData>(data.levelDataName);
                    // Resources.Load bulamazsa genel arama yap
                    if (loadedLevel == null)
                    {
                        var all = Resources.LoadAll<SimpleMarketData>("");
                        foreach(var d in all) if(d.name == data.levelDataName) { loadedLevel = d; break; }
                    }

                    if (loadedLevel != null)
                    {
                        ApplyUpgradeData(loadedLevel);
                    }
                }

                // 2. Parayı Yükle
                this.accumulatedCurrency = data.savedWalletAmount;
                
                isDataRestored = true;
                if (!isRunning) StartMarketLoop();
            }
        }
    }
}