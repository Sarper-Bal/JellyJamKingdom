/*
 * NPC EVİ - v4.0 (Kaynak Tipi Entegrasyonu)
 * DEĞİŞİKLİKLER:
 * - 'GetResourceType()' metodu eklendi.
 * - 'HandleNpcArrivedAtWork': Kaynak tipini belirleyip NPC'ye veriyor.
 * - 'HandleNpcArrivedAtHome': Gelen kaynağın tipini logluyor (veya işliyor).
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NpcHousing : MonoBehaviour
{
    [Header("Veri")]
    [SerializeField] private NpcHousingData housingData;
    
    [Header("Davranış")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    [SerializeField] private WorkSpotInteractable resourceTarget;
    [SerializeField] public NpcHousing houseTarget; 
    
    [Header("Konum")]
    [SerializeField] private Transform spawnPoint; 
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("İstatistik")]
    [SerializeField] private int tasksCompletedCounter = 0; 
    
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
        // ... (Spawn mantığı aynı, kısaltıldı)
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Transform home = (spawnPoint != null) ? spawnPoint : transform;
        string tag = housingData.genericNpcPrefab.name;

        for (int i = 0; i < housingData.populationCount; i++)
        {
            FriendlyNpcAI ai = NpcPooler.Instance.SpawnFromPool(tag, pos, Quaternion.identity);
            if (ai != null)
            {
                OnNpcReadyToWork?.Invoke(ai, this);
                Transform work = DetermineWorkTarget();
                ai.Activate(housingData.npcDataToSpawn, home, work, optionalNpcPath); 
                
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
            return (resourceTarget.interactionPoint != null) ? resourceTarget.interactionPoint : resourceTarget.transform;
        else if (jobType == NpcJobType.TransferResource && houseTarget != null)
            return houseTarget.GetSpawnPoint();
        return transform; 
    }
    
    // --- DEĞİŞİKLİK: Kaynak Tipi İşleme ---
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null) { npc.ReturnHome(0, ResourceType.None); return; }
        int capacity = data.maxCarryCapacity; 

        if (jobType == NpcJobType.GatherResource)
        {
            // Toplama işi: Kendi ürettiğimiz kaynağı topluyoruz
            StartCoroutine(WorkCycle(npc, capacity, housingData.producedResourceType));
        }
        else if (jobType == NpcJobType.TransferResource)
        {
            // Transfer işi: Hedef evden ne varsa onu alıyoruz
            int collected = 0;
            ResourceType type = ResourceType.None;

            if (houseTarget != null)
            {
                collected = houseTarget.DecreaseCounter(capacity);
                if (collected > 0)
                {
                    // Hedef evden kaynak tipini öğren
                    type = houseTarget.GetResourceType();
                }
            }
            // NPC'ye miktarı ve tipi ver
            npc.ReturnHome(collected, type); 
        }
    }
    
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, int amount, ResourceType type)
    {
        if (amount > 0)
        {
            tasksCompletedCounter += amount;
            Debug.Log($"Ev ({name}): {amount} adet {type} geldi. Toplam: {tasksCompletedCounter}");
        }
        StartCoroutine(RestCycle(npc, housingData.restDuration));
    }
    
    private IEnumerator WorkCycle(FriendlyNpcAI npc, int capacity, ResourceType type)
    {
        if (resourceTarget != null) resourceTarget.TriggerInteraction();
        yield return new WaitForSeconds(resourceTarget.workDuration);
        if(npc != null) 
        {
            npc.ReturnHome(capacity, type);
        }
    }
    
    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration)
    {
        yield return new WaitForSeconds(duration);
        if(npc != null)
        {
            OnNpcReadyToWork?.Invoke(npc, this);
            Transform newWork = DetermineWorkTarget();
            npc.Activate(housingData.npcDataToSpawn, (spawnPoint != null ? spawnPoint : transform), newWork, optionalNpcPath);
        }
    }
    
    public NpcHousingData GetHousingData() { return housingData; }
    public int GetResourceCount() { return tasksCompletedCounter; }
    
    // --- YENİ METOT ---
    public ResourceType GetResourceType() 
    { 
        return housingData != null ? housingData.producedResourceType : ResourceType.None; 
    }
    // ------------------

    public Transform GetSpawnPoint() { return (spawnPoint != null) ? spawnPoint : transform; }

    public void IncreaseCounter(int amount) { tasksCompletedCounter += amount; }

    public int DecreaseCounter(int amountToTake)
    {
        if (tasksCompletedCounter == 0) { return 0; }
        int actual = Mathf.Min(tasksCompletedCounter, amountToTake);
        tasksCompletedCounter -= actual;
        return actual;
    }
}