/*
 * SILO KONTROLCÜSÜ (Silo Controller) - v2.1 (Kapı Önü Hedefleme)
 *
 * * DEĞİŞİKLİKLER (v2.1):
 * - 'SendWorkerToBestTarget' metodu güncellendi.
 * - Hedef belirlerken artık 'bestTarget.transform' yerine
 * 'bestTarget.GetSpawnPoint()' kullanılıyor.
 * - Bu sayede Silo NPC'leri hedef evin içine girmez, kapısında durur.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SiloController : MonoBehaviour
{
    [Header("Veri Kaynağı")]
    [SerializeField] private NpcHousingData housingData;

    [Header("Hedefler")]
    [SerializeField] private List<NpcHousing> targetHouses;

    [Header("Konumlandırma")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private NpcPath optionalPath;

    [Header("Akıllı Sistem Ayarları")]
    [SerializeField] private float scanInterval = 2.0f;

    [Header("Silo Envanteri (İzleme)")]
    [SerializeField] private int totalStoredResources = 0;
    [SerializeField] private int currentActiveWorkers = 0;
    [SerializeField] private int resourcesWaitingToBeCollected = 0;

    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();

    private void Start()
    {
        if (housingData == null)
        {
            Debug.LogError($"Silo ({gameObject.name}): 'Housing Data' atanmamış!", this);
            return;
        }
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
        foreach (var house in targetHouses)
        {
            if (house != null)
            {
                resourcesWaitingToBeCollected += house.GetResourceCount();
            }
        }
    }

    private void ManageWorkforce()
    {
        int workerCapacity = housingData.npcDataToSpawn.maxCarryCapacity;
        int neededWorkers = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / workerCapacity);
        neededWorkers = Mathf.Clamp(neededWorkers, 0, housingData.populationCount);

        int workersToSpawn = neededWorkers - activeWorkers.Count;

        if (workersToSpawn > 0)
        {
            StartCoroutine(SpawnBatch(workersToSpawn));
        }
    }

    private IEnumerator SpawnBatch(int count)
    {
        string poolTag = housingData.genericNpcPrefab.name;
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
        NpcHousing bestTarget = targetHouses
            .Where(h => h != null && h.GetResourceCount() > 0)
            .OrderByDescending(h => h.GetResourceCount())
            .FirstOrDefault();

        Transform targetTransform;
        Transform myHome = (spawnPoint != null) ? spawnPoint : transform;

        if (bestTarget != null)
        {
            // --- DEĞİŞİKLİK (v2.1) ---
            // Evin merkezine değil, spawn noktasına (kapı önüne) git
            targetTransform = bestTarget.GetSpawnPoint(); // <-- DÜZELTİLDİ
            // ---
        }
        else
        {
            RetireWorker(npc);
            return;
        }

        npc.Activate(housingData.npcDataToSpawn, myHome, targetTransform, optionalPath);
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

        // İş bittiğinde tekrar durum değerlendirmesi yap
        CalculateAvailableResources();
        int workerCapacity = housingData.npcDataToSpawn.maxCarryCapacity;
        int neededWorkers = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / workerCapacity);
        neededWorkers = Mathf.Clamp(neededWorkers, 0, housingData.populationCount);

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

        string poolTag = housingData.genericNpcPrefab.name;
        NpcPooler.Instance.ReturnToPool(poolTag, npc);
    }

    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(housingData.restDuration);
        
        if (npc.gameObject.activeInHierarchy) 
        {
            SendWorkerToBestTarget(npc);
        }
    }

    private NpcHousing GetClosestHouse(Vector3 position)
    {
        NpcHousing closest = null;
        float minDst = Mathf.Infinity;
        foreach (var house in targetHouses)
        {
            if (house == null) continue;
            
            // Mesafeyi evin merkezine değil, spawn noktasına göre ölçmek daha hassas olabilir
            // ama şimdilik transform yeterli.
            float dst = Vector3.Distance(position, house.transform.position);
            if (dst < minDst && dst < 5.0f) 
            {
                minDst = dst;
                closest = house;
            }
        }
        return closest;
    }
    
    public NpcHousingData GetHousingData() { return housingData; }
}