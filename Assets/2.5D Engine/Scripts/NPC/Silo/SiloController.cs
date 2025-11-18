/*
 * SILO KONTROLCÜSÜ (Silo Controller) - v2.4 (Detaylı İstatistikler)
 * * * DEĞİŞİKLİKLER (v2.4):
 * - 'SiloTargetData' sınıfına 'collectedAmount' eklendi. Artık her evden
 * ne kadar kaynak toplandığı ayrı ayrı tutuluyor.
 * - 'workerAssignments' (Dictionary) eklendi. Hangi işçinin hangi evde
 * çalıştığını takip eder.
 * - 'SendWorkerToBestTarget' ve 'HandleWorkerReturnedHome' metotları,
 * bu takip sistemini güncelleyecek şekilde revize edildi.
 * - 'targets' listesindeki sayaçları Inspector'dan canlı izleyebilirsiniz.
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
        [Tooltip("Kaynak toplanacak hedef ev.")]
        public NpcHousing house;
        
        [Tooltip("Silo'dan bu eve giderken kullanılacak özel yol.")]
        public NpcPath path;

        // --- DEĞİŞİKLİK BAŞLANGICI (v2.4) ---
        [Tooltip("Bu evden şu ana kadar toplanan toplam kaynak miktarı.")]
        public int collectedAmount = 0; // İstatistik Sayacı
        // --- DEĞİŞİKLİK SONU ---
    }

    [Header("Veri Kaynağı")]
    [SerializeField] private SiloData siloData;

    [Header("Hedefler ve İstatistikler")]
    [Tooltip("Hedef evler ve her birinden toplanan kaynaklar.")]
    [SerializeField] private List<SiloTargetData> targets;

    [Header("Konumlandırma")]
    [SerializeField] private Transform spawnPoint;

    [Header("Akıllı Sistem Ayarları")]
    [SerializeField] private float scanInterval = 2.0f;

    [Header("Silo Genel Envanteri")]
    [SerializeField] private int totalStoredResources = 0;
    [SerializeField] private int currentActiveWorkers = 0;
    [SerializeField] private int resourcesWaitingToBeCollected = 0;

    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();

    // --- DEĞİŞİKLİK BAŞLANGICI (v2.4) ---
    // Hangi işçinin hangi hedef (TargetData) üzerinde çalıştığını tutan "Hafıza"
    private Dictionary<FriendlyNpcAI, SiloTargetData> workerAssignments = new Dictionary<FriendlyNpcAI, SiloTargetData>();
    // --- DEĞİŞİKLİK SONU ---

    private void Start()
    {
        if (siloData == null)
        {
            Debug.LogError($"Silo ({gameObject.name}): 'Silo Data' atanmamış!", this);
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
        int workerCapacity = siloData.npcDataToSpawn.maxCarryCapacity;
        int neededWorkers = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / workerCapacity);
        neededWorkers = Mathf.Clamp(neededWorkers, 0, siloData.populationCount);

        int workersToSpawn = neededWorkers - activeWorkers.Count;

        if (workersToSpawn > 0)
        {
            StartCoroutine(SpawnBatch(workersToSpawn));
        }
    }

    private IEnumerator SpawnBatch(int count)
    {
        string poolTag = siloData.genericNpcPrefab.name;
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
        // En zengin hedefi bul
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
            
            // --- DEĞİŞİKLİK BAŞLANGICI (v2.4) ---
            // İşçiyi ve hedefini deftere kaydet
            if (workerAssignments.ContainsKey(npc))
            {
                workerAssignments[npc] = bestTargetData;
            }
            else
            {
                workerAssignments.Add(npc, bestTargetData);
            }
            // --- DEĞİŞİKLİK SONU ---
        }
        else
        {
            // Kaynak yoksa kaydı sil ve emekli et
            if (workerAssignments.ContainsKey(npc)) workerAssignments.Remove(npc);
            RetireWorker(npc);
            return;
        }

        npc.Activate(siloData.npcDataToSpawn, myHome, targetTransform, pathForThisTarget);
    }

    private void HandleWorkerArrivedAtTarget(FriendlyNpcAI npc)
    {
        // Not: Artık "En Yakın Evi" aramamıza gerek yok, 
        // 'workerAssignments' sözlüğünden nereye gönderdiğimizi biliyoruz.
        // Ancak güvenlik için GetClosestHouse'u tutabiliriz veya direkt sözlükten bakabiliriz.
        // Tutarlılık için sözlükten hedef verisini alıp, ev referansını kullanalım.

        SiloTargetData assignedData = null;
        if (workerAssignments.ContainsKey(npc))
        {
            assignedData = workerAssignments[npc];
        }

        int collected = 0;

        if (assignedData != null && assignedData.house != null)
        {
            int capacity = npc.GetNpcData().maxCarryCapacity;
            collected = assignedData.house.DecreaseCounter(capacity);
        }
        else
        {
            // Yedek plan: Sözlükte yoksa en yakını bul (eski yöntem)
            NpcHousing closest = GetClosestHouse(npc.transform.position);
            if (closest != null)
            {
                int capacity = npc.GetNpcData().maxCarryCapacity;
                collected = closest.DecreaseCounter(capacity);
            }
        }

        npc.ReturnHome(collected);
    }

    private void HandleWorkerReturnedHome(FriendlyNpcAI npc, int amount)
    {
        if (amount > 0)
        {
            // 1. Genel Depoya Ekle
            totalStoredResources += amount;

            // --- DEĞİŞİKLİK BAŞLANGICI (v2.4) ---
            // 2. Özel (Ev Bazlı) İstatistiğe Ekle
            if (workerAssignments.ContainsKey(npc))
            {
                SiloTargetData sourceData = workerAssignments[npc];
                if (sourceData != null)
                {
                    sourceData.collectedAmount += amount;
                }
            }
            // --- DEĞİŞİKLİK SONU ---
        }

        // Yeniden Değerlendirme
        CalculateAvailableResources();
        int workerCapacity = siloData.npcDataToSpawn.maxCarryCapacity;
        int neededWorkers = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / workerCapacity);
        neededWorkers = Mathf.Clamp(neededWorkers, 0, siloData.populationCount);

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
        
        // --- DEĞİŞİKLİK (v2.4) ---
        // Sözlükten kaydı sil
        if (workerAssignments.ContainsKey(npc)) workerAssignments.Remove(npc);
        // ---

        string poolTag = siloData.genericNpcPrefab.name;
        NpcPooler.Instance.ReturnToPool(poolTag, npc);
    }

    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(siloData.restDuration);
        
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
    
    public SiloData GetSiloData() { return siloData; }
}