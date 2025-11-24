using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D; 

public class SiloController : MonoBehaviour, ISaveable, IResourceProvider
{
    [System.Serializable] public class SiloTargetData { public NpcHousing house; public NpcPath path; public int collectedAmount; }
    [System.Serializable] public class SiloInventoryEntry { public ResourceData resource; public int amount; }

    [Header("Veri & Seviye")]
    [SerializeField] private SiloData siloData;

    [Header("Görsel & Ödeme")]
    [SerializeField] private SpriteRenderer buildingRenderer;
    
    [Tooltip("Upgrade maliyetlerinin tahsil edileceği yerler (Silo, Market vb.).")]
    [SerializeField] private List<GameObject> paymentSources; // <-- ÇOKLU KAYNAK

    [Header("Hedefler")]
    [SerializeField] private List<SiloTargetData> targets;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private List<SiloInventoryEntry> inventoryDisplay = new List<SiloInventoryEntry>();
    
    private Dictionary<ResourceData, int> siloInventory = new Dictionary<ResourceData, int>();
    private List<FriendlyNpcAI> myWorkers = new List<FriendlyNpcAI>();
    private Dictionary<FriendlyNpcAI, SiloTargetData> workerAssignments = new Dictionary<FriendlyNpcAI, SiloTargetData>();
    
    private bool isRunning = false;
    private bool isDataRestored = false;
    private List<IResourceProvider> _cachedProviders;

    private void Start()
    {
        InitializePaymentSources(); // Sağlayıcıları hazırla

        if (isDataRestored) 
        {
            if (!isRunning) StartSilo();
            return;
        }

        if (siloData == null) return;

        UpdateVisuals();
        EnsureWorkforceCapacity();
        StartSilo();
    }

    private void InitializePaymentSources()
    {
        _cachedProviders = new List<IResourceProvider>();
        
        // Listekileri ekle
        if (paymentSources != null)
        {
            foreach (var sourceObj in paymentSources)
            {
                if (sourceObj != null)
                {
                    var provider = sourceObj.GetComponent<IResourceProvider>();
                    if (provider != null) _cachedProviders.Add(provider);
                }
            }
        }
        
        // Kendisini de ekle
        if (!_cachedProviders.Contains(this)) _cachedProviders.Add(this);
    }

    public void StartSilo()
    {
        if (isRunning) return;
        isRunning = true;
        StartCoroutine(SmartMonitorRoutine());
    }

    public void StopSilo()
    {
        isRunning = false;
        StopAllCoroutines();
        foreach (var worker in myWorkers) 
            if(worker != null) worker.gameObject.SetActive(false);
    }

    private IEnumerator SmartMonitorRoutine()
    {
        while (isRunning)
        {
            CalculateAvailableResources();
            ManageWorkforce();
            yield return new WaitForSeconds(2.0f); 
        }
    }

    // --- İŞÇİ YÖNETİMİ ---
    private void EnsureWorkforceCapacity()
    {
        if (siloData.genericNpcPrefab == null) return;
        int currentCount = myWorkers.Count;
        int targetCount = siloData.populationCount;
        if (targetCount > currentCount) {
            for (int i = 0; i < (targetCount - currentCount); i++) CreateWorker();
        }
    }
    private void CreateWorker() {
        GameObject workerObj = Instantiate(siloData.genericNpcPrefab, GetSpawnPoint().position, Quaternion.identity, transform);
        FriendlyNpcAI ai = workerObj.GetComponent<FriendlyNpcAI>();
        if (ai != null) {
            workerObj.SetActive(false);
            myWorkers.Add(ai);
            ai.OnArrivedAtWork += HandleWorkerArrivedAtTarget;
            ai.OnArrivedAtHome += HandleWorkerReturnedHome;
        }
    }
    private void ManageWorkforce() {
        int cap = siloData.npcDataToSpawn.maxCarryCapacity;
        int needed = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / cap);
        needed = Mathf.Clamp(needed, 0, siloData.populationCount);
        int currentActive = 0;
        foreach (var worker in myWorkers) if (worker.gameObject.activeInHierarchy) currentActive++;
        int toSpawn = needed - currentActive;
        if (toSpawn > 0) {
            int spawned = 0;
            foreach (var worker in myWorkers) {
                if (!worker.gameObject.activeInHierarchy && spawned < toSpawn) {
                    ActivateWorker(worker);
                    spawned++;
                }
            }
        }
    }
    private void ActivateWorker(FriendlyNpcAI npc) {
        npc.transform.position = GetSpawnPoint().position;
        npc.gameObject.SetActive(true);
        if (npc is IPooledNpc p) p.OnNpcSpawned();
        SendWorkerToBestTarget(npc);
    }

    // --- UPGRADE SİSTEMİ (Çoklu Kaynak) ---
    [ContextMenu("Upgrade Silo")]
    public void TryUpgrade()
    {
        if (siloData == null || siloData.nextLevelData == null) return;
        if (_cachedProviders == null || _cachedProviders.Count == 0) InitializePaymentSources();

        // A. Kontrol
        foreach (var cost in siloData.upgradeCosts)
        {
            int totalAvailable = 0;
            foreach (var provider in _cachedProviders) totalAvailable += provider.GetStoredAmount(cost.resource);
            if (totalAvailable < cost.amount) {
                Debug.Log($"Yetersiz Kaynak: {cost.resource.name}");
                return;
            }
        }

        // B. Ödeme
        foreach (var cost in siloData.upgradeCosts)
        {
            int remaining = cost.amount;
            foreach (var provider in _cachedProviders)
            {
                if (remaining <= 0) break;
                int taken = provider.TakeResource(cost.resource, remaining);
                remaining -= taken;
            }
        }

        // C. Uygula
        ApplyUpgradeData(siloData.nextLevelData);
        Debug.Log($"<color=green>SILO UPGRADE!</color> Yeni Seviye: {siloData.buildingName}");

        AutoSaveManager autoSave = FindObjectOfType<AutoSaveManager>();
        if (autoSave != null) autoSave.TriggerAutoSave("Silo Yükseltildi");
    }

    private void ApplyUpgradeData(SiloData newData)
    {
        this.siloData = newData;
        UpdateVisuals();
        EnsureWorkforceCapacity();
    }

    private void UpdateVisuals()
    {
        if (buildingRenderer != null && siloData.buildingSprite != null)
            buildingRenderer.sprite = siloData.buildingSprite;
    }

    #region Core Logic (Mevcut)
    // ... (Bu kısımdaki kodlar, önceki mesajımdaki ile birebir aynı, değişmedi) ...
    private int resourcesWaitingToBeCollected = 0;
    private void CalculateAvailableResources() {
        resourcesWaitingToBeCollected = 0;
        if (targets == null) return;
        foreach (var t in targets) { if (t.house != null) resourcesWaitingToBeCollected += t.house.GetResourceCount(); }
    }
    private void SendWorkerToBestTarget(FriendlyNpcAI npc) {
        SiloTargetData best = null;
        int maxResources = -1;
        if (targets != null) {
            foreach (var t in targets) {
                if (t.house != null) {
                    int currentResources = t.house.GetResourceCount();
                    if (currentResources > 0 && currentResources > maxResources) {
                        maxResources = currentResources;
                        best = t;
                    }
                }
            }
        }
        if (best != null) {
            if (!workerAssignments.ContainsKey(npc)) workerAssignments.Add(npc, best); else workerAssignments[npc] = best;
            npc.Activate(siloData.npcDataToSpawn, GetSpawnPoint(), best.house.GetSpawnPoint(), best.path);
        } else { RetireWorker(npc); }
    }
    public void IncreaseCounter(ResourceData resource, int amount) {
        if (resource == null || amount <= 0) return;
        if (siloInventory.ContainsKey(resource)) siloInventory[resource] += amount;
        else siloInventory.Add(resource, amount);
        UpdateInventoryDisplay();
    }
    public int TakeResource(ResourceData resource, int amountToTake) {
        if (resource == null || !siloInventory.ContainsKey(resource)) return 0; 
        int currentAmount = siloInventory[resource];
        int actualAmountGiven = Mathf.Min(currentAmount, amountToTake);
        siloInventory[resource] -= actualAmountGiven;
        UpdateInventoryDisplay();
        return actualAmountGiven;
    }
    public int GetStoredAmount(ResourceData resource) {
        if (resource == null || !siloInventory.ContainsKey(resource)) return 0;
        return siloInventory[resource];
    }
    public Transform GetSpawnPoint() { return (spawnPoint != null) ? spawnPoint : transform; }
    private void UpdateInventoryDisplay() {
        inventoryDisplay.Clear();
        foreach (var kvp in siloInventory) inventoryDisplay.Add(new SiloInventoryEntry { resource = kvp.Key, amount = kvp.Value });
    }
    private void HandleWorkerArrivedAtTarget(FriendlyNpcAI npc) {
        SiloTargetData data = workerAssignments.ContainsKey(npc) ? workerAssignments[npc] : null;
        int collected = 0; ResourceData resource = null;
        if (data != null && data.house != null) {
            int cap = npc.GetNpcData().maxCarryCapacity;
            collected = data.house.DecreaseCounter(cap);
            if (collected > 0) resource = data.house.GetProducedResource();
        }
        npc.ReturnHome(collected, resource);
    }
    private void HandleWorkerReturnedHome(FriendlyNpcAI npc, int amount, ResourceData resource) {
        if (amount > 0 && resource != null) IncreaseCounter(resource, amount);
        CalculateAvailableResources();
        int cap = siloData.npcDataToSpawn.maxCarryCapacity;
        int needed = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / cap);
        needed = Mathf.Clamp(needed, 0, siloData.populationCount);
        int activeCount = 0;
        foreach(var w in myWorkers) if(w.gameObject.activeInHierarchy) activeCount++;
        if (activeCount > needed || resourcesWaitingToBeCollected == 0) RetireWorker(npc);
        else StartCoroutine(RestAndRestart(npc));
    }
    private void RetireWorker(FriendlyNpcAI npc) {
        npc.gameObject.SetActive(false);
        if (workerAssignments.ContainsKey(npc)) workerAssignments.Remove(npc);
    }
    private IEnumerator RestAndRestart(FriendlyNpcAI npc) {
        yield return new WaitForSeconds(siloData.restDuration);
        if (npc.gameObject.activeInHierarchy) SendWorkerToBestTarget(npc);
    }
    public SiloData GetSiloData() { return siloData; }
    #endregion

    // --- SAVE SYSTEM (Aynı) ---
    [System.Serializable]
    public class SiloSaveData {
        public string levelDataName; 
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        [System.Serializable] public class InventoryEntry { public string resourceID; public int amount; }
    }
    public object CaptureState() {
        SiloSaveData data = new SiloSaveData();
        data.levelDataName = (siloData != null) ? siloData.name : "";
        foreach (var pair in siloInventory) {
            if (pair.Key != null) data.inventory.Add(new SiloSaveData.InventoryEntry { resourceID = pair.Key.name, amount = pair.Value });
        }
        return data;
    }
    public void RestoreState(object state) {
        string jsonString = state as string;
        if (string.IsNullOrEmpty(jsonString)) return;
        SiloSaveData data = JsonUtility.FromJson<SiloSaveData>(jsonString);
        if (data == null) return;
        if (!string.IsNullOrEmpty(data.levelDataName)) {
            SiloData loadedLevel = Resources.Load<SiloData>(data.levelDataName);
            if (loadedLevel == null) {
                var all = Resources.LoadAll<SiloData>("");
                foreach(var d in all) if(d.name == data.levelDataName) { loadedLevel = d; break; }
            }
            if (loadedLevel != null) ApplyUpgradeData(loadedLevel);
        }
        siloInventory.Clear();
        foreach (var entry in data.inventory) {
            ResourceData res = Resources.Load<ResourceData>(entry.resourceID);
            if (res != null) siloInventory.Add(res, entry.amount);
        }
        UpdateInventoryDisplay();
        isDataRestored = true;
        if (!isRunning) StartSilo();
    }
}