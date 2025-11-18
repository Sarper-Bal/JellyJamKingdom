/*
 * SILO KONTROLCÜSÜ (Silo Controller) - v2.3 (SiloData Entegrasyonu)
 * * DEĞİŞİKLİKLER (v2.3):
 * - 'housingData' alanı (tipi 'NpcHousingData' idi) SİLİNDİ.
 * - YENİ ALAN: 'siloData' (tipi 'SiloData'). Artık kendi özel verisini kullanıyor.
 * - 'GetHousingData' metodu SİLİNDİ.
 * - YENİ METOT: 'GetSiloData'. NpcPooler'ın okuması için.
 * - Tüm mantık (Spawn, ManageWorkforce vb.) artık 'siloData' üzerinden çalışıyor.
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
    }

    // --- DEĞİŞİKLİK BAŞLANGICI (Veri Tipi) ---
    [Header("Veri Kaynağı")]
    [Tooltip("Silo'ya özel veri dosyası.")]
    [SerializeField] private SiloData siloData; // <-- ARTIK SiloData KULLANIYOR
    // --- DEĞİŞİKLİK SONU ---

    [Header("Hedefler")]
    [SerializeField] private List<SiloTargetData> targets;

    [Header("Konumlandırma")]
    [SerializeField] private Transform spawnPoint;

    [Header("Akıllı Sistem Ayarları")]
    [SerializeField] private float scanInterval = 2.0f;

    [Header("Silo Envanteri")]
    [SerializeField] private int totalStoredResources = 0;
    [SerializeField] private int currentActiveWorkers = 0;
    [SerializeField] private int resourcesWaitingToBeCollected = 0;

    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();

    private void Start()
    {
        // --- DEĞİŞİKLİK: 'siloData' kontrolü ---
        if (siloData == null)
        {
            Debug.LogError($"Silo ({gameObject.name}): 'Silo Data' atanmamış!", this);
            return;
        }
        // ---
        StartCoroutine(SmartMonitorRoutine());
    }

    private IEnumerator SmartMonitorRoutine()
    {
        while (true)
        {
            CalculateAvailableResources();
            ManageWorkforce();
            yield return new WaitForSeconds(scanInterval);
        }
    }

    private void CalculateAvailableResources()
    {
        resourcesWaitingToBeCollected = 0;
        if (targets == null) return;
        
        foreach (var target in targets)
        {
            if (target.house != null)
            {
                resourcesWaitingToBeCollected += target.house.GetResourceCount();
            }
        }
    }

    private void ManageWorkforce()
    {
        // --- DEĞİŞİKLİK: 'siloData'dan okuma ---
        int workerCapacity = siloData.npcDataToSpawn.maxCarryCapacity;
        int neededWorkers = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / workerCapacity);
        neededWorkers = Mathf.Clamp(neededWorkers, 0, siloData.populationCount);
        // ---

        int workersToSpawn = neededWorkers - activeWorkers.Count;

        if (workersToSpawn > 0)
        {
            StartCoroutine(SpawnBatch(workersToSpawn));
        }
    }

    private IEnumerator SpawnBatch(int count)
    {
        // --- DEĞİŞİKLİK: 'siloData'dan okuma ---
        string poolTag = siloData.genericNpcPrefab.name;
        // ---
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            FriendlyNpcAI npc = NpcPooler.Instance.SpawnFromPool(poolTag, pos, Quaternion.identity);

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
        SiloTargetData bestTargetData = targets
            .Where(t => t.house != null && t.house.GetResourceCount() > 0)
            .OrderByDescending(t => t.house.GetResourceCount())
            .FirstOrDefault();

        Transform targetTransform;
        Transform myHome = (spawnPoint != null) ? spawnPoint : transform;
        NpcPath pathForThisTarget = null;

        if (bestTargetData != null)
        {
            targetTransform = bestTargetData.house.GetSpawnPoint();
            pathForThisTarget = bestTargetData.path;
        }
        else
        {
            RetireWorker(npc);
            return;
        }

        // --- DEĞİŞİKLİK: 'siloData'dan okuma ---
        npc.Activate(siloData.npcDataToSpawn, myHome, targetTransform, pathForThisTarget);
        // ---
    }

    private void HandleWorkerArrivedAtTarget(FriendlyNpcAI npc)
    {
        NpcHousing targetHouse = GetClosestHouse(npc.transform.position);
        int collected = 0;

        if (targetHouse != null)
        {
            int capacity = npc.GetNpcData().maxCarryCapacity;
            collected = targetHouse.DecreaseCounter(capacity);
        }

        npc.ReturnHome(collected);
    }

    private void HandleWorkerReturnedHome(FriendlyNpcAI npc, int amount)
    {
        if (amount > 0)
        {
            totalStoredResources += amount;
        }

        CalculateAvailableResources();
        // --- DEĞİŞİKLİK: 'siloData'dan okuma ---
        int workerCapacity = siloData.npcDataToSpawn.maxCarryCapacity;
        int neededWorkers = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / workerCapacity);
        neededWorkers = Mathf.Clamp(neededWorkers, 0, siloData.populationCount);
        // ---

        if (activeWorkers.Count > neededWorkers || resourcesWaitingToBeCollected == 0)
        {
            RetireWorker(npc);
        }
        else
        {
            StartCoroutine(RestAndRestart(npc));
        }
    }

    private void RetireWorker(FriendlyNpcAI npc)
    {
        npc.OnArrivedAtWork -= HandleWorkerArrivedAtTarget;
        npc.OnArrivedAtHome -= HandleWorkerReturnedHome;

        activeWorkers.Remove(npc);
        currentActiveWorkers = activeWorkers.Count;

        // --- DEĞİŞİKLİK: 'siloData'dan okuma ---
        string poolTag = siloData.genericNpcPrefab.name;
        // ---
        NpcPooler.Instance.ReturnToPool(poolTag, npc);
    }

    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        // --- DEĞİŞİKLİK: 'siloData'dan okuma ---
        yield return new WaitForSeconds(siloData.restDuration);
        // ---
        
        if (npc.gameObject.activeInHierarchy) 
        {
            SendWorkerToBestTarget(npc);
        }
    }

    private NpcHousing GetClosestHouse(Vector3 position)
    {
        NpcHousing closest = null;
        float minDst = Mathf.Infinity;
        if (targets == null) return null;
        
        foreach (var targetData in targets)
        {
            if (targetData.house == null) continue;
            float dst = Vector3.Distance(position, targetData.house.transform.position);
            if (dst < minDst && dst < 5.0f) 
            {
                minDst = dst;
                closest = targetData.house;
            }
        }
        return closest;
    }
    
    // --- DEĞİŞİKLİK BAŞLANGICI (Yeni Getter) ---
    // NpcPooler'ın yeni verilere erişmesi için
    public SiloData GetSiloData() { return siloData; }
    // --- DEĞİŞİKLİK SONU ---
}