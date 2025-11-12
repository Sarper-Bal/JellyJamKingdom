using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// YENİ: WaveManager'a erişim için eklendi
using IndianOceanAssets.Engine2_5D; 

public class ObjectPooler : MonoBehaviour
{
    #region Singleton
    public static ObjectPooler Instance;

    private void Awake()
    {
        Instance = this;
    }
    #endregion

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        
        // --- YENİ EKLENEN KISIM BAŞLANGICI ---
        [Tooltip("Eğer bu 'True' ise, 'size' değeri Inspector'dan okunmaz. " +
                 "Bunun yerine 'WaveManager'a bağlanarak gerekli düşman sayısı otomatik hesaplanır. " +
                 "Şu anda SADECE 'enemy' tag'i için geçerlidir.")]
        public bool autoCalculateSize = false;
        // --- YENİ EKLENEN KISIM SONU ---
    }

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            // --- YENİ EKLENEN KISIM BAŞLANGICI (Otomatik Boyutlandırma) ---
            
            // Eğer bu havuz 'autoCalculateSize' olarak işaretlendiyse...
            if (pool.autoCalculateSize)
            {
                // ...ve tag'i 'enemy' ise...
                if (pool.tag == "enemy")
                {
                    // WaveManager'ın 'Awake' metodunda hesapladığı değere eriş.
                    if (WaveManager.Instance != null)
                    {
                        pool.size = WaveManager.Instance.CalculatedEnemyPoolSize;
                    }
                    else
                    {
                        // WaveManager sahnede yoksa veya bir hata olduysa
                        Debug.LogError("'enemy' havuzu 'autoCalculateSize' olarak ayarlandı ancak sahnede WaveManager bulunamadı! " +
                                       "Havuz boyutu '10' olarak ayarlanıyor.");
                        pool.size = 10; // Güvenli varsayılan
                    }
                }
                else
                {
                    // 'projectile' veya 'explosion' gibi başka bir tag ise
                    Debug.LogWarning($"'{pool.tag}' havuzu 'autoCalculateSize' olarak ayarlandı, " +
                                     "ancak bu özellik şu an sadece 'enemy' tag'i için destekleniyor. " +
                                     "Inspector'daki 'size' değeri ({pool.size}) kullanılacak.");
                }
            }
            // --- YENİ EKLENEN KISIM SONU ---


            // Havuzu oluştur
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // 'pool.size' (ya Inspector'dan gelen ya da otomatik hesaplanan) kadar obje oluştur
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }
        
        // --- DEĞİŞİKLİK: Havuzun boşalması durumunda daha net uyarı ---
        if (poolDictionary[tag].Count == 0)
        {
            // Eğer 'enemy' havuzu boşaldıysa, bu ciddi bir hatadır (hesaplama yanlış demektir).
            // Diğer havuzlar (mermi, efekt) için boyut arttırılması uyarısı verilebilir.
            if (tag == "enemy")
            {
                 Debug.LogError($"'enemy' havuzu boşaldı! (Pool with tag {tag} is empty). " +
                                "WaveManager'daki hesaplama yetersiz kalmış olabilir veya düşmanlar havuza dönmüyor olabilir.");
            }
            else
            {
                 Debug.LogWarning($"Pool with tag {tag} is empty. Consider increasing pool size in Inspector.");
            }
            return null; // Boşsa null dön
        }
        // --- DEĞİŞİKLİK SONU ---

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Objenin bir "havuzlanabilir obje" olup olmadığını kontrol et.
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            // Eğer öyleyse, ona kendi etiketini söyle ve spawn olduğunu haber ver.
            pooledObj.PoolTag = tag;
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    // YENİ FONKSİYON: Bir objeyi havuza geri almak için kullanılır.
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return;
        }

        // Obje kapatılır ve tekrar kuyruğa eklenir.
        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }
}