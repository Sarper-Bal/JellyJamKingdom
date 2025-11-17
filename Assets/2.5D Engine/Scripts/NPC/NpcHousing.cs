/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v1.7 (Eve Dönüş Sayacı)
 *
 * GÖREVİ:
 * NPC'leri spawn eder VE onların yaşam döngüsünü yönetir.
 *
 * * DEĞİŞİKLİKLER (v1.7):
 * - Sayaç ve "Teşekkür" log'u 'HandleNpcArrivedAtWork'
 * metodundan KALDIRILDI.
 * - Sayaç ve "Teşekkür" log'u 'HandleNpcArrivedAtHome'
 * metoduna EKLENDİ.
 * - Artık bir NPC, işe gidip, çalışıp, evine (spawn point'e)
 * başarıyla geri döndüğünde sayaç artar.
 */

using UnityEngine;
using System.Collections; 

public class NpcHousing : MonoBehaviour
{
    [Header("NPC Ayarları")]
    [Tooltip("Bu evden spawn olacak NPC'lerin prefab'ı.")]
    [SerializeField] private GameObject genericNpcPrefab;

    [Tooltip("Bu evden çıkacak NPC'lerin kullanacağı 'FriendlyNpcData' (Statlar).")]
    [SerializeField] private FriendlyNpcData npcDataToSpawn;

    [Header("Davranış")]
    [Tooltip("NPC'lerin evden çıkıp gideceği hedef 'WorkSpotInteractable' script'i.")]
    [SerializeField] private WorkSpotInteractable workSpot;
    
    [Tooltip("NPC'lerin eve döndükten sonra tekrar işe gitmeden önce " +
             "kaç saniye 'dinlenecekleri'.")]
    [SerializeField] private float restDuration = 3.0f;
    
    [Tooltip("(Opsiyonel) NPC'lerin tam olarak spawn olacağı nokta.")]
    [SerializeField] private Transform spawnPoint; 

    [Tooltip("Bu evde yaşayan ve spawn edilecek toplam NPC sayısı.")]
    [SerializeField] private int populationCount = 3;

    [Tooltip("NPC'lerin evden teker teker çıkması için aradaki saniye farkı.")]
    [SerializeField] private float spawnInterval = 1.5f;

    [Header("Runtime İstatistikleri")]
    [Tooltip("Bu eve bağlı NPC'ler tarafından tamamlanan toplam görev döngüsü sayısı.")]
    [SerializeField]
    private int tasksCompletedCounter = 0; 
    
    private WorkSpotInteractable workSpotInteractable;
    
    private void Start()
    {
        // 1. Gerekli referanslar atanmış mı?
        if (genericNpcPrefab == null || npcDataToSpawn == null || workSpot == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): Referanslar eksik.", this);
            return;
        }
        
        workSpotInteractable = workSpot; 
        
        // 3. NPC'leri Spawn Etmeye Başla
        StartCoroutine(SpawnNpcs());
    }

    private IEnumerator SpawnNpcs()
    {
        Vector3 positionToSpawn = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Transform homeTarget = (spawnPoint != null) ? spawnPoint : this.transform;
        
        Transform workTarget = (workSpot.interactionPoint != null) 
            ? workSpot.interactionPoint 
            : workSpot.transform;
        
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
                
                // Event'lere abone ol
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
    
    /// <summary>
    /// Bir NPC iş yerine ulaştığında bu metot tetiklenir.
    /// </summary>
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        // --- DEĞİŞİKLİK (v1.7) ---
        // Sayaç buradan kaldırıldı.
        // --- DEĞİŞİKLİK SONU ---
        
        // Çalışma döngüsünü başlat
        StartCoroutine(WorkCycle(npc));
    }

    /// <summary>
    /// Bir NPC eve (spawn point'e) ulaştığında bu metot tetiklenir.
    /// </summary>
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc)
    {
        // --- DEĞİŞİKLİK BAŞLANGICI (v1.7) ---
        // 1. Sayaç buraya taşındı. Tam bir döngü tamamlandı.
        tasksCompletedCounter++;
        
        // 2. Teşekkür mesajı (Log)
        Debug.Log($"Ev ({gameObject.name}): {tasksCompletedCounter}. tam döngü tamamlandı! " +
                  $"Teşekkürler, {npc.name}, evine hoş geldin.");
        // --- DEĞİŞİKLİK SONU ---
        
        // 3. Dinlenme döngüsünü başlat
        StartCoroutine(RestCycle(npc));
    }

    /// <summary>
    /// NPC'nin iş yerindeki bekleme ve etkileşim sürecini yönetir.
    /// </summary>
    private IEnumerator WorkCycle(FriendlyNpcAI npc)
    {
        // 1. Etkileşimi (DOTween animasyonunu) tetikle
        if (workSpotInteractable != null)
        {
            workSpotInteractable.TriggerInteraction();
        }
        
        // 2. Bekleme süresini 'workSpot'tan oku
        yield return new WaitForSeconds(workSpotInteractable.workDuration);
        
        // 3. NPC'ye "Eve Dön" komutu ver
        if(npc != null) 
        {
            npc.ReturnHome();
        }
    }

    /// <summary>
    /// NPC'nin evdeki 'dinlenme' sürecini yönetir.
    /// </summary>
    private IEnumerator RestCycle(FriendlyNpcAI npc)
    {
        // 1. 'restDuration' kadar bekle
        yield return new WaitForSeconds(restDuration);
        
        // 2. NPC'ye "İşe Git" komutu ver
        if(npc != null)
        {
            npc.GoToWork();
        }
    }
}