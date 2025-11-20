using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMarketController : MonoBehaviour
{
    [Header("--- MODLAR (Modes) ---")]
    [Tooltip("İşaretliyse: İşçi görevden dönünce yok olmaz, kapıda bekler (Nöbetçi Modu).")]
    [SerializeField] private bool keepWorkerActive = true;

    [Tooltip("İşaretliyse: İşçi SADECE Silo'da kaynak varsa hareket eder.")]
    [SerializeField] private bool smartWaitMode = true; 

    [Header("--- AYARLAR (Settings) ---")]
    [SerializeField] private Transform[] queueSpots;
    [SerializeField] private List<ResourceData> possibleRequests;
    [SerializeField] private float customerSpawnInterval = 2.5f;
    
    [Header("--- KONUMLANDIRMA VE HAREKET (Position & Path) ---")]
    [Tooltip("İşçinin bekleyeceği ve doğacağı nokta.")]
    [SerializeField] private Transform workerSpawnPoint;

    [Tooltip("YENİ: İşçinin Silo'ya giderken takip edeceği yol. (Boş bırakılırsa direkt koşar)")]
    [SerializeField] private NpcPath workerPath; // <-- YENİ EKLENEN ALAN

    [Header("--- HAVUZ & PREFABLAR (Pool & Prefabs) ---")]
    [SerializeField] private SimpleCustomer customerPrefab; 
    
    [Header("--- ÖNEMLİ: İŞÇİ PREFABI ---")]
    [Tooltip("BURAYI BOŞ BIRAKMA! NpcPooler'a tanıtılacak İşçi Prefabı.")]
    [SerializeField] private FriendlyNpcAI workerPrefab; 
    
    [Header("--- BAĞIMLILIKLAR (Dependencies) ---")]
    [SerializeField] private SiloController targetSilo;
    
    [Header("--- İŞÇİ DETAYLARI (Worker Stats) ---")]
    [SerializeField] private FriendlyNpcData workerData; 
    [SerializeField] private string workerPoolTag = "NPC";

    // --- Runtime Değişkenleri ---
    private SimpleCustomer[] currentCustomers;
    private bool isWorkerBusy = false;
    private FriendlyNpcAI permanentWorker; 

    // Başlangıç (IEnumerator: Pooler'ın hazır olmasını beklemek için)
    private IEnumerator Start()
    {
        // 1. Önce sistemlerin (Pooler) hazır olmasını bekle
        yield return new WaitUntil(() => NpcPooler.Instance != null);
        
        // 2. Güvenlik Kontrolleri
        if (workerPrefab == null)
        {
            Debug.LogError($"HATA: '{gameObject.name}' üzerindeki SimpleMarketController'da 'Worker Prefab' atanmamış!");
            yield break;
        }
        
        if (queueSpots == null || queueSpots.Length == 0)
        {
             Debug.LogError("HATA: SimpleMarket kuyruk noktaları (Queue Spots) atanmamış!");
             yield break;
        }

        // 3. Müşteri Havuzunu Ayarla
        currentCustomers = new SimpleCustomer[queueSpots.Length];
        if (CustomerPooler.Instance != null && customerPrefab != null)
        {
            CustomerPooler.Instance.RegisterPool(customerPrefab, queueSpots.Length + 2);
        }

        // 4. İşçi Havuzu Rezervasyonu
        // Market başına 1 işçi lazım. Havuza bunu ekletiyoruz.
        NpcPooler.Instance.CreatePool(workerPoolTag, workerPrefab.gameObject, 1);
        
        Debug.Log($"SimpleMarket: '{name}' başladı. Yol atanmış mı? {(workerPath != null ? "EVET" : "HAYIR")}");

        // 5. Döngüleri Başlat
        StartCoroutine(SpawnRoutine());
        StartCoroutine(LogicRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            TrySpawnCustomer();
            yield return new WaitForSeconds(customerSpawnInterval);
        }
    }

    private IEnumerator LogicRoutine()
    {
        while (true)
        {
            ShiftQueue();
            ManageWorkerLogic(); 
            yield return new WaitForSeconds(0.5f);
        }
    }

    // --- MÜŞTERİ YÖNETİMİ ---
    private void TrySpawnCustomer()
    {
        int lastIndex = queueSpots.Length - 1;
        if (currentCustomers[lastIndex] == null) SpawnCustomerAtSlot(lastIndex);
    }

    private void SpawnCustomerAtSlot(int index)
    {
        if (possibleRequests == null || possibleRequests.Count == 0) return;
        if (CustomerPooler.Instance == null) return;

        SimpleCustomer newCustomer = CustomerPooler.Instance.GetCustomer(queueSpots[index].position, Quaternion.identity);

        if (newCustomer != null)
        {
            ResourceData randomResource = possibleRequests[Random.Range(0, possibleRequests.Count)];
            newCustomer.Initialize(randomResource);
            currentCustomers[index] = newCustomer;
        }
    }

    private void ShiftQueue()
    {
        for (int i = 0; i < queueSpots.Length - 1; i++)
        {
            if (currentCustomers[i] == null && currentCustomers[i + 1] != null)
            {
                currentCustomers[i] = currentCustomers[i + 1];
                currentCustomers[i + 1] = null; 
                currentCustomers[i].MoveToSpot(queueSpots[i].position);
            }
        }
    }

    // --- İŞÇİ MANTIĞI ---
    private void ManageWorkerLogic()
    {
        if (isWorkerBusy || currentCustomers[0] == null || targetSilo == null) return;

        ResourceData requestedRes = currentCustomers[0].RequestedResource;
        if (smartWaitMode)
        {
            // Silo'da mal yoksa işçi gönderme
            if (targetSilo.GetStoredAmount(requestedRes) < 1) return; 
        }

        StartCoroutine(DispatchWorker());
    }

    private IEnumerator DispatchWorker()
    {
        isWorkerBusy = true;

        // Nöbetçi İşçi (Permanent Worker) Yönetimi
        if (permanentWorker == null)
        {
            Vector3 spawnPos = (workerSpawnPoint != null) ? workerSpawnPoint.position : transform.position;
            Quaternion spawnRot = (workerSpawnPoint != null) ? workerSpawnPoint.rotation : Quaternion.identity;

            // Havuzdan çek
            permanentWorker = NpcPooler.Instance.SpawnFromPool(workerPoolTag, spawnPos, spawnRot);
            
            // Eğer havuz boşsa (acil durum), yeni yaratıp çek
            if (permanentWorker == null)
            {
                Debug.LogWarning("SimpleMarket: Havuz boş! Acil durum üretimi yapılıyor.");
                NpcPooler.Instance.CreatePool(workerPoolTag, workerPrefab.gameObject, 1);
                permanentWorker = NpcPooler.Instance.SpawnFromPool(workerPoolTag, spawnPos, spawnRot);
                
                if (permanentWorker == null)
                {
                     isWorkerBusy = false;
                     yield break;
                }
            }
        }
        else
        {
            // Zaten varsa ve kapalıysa aç
            if (!permanentWorker.gameObject.activeInHierarchy) permanentWorker.gameObject.SetActive(true);
        }

        // Olayları dinle (Eski abonelikleri temizleyerek)
        permanentWorker.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
        permanentWorker.OnArrivedAtHome -= OnWorkerReturnedToShop;
        permanentWorker.OnArrivedAtWork += OnWorkerArrivedAtSilo;
        permanentWorker.OnArrivedAtHome += OnWorkerReturnedToShop;

        Transform homePoint = (workerSpawnPoint != null) ? workerSpawnPoint : transform;
        
        // --- DEĞİŞİKLİK: Yolu (workerPath) parametre olarak gönderiyoruz ---
        // Eğer workerPath null ise NPC direkt koşar, atanmışsa yolu takip eder.
        permanentWorker.Activate(workerData, homePoint, targetSilo.GetSpawnPoint(), workerPath);
        
        yield return null;
    }

    private void OnWorkerArrivedAtSilo(FriendlyNpcAI npc)
    {
        if (currentCustomers[0] == null) { npc.ReturnHome(0, null); return; }

        ResourceData requested = currentCustomers[0].RequestedResource;
        int taken = targetSilo.TakeResource(requested, 1);
        
        // Eve dön (Dönerken genellikle aynı yolu tersten kullanır veya direkt döner, NPC AI mantığına bağlı)
        npc.ReturnHome(taken, requested);
    }

    private void OnWorkerReturnedToShop(FriendlyNpcAI npc, int amount, ResourceData resource)
    {
        npc.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
        npc.OnArrivedAtHome -= OnWorkerReturnedToShop;
        isWorkerBusy = false;

        if (!keepWorkerActive)
        {
            NpcPooler.Instance.ReturnToPool(workerPoolTag, npc);
            permanentWorker = null; 
        }

        if (amount > 0 && currentCustomers[0] != null)
        {
            currentCustomers[0].LeaveHappy();
            currentCustomers[0] = null; 
        }
    }
}