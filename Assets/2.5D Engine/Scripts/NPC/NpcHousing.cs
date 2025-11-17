/*
 * NPC EVİ (BEYİN/YÖNETİCİ) - v1.5 (Modüler Hedef)
 *
 * * DEĞİŞİKLİKLER (v1.5):
 * - 'workSpot' alanı 'Transform' yerine 'WorkSpotInteractable'
 * tipinde oldu. Artık doğrudan script'i atayacağız.
 * - 'workDuration' alanı bu script'ten SİLİNDİ (WorkSpotInteractable'a taşındı).
 * - 'Start()' metodu artık 'GetComponent' çağırmıyor.
 * - 'SpawnNpcs()' metodu güncellendi:
 * - NPC'ye hedef olarak 'workSpot.transform'u değil, 'workSpot'un
 * içindeki 'interactionPoint'u atıyor.
 * - Eğer 'interactionPoint' boşsa, güvenli olması için 'workSpot.transform'u
 * (objenin merkezini) atıyor.
 * - 'WorkCycle()' Coroutine'i güncellendi:
 * - Bekleme süresini artık 'workSpot.workDuration'
 * (yani hedefin kendi script'inden) okuyor.
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
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.5) ---
    [Tooltip("NPC'lerin evden çıkıp gideceği hedef 'WorkSpotInteractable' script'i. " +
             "(Doğrudan 'Ağaç' veya 'Taş' objesini sürükleyin)")]
    [SerializeField] private WorkSpotInteractable workSpot; // <-- Tipi Değişti
    
    // 'workDuration' alanı buradan kaldırıldı, 'WorkSpotInteractable'a taşındı
    // [SerializeField] private float workDuration = 5.0f; // <-- SİLİNDİ
    // --- DEĞİŞİKLİK SONU ---

    [Tooltip("NPC'lerin eve döndükten sonra tekrar işe gitmeden önce " +
             "kaç saniye 'dinlenecekleri'.")]
    [SerializeField] private float restDuration = 3.0f;
    
    [Tooltip("(Opsiyonel) NPC'lerin tam olarak spawn olacağı nokta.")]
    [SerializeField] private Transform spawnPoint; 

    [Tooltip("Bu evde yaşayan ve spawn edilecek toplam NPC sayısı.")]
    [SerializeField] private int populationCount = 3;

    [Tooltip("NPC'lerin evden teker teker çıkması için aradaki saniye farkı.")]
    [SerializeField] private float spawnInterval = 1.5f;
    
    // Not: 'workSpotInteractable' alanı, 'workSpot' olarak yeniden adlandırıldı.
    
    private void Start()
    {
        // 1. Gerekli referanslar atanmış mı?
        if (genericNpcPrefab == null || npcDataToSpawn == null || workSpot == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): Referanslar eksik. " +
                             "(Prefab, Data veya Work Spot atanmamış). NPC spawn edilemez.", this);
            return;
        }
        
        // 2. 'GetComponent' kısmı kaldırıldı, çünkü 'workSpot'
        // artık doğrudan 'WorkSpotInteractable' tipinde.
        
        // 3. NPC'leri Spawn Etmeye Başla
        StartCoroutine(SpawnNpcs());
    }

    /// <summary>
    /// NPC'leri 'spawnInterval' aralığıyla 'Instantiate' eder.
    /// </summary>
    private IEnumerator SpawnNpcs()
    {
        Vector3 positionToSpawn = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Transform homeTarget = (spawnPoint != null) ? spawnPoint : this.transform;

        // --- DEĞİŞİKLİK BAŞLANGICI (v1.5) ---
        // 1. NPC'nin gideceği hedef 'Transform'u belirle
        // 'WorkSpot'un 'interactionPoint'u ayarlanmış mı?
        Transform workTarget = (workSpot.interactionPoint != null) 
            ? workSpot.interactionPoint // Evet, o noktayı kullan
            : workSpot.transform;       // Hayır, objenin merkezini kullan
        // --- DEĞİŞİKLİK SONU ---

        for (int i = 0; i < populationCount; i++)
        {
            // 2. NPC'yi YARAT
            GameObject npcGO = Instantiate(
                genericNpcPrefab, 
                positionToSpawn,
                Quaternion.identity
            );

            // 3. NPC'nin motorunu (AI) bul
            FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
            if (ai != null)
            {
                // 4. NPC'yi başlat! (Eve dönüş hedefi 'homeTarget', iş hedefi 'workTarget')
                ai.Initialize(npcDataToSpawn, homeTarget, workTarget); // <-- GÜNCELLENDİ
                
                // 5. NPC'nin "Beyin"e rapor vermesi için event'lerine abone ol
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
            }
            else
            {
                Debug.LogError($"'{genericNpcPrefab.name}' prefab'ında 'FriendlyNpcAI' script'i " +
                               "bulunamadı!", genericNpcPrefab);
            }

            // 6. Bir sonraki spawn için bekle
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    // --- Event Dinleyicileri (Değişiklik Yok) ---
    
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        StartCoroutine(WorkCycle(npc));
    }

    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc)
    {
        StartCoroutine(RestCycle(npc));
    }

    // --- Coroutine'ler (WorkCycle Güncellendi) ---

    /// <summary>
    /// NPC'nin iş yerindeki bekleme ve etkileşim sürecini yönetir.
    /// </summary>
    private IEnumerator WorkCycle(FriendlyNpcAI npc)
    {
        // 1. Etkileşimi (DOTween animasyonunu) tetikle
        // (Artık 'workSpotInteractable' yerine 'workSpot' kullanıyoruz)
        if (workSpot != null)
        {
            workSpot.TriggerInteraction();
        }
        
        // --- DEĞİŞİKLİK BAŞLANGICI (v1.5) ---
        // 2. 'workDuration'ı artık 'workSpot'un
        //    kendi üzerinden (editörden) oku
        yield return new WaitForSeconds(workSpot.workDuration);
        // --- DEĞİŞİKLİK SONU ---
        
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