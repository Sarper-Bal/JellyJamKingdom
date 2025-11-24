using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NpcHousing : MonoBehaviour
{
    [Header("Veri")]
    [SerializeField] private NpcHousingData housingData;
    
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    [SerializeField] private WorkSpotInteractable resourceTarget;
    [SerializeField] public NpcHousing houseTarget; 
    [SerializeField] private Transform spawnPoint; 
    [SerializeField] private NpcPath optionalNpcPath; 
    [SerializeField] private int outputProductCount = 0; 
    [SerializeField] private int inputRawMaterialCount = 0;
    
    private bool isProducing = false;
    public enum NpcJobType { GatherResource, TransferResource }
    public event System.Action<FriendlyNpcAI, NpcHousing> OnNpcReadyToWork;
    private List<FriendlyNpcAI> managedNpcs = new List<FriendlyNpcAI>();
    
    private bool isRunning = false;

 // Start yerine OnEnable kullanırsan, Inspector'da her açıp kapattığında çalışır.
    private void Start()
    {
        if (housingData == null) return;
        
        // --- BURASI ÖNEMLİ: Pool Kontrolü ---
        // Bina oyuna dahil olduğunda (veya aktifleştiğinde) havuza haber ver
        if (NpcPooler.Instance != null)
        {
            NpcPooler.Instance.RecalculateAndExpandPools();
        }
        // ------------------------------------

        StartHousing();
    }
    // OnDestroy'da abonelikten çıkma işlemine gerek kalmadı.

    public void StartHousing()
    {
        if (isRunning) return;
        isRunning = true;
        StartCoroutine(SpawnNpcs());
        if (housingData.requiresConversion) StartCoroutine(ProductionRoutine());
    }

    public void StopHousing()
    {
        isRunning = false;
        StopAllCoroutines();
    }

    #region Core Logic (Unchanged)
    private IEnumerator ProductionRoutine() {
        while (isRunning) { 
            if (inputRawMaterialCount >= housingData.conversionRate) {
                isProducing = true;
                yield return new WaitForSeconds(housingData.conversionTime);
                if (inputRawMaterialCount >= housingData.conversionRate) {
                    inputRawMaterialCount -= housingData.conversionRate;
                    outputProductCount++; 
                }
            } else {
                isProducing = false;
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
    private IEnumerator SpawnNpcs() {
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Transform home = (spawnPoint != null) ? spawnPoint : transform;
        string tag = housingData.genericNpcPrefab.name;
        for (int i = 0; i < housingData.populationCount; i++) {
            if(!isRunning) yield break; 
            FriendlyNpcAI ai = NpcPooler.Instance.SpawnFromPool(tag, pos, Quaternion.identity);
            if (ai != null) {
                OnNpcReadyToWork?.Invoke(ai, this);
                Transform work = DetermineWorkTarget();
                ai.Activate(housingData.npcDataToSpawn, home, work, optionalNpcPath); 
                ai.OnArrivedAtWork -= HandleNpcArrivedAtWork; ai.OnArrivedAtHome -= HandleNpcArrivedAtHome;
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork; ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
                managedNpcs.Add(ai);
            }
            yield return new WaitForSeconds(housingData.spawnInterval);
        }
    }
    
    private Transform DetermineWorkTarget() {
        if (jobType == NpcJobType.GatherResource && resourceTarget != null) return (resourceTarget.interactionPoint != null) ? resourceTarget.interactionPoint : resourceTarget.transform;
        else if (jobType == NpcJobType.TransferResource && houseTarget != null) return houseTarget.GetSpawnPoint();
        return transform; 
    }
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc) {
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null) { npc.ReturnHome(0, null); return; }
        int capacity = data.maxCarryCapacity; 
        if (jobType == NpcJobType.GatherResource) StartCoroutine(WorkCycle(npc, capacity, null)); 
        else if (jobType == NpcJobType.TransferResource) {
            int collected = 0; ResourceData resource = null;
            if (houseTarget != null) {
                collected = houseTarget.DecreaseCounter(capacity);
                if (collected > 0) resource = houseTarget.GetProducedResource();
            }
            npc.ReturnHome(collected, resource); 
        }
    }
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, int amount, ResourceData resource) {
        if (amount > 0) {
            if (housingData.requiresConversion) inputRawMaterialCount += amount;
            else outputProductCount += amount;
        }
        StartCoroutine(RestCycle(npc, housingData.restDuration));
    }
    private IEnumerator WorkCycle(FriendlyNpcAI npc, int capacity, ResourceData resource) {
        if (resourceTarget != null) resourceTarget.TriggerInteraction();
        yield return new WaitForSeconds(resourceTarget.workDuration);
        if(npc != null) npc.ReturnHome(capacity, resource);
    }
    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration) {
        yield return new WaitForSeconds(duration);
        if(npc != null && isRunning) { 
            OnNpcReadyToWork?.Invoke(npc, this);
            Transform newWork = DetermineWorkTarget();
            npc.Activate(housingData.npcDataToSpawn, (spawnPoint != null ? spawnPoint : transform), newWork, optionalNpcPath);
        }
    }
    public NpcHousingData GetHousingData() { return housingData; }
    public int GetResourceCount() { return outputProductCount; }
    public ResourceData GetProducedResource() { return housingData != null ? housingData.producedResource : null; }
    public Transform GetSpawnPoint() { return (spawnPoint != null) ? spawnPoint : transform; }
    public void IncreaseCounter(int amount) { outputProductCount += amount; }
    public int DecreaseCounter(int amountToTake) {
        if (outputProductCount == 0) return 0;
        int actual = Mathf.Min(outputProductCount, amountToTake);
        outputProductCount -= actual;
        return actual;
    }
    #endregion
}