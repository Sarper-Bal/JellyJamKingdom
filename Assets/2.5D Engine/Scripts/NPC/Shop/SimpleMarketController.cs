using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMarketController : MonoBehaviour
{
    [Header("--- MODLAR ---")]
    [Tooltip("İşaretliyse: İşçi görevden dönünce yok olmaz, kapıda bekler (Nöbetçi Modu).")]
    [SerializeField] private bool keepWorkerActive = true;

    [Tooltip("İşaretliyse: İşçi SADECE Silo'da kaynak varsa hareket eder.")]
    [SerializeField] private bool smartWaitMode = true; 

    [Header("--- AYARLAR ---")]
    [SerializeField] private Transform[] queueSpots;
    [SerializeField] private List<ResourceData> possibleRequests;
    [SerializeField] private float customerSpawnInterval = 2.5f;
    
    [Header("--- KONUMLANDIRMA ---")]
    [Tooltip("İşçinin bekleyeceği ve doğacağı nokta.")]
    [SerializeField] private Transform workerSpawnPoint;

    [Header("--- HAVUZ & PREFABLAR ---")]
    [SerializeField] private SimpleCustomer customerPrefab; 
    
    [Header("--- ÖNEMLİ: İŞÇİ PREFABI ---")]
    [Tooltip("BURAYI BOŞ BIRAKMA! NpcPooler'a tanıtılacak İşçi Prefabı (Genellikle 'NPC' veya 'Worker' prefabı).")]
    [SerializeField] private FriendlyNpcAI workerPrefab; 
    
    [Header("--- BAĞIMLILIKLAR ---")]
    [SerializeField] private SiloController targetSilo;
    
    [Header("--- İŞÇİ DETAYLARI ---")]
    [SerializeField] private FriendlyNpcData workerData; 
    [SerializeField] private string workerPoolTag = "NPC";

    private SimpleCustomer[] currentCustomers;
    private bool isWorkerBusy = false;
    private FriendlyNpcAI permanentWorker; 

    // --- DEĞİŞİKLİK: Start artık IEnumerator (Bekleme yapabilmesi için) ---
    private IEnumerator Start()
    {
        // 1. Önce NpcPooler'ın ve CustomerPooler'ın hazır olmasını bekle (Race Condition Çözümü)
        yield return new WaitUntil(() => NpcPooler.Instance != null);
        
        // 2. Güvenlik Kontrolleri ve Hata Raporlama
        if (workerPrefab == null)
        {
            Debug.LogError($"HATA: '{gameObject.name}' üzerindeki SimpleMarketController'da 'Worker Prefab' atanmamış! Lütfen Inspector'dan bir NPC prefabı sürükleyin.");
            yield break; // Kodu durdur
        }
        
        if (queueSpots == null || queueSpots.Length == 0)
        {
             Debug.LogError("SimpleMarket: Queue Spots atanmamış!");
             yield break;
        }

        // 3. Müşteri Havuzu Rezervasyonu
        currentCustomers = new SimpleCustomer[queueSpots.Length];
        if (CustomerPooler.Instance != null && customerPrefab != null)
        {
            CustomerPooler.Instance.RegisterPool(customerPrefab, queueSpots.Length + 2);
        }

        // 4. İŞÇİ HAVUZU REZERVASYONU (GARANTİ EKLENDİ)
        // Market başına 1 işçi lazım. "NPC" havuzuna 1 tane ekle.
        // Eğer havuzda zaten 10 tane varsa, 11. yi ekler. Böylece "yetersiz havuz" sorunu çözülür.
        NpcPooler.Instance.CreatePool(workerPoolTag, workerPrefab.gameObject, 1);
        
        Debug.Log($"SimpleMarket: '{workerPoolTag}' havuzu başarıyla ayarlandı/genişletildi.");

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
            if (targetSilo.GetStoredAmount(requestedRes) < 1) return; 
        }

        StartCoroutine(DispatchWorker());
    }

    private IEnumerator DispatchWorker()
    {
        isWorkerBusy = true;

        if (permanentWorker == null)
        {
            Vector3 spawnPos = (workerSpawnPoint != null) ? workerSpawnPoint.position : transform.position;
            Quaternion spawnRot = (workerSpawnPoint != null) ? workerSpawnPoint.rotation : Quaternion.identity;

            // Burada havuzdan çekmeye çalışıyoruz
            permanentWorker = NpcPooler.Instance.SpawnFromPool(workerPoolTag, spawnPos, spawnRot);
            
            if (permanentWorker == null)
            {
                // EĞER HALA NULL GELİYORSA: Havuz Start'ta oluşturulmasına rağmen boş demektir.
                // Acil durum: Anında yeni bir tane yarat.
                Debug.LogWarning("SimpleMarket: Havuz boş kaldı! Acil durum üretimi yapılıyor.");
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
            if (!permanentWorker.gameObject.activeInHierarchy) permanentWorker.gameObject.SetActive(true);
        }

        permanentWorker.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
        permanentWorker.OnArrivedAtHome -= OnWorkerReturnedToShop;
        permanentWorker.OnArrivedAtWork += OnWorkerArrivedAtSilo;
        permanentWorker.OnArrivedAtHome += OnWorkerReturnedToShop;

        Transform homePoint = (workerSpawnPoint != null) ? workerSpawnPoint : transform;
        permanentWorker.Activate(workerData, homePoint, targetSilo.GetSpawnPoint(), null);
        yield return null;
    }

    private void OnWorkerArrivedAtSilo(FriendlyNpcAI npc)
    {
        if (currentCustomers[0] == null) { npc.ReturnHome(0, null); return; }

        ResourceData requested = currentCustomers[0].RequestedResource;
        int taken = targetSilo.TakeResource(requested, 1);
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