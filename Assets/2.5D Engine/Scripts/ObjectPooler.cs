using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    #region Singleton
    public static ObjectPooler Instance;
    private void Awake()
    {
        Instance = this;

        // YENİ MANTIK: Sözlükler, diğer her şeyden önce, Awake'te oluşturuluyor.
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolInfoDictionary = new Dictionary<string, Pool>();
    }
    #endregion

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public bool canExpand = true;
    }

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolInfoDictionary;

    // Start fonksiyonu artık sadece Inspector'dan eklenen manuel havuzları oluşturuyor.
    void Start()
    {
        foreach (Pool pool in pools)
        {
            CreatePool(pool.tag, pool.prefab, pool.size, pool.canExpand);
        }
    }

    public void CreatePool(string tag, GameObject prefab, int size, bool canExpand)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            return;
        }
        if (prefab == null)
        {
            Debug.LogError($"'{tag}' etiketi için Prefab atanmamış. Lütfen WaveProfile dosyasını kontrol edin.");
            return;
        }

        Pool newPoolInfo = new Pool { tag = tag, prefab = prefab, size = size, canExpand = canExpand };
        poolInfoDictionary.Add(tag, newPoolInfo);

        Queue<GameObject> objectPool = new Queue<GameObject>();
        for (int i = 0; i < size; i++)
        {
            AddObjectToPool(tag, objectPool);
        }

        poolDictionary.Add(tag, objectPool);
    }

    private GameObject AddObjectToPool(string tag, Queue<GameObject> targetQueue)
    {
        GameObject obj = Instantiate(poolInfoDictionary[tag].prefab);
        obj.SetActive(false);
        targetQueue.Enqueue(obj);
        return obj;
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
            if (poolInfoDictionary[tag].canExpand)
            {
                Debug.LogWarning($"Pool with tag {tag} was empty. Expanding pool size.");
                AddObjectToPool(tag, poolDictionary[tag]);
            }
            else
            {
                Debug.LogWarning("Pool with tag " + tag + " is empty and can't expand.");
                return null;
            }
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
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return;
        }

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }
}