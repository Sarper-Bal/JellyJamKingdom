/*
 * SILO CONTROLLER - v3.0 (Bağımsız / Decentralized)
 * DEĞİŞİKLİKLER:
 * - NpcPooler bağımlılığı kaldırıldı.
 * - Kendi işçi havuzunu (myWorkers) yönetir.
 * - 'ManageWorkforce' artık havuzdan çekmek yerine yerel listeden aktivasyon yapar.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D; // Save System ve ISaveable için

public class SiloController : MonoBehaviour, ISaveable
{
    [System.Serializable] public class SiloTargetData { public NpcHousing house; public NpcPath path; public int collectedAmount; }
    [System.Serializable] public class SiloInventoryEntry { public ResourceData resource; public int amount; }

    [Header("Veri")]
    [SerializeField] private SiloData siloData;

    [Header("Hedefler")]
    [SerializeField] private List<SiloTargetData> targets;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private List<SiloInventoryEntry> inventoryDisplay = new List<SiloInventoryEntry>();
    
    private Dictionary<ResourceData, int> siloInventory = new Dictionary<ResourceData, int>();
    
    // --- YEREL HAVUZ ---
    private List<FriendlyNpcAI> myWorkers = new List<FriendlyNpcAI>();
    private Dictionary<FriendlyNpcAI, SiloTargetData> workerAssignments = new Dictionary<FriendlyNpcAI, SiloTargetData>();
    
    private bool isRunning = false;

    private void Start()
    {
        if (siloData == null) return;

        // Kendi işçilerini hazırla
        InitializeWorkforce();
        
        StartSilo();
    }

    // --- YENİ: İŞÇİLERİ OLUŞTUR ---
    private void InitializeWorkforce()
    {
        if (siloData.genericNpcPrefab == null) return;

        for (int i = 0; i < siloData.populationCount; i++)
        {
            // İşçiyi Silo'nun çocuğu olarak yarat
            GameObject workerObj = Instantiate(siloData.genericNpcPrefab, GetSpawnPoint().position, Quaternion.identity, transform);
            FriendlyNpcAI ai = workerObj.GetComponent<FriendlyNpcAI>();
            
            if (ai != null)
            {
                workerObj.SetActive(false); // Pasif bekle
                myWorkers.Add(ai);
                
                // Eventleri bağla
                ai.OnArrivedAtWork += HandleWorkerArrivedAtTarget;
                ai.OnArrivedAtHome += HandleWorkerReturnedHome;
            }
        }
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
        // Tüm işçileri durdur
        foreach (var worker in myWorkers) worker.gameObject.SetActive(false);
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

    #region Core Logic
    private int resourcesWaitingToBeCollected = 0;
    
    private void CalculateAvailableResources() {
        resourcesWaitingToBeCollected = 0;
        if (targets == null) return;
        foreach (var t in targets) { if (t.house != null) resourcesWaitingToBeCollected += t.house.GetResourceCount(); }
    }

    // --- YENİ: YEREL HAVUZ YÖNETİMİ ---
    private void ManageWorkforce() {
        // 1. İhtiyacı hesapla
        int cap = siloData.npcDataToSpawn.maxCarryCapacity;
        int needed = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / cap);
        needed = Mathf.Clamp(needed, 0, siloData.populationCount); // Maksimum kapasiteyi aşma

        // 2. Şu an kaç kişi çalışıyor?
        int currentActive = 0;
        foreach (var worker in myWorkers) if (worker.gameObject.activeInHierarchy) currentActive++;

        // 3. Eksik varsa tamamla
        int toSpawn = needed - currentActive;
        if (toSpawn > 0)
        {
            int spawned = 0;
            foreach (var worker in myWorkers)
            {
                if (!worker.gameObject.activeInHierarchy && spawned < toSpawn)
                {
                    ActivateWorker(worker);
                    spawned++;
                }
            }
        }
    }

    private void ActivateWorker(FriendlyNpcAI npc)
    {
        Vector3 pos = GetSpawnPoint().position;
        npc.transform.position = pos;
        npc.gameObject.SetActive(true);
        
        if (npc is IPooledNpc p) p.OnNpcSpawned(); // Reset
        
        SendWorkerToBestTarget(npc);
    }
    
    private void SendWorkerToBestTarget(FriendlyNpcAI npc) {
        SiloTargetData best = null;
        int maxResources = -1;

        if (targets != null)
        {
            foreach (var t in targets)
            {
                if (t.house != null)
                {
                    int currentResources = t.house.GetResourceCount();
                    if (currentResources > 0 && currentResources > maxResources)
                    {
                        maxResources = currentResources;
                        best = t;
                    }
                }
            }
        }

        Transform dest; Transform home = GetSpawnPoint(); NpcPath path = null;
        if (best != null) {
            dest = best.house.GetSpawnPoint(); path = best.path;
            if (!workerAssignments.ContainsKey(npc)) workerAssignments.Add(npc, best); else workerAssignments[npc] = best;
        } else { 
            RetireWorker(npc); // İş yoksa emekli et (Pasif yap)
            return; 
        }
        npc.Activate(siloData.npcDataToSpawn, home, dest, path);
    }

    // ... (Stok yönetimi ve Helper'lar AYNI) ...
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
        
        // İş bitince tekrar değerlendir
        CalculateAvailableResources();
        int cap = siloData.npcDataToSpawn.maxCarryCapacity;
        int needed = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / cap);
        needed = Mathf.Clamp(needed, 0, siloData.populationCount);
        
        // Şu an çalışan sayısını bul
        int activeCount = 0;
        foreach(var w in myWorkers) if(w.gameObject.activeInHierarchy) activeCount++;

        // Eğer çalışan sayısı ihtiyacı aştıysa veya iş kalmadıysa -> Emekli et
        if (activeCount > needed || resourcesWaitingToBeCollected == 0) 
        {
            RetireWorker(npc);
        }
        else 
        {
            StartCoroutine(RestAndRestart(npc));
        }
    }

    // --- YENİ: YEREL EMEKLİLİK ---
    private void RetireWorker(FriendlyNpcAI npc) {
        // NpcPooler.ReturnToPool yerine sadece pasif yapıyoruz.
        npc.gameObject.SetActive(false);
        if (workerAssignments.ContainsKey(npc)) workerAssignments.Remove(npc);
    }

    private IEnumerator RestAndRestart(FriendlyNpcAI npc) {
        yield return new WaitForSeconds(siloData.restDuration);
        if (npc.gameObject.activeInHierarchy) SendWorkerToBestTarget(npc);
    }
    public SiloData GetSiloData() { return siloData; }
    #endregion

    // --- SAVE SYSTEM INTEGRATION (ISaveable) ---
    public object CaptureState()
    {
        SiloSaveData data = new SiloSaveData();
        foreach (var pair in siloInventory)
        {
            if (pair.Key != null)
            {
                SiloSaveData.InventoryEntry entry = new SiloSaveData.InventoryEntry();
                entry.resourceID = pair.Key.name; 
                entry.amount = pair.Value;
                data.inventory.Add(entry);
            }
        }
        return data;
    }

    public void RestoreState(object state)
    {
        string jsonString = state as string;
        if (!string.IsNullOrEmpty(jsonString))
        {
            SiloSaveData data = JsonUtility.FromJson<SiloSaveData>(jsonString);
            if (data == null) return;
            siloInventory.Clear();
            foreach (var entry in data.inventory)
            {
                ResourceData res = Resources.Load<ResourceData>(entry.resourceID);
                if (res != null) siloInventory.Add(res, entry.amount);
            }
            UpdateInventoryDisplay(); 
        }
    }
}