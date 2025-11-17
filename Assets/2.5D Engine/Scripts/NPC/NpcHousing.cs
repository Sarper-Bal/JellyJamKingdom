/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v1.4 (Eve Dönüş Düzeltmesi)
 *
 * * DEĞİŞİKLİKLER (v1.4):
 * - 'SpawnNpcs()' Coroutine'i güncellendi.
 * - 'homeTarget' adında yeni bir Transform değişkeni tanımlandı.
 * - Bu değişken, EĞER 'spawnPoint' atanmışsa 'spawnPoint'un
 * kendisini, atanmamışsa 'this.transform'u (evin merkezini) tutar.
 * - 'ai.Initialize()' metodu artık 'home' parametresi olarak 'this.transform'
 * yerine bu 'homeTarget' değişkenini gönderiyor.
 * - Sonuç: NPC'ler artık 'spawnPoint'e geri dönecekler.
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
    [Tooltip("NPC'lerin evden çıkıp gideceği hedef nokta (örn: Maden, Tarla). " +
             "Bu objenin üzerinde 'WorkSpotInteractable' script'i OLMALI.")]
    [SerializeField] private Transform workSpot;
    
    [Tooltip("NPC'lerin iş yerinde kaç saniye 'çalışacakları' (bekleyecekleri).")]
    [SerializeField] private float workDuration = 5.0f;

    [Tooltip("NPC'lerin eve döndükten sonra tekrar işe gitmeden önce " +
             "kaç saniye 'dinlenecekleri'.")]
    [SerializeField] private float restDuration = 3.0f;
    
    [Tooltip("(Opsiyonel) NPC'lerin tam olarak spawn olacağı nokta.")]
    [SerializeField] private Transform spawnPoint; 

    [Tooltip("Bu evde yaşayan ve spawn edilecek toplam NPC sayısı.")]
    [SerializeField] private int populationCount = 3;

    [Tooltip("NPC'lerin evden teker teker çıkması için aradaki saniye farkı.")]
    [SerializeField] private float spawnInterval = 1.5f;
    
    // Etkileşim script referansı
    private WorkSpotInteractable workSpotInteractable;
    
    private void Start()
    {
        // 1. Gerekli referanslar atanmış mı?
        if (genericNpcPrefab == null || npcDataToSpawn == null || workSpot == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): Referanslar eksik. NPC spawn edilemez.", this);
            return;
        }
        
        // 2. Etkileşim script'ini bul ve sakla
        workSpotInteractable = workSpot.GetComponent<WorkSpotInteractable>();
        if (workSpotInteractable == null)
        {
            Debug.LogWarning($"NpcHousing ({gameObject.name}): 'Work Spot' ({workSpot.name}) " +
                             "üzerinde 'WorkSpotInteractable' script'i bulunamadı. " +
                             "DOTween animasyonu tetiklenemeyecek.", this);
        }
        
        // 3. NPC'leri Spawn Etmeye Başla
        StartCoroutine(SpawnNpcs());
    }

    /// <summary>
    /// NPC'leri 'spawnInterval' aralığıyla 'Instantiate' eder.
    /// </summary>
    private IEnumerator SpawnNpcs()
    {
        // NPC'nin doğacağı POZİSYON
        Vector3 positionToSpawn = (spawnPoint != null) ? spawnPoint.position : transform.position;
        
        // --- DEĞİŞİKLİK BAŞLANGICI (v1.4) ---
        // NPC'nin geri döneceği HEDEF (Transform)
        // Eğer 'spawnPoint' atanmışsa, dönüş hedefi 'spawnPoint'tur.
        // Atanmamışsa, dönüş hedefi evin merkezidir ('this.transform').
        Transform homeTarget = (spawnPoint != null) ? spawnPoint : this.transform;
        // --- DEĞİŞİKLİK SONU ---

        for (int i = 0; i < populationCount; i++)
        {
            // 1. NPC'yi YARAT (Instantiate)
            GameObject npcGO = Instantiate(
                genericNpcPrefab, 
                positionToSpawn,
                Quaternion.identity
            );

            // 2. NPC'nin motorunu (AI) bul
            FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
            if (ai != null)
            {
                // --- DEĞİŞİKLİK BAŞLANGICI (v1.4) ---
                // 3. NPC'yi başlat! (Eve dönüş hedefi olarak 'homeTarget'i ver)
                ai.Initialize(npcDataToSpawn, homeTarget, workSpot);
                // --- DEĞİŞİKLİK SONU ---
                
                // 4. NPC'nin "Beyin"e rapor vermesi için event'lerine abone ol
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
            }
            else
            {
                Debug.LogError($"'{genericNpcPrefab.name}' prefab'ında 'FriendlyNpcAI' script'i " +
                               "bulunamadı!", genericNpcPrefab);
            }

            // 5. Bir sonraki spawn için bekle
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    // --- BU METOTLARDA DEĞİŞİKLİK YOK ---
    
    /// <summary>
    /// Bir NPC iş yerine ulaştığında bu metot tetiklenir.
    /// </summary>
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        StartCoroutine(WorkCycle(npc));
    }

    /// <summary>
    /// Bir NPC eve ulaştığında bu metot tetiklenir.
    /// </summary>
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc)
    {
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
        
        // 2. 'workDuration' kadar bekle
        yield return new WaitForSeconds(workDuration);
        
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