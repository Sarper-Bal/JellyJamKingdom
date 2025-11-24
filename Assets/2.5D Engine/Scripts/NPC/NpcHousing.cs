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
    [SerializeField] private SiloController paymentSilo;

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
    
    // --- ÇAKIŞMA ÖNLEYİCİ ---
    private bool isDataRestored = false; // Kayıttan veri geldi mi?

    private void Start()
    {
        // Eğer kayıt sistemi veriyi yüklediyse, Start'ın tekrar sıfırlamasına izin verme
        if (isDataRestored) 
        {
            // Zaten yüklendi, sadece çalıştır
            if (!isRunning) StartHousing();
            return; 
        }

        if (housingData == null) return;
        
        // Normal başlangıç (Kayıt yoksa)
        UpdateVisuals();
        InitializeWorkforce();
        StartHousing();
    }

    public void StartHousing()
    {
        if (isRunning) return;
        isRunning = true;
        StartCoroutine(DeployWorkersRoutine());
        if (housingData != null && housingData.requiresConversion) 
            StartCoroutine(ProductionRoutine());
    }

    // ... (StopHousing, InitializeWorkforce, CreateWorker, DeployWorkersRoutine, SendWorkerToTask AYNEN KALSIN) ...
    // Kod tekrarı olmasın diye burayı atlıyorum, eski metotlarını koru.
    // Ancak aşağıdaki RestoreState ve TryUpgrade kısımlarını mutlaka güncelle.

    public void StopHousing()
    {
        isRunning = false;
        StopAllCoroutines();
        foreach (var worker in myWorkers) if(worker != null) worker.gameObject.SetActive(false);
    }

    private void InitializeWorkforce()
    {
        if (housingData.genericNpcPrefab == null) return;
        int currentCount = myWorkers.Count;
        int targetCount = housingData.populationCount;
        if (targetCount > currentCount)
        {
            for (int i = 0; i < (targetCount - currentCount); i++) CreateWorker();
        }
    }

    private FriendlyNpcAI CreateWorker()
    {
        GameObject workerObj = Instantiate(housingData.genericNpcPrefab, GetSpawnPoint().position, Quaternion.identity, transform);
        FriendlyNpcAI ai = workerObj.GetComponent<FriendlyNpcAI>();
        if (ai != null)
        {
            workerObj.SetActive(false);
            myWorkers.Add(ai);
            ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
            ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
        }
        return ai;
    }

    private IEnumerator DeployWorkersRoutine()
    {
        foreach (var worker in myWorkers)
        {
            if (!isRunning) yield break;
            if (!worker.gameObject.activeInHierarchy)
            {
                worker.transform.position = GetSpawnPoint().position;
                worker.gameObject.SetActive(true);
                if (worker is IPooledNpc p) p.OnNpcSpawned();
                SendWorkerToTask(worker);
            }
            yield return new WaitForSeconds(housingData.spawnInterval);
        }
    }

    private void SendWorkerToTask(FriendlyNpcAI ai)
    {
        OnNpcReadyToWork?.Invoke(ai, this);
        Transform workTarget = DetermineWorkTarget();
        ai.Activate(housingData.npcDataToSpawn, GetSpawnPoint(), workTarget, optionalNpcPath); 
    }

    // --- UPGRADE SİSTEMİ ---
    [ContextMenu("Upgrade Building")]
    public void TryUpgrade()
    {
        if (housingData == null || housingData.nextLevelData == null) return;
        if (paymentSilo == null) return;

        foreach (var cost in housingData.upgradeCosts)
        {
            if (paymentSilo.GetStoredAmount(cost.resource) < cost.amount) return;
        }

        foreach (var cost in housingData.upgradeCosts)
        {
            paymentSilo.TakeResource(cost.resource, cost.amount);
        }

        ApplyUpgradeData(housingData.nextLevelData);
        
        Debug.Log($"<color=green>UPGRADE BAŞARILI!</color> Yeni Seviye: {housingData.buildingName}");

        if (SaveManager.Instance != null)
        {
            // DİKKAT: AutoSaveManager yerine direkt SaveManager'ı tetikliyoruz (Daha güvenli)
            // Ama AutoSaveManager varsa onu kullanmak daha iyidir.
             AutoSaveManager autoSave = FindObjectOfType<AutoSaveManager>();
             if(autoSave != null) autoSave.TriggerAutoSave("Bina Yükseltildi");
        }
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
        {
            buildingRenderer.sprite = housingData.buildingSprite;
        }
    }

    // ... (Core Logic: Production, HandleNpc... metodları AYNEN KALSIN) ...
    private IEnumerator ProductionRoutine() {
        while (isRunning) { 
            if (inputRawMaterialCount >= housingData.conversionRate) {
                isProducing = true;
                yield return new WaitForSeconds(housingData.conversionTime);
                if (inputRawMaterialCount >= housingData.conversionRate) {
                    inputRawMaterialCount -= housingData.conversionRate;
                    outputProductCount++; 
                }
            } else {
                isProducing = false;
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
    private Transform DetermineWorkTarget() {
        if (jobType == NpcJobType.GatherResource && resourceTarget != null) 
            return (resourceTarget.interactionPoint != null) ? resourceTarget.interactionPoint : resourceTarget.transform;
        else if (jobType == NpcJobType.TransferResource && houseTarget != null) 
            return houseTarget.GetSpawnPoint();
        return transform; 
    }
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc) {
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null) { npc.ReturnHome(0, null); return; }
        int capacity = data.maxCarryCapacity; 
        if (jobType == NpcJobType.GatherResource) StartCoroutine(WorkCycle(npc, capacity, null)); 
        else if (jobType == NpcJobType.TransferResource) {
            int collected = 0; ResourceData resource = null;
            if (houseTarget != null) {
                collected = houseTarget.DecreaseCounter(capacity);
                if (collected > 0) resource = houseTarget.GetProducedResource();
            }
            npc.ReturnHome(collected, resource); 
        }
    }
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, int amount, ResourceData resource) {
        if (amount > 0) {
            if (housingData.requiresConversion) inputRawMaterialCount += amount;
            else outputProductCount += amount;
        }
        StartCoroutine(RestCycle(npc, housingData.restDuration));
    }
    private IEnumerator WorkCycle(FriendlyNpcAI npc, int capacity, ResourceData resource) {
        if (resourceTarget != null) resourceTarget.TriggerInteraction();
        yield return new WaitForSeconds(resourceTarget.workDuration);
        if(npc != null) npc.ReturnHome(capacity, resource);
    }
    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration) {
        yield return new WaitForSeconds(duration);
        if(npc != null && isRunning) SendWorkerToTask(npc); 
    }
    public NpcHousingData GetHousingData() { return housingData; }
    public int GetResourceCount() { return outputProductCount; }
    public ResourceData GetProducedResource() { return housingData != null ? housingData.producedResource : null; }
    public Transform GetSpawnPoint() { return (spawnPoint != null) ? spawnPoint : transform; }
    public void IncreaseCounter(int amount) { outputProductCount += amount; }
    public int DecreaseCounter(int amountToTake) {
        if (outputProductCount == 0) return 0;
        int actual = Mathf.Min(outputProductCount, amountToTake);
        outputProductCount -= actual;
        return actual;
    }

    // --- SAVE SYSTEM ENTEGRASYONU (DÜZELTİLDİ) ---
    
    [System.Serializable]
    public class HousingSaveData
    {
        public string dataID;
        public int storedProduct;
        public int storedRaw;
    }

    public object CaptureState()
    {
        return new HousingSaveData 
        { 
            dataID = housingData != null ? housingData.name : "",
            storedProduct = this.outputProductCount,
            storedRaw = this.inputRawMaterialCount
        };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        if (string.IsNullOrEmpty(json)) return;

        HousingSaveData data = JsonUtility.FromJson<HousingSaveData>(json);
        if (data != null)
        {
            // 1. AKILLI DATA YÜKLEME (ResourceManager Kullanıyor)
            if (!string.IsNullOrEmpty(data.dataID))
            {
                // ResourceManager nerede olursa olsun bulur
                NpcHousingData loadedData = ResourceManager.LoadHousingData(data.dataID);
                
                if (loadedData != null)
                {
                    ApplyUpgradeData(loadedData);
                    Debug.Log($"NpcHousing: '{loadedData.buildingName}' başarıyla yüklendi.");
                }
            }

            this.outputProductCount = data.storedProduct;
            this.inputRawMaterialCount = data.storedRaw;
            
            // 2. Start'ın bunu ezmesini engelle
            isDataRestored = true; 
            
            // Hemen başlat (Çünkü Start çalışmayabilir)
            if (!isRunning) StartHousing();
        }
    }
}