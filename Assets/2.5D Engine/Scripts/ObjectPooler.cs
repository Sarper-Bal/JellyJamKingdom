/*
 * OBJECT POOLER (HİBRİT MODEL)
 * * DEĞİŞİKLİKLER (Hata Düzeltmesi):
 * - 'Pool' sınıfı ve 'pools' listesi (Inspector'da görünen) geri getirildi.
 * - 'Start()' metodu, 'pools' listesindeki 'autoCalculateSize' olarak İŞARETLENMEMİŞ
 * tüm statik havuzları (projectile, explosion vb.) oluşturacak şekilde güncellendi.
 * - 'CreatePool' metodu, 'Start' içinden de kullanılabilecek şekilde korundu.
 * - 'WaveManager' hala 'CreatePool("enemy", ...)' çağrısını yaparak DİNAMİK havuzu oluşturacak.
 * - 'RoundManager' hala 'DestroyPool("enemy")' çağrısını yaparak DİNAMİK havuzu yok edecek.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    #region Singleton
    public static ObjectPooler Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Sözlüğü (Dictionary) hemen 'Awake' içinde başlat.
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
    }
    #endregion

    // --- DEĞİŞİKLİK: 'Pool' sınıfı ve 'pools' listesi geri getirildi ---
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        
        [Tooltip("EĞER BU İŞARETLENİRSE, bu havuz 'Start'ta OLUŞTURULMAZ. " +
                 "Bunun yerine 'WaveManager' gibi bir yöneticinin 'CreatePool' komutu vermesi beklenir.")]
        public bool isDynamicallyManaged = false; // 'autoCalculateSize' idi, ismi netleşti.
    }

    [Tooltip("Oyun başında oluşturulacak STATİK havuzlar (Projectile, Effects vb.)")]
    public List<Pool> pools;
    // --- DEĞİŞİKLİK SONU ---

    private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Start()
    {
        // --- DEĞİŞİKLİK: Statik havuzları oluşturma ---
        
        // Inspector'dan atanan 'pools' listesini döngüye al
        foreach (Pool pool in pools)
        {
            // Eğer havuz 'Dinamik' olarak işaretlendiyse (örn: "enemy" havuzu),
            // bu havuzu 'Start'ta oluşturma. WaveManager'ın oluşturmasını bekle.
            if (pool.isDynamicallyManaged)
            {
                continue; 
            }

            // 'projectile', 'explosion' gibi statik havuzları oluştur.
            CreatePool(pool.tag, pool.prefab, pool.size);
        }
        // --- DEĞİŞİKLİK SONU ---
    }

    /// <summary>
    /// Belirtilen 'tag' için yeni bir obje havuzu oluşturur.
    /// Statik havuzlar için 'Start()'ta, dinamik havuzlar için 'WaveManager' tarafından çağrılır.
    /// </summary>
    public void CreatePool(string tag, GameObject prefab, int size)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"ObjectPooler: '{tag}' etiketine sahip bir havuz zaten mevcut. " +
                             "Yeni havuz oluşturma işlemi iptal edildi.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError($"ObjectPooler: '{tag}' etiketi için havuz oluşturulmak istendi " +
                           "ancak 'prefab' atanmamış (null)! Havuz oluşturulamadı.");
            return;
        }

        Queue<GameObject> objectPool = new Queue<GameObject>();

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false); 
            objectPool.Enqueue(obj); 
        }

        poolDictionary.Add(tag, objectPool);
        
        // (Bu log'u statik havuzlar için de göreceğiz)
        Debug.Log($"ObjectPooler: '{tag}' havuzu, {size} adet '{prefab.name}' objesi ile başarıyla oluşturuldu.");
    }
    
    /// <summary>
    /// Belirtilen 'tag'e sahip DİNAMİK havuzu ve içindeki tüm GameObjec'leri yok eder.
    /// </summary>
    public void DestroyPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"ObjectPooler: '{tag}' etiketine sahip bir havuz bulunamadı. " +
                             "Yok etme işlemi iptal edildi.");
            return;
        }

        Queue<GameObject> objectPool = poolDictionary[tag];

        int destroyedCount = 0;
        while (objectPool.Count > 0)
        {
            GameObject objToDestroy = objectPool.Dequeue();
            Destroy(objToDestroy);
            destroyedCount++;
        }

        poolDictionary.Remove(tag);
        
        Debug.Log($"ObjectPooler: '{tag}' havuzu temizlendi. {destroyedCount} adet obje yok edildi.");
    }


    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }
        
        if (poolDictionary[tag].Count == 0)
        {
            // Statik havuzlar (mermi vb.) için bu uyarı normaldir.
            if (tag != "enemy")
            {
                Debug.LogWarning($"Pool with tag '{tag}' is empty. 'Size' değerini " +
                                 $"ObjectPooler Inspector'ından arttırmayı düşünün.");
            }
            else
            {
                // "enemy" havuzu için bu kritik bir hatadır.
                Debug.LogError($"Pool with tag '{tag}' is EMPTY! " +
                           "Hesaplanan havuz boyutu yetersiz veya düşmanlar havuza dönmüyor (ReturnToPool).");
            }
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.PoolTag = tag;
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        // Havuz tur sonunda yok edildiyse, dönmeye çalışan objeyi Destroy et.
        if (!poolDictionary.ContainsKey(tag))
        {
            Destroy(objectToReturn);
            return;
        }

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }
}