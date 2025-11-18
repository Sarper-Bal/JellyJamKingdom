/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v3.6 (Spawn Point Erişimi)
 *
 * * DEĞİŞİKLİKLER (v3.6):
 * - YENİ METOT: 'GetSpawnPoint()'.
 * - Bu public metot, Silo gibi dış sistemlerin evin "kapı önü" (spawnPoint)
 * - noktasına erişmesini sağlar. Eğer spawnPoint atanmamışsa, evin merkezini döner.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NpcHousing : MonoBehaviour
{
    [Header("Veri Kaynağı (ZORUNLU)")]
    [SerializeField] private NpcHousingData housingData;
    
    [Header("Davranış (Prefab Üzerinde)")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    
    [Header("Sahne Hedefleri (Prefab Üzerinde)")]
    [SerializeField] private WorkSpotInteractable resourceTarget;
    
    [Tooltip("EĞER JobType = TransferResource ise, NPC'lerin gideceği hedef 'Ev'.")]
    [SerializeField] public NpcHousing houseTarget; 
    
    [SerializeField] private Transform spawnPoint; 
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("Runtime İstatistikleri")]
    [SerializeField]
    private int tasksCompletedCounter = 0; 
    
    public enum NpcJobType { GatherResource, TransferResource }
    
    public event System.Action<FriendlyNpcAI, NpcHousing> OnNpcReadyToWork;

    private List<FriendlyNpcAI> managedNpcs = new List<FriendlyNpcAI>();

    private void Start()
    {
        if (housingData == null) return;
        StartCoroutine(SpawnNpcs());
    }

    private IEnumerator SpawnNpcs()
    {
        Vector3 positionToSpawn = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Transform homeTarget = (spawnPoint != null) ? spawnPoint : this.transform;
        string poolTag = housingData.genericNpcPrefab.name;

        for (int i = 0; i < housingData.populationCount; i++)
        {
            FriendlyNpcAI ai = NpcPooler.Instance.SpawnFromPool(
                poolTag, 
                positionToSpawn, 
                Quaternion.identity
            );

            if (ai != null)
            {
                OnNpcReadyToWork?.Invoke(ai, this);
                Transform workTarget = DetermineWorkTarget();
                ai.Activate(housingData.npcDataToSpawn, homeTarget, workTarget, optionalNpcPath); 
                
                ai.OnArrivedAtWork -= HandleNpcArrivedAtWork; 
                ai.OnArrivedAtHome -= HandleNpcArrivedAtHome;
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
                
                managedNpcs.Add(ai);
            }
            yield return new WaitForSeconds(housingData.spawnInterval);
        }
    }
    
    private Transform DetermineWorkTarget()
    {
        if (jobType == NpcJobType.GatherResource && resourceTarget != null)
        {
            return (resourceTarget.interactionPoint != null) 
                ? resourceTarget.interactionPoint 
                : resourceTarget.transform;
        }
        else if (jobType == NpcJobType.TransferResource && houseTarget != null)
        {
            // Kendi sınıfımız olduğu için private alana erişebiliyoruz ama
            // dışarıdan erişim için GetSpawnPoint kullanmak daha güvenlidir.
            return houseTarget.GetSpawnPoint();
        }
        return transform; 
    }
    
    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if(npc != null)
        {
            OnNpcReadyToWork?.Invoke(npc, this);
            Transform newWorkTarget = DetermineWorkTarget();
            
            npc.Activate(housingData.npcDataToSpawn, (spawnPoint != null ? spawnPoint : transform), newWorkTarget, optionalNpcPath);
        }
    }
    
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null) { npc.ReturnHome(0); return; }
        int capacity = data.maxCarryCapacity; 

        if (jobType == NpcJobType.GatherResource)
        {
            StartCoroutine(WorkCycle(npc, capacity));
        }
        else if (jobType == NpcJobType.TransferResource)
        {
            int collectedAmount = 0;
            if (houseTarget != null)
            {
                collectedAmount = houseTarget.DecreaseCounter(capacity);
            }
            if(npc != null) npc.ReturnHome(collectedAmount); 
        }
    }
    
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, int collectedAmount)
    {
        if (collectedAmount > 0)
        {
            tasksCompletedCounter += collectedAmount;
            Debug.Log($"Ev ({gameObject.name}): {collectedAmount} kaynak geldi. Toplam: {tasksCompletedCounter}.");
        }
        StartCoroutine(RestCycle(npc, housingData.restDuration));
    }
    
    private IEnumerator WorkCycle(FriendlyNpcAI npc, int capacity)
    {
        if (resourceTarget != null) resourceTarget.TriggerInteraction();
        yield return new WaitForSeconds(resourceTarget.workDuration);
        if(npc != null) npc.ReturnHome(capacity);
    }
    
    // --- PUBLIC METOTLAR ---
    
    public NpcHousingData GetHousingData() { return housingData; }
    public int GetResourceCount() { return tasksCompletedCounter; }

    // --- DEĞİŞİKLİK BAŞLANGICI (v3.6 - Yeni Erişim Metodu) ---
    /// <summary>
    /// Silo veya diğer sistemlerin bu evin "kapı önü" noktasına erişmesi için.
    /// </summary>
    public Transform GetSpawnPoint()
    {
        // Eğer spawnPoint atanmışsa onu dön, yoksa evin merkezini dön
        return (spawnPoint != null) ? spawnPoint : transform;
    }
    // --- DEĞİŞİKLİK SONU ---

    public void IncreaseCounter(int amount) { tasksCompletedCounter += amount; }

    public int DecreaseCounter(int amountToTake)
    {
        if (tasksCompletedCounter == 0) { return 0; }
        int actualAmountTaken = Mathf.Min(tasksCompletedCounter, amountToTake);
        tasksCompletedCounter -= actualAmountTaken;
        return actualAmountTaken;
    }
}