using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    /// <summary>
    /// Sadece Müşterileri (SimpleCustomer) yöneten özelleşmiş havuz sistemi.
    /// Marketler başladığında otomatik olarak ihtiyaçları kadar havuz oluşturulmasını talep eder.
    /// </summary>
    public class CustomerPooler : MonoBehaviour
    {
        #region Singleton
        public static CustomerPooler Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        #endregion

        // Havuzumuz (Sıra yapısı en hızlısıdır)
        private Queue<SimpleCustomer> poolQueue = new Queue<SimpleCustomer>();
        
        // Referans prefab (Eğer havuz boşalırsa acil durum üretimi için saklıyoruz)
        private SimpleCustomer backupPrefab;

        /// <summary>
        /// Marketler tarafından çağrılır. Havuza belirtilen miktarda müşteri ekler.
        /// </summary>
        /// <param name="prefab">Üretilecek Müşteri Prefabı</param>
        /// <param name="quantity">Kuyruk boyutu kadar miktar</param>
        public void RegisterPool(SimpleCustomer prefab, int quantity)
        {
            if (prefab == null) return;
            
            // Yedek prefabı sakla (Acil durumlar için)
            if (backupPrefab == null) backupPrefab = prefab;

            // İstenen miktar kadar üret ve havuza at
            for (int i = 0; i < quantity; i++)
            {
                CreateAndEnqueue(prefab);
            }
            
            Debug.Log($"CustomerPooler: Havuza {quantity} adet müşteri eklendi. Toplam: {poolQueue.Count}");
        }

        private SimpleCustomer CreateAndEnqueue(SimpleCustomer prefab)
        {
            SimpleCustomer obj = Instantiate(prefab, transform); // Pooler'ın altına topla (Hiyerarşi temizliği)
            obj.gameObject.SetActive(false);
            poolQueue.Enqueue(obj);
            return obj;
        }

        /// <summary>
        /// Havuzdan bir müşteri çeker.
        /// </summary>
        public SimpleCustomer GetCustomer(Vector3 position, Quaternion rotation)
        {
            // 1. Havuz boş mu kontrol et
            if (poolQueue.Count == 0)
            {
                Debug.LogWarning("CustomerPooler: Havuz boşaldı! Acil durum üretimi yapılıyor.");
                if (backupPrefab != null)
                {
                    CreateAndEnqueue(backupPrefab);
                }
                else
                {
                    return null; // Yapacak bir şey yok
                }
            }

            // 2. Kuyruktan al
            SimpleCustomer customer = poolQueue.Dequeue();

            // 3. Pozisyonla ve Aktifleştir
            customer.transform.position = position;
            customer.transform.rotation = rotation;
            customer.gameObject.SetActive(true);
            
            // 4. Resetleme mantığını tetikle
            customer.OnSpawnFromPool();

            return customer;
        }

        /// <summary>
        /// Müşteriyi havuza geri döndürür.
        /// </summary>
        public void ReturnCustomer(SimpleCustomer customer)
        {
            customer.gameObject.SetActive(false);
            poolQueue.Enqueue(customer);
        }
    }
}