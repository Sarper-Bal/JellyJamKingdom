using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // List aramaları için gerekli

public class SimpleMarketController : MonoBehaviour
{
    // --- YENİ EKONOMİ SİSTEMİ BAŞLANGICI ---
    [System.Serializable]
    public struct TradeItem
    {
        public ResourceData itemToSell; // Örn: Stone
        public int pricePerUnit;        // Örn: 2 (Coin)
    }

    [Header("--- EKONOMİ (Economy) ---")]
    [Tooltip("Bu market satış karşılığında ne kazanacak? (Örn: Coin)")]
    [SerializeField] private ResourceData currencyResource;

    [Tooltip("Hangi ürün kaç para ediyor?")]
    [SerializeField] private List<TradeItem> priceList;

    [Header("--- KASA (Wallet) ---")]
    [Tooltip("Marketin şu ana kadar kazandığı toplam para.")]
    [SerializeField] private int accumulatedCurrency = 0;
    // --- YENİ EKONOMİ SİSTEMİ SONU ---

    [Header("--- MODLAR (Modes) ---")]
    [Tooltip("İşaretliyse: İşçi görevden dönünce yok olmaz, kapıda bekler.")]
    [SerializeField] private bool keepWorkerActive = true;

    [Tooltip("İşaretliyse: İşçi SADECE Silo'da kaynak varsa hareket eder.")]
    [SerializeField] private bool smartWaitMode = true; 

    [Header("--- AYARLAR ---")]
    [SerializeField] private Transform[] queueSpots;
    // possibleRequests'i artık priceList'ten otomatik çekebiliriz ama manuel kontrol için kalsın.
    [SerializeField] private List<ResourceData> possibleRequests; 
    [SerializeField] private float customerSpawnInterval = 2.5f;
    
    [Header("--- KONUMLANDIRMA & HAREKET ---")]
    [SerializeField] private Transform workerSpawnPoint;
    [SerializeField] private NpcPath workerPath;

    [Header("--- HAVUZ & PREFABLAR ---")]
    [SerializeField] private SimpleCustomer customerPrefab; 
    [Tooltip("NpcPooler'a tanıtılacak İşçi Prefabı.")]
    [SerializeField] private FriendlyNpcAI workerPrefab; 
    
    [Header("--- BAĞIMLILIKLAR ---")]
    [SerializeField] private SiloController targetSilo;
    
    [Header("--- İŞÇİ DETAYLARI ---")]
    [SerializeField] private FriendlyNpcData workerData; 
    [SerializeField] private string workerPoolTag = "NPC";

    private SimpleCustomer[] currentCustomers;
    private bool isWorkerBusy = false;
    private FriendlyNpcAI permanentWorker; 

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => NpcPooler.Instance != null);
        
        if (workerPrefab == null || queueSpots == null || queueSpots.Length == 0)
        {
            Debug.LogError("HATA: SimpleMarketController eksik referans!");
            yield break;
        }

        // Fiyat listesi kontrolü
        if (currencyResource == null)
        {
            Debug.LogWarning("UYARI: Marketin 'Currency Resource' (Para Birimi) atanmamış! Kazanç sağlanamayacak.");
        }

        // Havuz İşlemleri
        currentCustomers = new SimpleCustomer[queueSpots.Length];
        if (CustomerPooler.Instance != null && customerPrefab != null)
        {
            CustomerPooler.Instance.RegisterPool(customerPrefab, queueSpots.Length + 2);
        }
        NpcPooler.Instance.CreatePool(workerPoolTag, workerPrefab.gameObject, 1);
        
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

            permanentWorker = NpcPooler.Instance.SpawnFromPool(workerPoolTag, spawnPos, spawnRot);
            
            if (permanentWorker == null)
            {
                // Acil durum
                NpcPooler.Instance.CreatePool(workerPoolTag, workerPrefab.gameObject, 1);
                permanentWorker = NpcPooler.Instance.SpawnFromPool(workerPoolTag, spawnPos, spawnRot);
                if (permanentWorker == null) { isWorkerBusy = false; yield break; }
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
        permanentWorker.Activate(workerData, homePoint, targetSilo.GetSpawnPoint(), workerPath);
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

        // Satış Başarılı mı?
        if (amount > 0 && currentCustomers[0] != null)
        {
            // --- YENİ: PARA HESAPLAMA ---
            CalculateEarnings(resource, amount);
            // ----------------------------

            currentCustomers[0].LeaveHappy();
            currentCustomers[0] = null; 
        }
    }

    // --- YENİ: KAZANÇ HESAPLAMA METODU ---
    private void CalculateEarnings(ResourceData soldItem, int quantity)
    {
        if (currencyResource == null) return;

        // Listeden fiyatı bul (Linq kullanarak)
        // Eğer listede yoksa varsayılan olarak 0 döner.
        var priceEntry = priceList.FirstOrDefault(x => x.itemToSell == soldItem);
        
        if (priceEntry.itemToSell != null) // Listede bulduysak
        {
            int totalEarned = priceEntry.pricePerUnit * quantity;
            accumulatedCurrency += totalEarned;
            
            Debug.Log($"KAZANÇ: {quantity} adet {soldItem.resourceName} satıldı. " +
                      $"Kazanılan: {totalEarned} {currencyResource.resourceName}. " +
                      $"Kasadaki Toplam: {accumulatedCurrency}");
        }
        else
        {
            Debug.LogWarning($"Market: '{soldItem.resourceName}' satıldı ama Fiyat Listesinde (Price List) tanımı yok! Para kazanılmadı.");
        }
    }
}