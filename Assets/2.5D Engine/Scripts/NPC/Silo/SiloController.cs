/*
 * SILO KONTROLCÜSÜ - v3.2 (Hata Düzeltmesi & Tam ResourceData Entegrasyonu)
 *
 * * HATA DÜZELTMESİ (CS0246):
 * - Kodun içindeki tüm 'ResourceType' (eski Enum) referansları temizlendi.
 * - Yerine 'ResourceData' (ScriptableObject) getirildi.
 * - 'HandleWorkerArrivedAtTarget' metodunda 'GetProducedResource()' çağrısı düzeltildi.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SiloController : MonoBehaviour
{
    [System.Serializable]
    public class SiloTargetData
    {
        public NpcHousing house;
        public NpcPath path;
        public int collectedAmount;
    }

    // Envanter artık ResourceData (Asset) tabanlı
    [System.Serializable]
    public class SiloInventoryEntry
    {
        public ResourceData resource; 
        public int amount;
    }

    [Header("Veri")]
    [SerializeField] private SiloData siloData;

    [Header("Hedefler")]
    [SerializeField] private List<SiloTargetData> targets;

    [Header("Konum")]
    [SerializeField] private Transform spawnPoint;

    [Header("Silo Envanteri")]
    [SerializeField] private List<SiloInventoryEntry> inventoryDisplay = new List<SiloInventoryEntry>();
    
    // Sözlük anahtarı ResourceData (Asset) oldu
    private Dictionary<ResourceData, int> siloInventory = new Dictionary<ResourceData, int>();

    [Header("İzleme")]
    [SerializeField] private int currentActiveWorkers = 0;
    [SerializeField] private int resourcesWaitingToBeCollected = 0;

    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();
    private Dictionary<FriendlyNpcAI, SiloTargetData> workerAssignments = new Dictionary<FriendlyNpcAI, SiloTargetData>();

    private void Start()
    {
        if (siloData == null) return;
        StartCoroutine(SmartMonitorRoutine());
    }

    private IEnumerator SmartMonitorRoutine()
    {
        while (true)
        {
            CalculateAvailableResources();
            ManageWorkforce();
            yield return new WaitForSeconds(2.0f); 
        }
    }
    
    private void CalculateAvailableResources()
    {
        resourcesWaitingToBeCollected = 0;
        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t.house != null)
            {
                resourcesWaitingToBeCollected += t.house.GetResourceCount();
            }
        }
    }
    
    private void ManageWorkforce()
    {
        int cap = siloData.npcDataToSpawn.maxCarryCapacity;
        int needed = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / cap);
        needed = Mathf.Clamp(needed, 0, siloData.populationCount);
        int toSpawn = needed - activeWorkers.Count;
        if (toSpawn > 0) StartCoroutine(SpawnBatch(toSpawn));
    }
    
    private IEnumerator SpawnBatch(int count)
    {
        string tag = siloData.genericNpcPrefab.name;
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        for (int i = 0; i < count; i++)
        {
            FriendlyNpcAI npc = NpcPooler.Instance.SpawnFromPool(tag, pos, Quaternion.identity);
            if (npc != null)
            {
                activeWorkers.Add(npc);
                currentActiveWorkers = activeWorkers.Count;
                npc.OnArrivedAtWork += HandleWorkerArrivedAtTarget;
                npc.OnArrivedAtHome += HandleWorkerReturnedHome;
                SendWorkerToBestTarget(npc);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void SendWorkerToBestTarget(FriendlyNpcAI npc)
    {
        SiloTargetData best = targets
            .Where(t => t.house != null && t.house.GetResourceCount() > 0)
            .OrderByDescending(t => t.house.GetResourceCount())
            .FirstOrDefault();

        Transform dest;
        Transform home = (spawnPoint != null) ? spawnPoint : transform;
        NpcPath path = null;

        if (best != null)
        {
            dest = best.house.GetSpawnPoint();
            path = best.path;
            if (!workerAssignments.ContainsKey(npc)) workerAssignments.Add(npc, best);
            else workerAssignments[npc] = best;
        }
        else
        {
            if (workerAssignments.ContainsKey(npc)) workerAssignments.Remove(npc);
            RetireWorker(npc);
            return;
        }

        npc.Activate(siloData.npcDataToSpawn, home, dest, path);
    }

    // --- DÜZELTME BURADA ---
    private void HandleWorkerArrivedAtTarget(FriendlyNpcAI npc)
    {
        SiloTargetData data = workerAssignments.ContainsKey(npc) ? workerAssignments[npc] : null;
        int collected = 0;
        
        // ResourceType yerine ResourceData kullanıyoruz
        ResourceData resource = null;

        if (data != null && data.house != null)
        {
            int cap = npc.GetNpcData().maxCarryCapacity;
            collected = data.house.DecreaseCounter(cap);
            
            if (collected > 0)
            {
                // Evden üretilen kaynağın verisini (Asset) al
                resource = data.house.GetProducedResource();
            }
        }
        
        // NPC'ye dönüş emrini, toplanan miktar ve kaynak verisiyle ver
        npc.ReturnHome(collected, resource);
    }
    // -----------------------

    private void HandleWorkerReturnedHome(FriendlyNpcAI npc, int amount, ResourceData resource)
    {
        if (amount > 0 && resource != null)
        {
            // Sözlüğe ekle
            if (siloInventory.ContainsKey(resource)) siloInventory[resource] += amount;
            else siloInventory.Add(resource, amount);

            // İstatistikleri güncelle
            if (workerAssignments.ContainsKey(npc))
            {
                workerAssignments[npc].collectedAmount += amount;
            }
            
            UpdateInventoryDisplay();
        }

        // İşçi yönetimi
        CalculateAvailableResources();
        int cap = siloData.npcDataToSpawn.maxCarryCapacity;
        int needed = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / cap);
        needed = Mathf.Clamp(needed, 0, siloData.populationCount);

        if (activeWorkers.Count > needed || resourcesWaitingToBeCollected == 0)
        {
            RetireWorker(npc);
        }
        else
        {
            StartCoroutine(RestAndRestart(npc));
        }
    }
    
    private void UpdateInventoryDisplay()
    {
        inventoryDisplay.Clear();
        foreach (var kvp in siloInventory)
        {
            inventoryDisplay.Add(new SiloInventoryEntry { resource = kvp.Key, amount = kvp.Value });
        }
    }

    private void RetireWorker(FriendlyNpcAI npc)
    {
        npc.OnArrivedAtWork -= HandleWorkerArrivedAtTarget;
        npc.OnArrivedAtHome -= HandleWorkerReturnedHome;
        activeWorkers.Remove(npc);
        currentActiveWorkers = activeWorkers.Count;
        if (workerAssignments.ContainsKey(npc)) workerAssignments.Remove(npc);
        
        string tag = siloData.genericNpcPrefab.name;
        NpcPooler.Instance.ReturnToPool(tag, npc);
    }

    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(siloData.restDuration);
        if (npc.gameObject.activeInHierarchy) SendWorkerToBestTarget(npc);
    }
    
    public SiloData GetSiloData() { return siloData; }
    
    // --- Market Desteği İçin ---
    public int TakeResource(ResourceData resource, int amountToTake)
    {
        if (resource == null || !siloInventory.ContainsKey(resource))
        {
            return 0; 
        }

        int currentAmount = siloInventory[resource];
        int actualAmountGiven = Mathf.Min(currentAmount, amountToTake);
        
        siloInventory[resource] -= actualAmountGiven;
        
        UpdateInventoryDisplay();
        return actualAmountGiven;
    }

    public Transform GetSpawnPoint()
    {
        return (spawnPoint != null) ? spawnPoint : transform;
    }
}