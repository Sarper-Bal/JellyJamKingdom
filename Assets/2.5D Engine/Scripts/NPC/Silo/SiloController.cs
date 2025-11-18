/*
 * SILO KONTROLCÜSÜ (Silo Controller) - v2.2 (Hedefe Özel Yollar)
 * * GÖREVİ:
 * - Silo'nun kaynak toplayacağı evleri ve bu evlere giden yolları yönetir.
 *
 * * DEĞİŞİKLİKLER (v2.2):
 * - YENİ SINIF: 'SiloTargetData'.
 * - Bu sınıf, bir 'NpcHousing' (Ev) ve ona giden 'NpcPath' (Yol) çiftini tutar.
 * - 'targetHouses' listesi, 'targets' (List<SiloTargetData>) olarak değiştirildi.
 * - 'optionalPath' (Global yol) KALDIRILDI.
 * - 'SendWorkerToBestTarget' metodu güncellendi:
 * - Artık en zengin hedefi bulurken 'targets' listesini tarıyor.
 * - NPC'yi göreve gönderirken, o hedefe özel atanmış yolu (target.path) kullanıyor.
 * - 'CalculateAvailableResources' ve 'GetClosestHouse' metotları yeni liste yapısına uyarlandı.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SiloController : MonoBehaviour
{
    // --- DEĞİŞİKLİK BAŞLANGICI (v2.2 - Veri Yapısı) ---
    [System.Serializable]
    public class SiloTargetData
    {
        [Tooltip("Kaynak toplanacak hedef ev.")]
        public NpcHousing house;
        
        [Tooltip("Silo'dan bu eve giderken kullanılacak özel yol (Opsiyonel).")]
        public NpcPath path;
    }
    // --- DEĞİŞİKLİK SONU ---

    [Header("Veri Kaynağı")]
    [SerializeField] private NpcHousingData housingData;

    [Header("Hedefler")]
    // --- DEĞİŞİKLİK BAŞLANGICI (v2.2 - Yeni Liste) ---
    [Tooltip("Silo'nun kaynak toplayacağı evler ve yolları.")]
    [SerializeField] private List<SiloTargetData> targets;
    // [SerializeField] private List<NpcHousing> targetHouses; // <-- SİLİNDİ
    // --- DEĞİŞİKLİK SONU ---

    [Header("Konumlandırma")]
    [SerializeField] private Transform spawnPoint;
    // [SerializeField] private NpcPath optionalPath; // <-- SİLİNDİ (Artık her hedefin kendi yolu var)

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
        // --- DEĞİŞİKLİK (v2.2) ---
        if (targets == null) return;
        
        foreach (var target in targets)
        {
            if (target.house != null)
            {
                resourcesWaitingToBeCollected += target.house.GetResourceCount();
            }
        }
        // ---
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
        // --- DEĞİŞİKLİK BAŞLANGICI (v2.2 - Hedef Seçimi) ---
        
        // 1. 'targets' listesinden, evi (house) dolu olan ve kaynağı olanlar arasından
        // en çok kaynağı olan 'SiloTargetData'yı bul.
        SiloTargetData bestTargetData = targets
            .Where(t => t.house != null && t.house.GetResourceCount() > 0)
            .OrderByDescending(t => t.house.GetResourceCount())
            .FirstOrDefault();

        Transform targetTransform;
        Transform myHome = (spawnPoint != null) ? spawnPoint : transform;
        
        // Bu hedefe özel yol var mı?
        NpcPath pathForThisTarget = null;

        if (bestTargetData != null)
        {
            // Hedefin kapısına git
            targetTransform = bestTargetData.house.GetSpawnPoint();
            
            // Hedefin özel yolunu al
            pathForThisTarget = bestTargetData.path;
        }
        else
        {
            // Kaynak yoksa emekli et
            RetireWorker(npc);
            return;
        }
        // --- DEĞİŞİKLİK SONU ---

        // NPC'ye o hedefe özel yolu vererek gönder
        npc.Activate(housingData.npcDataToSpawn, myHome, targetTransform, pathForThisTarget);
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
            // Debug.Log($"Silo: +{amount} kaynak. Toplam: {totalStoredResources}");
        }

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
        
        // --- DEĞİŞİKLİK (v2.2) ---
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
        // ---
        return closest;
    }
    
    public NpcHousingData GetHousingData() { return housingData; }
}