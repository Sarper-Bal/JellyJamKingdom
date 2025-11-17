/*
 * NPC EVİ (SPAWNER) - v1.2 (Özel Spawn Noktası)
 *
 * GÖREVİ:
 * 'Start()' metodunda, 'populationCount' kadar NPC'yi
 * 'Instantiate' (Klonlama) yoluyla yaratır.
 *
 * * DEĞİŞİKLİKLER (v1.2):
 * - 'spawnPoint' (Transform) adında opsiyonel bir alan eklendi.
 * - 'SpawnNpcs()' Coroutine'i güncellendi:
 * - Artık NPC'leri 'transform.position' yerine, 'spawnPoint'
 * atanmışsa 'spawnPoint.position'dan yaratır.
 * - Eğer 'spawnPoint' atanmamışsa (null ise), hata vermemek için
 * varsayılan olarak 'transform.position'ı (evin merkezini) kullanır.
 */

using UnityEngine;
using System.Collections; 

public class NpcHousing : MonoBehaviour
{
    [Header("NPC Ayarları")]
    [Tooltip("Bu evden spawn olacak NPC'lerin prefab'ı. " +
             "Üzerinde 'FriendlyNpcAI' script'i olmalı.")]
    [SerializeField] private GameObject genericNpcPrefab;

    [Tooltip("Bu evden çıkacak NPC'lerin kullanacağı 'FriendlyNpcData' (Statlar).")]
    [SerializeField] private FriendlyNpcData npcDataToSpawn;

    [Header("Davranış")]
    [Tooltip("NPC'lerin evden çıkıp gideceği hedef nokta (örn: Maden, Tarla).")]
    [SerializeField] private Transform workSpot;
    
    // --- DEĞİŞİKLİK BAŞLANGICI ---
    [Tooltip("(Opsiyonel) NPC'lerin tam olarak spawn olacağı noktayı belirler " +
             "(örn: Evin kapısının önü). Boş bırakılırsa bu objenin merkezi kullanılır.")]
    [SerializeField] private Transform spawnPoint; // <-- YENİ EKLENDİ
    // --- DEĞİŞİKLİK SONU ---

    [Tooltip("Bu evde yaşayan ve spawn edilecek toplam NPC sayısı.")]
    [SerializeField] private int populationCount = 3;

    [Tooltip("NPC'lerin evden teker teker çıkması için aradaki saniye farkı.")]
    [SerializeField] private float spawnInterval = 1.5f;

    private void Start()
    {
        // 1. Gerekli referanslar atanmış mı?
        if (genericNpcPrefab == null || npcDataToSpawn == null || workSpot == null)
        {
            Debug.LogError($"NpcHousing ({gameObject.name}): 'Generic Npc Prefab', " +
                             "'Npc Data To Spawn' veya 'Work Spot' alanlarından biri atanmamış. " +
                             "NPC spawn edilemez.", this);
            return;
        }
        
        // 2. Havuzlama kapalı

        // 3. NPC'leri Spawn Etmeye Başla
        StartCoroutine(SpawnNpcs());
    }

    /// <summary>
    /// NPC'leri 'spawnInterval' aralığıyla 'Instantiate' eder.
    /// </summary>
    private IEnumerator SpawnNpcs()
    {
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // Spawn pozisyonunu belirle.
        // Eğer 'spawnPoint' Inspector'da atanmışsa onun pozisyonunu,
        // atanmamışsa (null ise) bu 'Ev' objesinin kendi pozisyonunu kullan.
        Vector3 positionToSpawn = (spawnPoint != null) ? spawnPoint.position : transform.position;
        // --- DEĞİŞİKLİK SONU ---

        for (int i = 0; i < populationCount; i++)
        {
            // 1. NPC'yi YARAT (Instantiate)
            GameObject npcGO = Instantiate(
                genericNpcPrefab, 
                positionToSpawn, // <-- DEĞİŞTİRİLDİ
                Quaternion.identity
            );

            // 2. NPC'nin motorunu (AI) bul
            FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
            if (ai != null)
            {
                // 3. NPC'yi başlat! (Statları, Evi ve İş Yerini ver)
                // NPC'nin "eve dönüş" hedefi hala evin kendisidir
                // (doğduğu nokta değil), bu yüzden 'this.transform' gönderiyoruz.
                ai.Initialize(npcDataToSpawn, this.transform, workSpot);
            }
            else
            {
                Debug.LogError($"'{genericNpcPrefab.name}' prefab'ında 'FriendlyNpcAI' script'i " +
                               "bulunamadı!", genericNpcPrefab);
            }

            // 4. Bir sonraki spawn için bekle
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}