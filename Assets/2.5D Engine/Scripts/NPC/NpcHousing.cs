using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D;

public class NpcHousing : MonoBehaviour, ISaveable
{
    [Header("Veri & Seviye")]
    [SerializeField] private NpcHousingData housingData;
    
    [Header("Görsel Referanslar")]
    [SerializeField] private SpriteRenderer buildingRenderer;
    [SerializeField] private Transform spawnPoint; 

    [Header("Upgrade & Ödeme")]
    // --- DEĞİŞİKLİK: Liste yapısı ---
    [Tooltip("Geliştirme maliyetinin tahsil edileceği yerler (Silo, Market).")]
    [SerializeField] private List<GameObject> paymentSources;
    // --------------------------------

    [Header("İş Ayarları")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    [SerializeField] private WorkSpotInteractable resourceTarget;
    [SerializeField] public NpcHousing houseTarget;
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("Stok Durumu")]
    [SerializeField] private int outputProductCount = 0; 
    [SerializeField] private int inputRawMaterialCount = 0;
    
    public enum NpcJobType { GatherResource, TransferResource }
    public event System.Action<FriendlyNpcAI, NpcHousing> OnNpcReadyToWork;
    
    private List<FriendlyNpcAI> myWorkers = new List<FriendlyNpcAI>();
    private bool isRunning = false;
    private bool isProducing = false;
    private bool isDataRestored = false;
    
    // Cache
    private List<IResourceProvider> _cachedProviders;

    private void Start()
    {
        InitializePaymentSources(); // Kaynakları tara

        if (isDataRestored) 
        {
            if (!isRunning) StartHousing();
            return; 
        }
        if (housingData == null) return;
        UpdateVisuals();
        if (myWorkers.Count == 0) InitializeWorkforce();
        StartHousing();
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
    }

    // --- UPGRADE SİSTEMİ (Çoklu Kaynak) ---
    [ContextMenu("Upgrade Building")]
    public void TryUpgrade()
    {
        if (housingData == null || housingData.nextLevelData == null) return;
        if (_cachedProviders == null || _cachedProviders.Count == 0) InitializePaymentSources();

        // A. Kontrol
        foreach (var cost in housingData.upgradeCosts)
        {
            int totalAvailable = 0;
            foreach (var provider in _cachedProviders) totalAvailable += provider.GetStoredAmount(cost.resource);
            
            if (totalAvailable < cost.amount)
            {
                Debug.Log($"Yetersiz Kaynak: {cost.resource.name}");
                return;
            }
        }

        // B. Ödeme
        foreach (var cost in housingData.upgradeCosts)
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
        ApplyUpgradeData(housingData.nextLevelData);
        
        Debug.Log($"<color=green>UPGRADE BAŞARILI!</color> Yeni Seviye: {housingData.buildingName}");

        AutoSaveManager autoSave = FindObjectOfType<AutoSaveManager>();
        if (autoSave != null) autoSave.TriggerAutoSave("Bina Yükseltildi");
    }

    private void ApplyUpgradeData(NpcHousingData newData)
    {
        this.housingData = newData;
        UpdateVisuals();
        InitializeWorkforce();
        if (isRunning) StartCoroutine(DeployWorkersRoutine());
    }

    private void UpdateVisuals()
    {
        if (buildingRenderer != null && housingData.buildingSprite != null)
            buildingRenderer.sprite = housingData.buildingSprite;
    }

    // ... (InitializeWorkforce, CreateWorker, Core Logic, Save System vb. AYNI) ...
    public void StartHousing() { if (isRunning) return; isRunning = true; StartCoroutine(DeployWorkersRoutine()); if (housingData.requiresConversion) StartCoroutine(ProductionRoutine()); }
    public void StopHousing() { isRunning = false; StopAllCoroutines(); foreach (var worker in myWorkers) if(worker != null) worker.gameObject.SetActive(false); }
    private void InitializeWorkforce() { if (housingData.genericNpcPrefab == null) return; int currentCount = myWorkers.Count; int targetCount = housingData.populationCount; if (targetCount > currentCount) { for (int i = 0; i < (targetCount - currentCount); i++) CreateWorker(); } }
    private FriendlyNpcAI CreateWorker() { GameObject workerObj = Instantiate(housingData.genericNpcPrefab, GetSpawnPoint().position, Quaternion.identity, transform); FriendlyNpcAI ai = workerObj.GetComponent<FriendlyNpcAI>(); if (ai != null) { workerObj.SetActive(false); myWorkers.Add(ai); ai.OnArrivedAtWork += HandleNpcArrivedAtWork; ai.OnArrivedAtHome += HandleNpcArrivedAtHome; } return ai; }
    private IEnumerator DeployWorkersRoutine() { foreach (var worker in myWorkers) { if (!isRunning) yield break; if (!worker.gameObject.activeInHierarchy) { worker.transform.position = GetSpawnPoint().position; worker.gameObject.SetActive(true); if (worker is IPooledNpc p) p.OnNpcSpawned(); SendWorkerToTask(worker); } yield return new WaitForSeconds(housingData.spawnInterval); } }
    private void SendWorkerToTask(FriendlyNpcAI ai) { OnNpcReadyToWork?.Invoke(ai, this); Transform workTarget = DetermineWorkTarget(); ai.Activate(housingData.npcDataToSpawn, GetSpawnPoint(), workTarget, optionalNpcPath); }
    
    private IEnumerator ProductionRoutine() { while (isRunning) { if (inputRawMaterialCount >= housingData.conversionRate) { isProducing = true; yield return new WaitForSeconds(housingData.conversionTime); if (inputRawMaterialCount >= housingData.conversionRate) { inputRawMaterialCount -= housingData.conversionRate; outputProductCount++; } } else { isProducing = false; yield return new WaitForSeconds(1.0f); } } }
    private Transform DetermineWorkTarget() { if (jobType == NpcJobType.GatherResource && resourceTarget != null) return (resourceTarget.interactionPoint != null) ? resourceTarget.interactionPoint : resourceTarget.transform; else if (jobType == NpcJobType.TransferResource && houseTarget != null) return houseTarget.GetSpawnPoint(); return transform; }
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc) { FriendlyNpcData data = npc.GetNpcData(); if (data == null) { npc.ReturnHome(0, null); return; } int capacity = data.maxCarryCapacity; if (jobType == NpcJobType.GatherResource) StartCoroutine(WorkCycle(npc, capacity, null)); else if (jobType == NpcJobType.TransferResource) { int collected = 0; ResourceData resource = null; if (houseTarget != null) { collected = houseTarget.DecreaseCounter(capacity); if (collected > 0) resource = houseTarget.GetProducedResource(); } npc.ReturnHome(collected, resource); } }
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, int amount, ResourceData resource) { if (amount > 0) { if (housingData.requiresConversion) inputRawMaterialCount += amount; else outputProductCount += amount; } StartCoroutine(RestCycle(npc, housingData.restDuration)); }
    private IEnumerator WorkCycle(FriendlyNpcAI npc, int capacity, ResourceData resource) { if (resourceTarget != null) resourceTarget.TriggerInteraction(); yield return new WaitForSeconds(resourceTarget.workDuration); if(npc != null) npc.ReturnHome(capacity, resource); }
    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration) { yield return new WaitForSeconds(duration); if(npc != null && isRunning) SendWorkerToTask(npc); }
    public NpcHousingData GetHousingData() { return housingData; }
    public int GetResourceCount() { return outputProductCount; }
    public ResourceData GetProducedResource() { return housingData != null ? housingData.producedResource : null; }
    public Transform GetSpawnPoint() { return (spawnPoint != null) ? spawnPoint : transform; }
    public void IncreaseCounter(int amount) { outputProductCount += amount; }
    public int DecreaseCounter(int amountToTake) { if (outputProductCount == 0) return 0; int actual = Mathf.Min(outputProductCount, amountToTake); outputProductCount -= actual; return actual; }

    // Save System
    [System.Serializable] public class HousingSaveData { public string dataID; public int storedProduct; public int storedRaw; }
    public object CaptureState() { return new HousingSaveData { dataID = housingData != null ? housingData.name : "", storedProduct = this.outputProductCount, storedRaw = this.inputRawMaterialCount }; }
    public void RestoreState(object state) { string json = state as string; if (string.IsNullOrEmpty(json)) return; HousingSaveData data = JsonUtility.FromJson<HousingSaveData>(json); if (data != null) { if (!string.IsNullOrEmpty(data.dataID)) { NpcHousingData loadedData = ResourceManager.LoadHousingData(data.dataID); if (loadedData != null) { ApplyUpgradeData(loadedData); } } this.outputProductCount = data.storedProduct; this.inputRawMaterialCount = data.storedRaw; isDataRestored = true; if (!isRunning) StartHousing(); } }
}