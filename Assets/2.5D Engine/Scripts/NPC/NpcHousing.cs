/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v1.9 (Gerçek Kaynak Transferi)
 *
 * * DEĞİŞİKLİKLER (v1.9):
 * - 'DecreaseCounter' metodu artık 'bool' (doğru/yanlış)
 * döndürüyor. Kaynak varsa 'true' ve eksiltir, kaynak yoksa 'false' döndürür.
 * - 'SpawnNpcs()' metodu, yeni 'OnArrivedAtHome(npc, payload)'
 * event'ine abone olacak şekilde güncellendi.
 * - 'HandleNpcArrivedAtWork()' (Transfer Modu) mantığı değişti:
 * - Artık sayaçları ANINDA değiştirmiyor.
 * - 'houseTarget.DecreaseCounter(1)' çağırarak kaynak almayı DENER.
 * - Dönen 'bool' (başarılı/başarısız) sonucunu 'npc.ReturnHome(bool)'
 * metoduna iletir.
 * - 'HandleNpcArrivedAtHome()' metodunun imzası değişti:
 * 'HandleNpcArrivedAtHome(FriendlyNpcAI npc, bool collectedResource)'
 * - Artık sayaç artışı burada, NPC'nin eve geri döndüğü ve
 * 'collectedResource' bayrağının 'true' olduğu anda yapılıyor.
 * - 'WorkCycle()' Coroutine'i güncellendi: 'npc.ReturnHome()' artık
 * 'npc.ReturnHome(true)' olarak çağrılıyor (Kaynak toplama her zaman başarılıdır).
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
            // Diğer evin 'spawnPoint'unu (veya merkezini) hedef al
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
                
                // --- DEĞİŞİKLİK BAŞLANGICI (v1.9 - Yeni Event Aboneliği) ---
                // Event'lere abone ol
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                // Yeni (bool) imzalı event'e abone ol
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
        // --- DEĞİŞİKLİK BAŞLANGICI (v1.9 - Kaynak Kontrolü) ---
        if (jobType == NpcJobType.GatherResource)
        {
            // İŞ 1: Kaynak Topla
            StartCoroutine(WorkCycle(npc));
        }
        else if (jobType == NpcJobType.TransferResource)
        {
            // İŞ 2: Transfer Et
            bool collectedResource = false;
            if (houseTarget != null)
            {
                // 1. Hedef evden kaynak almayı DENE.
                // 'DecreaseCounter' metodu artık kaynak varsa 'true' döner.
                collectedResource = houseTarget.DecreaseCounter(1);
            }
            
            if (collectedResource)
                Debug.Log($"TRANSFER BAŞLADI: {houseTarget.name}'den 1 kaynak alındı.", this);
            else
                Debug.Log($"TRANSFER BAŞARISIZ: {houseTarget.name}'de kaynak yok! Eli boş dönülüyor...", this);

            // 2. NPC'ye (başarılı veya başarısız) eve dön komutu ver
            if(npc != null)
                npc.ReturnHome(collectedResource); 
        }
        // --- DEĞİŞİKLİK SONU ---
    }

    /// <summary>
    /// Bir NPC eve (spawn point'e) ulaştığında bu metot tetiklenir.
    /// </summary>
    /// <param name="npc">Geri dönen NPC</param>
    /// <param name="collectedResource">NPC'nin elinde kaynak olup olmadığı</param>
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, bool collectedResource)
    {
        // --- DEĞİŞİKLİK BAŞLANGICI (v1.9 - Sayaç Mantığı) ---
        
        // 1. Kaynak Toplama işinden mi döndü?
        if (jobType == NpcJobType.GatherResource)
        {
            // 'WorkCycle' her zaman 'true' payload ile döner,
            // yani kaynak topladı. Sayacı artır.
            tasksCompletedCounter++;
            Debug.Log($"Ev ({gameObject.name}): {tasksCompletedCounter}. kaynak toplandı! " +
                      $"Teşekkürler, {npc.name}, evine hoş geldin.");
        }
        // 2. Transfer işinden mi döndü?
        else if (jobType == NpcJobType.TransferResource)
        {
            // SADECE 'collectedResource' true ise (yani hedef evde kaynak varsa)
            // sayacı artır.
            if (collectedResource)
            {
                tasksCompletedCounter++;
                Debug.Log($"Ev ({gameObject.name}): Transfer tamamlandı! " +
                          $"Toplam kaynağımız: {tasksCompletedCounter}");
            }
            else
            {
                // Eli boş döndü, sayaç artmaz.
                Debug.Log($"Ev ({gameObject.name}): {npc.name} transferden eli boş döndü. Dinleniyor...");
            }
        }
        // --- DEĞİŞİKLİK SONU ---

        // 3. Dinlenme döngüsünü başlat (Her iki iş tipi için de ortak)
        StartCoroutine(RestCycle(npc));
    }

    /// <summary>
    /// NPC'nin iş yerindeki bekleme ve etkileşim sürecini yönetir.
    /// (Sadece 'GatherResource' işi için çağrılır)
    /// </summary>
    private IEnumerator WorkCycle(FriendlyNpcAI npc)
    {
        if (resourceTarget != null)
        {
            resourceTarget.TriggerInteraction();
        }
        
        yield return new WaitForSeconds(resourceTarget.workDuration);
        
        if(npc != null) 
        {
            // --- DEĞİŞİKLİK BAŞLANGICI (v1.9) ---
            // Eve dönerken elinin "dolu" olduğunu bildir
            npc.ReturnHome(true);
            // --- DEĞİŞİKLİK SONU ---
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
    
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.9 - Geliştirilmiş Sayaç Metotları) ---
    
    /// <summary>
    /// Bu evin sayacını dışarıdan artırır.
    /// </summary>
    public void IncreaseCounter(int amount)
    {
        tasksCompletedCounter += amount;
    }
    
    /// <summary>
    /// Bu evin sayacını dışarıdan azaltmayı DENER.
    /// Sadece yeterli kaynak varsa 'true' döner.
    /// </summary>
    /// <returns>Kaynak başarıyla alındıysa 'true'</returns>
    public bool DecreaseCounter(int amount)
    {
        // Yeterli kaynak var mı?
        if (tasksCompletedCounter >= amount)
        {
            // Evet, kaynağı azalt ve 'başarılı' dön
            tasksCompletedCounter -= amount;
            return true;
        }
        else
        {
            // Hayır, kaynak yok. 'Başarısız' (eli boş) dön
            return false;
        }
    }
    // --- DEĞİŞİKLİK SONU ---
}