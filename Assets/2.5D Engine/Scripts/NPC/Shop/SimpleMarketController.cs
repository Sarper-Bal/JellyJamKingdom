using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    public class SimpleMarketController : MonoBehaviour
    {
        [Header("--- AYARLAR ---")]
        [SerializeField] private Transform[] queueSpots;
        [SerializeField] private List<ResourceData> possibleRequests;
        [SerializeField] private float customerSpawnInterval = 2.5f;

        [Header("--- HAVUZ AYARLARI (Auto-Managed) ---")]
        [Tooltip("Bu Marketin kullanacağı müşteri prefabı. Pool otomatik oluşturulacak.")]
        [SerializeField] private SimpleCustomer customerPrefab; 
        
        // customerPoolTag SİLİNDİ. Artık ihtiyacımız yok.
        
        [Header("--- BAĞIMLILIKLAR ---")]
        [SerializeField] private SiloController targetSilo;
        
        [Header("--- İŞÇİ (WORKER) ---")]
        [SerializeField] private FriendlyNpcData workerData; 
        [SerializeField] private string workerPoolTag = "NPC";

        private SimpleCustomer[] currentCustomers;
        private bool isWorkerBusy = false;
        private FriendlyNpcAI activeWorker; 

        private void Start()
        {
            if (queueSpots == null || queueSpots.Length == 0) return;
            if (customerPrefab == null)
            {
                Debug.LogError("SimpleMarket: 'Customer Prefab' atanmamış!");
                return;
            }

            // 1. DİZİYİ BAŞLAT
            currentCustomers = new SimpleCustomer[queueSpots.Length];

            // 2. POOLER'I HAZIRLA (Modüler Entegrasyon)
            // Kuyruk sayısı kadar + 2 tane yedek (buffer) üretmesini istiyoruz.
            if (CustomerPooler.Instance != null)
            {
                CustomerPooler.Instance.RegisterPool(customerPrefab, queueSpots.Length + 2);
            }
            else
            {
                Debug.LogError("SimpleMarket: Sahnede 'CustomerPooler' bulunamadı! Lütfen sahneye ekleyin.");
            }

            // 3. DÖNGÜLERİ BAŞLAT
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
                ProcessQueue();
                yield return new WaitForSeconds(0.5f);
            }
        }

        // --- MÜŞTERİ YÖNETİMİ (CustomerPooler Kullanımı) ---

        private void TrySpawnCustomer()
        {
            int lastIndex = queueSpots.Length - 1;

            if (currentCustomers[lastIndex] == null)
            {
                SpawnCustomerAtSlot(lastIndex);
            }
        }

        private void SpawnCustomerAtSlot(int index)
        {
            if (possibleRequests == null || possibleRequests.Count == 0) return;
            if (CustomerPooler.Instance == null) return;

            // DEĞİŞİKLİK: Özel Pooler'dan çekiyoruz
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

        // --- İŞÇİ VE SATIŞ (Değişiklik Yok) ---

        private void ProcessQueue()
        {
            if (isWorkerBusy || currentCustomers[0] == null || targetSilo == null) return;
            StartCoroutine(DispatchWorker(currentCustomers[0]));
        }

        private IEnumerator DispatchWorker(SimpleCustomer customer)
        {
            isWorkerBusy = true;

            // İşçi hala genel NPC havuzunu kullanıyor (Bu doğru, çünkü işçiler ortak kaynaktır)
            FriendlyNpcAI worker = NpcPooler.Instance.SpawnFromPool(workerPoolTag, transform.position, Quaternion.identity);
            
            if (worker == null)
            {
                isWorkerBusy = false;
                yield break;
            }

            activeWorker = worker;
            activeWorker.OnArrivedAtWork += OnWorkerArrivedAtSilo;
            activeWorker.OnArrivedAtHome += OnWorkerReturnedToShop;
            
            activeWorker.Activate(workerData, transform, targetSilo.GetSpawnPoint(), null);
        }

        private void OnWorkerArrivedAtSilo(FriendlyNpcAI npc)
        {
            if (currentCustomers[0] == null)
            {
                npc.ReturnHome(0, null);
                return;
            }

            ResourceData requested = currentCustomers[0].RequestedResource;
            int taken = targetSilo.TakeResource(requested, 1);
            npc.ReturnHome(taken, requested);
        }

        private void OnWorkerReturnedToShop(FriendlyNpcAI npc, int amount, ResourceData resource)
        {
            npc.OnArrivedAtWork -= OnWorkerArrivedAtSilo;
            npc.OnArrivedAtHome -= OnWorkerReturnedToShop;

            NpcPooler.Instance.ReturnToPool(workerPoolTag, npc);
            
            activeWorker = null;
            isWorkerBusy = false;

            if (amount > 0 && currentCustomers[0] != null)
            {
                currentCustomers[0].LeaveHappy();
                currentCustomers[0] = null; 
            }
        }
    }
}