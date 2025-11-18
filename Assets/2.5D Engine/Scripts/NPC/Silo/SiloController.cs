/*
 * SILO KONTROLCÜSÜ (Silo Controller)
 * GÖREVİ:
 * - Özel bir bina türüdür. 'NpcHousing'den bağımsız çalışır.
 * - Kendi NPC'lerini 'NpcPooler' üzerinden spawn eder.
 * - Hedeflediği evler (targetHouses) listesinden, en çok kaynağı olanı seçer.
 * - NPC'leri o eve gönderip kaynakları "çalar" (transfer eder) ve Silo'da toplar.
 * * ÖZELLİKLER:
 * - Modüler: İstediğiniz kadar evi listeye ekleyebilirsiniz.
 * - Akıllı: Her seferinde en karlı hedefi seçer.
 * - Optimize: NpcPooler ve Event-Driven mimariyi kullanır.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // En zengin evi bulmak (Sıralama) için gerekli

public class SiloController : MonoBehaviour
{
    [Header("Veri Kaynağı")]
    [Tooltip("Silo'nun çıkaracağı taşıyıcı NPC'lerin verisi (Prefab, Sayı, Hız vb.).")]
    [SerializeField] private NpcHousingData housingData;

    [Header("Hedef Evler")]
    [Tooltip("Silo'nun kaynak toplayacağı evlerin listesi.")]
    [SerializeField] private List<NpcHousing> targetHouses;

    [Header("Konumlandırma")]
    [Tooltip("Silo NPC'lerinin doğacağı ve kaynakları getireceği nokta.")]
    [SerializeField] private Transform spawnPoint;
    
    [Tooltip("Silo ile hedefler arasındaki yol (Opsiyonel).")]
    [SerializeField] private NpcPath optionalPath;

    [Header("Silo Envanteri")]
    [SerializeField] private int totalResources = 0;

    // Yönetilen NPC Listesi
    private List<FriendlyNpcAI> managedNpcs = new List<FriendlyNpcAI>();

    private void Start()
    {
        if (housingData == null)
        {
            Debug.LogError($"Silo ({gameObject.name}): 'Housing Data' atanmamış!", this);
            return;
        }

        // NPC'leri (Havuzdan) Spawn Etmeye Başla
        StartCoroutine(SpawnWorkers());
    }

    /// <summary>
    /// NpcPooler'dan işçileri çağırır.
    /// </summary>
    private IEnumerator SpawnWorkers()
    {
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        string poolTag = housingData.genericNpcPrefab.name;

        for (int i = 0; i < housingData.populationCount; i++)
        {
            // 1. Havuzdan Çek
            FriendlyNpcAI npc = NpcPooler.Instance.SpawnFromPool(poolTag, pos, Quaternion.identity);

            if (npc != null)
            {
                // 2. Yönetilenlere ekle
                managedNpcs.Add(npc);

                // 3. Event'lerine Abone Ol
                npc.OnArrivedAtWork += HandleWorkerArrivedAtTarget;
                npc.OnArrivedAtHome += HandleWorkerReturnedHome;

                // 4. İlk Göreve Gönder
                SendWorkerToBestTarget(npc);
            }

            yield return new WaitForSeconds(housingData.spawnInterval);
        }
    }

    /// <summary>
    /// NPC'yi o anki en zengin eve gönderir.
    /// </summary>
    private void SendWorkerToBestTarget(FriendlyNpcAI npc)
    {
        // 1. Hedef listesinden, kaynağı en çok olanı bul
        // (GetResourceCount > 0 olanlar arasından)
        NpcHousing bestTarget = targetHouses
            .Where(h => h != null && h.GetResourceCount() > 0)
            .OrderByDescending(h => h.GetResourceCount())
            .FirstOrDefault();

        // 2. Hedef pozisyonu belirle
        Transform targetTransform = null;
        
        // Ev hedefi varsa onun spawn noktasını, yoksa (kaynak yoksa) Silo'nun önünde beklemesi için kendi spawn noktamızı verelim.
        if (bestTarget != null)
        {
            // Hedef evin spawn noktasına (kapısına) git
            // (NpcHousing'e 'GetSpawnPoint' metodu eklemek şık olurdu ama
            // şimdilik transform'una gidiyoruz, çünkü spawnPoint private)
            targetTransform = bestTarget.transform; 
        }
        else
        {
            // Hiçbir evde kaynak yoksa, Silo'nun önünde bekle
            targetTransform = (spawnPoint != null) ? spawnPoint : transform;
        }

        // 3. NPC'yi Oraya Gönder (Activate/Initialize)
        // Silo'nun kendi 'spawnPoint'unu EV (Home), hedef evi İŞ (Work) olarak veriyoruz.
        Transform myHome = (spawnPoint != null) ? spawnPoint : transform;
        
        // NPC'ye "Senin hedefin bu" diyoruz.
        // Not: bestTarget'ı 'npc' üzerinde saklamıyoruz (NPC aptaldır),
        // sadece oraya gitmesini söylüyoruz. Vardığında 'HandleWorkerArrivedAtTarget'ta
        // tekrar 'bestTarget'ı bulacağız veya basitçe o anki pozisyona en yakın evi arayacağız.
        // **Daha İyisi:** NPC'ye hedefi verdik ama vardığında hangi evde olduğunu bilmemiz lazım.
        // Şimdilik basit tutalım: Vardığında tekrar en yakın evi kontrol et veya
        // targetHouses listesinden mesafe kontrolü yap.
        
        npc.Activate(housingData.npcDataToSpawn, myHome, targetTransform, optionalPath);
    }

    /// <summary>
    /// İşçi hedef eve vardığında çalışır.
    /// </summary>
    private void HandleWorkerArrivedAtTarget(FriendlyNpcAI npc)
    {
        // NPC şu an bir evin kapısında. Hangi ev?
        // Basit yöntem: En yakın evi bul.
        NpcHousing currentHouse = GetClosestHouse(npc.transform.position);

        int collected = 0;
        if (currentHouse != null)
        {
            // Kapasitesi kadar al
            int capacity = npc.GetNpcData().maxCarryCapacity;
            collected = currentHouse.DecreaseCounter(capacity);
        }

        if (collected > 0)
        {
            // Kaynak aldı, eve dön
            npc.ReturnHome(collected);
        }
        else
        {
            // Kaynak alamadı (yolda bitmiş olabilir), eli boş dön
            npc.ReturnHome(0);
        }
    }

    /// <summary>
    /// İşçi Silo'ya döndüğünde çalışır.
    /// </summary>
    private void HandleWorkerReturnedHome(FriendlyNpcAI npc, int amount)
    {
        // 1. Kaynakları Silo'ya boşalt
        if (amount > 0)
        {
            totalResources += amount;
            Debug.Log($"Silo: {amount} kaynak geldi. Toplam Stok: {totalResources}");
        }

        // 2. Dinlen ve sonra tekrar ava çık
        StartCoroutine(RestAndRestart(npc));
    }

    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(housingData.restDuration);
        
        // Tekrar en iyi hedefi bul ve gönder
        SendWorkerToBestTarget(npc);
    }

    // --- Yardımcı Metotlar ---

    private NpcHousing GetClosestHouse(Vector3 position)
    {
        NpcHousing closest = null;
        float minDst = Mathf.Infinity;
        
        foreach (var house in targetHouses)
        {
            if (house == null) continue;
            float dst = Vector3.Distance(position, house.transform.position);
            if (dst < minDst && dst < 2.0f) // 2 birim yakınındaysa o evdedir
            {
                minDst = dst;
                closest = house;
            }
        }
        return closest;
    }
    
    // NpcPooler'ın bu script'ten veri okuyabilmesi için (Interface kullanmıyorsak)
    public NpcHousingData GetHousingData()
    {
        return housingData;
    }
}