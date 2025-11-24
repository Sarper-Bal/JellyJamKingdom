/*
 * NPC HOUSING - FINAL (Local Pool + Upgrade + Save)
 * ÖZELLİKLER:
 * 1. Bağımsızdır (NpcPooler kullanmaz, kendi işçisini yönetir).
 * 2. Seviye atlayabilir (Görseli, işçi sayısını ve verisini değiştirir).
 * 3. Kaydedilebilir (Seviyesini ve içindeki stoğu hatırlar).
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D; // Namespace'leri unutma

public class NpcHousing : MonoBehaviour, ISaveable
{
    [Header("Veri & Seviye")]
    [SerializeField] private NpcHousingData housingData;
    
    [Header("Görsel Referanslar")]
    [Tooltip("Binanın görselinin değişmesi için SpriteRenderer referansı.")]
    [SerializeField] private SpriteRenderer buildingRenderer;
    [SerializeField] private Transform spawnPoint; 

    [Header("Upgrade & Ödeme")]
    [Tooltip("Geliştirme maliyetinin tahsil edileceği Silo.")]
    [SerializeField] private SiloController paymentSilo;

    [Header("İş Ayarları")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    [SerializeField] private WorkSpotInteractable resourceTarget; // Kaynak noktası
    [SerializeField] public NpcHousing houseTarget; // Transfer için hedef ev
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("Stok Durumu")]
    [SerializeField] private int outputProductCount = 0; 
    [SerializeField] private int inputRawMaterialCount = 0;
    
    public enum NpcJobType { GatherResource, TransferResource }
    public event System.Action<FriendlyNpcAI, NpcHousing> OnNpcReadyToWork;
    
    // --- YEREL İŞÇİ HAVUZU ---
    private List<FriendlyNpcAI> myWorkers = new List<FriendlyNpcAI>();
    
    private bool isRunning = false;
    private bool isProducing = false;

    private void Start()
    {
        // Eğer Data yoksa dur
        if (housingData == null) return;
        
        // Görseli başlangıç datasına göre ayarla (Eğer kayıt yüklenmediyse)
        UpdateVisuals();

        // İşçileri hazırla (Eğer kayıt yüklenip işçi yaratılmadıysa)
        if (myWorkers.Count == 0) InitializeWorkforce();
        
        // Binayı çalıştır
        StartHousing();
    }

    // --- BAŞLATMA & DURDURMA ---

    public void StartHousing()
    {
        if (isRunning) return;
        isRunning = true;
        
        // İşçileri sahaya sür
        StartCoroutine(DeployWorkersRoutine());
        
        if (housingData.requiresConversion) 
            StartCoroutine(ProductionRoutine());
    }

    public void StopHousing()
    {
        isRunning = false;
        StopAllCoroutines();
        // İşçileri eve çek (Pasif yap)
        foreach (var worker in myWorkers) 
            if(worker != null) worker.gameObject.SetActive(false);
    }

    // --- İŞÇİ YÖNETİMİ (LOCAL POOL) ---

    private void InitializeWorkforce()
    {
        if (housingData.genericNpcPrefab == null) return;

        // Eksik kadar üret (Upgrade sonrası için de çalışır)
        int currentCount = myWorkers.Count;
        int targetCount = housingData.populationCount;

        if (targetCount > currentCount)
        {
            for (int i = 0; i < (targetCount - currentCount); i++)
            {
                CreateWorker();
            }
        }
    }

    private FriendlyNpcAI CreateWorker()
    {
        // İşçiyi binanın çocuğu (Child) olarak üret
        GameObject workerObj = Instantiate(housingData.genericNpcPrefab, GetSpawnPoint().position, Quaternion.identity, transform);
        FriendlyNpcAI ai = workerObj.GetComponent<FriendlyNpcAI>();
        
        if (ai != null)
        {
            workerObj.SetActive(false);
            myWorkers.Add(ai);
            
            // Eventleri bağla
            ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
            ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
        }
        return ai;
    }

    private IEnumerator DeployWorkersRoutine()
    {
        // Mevcut işçileri sırayla çıkar
        foreach (var worker in myWorkers)
        {
            if (!isRunning) yield break;

            if (!worker.gameObject.activeInHierarchy)
            {
                worker.transform.position = GetSpawnPoint().position;
                worker.gameObject.SetActive(true);
                
                // Reset
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

    [ContextMenu("Upgrade Building")] // Test Butonu
    public void TryUpgrade()
    {
        // 1. Kontroller
        if (housingData == null || housingData.nextLevelData == null)
        {
            Debug.Log("Upgrade başarısız: Son seviye veya data eksik.");
            return;
        }
        if (paymentSilo == null)
        {
            Debug.LogError("Upgrade başarısız: Ödeme Silosu atanmamış!");
            return;
        }

        // 2. Bakiye Kontrolü
        foreach (var cost in housingData.upgradeCosts)
        {
            if (paymentSilo.GetStoredAmount(cost.resource) < cost.amount)
            {
                Debug.Log($"Yetersiz Kaynak: {cost.resource.name}");
                return;
            }
        }

        // 3. Ödeme Al
        foreach (var cost in housingData.upgradeCosts)
        {
            paymentSilo.TakeResource(cost.resource, cost.amount);
        }

        // 4. Seviyeyi Uygula
        ApplyUpgradeData(housingData.nextLevelData);
        
        Debug.Log($"<color=green>UPGRADE BAŞARILI!</color> Yeni Seviye: {housingData.buildingName}");
    }

    private void ApplyUpgradeData(NpcHousingData newData)
    {
        this.housingData = newData;
        UpdateVisuals();
        
        // Yeni işçi gerekiyorsa ekle ve hemen sahaya sür
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

    // --- İŞ MANTIĞI (CORE LOGIC) ---

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
        
        if (jobType == NpcJobType.GatherResource) 
            StartCoroutine(WorkCycle(npc, capacity, null)); 
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

    // --- HELPER METHODS ---
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

    // --- SAVE SYSTEM ENTEGRASYONU ---
    
    [System.Serializable]
    public class HousingSaveData
    {
        public string levelDataName; // Hangi seviye verisi?
        public int storedProduct;    // İçindeki ürün
        public int storedRaw;        // İçindeki hammadde
    }

    public object CaptureState()
    {
        return new HousingSaveData
        {
            levelDataName = housingData.name,
            storedProduct = this.outputProductCount,
            storedRaw = this.inputRawMaterialCount
        };
    }

    public void RestoreState(object state)
    {
        string jsonString = state as string;
        if (string.IsNullOrEmpty(jsonString)) return;

        HousingSaveData data = JsonUtility.FromJson<HousingSaveData>(jsonString);
        if (data != null)
        {
            // 1. Seviyeyi Geri Yükle
            // Not: Data dosyaları "Resources" klasöründe olmalı!
            if (!string.IsNullOrEmpty(data.levelDataName))
            {
                NpcHousingData levelData = Resources.Load<NpcHousingData>(data.levelDataName);
                if (levelData != null)
                {
                    ApplyUpgradeData(levelData); // Görseli ve işçileri güncelle
                }
            }

            // 2. Stokları Geri Yükle
            this.outputProductCount = data.storedProduct;
            this.inputRawMaterialCount = data.storedRaw;
        }
    }
}