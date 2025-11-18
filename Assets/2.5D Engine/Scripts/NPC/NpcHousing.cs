/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v3.3 (Hata Düzeltmesi)
 *
 * * DEĞİŞİKLİKLER (v3.3):
 * - HATA DÜZELTMESİ (CS1061): 'Awake()' metodu ve içindeki
 * 'NpcPooler.Instance.RegisterNeeds()' çağrısı
 * TAMAMEN KALDIRILDI.
 * - 'NpcPooler' (v3.2) artık 'Awake()' içinde 'FindObjectsOfType'
 * kullanarak evleri otomatik olarak bulduğu için, 'NpcHousing'in
 * kendini kaydetmesine (register) gerek yoktur.
 * - 'GetHousingData()' metodu 'NpcPooler'ın veriyi okuyabilmesi
 * için public olarak duruyor (v3.2'deki gibi).
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic; // List için

public class NpcHousing : MonoBehaviour
{
    // --- Veri ve Referans Alanları (Değişiklik yok v3.0) ---
    [Header("Veri Kaynağı (ZORUNLU)")]
    [SerializeField] private NpcHousingData housingData;
    
    [Header("Davranış (Prefab Üzerinde)")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    
    [Header("Sahne Hedefleri (Prefab Üzerinde)")]
    [SerializeField] private WorkSpotInteractable resourceTarget;
    [SerializeField] private NpcHousing houseTarget;
    [SerializeField] private Transform spawnPoint; 
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("Runtime İstatistikleri")]
    [SerializeField]
    private int tasksCompletedCounter = 0; 
    
    public enum NpcJobType { GatherResource, TransferResource }
    
    /// <summary>
    /// Bu ev tarafından yaratılan ve yönetilen tüm NPC'lerin listesi.
    /// </summary>
    private List<FriendlyNpcAI> managedNpcs = new List<FriendlyNpcAI>();

    
    // --- DEĞİŞİKLİK BAŞLANGICI (v3.3 - Hata Düzeltmesi) ---
    // 'Awake()' metodu kaldırıldı. 'NpcPooler' (v3.2)
    // zaten bu objeyi 'FindObjectsOfType' ile bulacaktır.
    /*
    private void Awake()
    {
        if (housingData == null) { return; }
        
        if (NpcPooler.Instance != null)
        {
            // BU SATIR HATALIYDI:
            NpcPooler.Instance.RegisterNeeds(housingData);
        }
    }
    */
    // --- DEĞİŞİKLİK SONU ---

    
    private void Start()
    {
        // 1. Referans kontrolleri (Değişiklik yok)
        if (housingData == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): 'Housing Data' atanmamış!", this);
            return;
        }
        if (housingData.genericNpcPrefab == null || housingData.npcDataToSpawn == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): 'Housing Data' içindeki 'Prefab' veya 'Data' atanmamış.", this);
            return;
        }
        if (jobType == NpcJobType.GatherResource && resourceTarget == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): JobType 'GatherResource' seçili ancak 'Resource Target' atanmamış.", this);
            return;
        }
        if (jobType == NpcJobType.TransferResource && houseTarget == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): JobType 'TransferResource' seçili ancak 'House Target' atanmamış.", this);
            return;
        }
        
        // 2. NPC'leri (artık havuzdan) Spawn Etmeye Başla
        StartCoroutine(SpawnNpcs());
    }

    /// <summary>
    /// NPC'leri 'spawnInterval' aralığıyla havuzdan çeker.
    /// (Bu metot v3.2 ile aynı, değişiklik yok)
    /// </summary>
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
                // NPC'yi aktive et
                ai.Activate(housingData.npcDataToSpawn, homeTarget, workTarget, optionalNpcPath); 
                
                // Event'lere abone ol
                ai.OnArrivedAtWork -= HandleNpcArrivedAtWork; 
                ai.OnArrivedAtHome -= HandleNpcArrivedAtHome;
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
                
                managedNpcs.Add(ai);
            }
            else
            {
                Debug.LogError($"NpcHousing ({gameObject.name}): Havuz boşaldı! " +
                               $"'{poolTag}' havuzu 'populationCount'ı karşılamıyor.", this);
                yield break; 
            }

            yield return new WaitForSeconds(housingData.spawnInterval);
        }
    }
    
    
    // --- BU METOTLARDA DEĞİŞİKLİK YOK ---
    #region Event Handlers & Coroutines (No Change v3.2)
    
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null)
        {
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

        StartCoroutine(RestCycle(npc, housingData.restDuration));
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
    
    /// <summary>
    /// "Zıplama" hatasını çözen havuzsuz döngü
    /// </summary>
    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if(npc != null)
        {
            npc.GoToWork(); // 'SetActive(false)' YAPMA
        }
    }
    
    // --- Sayaç Metotları (v2.0) & Data Getter (v3.2) ---
    
    /// <summary>
    /// 'NpcPooler'ın 'housingData'yı okuyabilmesi için public
    /// </summary>
    public NpcHousingData GetHousingData()
    {
        return housingData;
    }
    
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
    #endregion
}