/*
 * NPC HOUSING - v3.0 (Bağımsız / Decentralized)
 * DEĞİŞİKLİKLER:
 * - NpcPooler (Singleton) bağımlılığı tamamen kaldırıldı.
 * - Her bina kendi işçisini (Prefab) kendi içinde üretir ve yönetir (Local Pooling).
 * - İşçiler hiyerarşide binanın altında (Child) durur, bina silinirse işçiler de silinir.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NpcHousing : MonoBehaviour
{
    [Header("Veri")]
    [SerializeField] private NpcHousingData housingData;
    
    [Header("Ayarlar")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    [SerializeField] private WorkSpotInteractable resourceTarget;
    [SerializeField] public NpcHousing houseTarget; // Transfer modu için hedef ev
    [SerializeField] private Transform spawnPoint; 
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("Durum")]
    [SerializeField] private int outputProductCount = 0; 
    [SerializeField] private int inputRawMaterialCount = 0;
    
    public enum NpcJobType { GatherResource, TransferResource }
    public event System.Action<FriendlyNpcAI, NpcHousing> OnNpcReadyToWork;
    
    // --- YEREL HAVUZ (LOCAL POOL) ---
    // Bu bina kendi işçilerini bu listede tutar.
    private List<FriendlyNpcAI> myWorkers = new List<FriendlyNpcAI>();
    
    private bool isRunning = false;
    private bool isProducing = false;

    private void Start()
    {
        if (housingData == null)
        {
            Debug.LogError($"NpcHousing: '{name}' objesinde Data eksik!");
            return;
        }
        
        // Başlangıçta kendi işgücünü kur
        InitializeWorkforce();
        
        StartHousing();
    }

    /// <summary>
    /// Binanın ihtiyaç duyduğu işçileri (Prefab'den) oluşturur ve saklar.
    /// </summary>
    private void InitializeWorkforce()
    {
        // Eğer Data'da prefab yoksa işlem yapma
        if (housingData.genericNpcPrefab == null) return;

        // İstenen sayı kadar üret
        for (int i = 0; i < housingData.populationCount; i++)
        {
            CreateWorker();
        }
    }

    private FriendlyNpcAI CreateWorker()
    {
        // İşçiyi bu binanın "Çocuğu" olarak üret (transform).
        // Böylece bina silinirse işçiler de otomatik silinir.
        GameObject workerObj = Instantiate(housingData.genericNpcPrefab, GetSpawnPoint().position, Quaternion.identity, transform);
        
        FriendlyNpcAI ai = workerObj.GetComponent<FriendlyNpcAI>();
        if (ai != null)
        {
            // Başlangıçta pasif olsun, görev gelince açılır.
            workerObj.SetActive(false);
            myWorkers.Add(ai);
            
            // Event dinleyicilerini ayarla
            ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
            ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
        }
        return ai;
    }

    public void StartHousing()
    {
        if (isRunning) return;
        isRunning = true;
        
        // İşçileri sahaya sür
        StartCoroutine(DeployWorkersRoutine());
        
        if (housingData.requiresConversion) 
            StartCoroutine(ProductionRoutine());
    }

    public void StopHousing()
    {
        isRunning = false;
        StopAllCoroutines();
        
        // Tüm işçileri eve çağır (Pasif yap)
        foreach (var worker in myWorkers)
        {
            if (worker != null) worker.gameObject.SetActive(false);
        }
    }

    private IEnumerator DeployWorkersRoutine()
    {
        foreach (var worker in myWorkers)
        {
            if (!isRunning) yield break;

            // İşçiyi aktifleştir ve göreve gönder
            if (!worker.gameObject.activeInHierarchy)
            {
                worker.transform.position = GetSpawnPoint().position;
                worker.gameObject.SetActive(true);
                
                // Reset (Varsa IPooledNpc arayüzü ile)
                if (worker is IPooledNpc p) p.OnNpcSpawned();

                // İlk görevi ata
                SendWorkerToTask(worker);
            }
            
            // Hepsini aynı anda çıkarmamak için bekle
            yield return new WaitForSeconds(housingData.spawnInterval);
        }
    }

    private void SendWorkerToTask(FriendlyNpcAI ai)
    {
        OnNpcReadyToWork?.Invoke(ai, this);
        Transform workTarget = DetermineWorkTarget();
        
        // Data'daki NpcData ayarlarını kullanarak işçiyi başlat
        ai.Activate(housingData.npcDataToSpawn, GetSpawnPoint(), workTarget, optionalNpcPath); 
    }

    #region Core Logic (İş Mantığı)
    
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

    private Transform DetermineWorkTarget() {
        if (jobType == NpcJobType.GatherResource && resourceTarget != null) 
            return (resourceTarget.interactionPoint != null) ? resourceTarget.interactionPoint : resourceTarget.transform;
        else if (jobType == NpcJobType.TransferResource && houseTarget != null) 
            return houseTarget.GetSpawnPoint();
        return transform; 
    }

    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc) {
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null) { npc.ReturnHome(0, null); return; }
        
        int capacity = data.maxCarryCapacity; 
        
        if (jobType == NpcJobType.GatherResource) 
        {
            StartCoroutine(WorkCycle(npc, capacity, null)); 
        }
        else if (jobType == NpcJobType.TransferResource) 
        {
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
        // İşçi evde dinleniyor (Görünür kalabilir veya gizlenebilir, tasarım tercihi)
        // Şimdilik evin önünde bekliyor.
        yield return new WaitForSeconds(duration);
        
        if(npc != null && isRunning) { 
            SendWorkerToTask(npc); // Tekrar işe dön
        }
    }

    // --- YARDIMCI METOTLAR ---
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