/*
 * SILO KONTROLCÜSÜ (Silo Controller) - v2.0 (Akıllı & Talep Üzerine)
 * * GÖREVİ:
 * - Hedef evlerdeki toplam kaynağı sürekli izler.
 * - İhtiyaç duyulan işçi sayısını (Toplam Kaynak / Kapasite) hesaplar.
 * - SADECE ihtiyaç kadar işçiyi 'NpcPooler'dan çağırır.
 * - İşçiler görevden döndüğünde, hala ihtiyaç yoksa onları havuza geri gönderir (emekli eder).
 * - Bu sayede sahnede asla gereksiz işçi bulunmaz (Tam Optimizasyon).
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SiloController : MonoBehaviour
{
    [Header("Veri Kaynağı")]
    [Tooltip("Silo'nun çıkaracağı taşıyıcı NPC'lerin verisi.")]
    [SerializeField] private NpcHousingData housingData;

    [Header("Hedefler")]
    [Tooltip("Kaynak toplanacak evlerin listesi.")]
    [SerializeField] private List<NpcHousing> targetHouses;

    [Header("Konumlandırma")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private NpcPath optionalPath;

    [Header("Akıllı Sistem Ayarları")]
    [Tooltip("Evleri ne sıklıkla (saniye) tarayıp işçi sayısını güncelleyecek?")]
    [SerializeField] private float scanInterval = 2.0f;

    [Header("Silo Envanteri (İzleme)")]
    [SerializeField] private int totalStoredResources = 0;
    [SerializeField] private int currentActiveWorkers = 0;
    [SerializeField] private int resourcesWaitingToBeCollected = 0;

    // Şu an aktif olarak çalışan işçilerin listesi
    private List<FriendlyNpcAI> activeWorkers = new List<FriendlyNpcAI>();

    private void Start()
    {
        if (housingData == null)
        {
            Debug.LogError($"Silo ({gameObject.name}): 'Housing Data' atanmamış!", this);
            return;
        }

        // Sonsuz döngüde tarama yapacak Coroutine'i başlat
        StartCoroutine(SmartMonitorRoutine());
    }

    /// <summary>
    /// Belirli aralıklarla kaynakları tarar ve işçi sayısını ayarlar.
    /// </summary>
    private IEnumerator SmartMonitorRoutine()
    {
        while (true)
        {
            // 1. Hedeflerdeki toplam kaynağı hesapla
            CalculateAvailableResources();

            // 2. Gerekli işçi sayısını hesapla ve yönet
            ManageWorkforce();

            yield return new WaitForSeconds(scanInterval);
        }
    }

    private void CalculateAvailableResources()
    {
        resourcesWaitingToBeCollected = 0;
        foreach (var house in targetHouses)
        {
            if (house != null)
            {
                resourcesWaitingToBeCollected += house.GetResourceCount();
            }
        }
    }

    private void ManageWorkforce()
    {
        // 1 işçinin taşıma kapasitesi
        int workerCapacity = housingData.npcDataToSpawn.maxCarryCapacity;
        
        // Matematik: (Toplam Kaynak / Kapasite) yukarı yuvarla
        // Örn: 12 kaynak var, kapasite 5 => 2.4 => 3 işçi lazım.
        // Eğer kaynak 0 ise, 0 işçi lazım.
        int neededWorkers = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / workerCapacity);

        // Maksimum nüfus sınırını (HousingData'dan) aşma
        neededWorkers = Mathf.Clamp(neededWorkers, 0, housingData.populationCount);

        // Şu an kaç eksiğimiz var?
        int workersToSpawn = neededWorkers - activeWorkers.Count;

        if (workersToSpawn > 0)
        {
            // İşçi lazım! Spawn et.
            StartCoroutine(SpawnBatch(workersToSpawn));
        }
        // Not: Eğer workersToSpawn < 0 ise (fazla işçi varsa),
        // onları burada anında silmiyoruz. Görevden dönmelerini bekliyoruz (HandleWorkerReturnedHome).
        // Bu daha doğal görünür.
    }

    private IEnumerator SpawnBatch(int count)
    {
        string poolTag = housingData.genericNpcPrefab.name;
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            // Çifte kontrol: Spawn sırasında kaynaklar tükenmiş olabilir mi?
            // (Basitlik için şimdilik atlıyoruz, ama eklenebilir)

            FriendlyNpcAI npc = NpcPooler.Instance.SpawnFromPool(poolTag, pos, Quaternion.identity);

            if (npc != null)
            {
                activeWorkers.Add(npc);
                currentActiveWorkers = activeWorkers.Count;

                // Event'lere Abone Ol
                npc.OnArrivedAtWork += HandleWorkerArrivedAtTarget;
                npc.OnArrivedAtHome += HandleWorkerReturnedHome;

                // Göreve Gönder
                SendWorkerToBestTarget(npc);
            }

            // Hepsini aynı karede (frame) spawn etmemek için minik bir bekleme
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void SendWorkerToBestTarget(FriendlyNpcAI npc)
    {
        // En zengin evi bul
        NpcHousing bestTarget = targetHouses
            .Where(h => h != null && h.GetResourceCount() > 0)
            .OrderByDescending(h => h.GetResourceCount())
            .FirstOrDefault();

        // Hedef pozisyon
        Transform targetTransform;
        Transform myHome = (spawnPoint != null) ? spawnPoint : transform;

        if (bestTarget != null)
        {
            targetTransform = (bestTarget.houseTarget != null && bestTarget.houseTarget.transform != null) 
                              ? bestTarget.transform // Basitçe evin kendisine gitsin (veya spawnPoint'una)
                              : bestTarget.transform;
            
            // (Not: NpcHousing'e public 'GetSpawnPoint' eklerseniz daha temiz olur, 
            // şimdilik transform kullanıyoruz)
        }
        else
        {
            // Kaynak kalmadıysa, NPC'yi hemen havuza gönderelim (evde beklemesine gerek yok)
            RetireWorker(npc);
            return;
        }

        // NPC'yi Aktive Et ve Gönder
        npc.Activate(housingData.npcDataToSpawn, myHome, targetTransform, optionalPath);
    }

    private void HandleWorkerArrivedAtTarget(FriendlyNpcAI npc)
    {
        // En yakın evi bul (basit collision/mesafe kontrolü yerine mantıksal hedefleme)
        // Not: SendWorkerToBestTarget'da hedefi vermiştik ama NPC 'aptal' olduğu için
        // kime vardığını bilmiyor. Tekrar en yakını bulalım.
        
        NpcHousing targetHouse = GetClosestHouse(npc.transform.position);
        int collected = 0;

        if (targetHouse != null)
        {
            int capacity = npc.GetNpcData().maxCarryCapacity;
            collected = targetHouse.DecreaseCounter(capacity);
        }

        // Kaynak aldıysa veya alamadıysa eve dön
        npc.ReturnHome(collected);
    }

    private void HandleWorkerReturnedHome(FriendlyNpcAI npc, int amount)
    {
        // 1. Kaynağı boşalt
        if (amount > 0)
        {
            totalStoredResources += amount;
            // Debug.Log($"Silo: +{amount} kaynak. Toplam: {totalStoredResources}");
        }

        // 2. KARAR ANI: Bu işçiye hala ihtiyaç var mı?
        
        // Tekrar hesap yapalım
        CalculateAvailableResources();
        int workerCapacity = housingData.npcDataToSpawn.maxCarryCapacity;
        int neededWorkers = Mathf.CeilToInt((float)resourcesWaitingToBeCollected / workerCapacity);
        neededWorkers = Mathf.Clamp(neededWorkers, 0, housingData.populationCount);

        // Eğer şu anki aktif işçi sayısı, gerekenden fazlaysa => EMEKLİ ET
        // (activeWorkers.Count > neededWorkers)
        // Veya hiç kaynak kalmadıysa => EMEKLİ ET
        if (activeWorkers.Count > neededWorkers || resourcesWaitingToBeCollected == 0)
        {
            RetireWorker(npc);
        }
        else
        {
            // Hala ihtiyaç var, dinlenip çalışmaya devam et
            StartCoroutine(RestAndRestart(npc));
        }
    }

    private void RetireWorker(FriendlyNpcAI npc)
    {
        // Event aboneliklerini kaldır
        npc.OnArrivedAtWork -= HandleWorkerArrivedAtTarget;
        npc.OnArrivedAtHome -= HandleWorkerReturnedHome;

        // Listeden çıkar
        activeWorkers.Remove(npc);
        currentActiveWorkers = activeWorkers.Count;

        // Havuza geri gönder (NpcPooler'ın ReturnToPool metodu)
        string poolTag = housingData.genericNpcPrefab.name;
        NpcPooler.Instance.ReturnToPool(poolTag, npc);
        
        // Debug.Log("Silo: İşçi görevi bitti, havuza döndü.");
    }

    private IEnumerator RestAndRestart(FriendlyNpcAI npc)
    {
        yield return new WaitForSeconds(housingData.restDuration);
        
        // Dinlenme bittiğinde tekrar kontrol et (belki o arada kaynaklar bitti?)
        if (npc.gameObject.activeInHierarchy) // NPC hala bizdeyse
        {
            SendWorkerToBestTarget(npc);
        }
    }

    private NpcHousing GetClosestHouse(Vector3 position)
    {
        NpcHousing closest = null;
        float minDst = Mathf.Infinity;
        foreach (var house in targetHouses)
        {
            if (house == null) continue;
            float dst = Vector3.Distance(position, house.transform.position);
            // Biraz toleranslı mesafe (NPC tam üstüne gelmeyebilir)
            if (dst < minDst && dst < 5.0f) 
            {
                minDst = dst;
                closest = house;
            }
        }
        return closest;
    }
    
    public NpcHousingData GetHousingData() { return housingData; }
}