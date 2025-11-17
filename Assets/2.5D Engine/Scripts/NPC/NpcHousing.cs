/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v3.0 (Data-Driven Refactor)
 *
 * GÖREVİ:
 * Artık bir "Motor"dur. 'NpcHousingData' asset'inden aldığı veriyi
 * kullanarak, 'jobType' ile belirlenen mantığı çalıştırır.
 *
 * * DEĞİŞİKLİKLER (v3.0):
 * - 'genericNpcPrefab', 'npcDataToSpawn', 'restDuration', 
 * 'populationCount', 'spawnInterval' alanları YENİ 'NpcHousingData'
 * ScriptableObject'ine taşındı.
 * - YENİ ALAN: '[SerializeField] private NpcHousingData housingData;'
 * eklendi.
 * - KULLANICI İSTEĞİ: 'jobType' (davranış) prefab üzerinde kalmaya
 * devam ediyor.
 * - Sahne referansları ('resourceTarget', 'houseTarget', 'spawnPoint', 
 * 'optionalNpcPath') prefab üzerinde kalmaya devam ediyor.
 * - Tüm metotlar artık verileri 'housingData' asset'inden okuyor.
 */

using UnityEngine;
using System.Collections; 

public class NpcHousing : MonoBehaviour
{
    // --- VERİ ---
    [Header("Veri Kaynağı (ZORUNLU)")]
    [Tooltip("Bu evin 'ne' spawn edeceği, 'kaç tane' spawn edeceği " +
             "gibi temel verilerini tutan ScriptableObject asset'i.")]
    [SerializeField] private NpcHousingData housingData;
    
    // --- DAVRANIŞ (PREFAB ÜZERİNDE) ---
    [Header("Davranış (Prefab Üzerinde)")]
    [Tooltip("Bu evin NPC'lerinin yapacağı işin tipi.")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; // <-- BURADA KALDI
    
    // --- SAHNE HEDEFLERİ (PREFAB ÜZERİNDE) ---
    [Header("Sahne Hedefleri (Prefab Üzerinde)")]
    [Tooltip("EĞER JobType = GatherResource ise, NPC'lerin gideceği hedef " +
             "(Üzerinde WorkSpotInteractable olmalı).")]
    [SerializeField] private WorkSpotInteractable resourceTarget;
    
    [Tooltip("EĞER JobType = TransferResource ise, NPC'lerin gideceği hedef 'Ev'.")]
    [SerializeField] private NpcHousing houseTarget;
    
    [Tooltip("(Opsiyonel) NPC'lerin doğacağı ve döneceği nokta.")]
    [SerializeField] private Transform spawnPoint; 
    
    [Tooltip("(Opsiyonel) NPC'lerin kullanacağı ara yol.")]
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("Runtime İstatistikleri")]
    [Tooltip("Bu evde toplanan/transfer edilen kaynak sayısı (sayaç).")]
    [SerializeField]
    private int tasksCompletedCounter = 0; 
    
    // Enum (Değişiklik yok)
    public enum NpcJobType { GatherResource, TransferResource }

    // --- BU ALANLAR SİLİNDİ (DATA'YA TAŞINDI) ---
    // [SerializeField] private GameObject genericNpcPrefab;
    // [SerializeField] private FriendlyNpcData npcDataToSpawn;
    // [SerializeField] private float restDuration = 3.0f;
    // [SerializeField] private int populationCount = 3;
    // [SerializeField] private float spawnInterval = 1.5f;
    // --- ---

    
    private void Start()
    {
        // 1. Gerekli referanslar atanmış mı?
        // --- DEĞİŞİKLİK: 'housingData' kontrolü eklendi ---
        if (housingData == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): 'Housing Data' atanmamış! " +
                             "NPC spawn edilemez.", this);
            return;
        }
        if (housingData.genericNpcPrefab == null || housingData.npcDataToSpawn == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): 'Housing Data' ({housingData.name}) " +
                             "içindeki 'Prefab' veya 'Data' atanmamış.", this);
            return;
        }
        // ---
        
        // 2. 'jobType'a göre hedef kontrolü (Değişiklik yok)
        if (jobType == NpcJobType.GatherResource && resourceTarget == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): JobType 'GatherResource' seçili " +
                             "ancak 'Resource Target' (WorkSpot) atanmamış.", this);
            return;
        }
        if (jobType == NpcJobType.TransferResource && houseTarget == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): JobType 'TransferResource' seçili " +
                             "ancak 'House Target' (diğer ev) atanmamış.", this);
            return;
        }
        
        StartCoroutine(SpawnNpcs());
    }

    private IEnumerator SpawnNpcs()
    {
        Vector3 positionToSpawn = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Transform homeTarget = (spawnPoint != null) ? spawnPoint : this.transform;
        
        Transform workTarget = null;
        if (jobType == NpcJobType.GatherResource)
        {
            workTarget = (resourceTarget.interactionPoint != null) 
                ? resourceTarget.interactionPoint 
                : resourceTarget.transform;
        }
        else if (jobType == NpcJobType.TransferResource)
        {
            workTarget = (houseTarget.spawnPoint != null) 
                ? houseTarget.spawnPoint 
                : houseTarget.transform;
        }

        // --- DEĞİŞİKLİK: Veriler 'housingData'dan okunuyor ---
        for (int i = 0; i < housingData.populationCount; i++) // <-- DEĞİŞTİ
        {
            // 1. NPC'yi YARAT (Data'dan okuyarak)
            GameObject npcGO = Instantiate(
                housingData.genericNpcPrefab, // <-- DEĞİŞTİ
                positionToSpawn,
                Quaternion.identity
            );
        // ---

            FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
            if (ai != null)
            {
                // 2. NPC'yi başlat! (Data'dan okuyarak)
                // --- DEĞİŞİKLİK: 'housingData'dan okunuyor ---
                ai.Initialize(housingData.npcDataToSpawn, homeTarget, workTarget, optionalNpcPath); // <-- DEĞİŞTİ
                // ---
                
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
            }
            else
            {
                Debug.LogError($"'{housingData.genericNpcPrefab.name}' prefab'ında 'FriendlyNpcAI' script'i " +
                               "bulunamadı!", housingData.genericNpcPrefab);
            }

            // 3. Bekle (Data'dan okuyarak)
            // --- DEĞİŞİKLİK: 'housingData'dan okunuyor ---
            yield return new WaitForSeconds(housingData.spawnInterval); // <-- DEĞİŞTİ
            // ---
        }
    }
    
    // --- BU METOTLARDA HİÇBİR DEĞİŞİKLİK YOK ---
    // (Çünkü tüm mantıkları 'jobType', 'resourceTarget' ve 'houseTarget'a
    // bağlı ve bu değişkenler zaten bu script'te kaldı)
    #region Event Handlers & Coroutines (No Change)
    
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null)
        {
            Debug.LogError("NPC'nin datası null, eve gönderiliyor.", npc);
            npc.ReturnHome(0); 
            return;
        }
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
            
            if (collectedAmount > 0)
                Debug.Log($"TRANSFER BAŞLADI: {houseTarget.name}'den {collectedAmount} kaynak alındı.", this);
            else
                Debug.Log($"TRANSFER BAŞARISIZ: {houseTarget.name}'de kaynak yok! Eli boş dönülüyor...", this);

            if(npc != null)
                npc.ReturnHome(collectedAmount); 
        }
    }
    
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, int collectedAmount)
    {
        if (collectedAmount > 0)
        {
            tasksCompletedCounter += collectedAmount;
            if (jobType == NpcJobType.GatherResource)
            {
                 Debug.Log($"Ev ({gameObject.name}): {collectedAmount} kaynak toplandı! Toplam: {tasksCompletedCounter}.");
            }
            else if (jobType == NpcJobType.TransferResource)
            {
                 Debug.Log($"Ev ({gameObject.name}): {collectedAmount} kaynak transfer edildi! Toplam: {tasksCompletedCounter}.");
            }
        }
        else
        {
             Debug.Log($"Ev ({gameObject.name}): {npc.name} eli boş döndü. Dinleniyor...");
        }

        // --- DEĞİŞİKLİK: 'housingData'dan okunuyor ---
        StartCoroutine(RestCycle(npc, housingData.restDuration)); // <-- DEĞİŞTİ
        // ---
    }
    
    private IEnumerator WorkCycle(FriendlyNpcAI npc, int capacity)
    {
        if (resourceTarget != null)
        {
            resourceTarget.TriggerInteraction();
        }
        yield return new WaitForSeconds(resourceTarget.workDuration);
        if(npc != null) 
        {
            npc.ReturnHome(capacity);
        }
    }

    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration)
    {
        // --- DEĞİŞİKLİK: 'housingData'dan okunuyor ---
        yield return new WaitForSeconds(duration); // <-- DEĞİŞTİ
        // ---
        
        if(npc != null)
        {
            npc.GoToWork();
        }
    }
    
    public void IncreaseCounter(int amount)
    {
        tasksCompletedCounter += amount;
    }
    
    public int DecreaseCounter(int amountToTake)
    {
        if (tasksCompletedCounter == 0)
        {
            return 0; 
        }
        int actualAmountTaken = Mathf.Min(tasksCompletedCounter, amountToTake);
        tasksCompletedCounter -= actualAmountTaken;
        return actualAmountTaken;
    }
    #endregion
}