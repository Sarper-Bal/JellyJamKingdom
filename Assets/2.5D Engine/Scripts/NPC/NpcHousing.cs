/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v2.1 (Opsiyonel Yol Ataması)
 *
 * * DEĞİŞİKLİKLER (v2.1):
 * - YENİ ALAN: '[SerializeField] private NpcPath optionalNpcPath;' eklendi.
 * - Bu, Inspector'dan bu eve özel bir yol atamamızı sağlar.
 * - 'SpawnNpcs()' metodu güncellendi:
 * - 'ai.Initialize()' metodunu 4 parametre ile çağırıyor
 * ve 'optionalNpcPath' referansını NPC'ye iletiyor.
 * - Eğer 'optionalNpcPath' boş (null) bırakılırsa,
 * 'FriendlyNpcAI' bunu algılayıp direkt gitme modunda çalışacaktır.
 */

using UnityEngine;
using System.Collections; 

public class NpcHousing : MonoBehaviour
{
    public enum NpcJobType
    {
        GatherResource,
        TransferResource
    }

    [Header("NPC Ayarları")]
    [SerializeField] private GameObject genericNpcPrefab;
    [SerializeField] private FriendlyNpcData npcDataToSpawn;

    [Header("Davranış")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource;
    [SerializeField] private WorkSpotInteractable resourceTarget;
    [SerializeField] private NpcHousing houseTarget;
    [SerializeField] private float restDuration = 3.0f;
    
    [Header("Konumlandırma")]
    [Tooltip("(Opsiyonel) NPC'lerin doğacağı ve döneceği nokta.")]
    [SerializeField] private Transform spawnPoint; 
    
    // --- DEĞİŞİKLİK BAŞLANGICI (v2.1) ---
    [Tooltip("(Opsiyonel) NPC'lerin işe giderken ve dönerken kullanacağı ara yol. " +
             "Boş bırakılırsa, hedefe direkt giderler.")]
    [SerializeField] private NpcPath optionalNpcPath; // <-- YENİ EKLENDİ
    // --- DEĞİŞİKLİK SONU ---

    [Header("Nüfus")]
    [SerializeField] private int populationCount = 3;
    [SerializeField] private float spawnInterval = 1.5f;

    [Header("Runtime İstatistikleri")]
    [SerializeField]
    private int tasksCompletedCounter = 0; 
    
    
    private void Start()
    {
        // Referans kontrolleri (Değişiklik yok)
        if (genericNpcPrefab == null || npcDataToSpawn == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): 'Prefab' veya 'Data' atanmamış.", this);
            return;
        }
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
        
        // NPC'nin gideceği hedef 'Transform'u 'jobType'a göre belirle
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

        for (int i = 0; i < populationCount; i++)
        {
            GameObject npcGO = Instantiate(
                genericNpcPrefab, 
                positionToSpawn,
                Quaternion.identity
            );

            FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
            if (ai != null)
            {
                // --- DEĞİŞİKLİK BAŞLANGICI (v2.1) ---
                // 4. NPC'yi başlat! (Statları, Evi, İş Yerini VE OPSİYONEL YOLU ver)
                ai.Initialize(npcDataToSpawn, homeTarget, workTarget, optionalNpcPath); 
                // --- DEĞİŞİKLİK SONU ---
                
                // 5. Event'lere abone ol
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
            }
            else
            {
                Debug.LogError($"'{genericNpcPrefab.name}' prefab'ında 'FriendlyNpcAI' script'i " +
                               "bulunamadı!", genericNpcPrefab);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    // --- BU METOTLARDA DEĞİŞİKLİK YOK ---
    // (Beyin, NPC'nin nasıl gittiğiyle ilgilenmez,
    // sadece vardığını ('OnArrived') bilmesi yeterlidir)
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

        StartCoroutine(RestCycle(npc));
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
    
    private IEnumerator RestCycle(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(restDuration);
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