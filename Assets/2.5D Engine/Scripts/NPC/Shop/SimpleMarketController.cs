using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    public class SimpleMarketController : MonoBehaviour
    {
        [Header("--- AYARLAR ---")]
        [Tooltip("Sıra bekleyen müşterilerin duracağı noktalar (Sırasıyla 1., 2., 3. nokta).")]
        [SerializeField] private Transform[] queueSpots;

        [Tooltip("Müşterilerin isteyebileceği kaynaklar listesi.")]
        [SerializeField] private List<ResourceData> possibleRequests;

        [Tooltip("Yeni müşteri gelme sıklığı (saniye).")]
        [SerializeField] private float customerSpawnInterval = 2.5f;
        
        [Header("--- BAĞIMLILIKLAR ---")]
        [SerializeField] private SiloController targetSilo;
        [SerializeField] private SimpleCustomer customerPrefab;
        
        [Header("--- İŞÇİ (WORKER) ---")]
        [SerializeField] private FriendlyNpcData workerData; 
        [SerializeField] private string workerPoolTag = "NPC";

        // --- RUNTIME DEĞİŞKENLERİ ---
        private SimpleCustomer[] currentCustomers; // Müşterileri takip eden dizi
        private bool isWorkerBusy = false;
        private FriendlyNpcAI activeWorker; 

        private void Start()
        {
            // 1. Kontroller
            if (queueSpots == null || queueSpots.Length == 0)
            {
                Debug.LogError("HATA: Market 'Queue Spots' noktaları atanmamış!");
                return;
            }
            if (targetSilo == null)
            {
                Debug.LogError("HATA: Market için 'Target Silo' atanmamış!");
            }

            // 2. Diziyi Başlat
            currentCustomers = new SimpleCustomer[queueSpots.Length];

            // 3. Döngüleri Başlat
            // Müşteri Spawn Döngüsü
            StartCoroutine(SpawnRoutine());
            
            // Kuyruk ve İşçi Kontrol Döngüsü (Daha sık çalışır)
            StartCoroutine(LogicRoutine());
        }

        /// <summary>
        /// Belirli aralıklarla yeni müşteri spawn etmeye çalışır.
        /// </summary>
        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                TrySpawnCustomer();
                yield return new WaitForSeconds(customerSpawnInterval);
            }
        }

        /// <summary>
        /// Sürekli olarak kuyruğu kaydırır ve işçi durumunu kontrol eder.
        /// </summary>
        private IEnumerator LogicRoutine()
        {
            while (true)
            {
                // 1. Kuyruğu Kaydır (Önü boşsa ilerlet)
                ShiftQueue();

                // 2. En öndeki müşteriye bak, işçi boşsa gönder
                ProcessQueue();

                // 0.5 saniyede bir kontrol yeterli (Performans için)
                yield return new WaitForSeconds(0.5f);
            }
        }

        // --- MÜŞTERİ YÖNETİMİ ---

        private void TrySpawnCustomer()
        {
            int lastIndex = queueSpots.Length - 1;

            // Eğer kuyruğun EN ARKASI boşsa müşteri al
            if (currentCustomers[lastIndex] == null)
            {
                SpawnCustomerAtSlot(lastIndex);
            }
            else
            {
                // Debug.Log("Market: Kuyruk dolu, müşteri gelemiyor.");
            }
        }

        private void SpawnCustomerAtSlot(int index)
        {
            if (possibleRequests == null || possibleRequests.Count == 0) return;

            // Rastgele kaynak seç
            ResourceData randomResource = possibleRequests[Random.Range(0, possibleRequests.Count)];

            // Müşteriyi yarat
            SimpleCustomer newCustomer = Instantiate(customerPrefab, queueSpots[index].position, Quaternion.identity);
            
            // Başlat ve Listeye ekle
            newCustomer.Initialize(randomResource);
            currentCustomers[index] = newCustomer;
            
            Debug.Log($"Market: Yeni Müşteri (İstek: {randomResource.resourceName}) {index}. sıraya geldi.");
        }

        /// <summary>
        /// Kuyruktaki boşlukları doldurur (Müşterileri öne kaydırır).
        /// </summary>
        private void ShiftQueue()
        {
            // Sondan başa doğru değil, baştan sona doğru tarayalım
            for (int i = 0; i < queueSpots.Length - 1; i++)
            {
                // Eğer şu anki sıra (i) boşsa VE arkasında (i+1) biri varsa
                if (currentCustomers[i] == null && currentCustomers[i + 1] != null)
                {
                    // Arkadakini öne al
                    currentCustomers[i] = currentCustomers[i + 1];
                    currentCustomers[i + 1] = null; // Arkasını boşalt

                    // Görsel olarak hareket ettir
                    currentCustomers[i].MoveToSpot(queueSpots[i].position);
                    
                    // Debug.Log($"Market: Müşteri {i+1}. sıradan {i}. sıraya kaydı.");
                }
            }
        }

        // --- İŞÇİ VE SATIŞ ---

        private void ProcessQueue()
        {
            // İşçi meşgulse veya 1. sırada müşteri yoksa işlem yapma
            if (isWorkerBusy || currentCustomers[0] == null) return;
            
            if (targetSilo == null) return;

            // İşçiyi Göreve Çıkar
            StartCoroutine(DispatchWorker(currentCustomers[0]));
        }

        private IEnumerator DispatchWorker(SimpleCustomer customer)
        {
            isWorkerBusy = true;
            Debug.Log("Market: İşçi çağrılıyor...");

            // 1. İşçi Spawn Et
            FriendlyNpcAI worker = NpcPooler.Instance.SpawnFromPool(workerPoolTag, transform.position, Quaternion.identity);
            
            if (worker == null)
            {
                Debug.LogError("Market HATA: Havuzda boş NPC yok! Pool Size'ı arttırın.");
                isWorkerBusy = false;
                yield break;
            }

            activeWorker = worker;
            
            // Eventleri Bağla
            activeWorker.OnArrivedAtWork += OnWorkerArrivedAtSilo;
            activeWorker.OnArrivedAtHome += OnWorkerReturnedToShop;

            // 2. Silo'ya Gönder
            activeWorker.Activate(workerData, transform, targetSilo.GetSpawnPoint(), null);
        }

        // --- EVENT CALLBACKS ---

        private void OnWorkerArrivedAtSilo(FriendlyNpcAI npc)
        {
            // Müşteri gitmiş mi kontrol et
            if (currentCustomers[0] == null)
            {
                npc.ReturnHome(0, null);
                return;
            }

            ResourceData requested = currentCustomers[0].RequestedResource;
            
            // Silo'dan 1 adet çek
            int taken = targetSilo.TakeResource(requested, 1);
            
            if(taken > 0)
                Debug.Log($"Market: İşçi Silo'dan {requested.resourceName} aldı. Dönüyor.");
            else
                Debug.LogWarning($"Market: Silo'da {requested.resourceName} YOK! İşçi boş dönüyor.");

            // Eve Dön
            npc.ReturnHome(taken, requested);
        }

        private void OnWorkerReturnedToShop(FriendlyNpcAI npc, int amount, ResourceData resource)
        {
            // Eventleri Temizle
            npc.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
            npc.OnArrivedAtHome -= OnWorkerReturnedToShop;

            // İşçiyi Havuza İade Et
            NpcPooler.Instance.ReturnToPool(workerPoolTag, npc);
            
            activeWorker = null;
            isWorkerBusy = false;

            // Satış Sonucu
            if (amount > 0 && currentCustomers[0] != null)
            {
                Debug.Log("Market: SATIŞ BAŞARILI! Müşteri ayrılıyor.");
                
                // Müşteriyi gönder
                currentCustomers[0].LeaveHappy();
                currentCustomers[0] = null; // 1. Sırayı boşalt
                
                // (LogicRoutine bir sonraki turda kuyruğu otomatik kaydıracak)
            }
            else
            {
                // Ürün yoksa müşteri beklemeye devam eder (veya istersen kızıp gidebilir)
                Debug.Log("Market: Satış başarısız. Ürün yok.");
            }
        }
    }
}