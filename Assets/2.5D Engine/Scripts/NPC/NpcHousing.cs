/*
 * NPC EVİ (SPAWNER) - v1.1 (Havuzlama OLMADAN)
 *
 * GÖREVİ:
 * 'Start()' metodunda, 'populationCount' kadar NPC'yi
 * 'Instantiate' (Klonlama) yoluyla yaratır.
 * Yarattığı her NPC'ye 'Initialize' komutu vererek
 * statlarını, evini ve iş yerini bildirir.
 *
 * * DEĞİŞİKLİKLER (v1.1):
 * - 'ObjectPooler' ile ilgili tüm kodlar kaldırıldı.
 * - 'npcPoolTag' kaldırıldı.
 * - 'Start()' içindeki 'CreatePool' çağrısı kaldırıldı.
 * - 'SpawnNpcs()' Coroutine'i, 'ObjectPooler.SpawnFromPool' yerine
 * 'Instantiate' kullanacak şekilde güncellendi.
 * - 'IPooledObject' ile ilgili kodlar kaldırıldı.
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

    [Tooltip("Bu evde yaşayan ve spawn edilecek toplam NPC sayısı.")]
    [SerializeField] private int populationCount = 3;

    [Tooltip("NPC'lerin evden teker teker çıkması için aradaki saniye farkı.")]
    [SerializeField] private float spawnInterval = 1.5f;

    // 'npcPoolTag' kaldırıldı

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
        
        // 2. Havuz Oluşturma kısmı kaldırıldı.
        // ObjectPooler.Instance.CreatePool(...) // <-- SİLİNDİ

        // 3. NPC'leri Spawn Etmeye Başla
        StartCoroutine(SpawnNpcs());
    }

    /// <summary>
    /// NPC'leri 'spawnInterval' aralığıyla 'Instantiate' eder.
    /// </summary>
    private IEnumerator SpawnNpcs()
    {
        for (int i = 0; i < populationCount; i++)
        {
            // --- DEĞİŞİKLİK BAŞLANGICI (v1.1) ---
            // 1. NPC'yi havuzdan ALMA, direkt YARAT (Instantiate)
            GameObject npcGO = Instantiate(
                genericNpcPrefab, 
                transform.position, // Evin pozisyonunda
                Quaternion.identity
            );
            // --- DEĞİŞİKLİK SONU ---

            // 2. NPC'nin motorunu (AI) bul
            FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
            if (ai != null)
            {
                // 3. NPC'yi başlat! (Statları, Evi ve İş Yerini ver)
                ai.Initialize(npcDataToSpawn, this.transform, workSpot);
            }
            else
            {
                Debug.LogError($"'{genericNpcPrefab.name}' prefab'ında 'FriendlyNpcAI' script'i " +
                               "bulunamadı!", genericNpcPrefab);
            }
            
            // 4. 'IPooledObject' kısmı kaldırıldı.

            // 5. Bir sonraki spawn için bekle
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}