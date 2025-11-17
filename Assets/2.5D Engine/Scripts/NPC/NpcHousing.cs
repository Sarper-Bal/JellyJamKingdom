/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v2.0 (Kapasite Yönetimi)
 *
 * * DEĞİŞİKLİKLER (v2.0):
 * - 'DecreaseCounter' metodu artık 'int amountToTake' alıyor
 * ve ne kadar alabilirse o kadarını (int) döndürüyor.
 * - 'SpawnNpcs()' metodu, yeni 'OnArrivedAtHome(npc, amount)'
 * event'ine abone olacak şekilde güncellendi.
 * - 'HandleNpcArrivedAtWork()' (Transfer Modu) mantığı değişti:
 * - NPC'nin 'GetNpcData().maxCarryCapacity' bilgisini okur.
 * - 'houseTarget.DecreaseCounter(capacity)' çağırarak hedef evden
 * o kapasite kadar kaynak almayı DENER.
 * - Hedef evin ne kadar (int) kaynak verdiğini ('amountCollected')
 * 'npc.ReturnHome(amountCollected)' metoduna iletir.
 * - 'WorkCycle()' Coroutine'i güncellendi:
 * - NPC'nin 'maxCarryCapacity' bilgisini okur.
 * - 'npc.ReturnHome(capacity)' çağırır (kaynak toplama
 * her zaman tam kapasite verir varsayımıyla).
 * - 'HandleNpcArrivedAtHome()' metodunun imzası değişti:
 * 'HandleNpcArrivedAtHome(FriendlyNpcAI npc, int collectedAmount)'
 * - Artık sayacı '1' değil, eve getirilen 'collectedAmount'
 * (0 veya daha fazla) kadar artırır.
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
    
    [Tooltip("NPC'lerin evde dinlenme süresi.")]
    [SerializeField] private float restDuration = 3.0f;
    [Tooltip("(Opsiyonel) NPC'lerin doğacağı ve döneceği nokta.")]
    [SerializeField] private Transform spawnPoint; 
    [SerializeField] private int populationCount = 3;
    [SerializeField] private float spawnInterval = 1.5f;

    [Header("Runtime İstatistikleri")]
    [Tooltip("Bu evde toplanan/transfer edilen kaynak sayısı (sayaç).")]
    [SerializeField]
    private int tasksCompletedCounter = 0; 
    
    
    private void Start()
    {
        // 1. Gerekli referanslar atanmış mı?
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
        
        // 3. NPC'leri Spawn Etmeye Başla
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
                ai.Initialize(npcDataToSpawn, homeTarget, workTarget); 
                
                // --- DEĞİŞİKLİK BAŞLANGICI (v2.0 - Yeni Event Aboneliği) ---
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                // Yeni (int) imzalı event'e abone ol
                ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
                // --- DEĞİŞİKLİK SONU ---
            }
            else
            {
                Debug.LogError($"'{genericNpcPrefab.name}' prefab'ında 'FriendlyNpcAI' script'i " +
                               "bulunamadı!", genericNpcPrefab);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    
    /// <summary>
    /// Bir NPC iş yerine (veya hedef eve) ulaştığında bu metot tetiklenir.
    /// </summary>
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        // --- DEĞİŞİKLİK BAŞLANGICI (v2.0 - Kapasiteye Göre) ---
        
        // NPC'nin verisini ve kapasitesini al
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null)
        {
            Debug.LogError("NPC'nin datası null, eve gönderiliyor.", npc);
            npc.ReturnHome(0); // Eli boş dön
            return;
        }
        int capacity = data.maxCarryCapacity; // Örn: 5

        if (jobType == NpcJobType.GatherResource)
        {
            // İŞ 1: Kaynak Topla
            StartCoroutine(WorkCycle(npc, capacity));
        }
        else if (jobType == NpcJobType.TransferResource)
        {
            // İŞ 2: Transfer Et
            int collectedAmount = 0;
            if (houseTarget != null)
            {
                // 1. Hedef evden 'kapasitesi kadar' kaynak almayı DENE.
                collectedAmount = houseTarget.DecreaseCounter(capacity);
            }
            
            if (collectedAmount > 0)
                Debug.Log($"TRANSFER BAŞLADI: {houseTarget.name}'den {collectedAmount} kaynak alındı.", this);
            else
                Debug.Log($"TRANSFER BAŞARISIZ: {houseTarget.name}'de kaynak yok! Eli boş dönülüyor...", this);

            // 2. NPC'ye (topladığı miktar kadar) eve dön komutu ver
            if(npc != null)
                npc.ReturnHome(collectedAmount); 
        }
        // --- DEĞİŞİKLİK SONU ---
    }

    /// <summary>
    /// Bir NPC eve (spawn point'e) ulaştığında bu metot tetiklenir.
    /// </summary>
    /// <param name="npc">Geri dönen NPC</param>
    /// <param name="collectedAmount">NPC'nin elinde getirdiği kaynak miktarı (0 veya daha fazla)</param>
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, int collectedAmount)
    {
        // --- DEĞİŞİKLİK BAŞLANGICI (v2.0 - Kapasiteye Göre Artış) ---
        
        // 1. NPC elinde kaynakla mı döndü?
        if (collectedAmount > 0)
        {
            // Evet, sayacı '1' değil, 'collectedAmount' kadar artır
            tasksCompletedCounter += collectedAmount;
            
            if (jobType == NpcJobType.GatherResource)
            {
                 Debug.Log($"Ev ({gameObject.name}): {collectedAmount} kaynak toplandı! " +
                           $"Toplam: {tasksCompletedCounter}. Teşekkürler, {npc.name}.");
            }
            else if (jobType == NpcJobType.TransferResource)
            {
                 Debug.Log($"Ev ({gameObject.name}): {collectedAmount} kaynak transfer edildi! " +
                           $"Toplam: {tasksCompletedCounter}. Teşekkürler, {npc.name}.");
            }
        }
        else
        {
            // Hayır, eli boş döndü.
             Debug.Log($"Ev ({gameObject.name}): {npc.name} eli boş döndü. Dinleniyor...");
        }
        // --- DEĞİŞİKLİK SONU ---

        // 2. Dinlenme döngüsünü başlat
        StartCoroutine(RestCycle(npc));
    }

    /// <summary>
    /// NPC'nin iş yerindeki bekleme ve etkileşim sürecini yönetir.
    /// </summary>
    private IEnumerator WorkCycle(FriendlyNpcAI npc, int capacity)
    {
        if (resourceTarget != null)
        {
            resourceTarget.TriggerInteraction();
        }
        
        yield return new WaitForSeconds(resourceTarget.workDuration);
        
        if(npc != null) 
        {
            // Eve dönerken elinin "tam kapasite" dolu olduğunu bildir
            npc.ReturnHome(capacity);
        }
    }

    /// <summary>
    /// NPC'nin evdeki 'dinlenme' sürecini yönetir.
    /// </summary>
    private IEnumerator RestCycle(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(restDuration);
        
        if(npc != null)
        {
            npc.GoToWork();
        }
    }
    
    // --- DEĞİŞİKLİK BAŞLANGICI (v2.0 - Kapasiteye Göre Azaltma) ---
    
    /// <summary>
    /// Bu evin sayacını dışarıdan artırır.
    /// </summary>
    public void IncreaseCounter(int amount)
    {
        tasksCompletedCounter += amount;
    }
    
    /// <summary>
    /// Bu evin sayacını dışarıdan azaltmayı DENER.
    /// Alınmak istenen miktar (amountToTake) kadar veya
    /// evde ne kadar varsa o kadarını (daha azsa) azaltır.
    /// </summary>
    /// <param name="amountToTake">NPC'nin 'maxCarryCapacity' değeri</param>
    /// <returns>NPC'nin alabildiği gerçek kaynak miktarı (0 olabilir)</returns>
    public int DecreaseCounter(int amountToTake)
    {
        // 1. Evde hiç kaynak yoksa, 0 döndür
        if (tasksCompletedCounter == 0)
        {
            return 0; // Eli boş dön
        }
        
        // 2. Alınabilecek gerçek miktarı hesapla
        //    (Evdeki miktar, NPC'nin kapasitesinden az olabilir)
        int actualAmountTaken = Mathf.Min(tasksCompletedCounter, amountToTake);
        
        // 3. Evin sayacını güncelle
        tasksCompletedCounter -= actualAmountTaken;
        
        // 4. NPC'ye ne kadar aldığını söyle
        return actualAmountTaken;
    }
    // --- DEĞİŞİKLİK SONU ---
}