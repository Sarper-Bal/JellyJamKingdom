/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v3.5 (Hata Düzeltmesi)
 *
 * * DÜZELTME:
 * - 'GetResourceCount()' metodu eklendi. (CS1061 Hatası Çözümü)
 * - Bu metot sayesinde 'SiloController' bu evde kaç kaynak olduğunu okuyabilir.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NpcHousing : MonoBehaviour
{
    // --- Veri ve Referans Alanları ---
    [Header("Veri Kaynağı (ZORUNLU)")]
    [SerializeField] private NpcHousingData housingData;
    
    [Header("Davranış (Prefab Üzerinde)")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    
    [Header("Sahne Hedefleri (Prefab Üzerinde)")]
    [SerializeField] private WorkSpotInteractable resourceTarget;
    
    // Silo'nun hedefi değiştirebilmesi için public
    [Tooltip("EĞER JobType = TransferResource ise, NPC'lerin gideceği hedef 'Ev'.")]
    [SerializeField] public NpcHousing houseTarget; 
    
    [SerializeField] private Transform spawnPoint; 
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("Runtime İstatistikleri")]
    [SerializeField]
    private int tasksCompletedCounter = 0; 
    
    public enum NpcJobType { GatherResource, TransferResource }
    
    // Silo için Event
    public event System.Action<FriendlyNpcAI, NpcHousing> OnNpcReadyToWork;

    private List<FriendlyNpcAI> managedNpcs = new List<FriendlyNpcAI>();

    private void Start()
    {
        if (housingData == null) return;
        // Diğer null kontrolleri...
        
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
                // 1. Silo'ya (varsa) haber ver
                OnNpcReadyToWork?.Invoke(ai, this);
                
                // 2. Hedefleri belirle
                Transform workTarget = DetermineWorkTarget();
                
                // 3. Başlat
                ai.Activate(housingData.npcDataToSpawn, homeTarget, workTarget, optionalNpcPath); 
                
                // 4. Abone ol
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
            return (houseTarget.spawnPoint != null) 
                ? houseTarget.spawnPoint 
                : houseTarget.transform;
        }
        return transform; 
    }
    
    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if(npc != null)
        {
            // Dinlenme bitti, Silo'ya haber ver ve tekrar yola koyul
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
    
    // --- PUBLIC METOTLAR (Silo ve NpcPooler için) ---
    
    public NpcHousingData GetHousingData() { return housingData; }

    // --- EKSİK OLAN METOT BUYDU ---
    public int GetResourceCount() 
    { 
        return tasksCompletedCounter; 
    }
    // ------------------------------

    public void IncreaseCounter(int amount) 
    { 
        tasksCompletedCounter += amount; 
    }

    public int DecreaseCounter(int amountToTake)
    {
        if (tasksCompletedCounter == 0) { return 0; }
        int actualAmountTaken = Mathf.Min(tasksCompletedCounter, amountToTake);
        tasksCompletedCounter -= actualAmountTaken;
        return actualAmountTaken;
    }
}