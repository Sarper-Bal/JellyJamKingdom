/*
 * SILO CONTROLLER - v2.0 (Save System Entegreli)
 * - 'ISaveable' arayüzü eklendi.
 * - Envanter verilerini JSON uyumlu formatta kaydedip yükleyebilir.
 * - ResourceData referanslarını "Resources.Load" ile tekrar bağlar.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D; // ISaveable ve SaveData sınıfları için gerekli

// LINQ kullanmiyoruz, performans icin manuel dongu yapiyoruz.

public class SiloController : MonoBehaviour, ISaveable // <-- YENİ: ISaveable eklendi
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
    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();
    private Dictionary<FriendlyNpcAI, SiloTargetData> workerAssignments = new Dictionary<FriendlyNpcAI, SiloTargetData>();
    
    private bool isRunning = false;

  // ... (ISaveable ve değişkenler aynı) ...

    private void Start()
    {
        if (siloData == null) return;

        // --- YENİ: Havuz Genişletme Tetikleyicisi ---
        if (NpcPooler.Instance != null)
        {
            NpcPooler.Instance.RecalculateAndExpandPools();
        }
        // --------------------------------------------

        StartSilo();
    }
    
    // ... (Geri kalan kodlar aynı) ...

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
    private void ManageWorkforce() {
        int cap = siloData.npcDataToSpawn.maxCarryCapacity;
        int needed = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / cap);
        needed = Mathf.Clamp(needed, 0, siloData.populationCount);
        int toSpawn = needed - activeWorkers.Count;
        if (toSpawn > 0) StartCoroutine(SpawnBatch(toSpawn));
    }
    private IEnumerator SpawnBatch(int count) {
        string tag = siloData.genericNpcPrefab.name;
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        for (int i = 0; i < count; i++) {
            FriendlyNpcAI npc = NpcPooler.Instance.SpawnFromPool(tag, pos, Quaternion.identity);
            if (npc != null) {
                activeWorkers.Add(npc);
                npc.OnArrivedAtWork += HandleWorkerArrivedAtTarget;
                npc.OnArrivedAtHome += HandleWorkerReturnedHome;
                SendWorkerToBestTarget(npc);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    // OPTIMIZE EDILMIS HEDEF BULMA (GC Allocation yok)
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

        Transform dest; Transform home = (spawnPoint != null) ? spawnPoint : transform; NpcPath path = null;
        if (best != null) {
            dest = best.house.GetSpawnPoint(); path = best.path;
            if (!workerAssignments.ContainsKey(npc)) workerAssignments.Add(npc, best); else workerAssignments[npc] = best;
        } else { RetireWorker(npc); return; }
        npc.Activate(siloData.npcDataToSpawn, home, dest, path);
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
        if (activeWorkers.Count > needed || resourcesWaitingToBeCollected == 0) RetireWorker(npc);
        else StartCoroutine(RestAndRestart(npc));
    }
    private void RetireWorker(FriendlyNpcAI npc) {
        npc.OnArrivedAtWork -= HandleWorkerArrivedAtTarget;
        npc.OnArrivedAtHome -= HandleWorkerReturnedHome;
        activeWorkers.Remove(npc);
        if (workerAssignments.ContainsKey(npc)) workerAssignments.Remove(npc);
        string tag = siloData.genericNpcPrefab.name;
        NpcPooler.Instance.ReturnToPool(tag, npc);
    }
    private IEnumerator RestAndRestart(FriendlyNpcAI npc) {
        yield return new WaitForSeconds(siloData.restDuration);
        if (npc.gameObject.activeInHierarchy) SendWorkerToBestTarget(npc);
    }
    public SiloData GetSiloData() { return siloData; }
    #endregion

    // --- SAVE SYSTEM INTEGRATION ---
    // Bu bölüm ISaveable arayüzü gerekliliklerini yerine getirir.
    
  // ... (Kodun üst kısımları aynı) ...

    // --- SAVE SYSTEM ENTEGRASYONU (GÜNCELLENDİ) ---
    
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
        // 1. String'e çevir (JSON)
        string jsonString = state as string;
        
        if (!string.IsNullOrEmpty(jsonString))
        {
            // 2. JSON'dan Class'a çevir
            SiloSaveData data = JsonUtility.FromJson<SiloSaveData>(jsonString);
            
            if (data == null) return;

            siloInventory.Clear();

            foreach (var entry in data.inventory)
            {
                // Resources'dan yükle
                ResourceData res = Resources.Load<ResourceData>(entry.resourceID);
                if (res != null)
                {
                    siloInventory.Add(res, entry.amount);
                }
            }
            UpdateInventoryDisplay(); 
        }
    }
}