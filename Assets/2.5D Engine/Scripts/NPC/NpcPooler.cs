using UnityEngine;
using System.Collections.Generic;

public class NpcPooler : MonoBehaviour
{
    public static NpcPooler Instance { get; private set; }

    private Dictionary<string, Queue<FriendlyNpcAI>> poolDictionary;
    private Dictionary<string, int> poolCapacities; 
    private Transform poolParent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        poolDictionary = new Dictionary<string, Queue<FriendlyNpcAI>>();
        poolCapacities = new Dictionary<string, int>(); 
        poolParent = new GameObject("NpcPool").transform;
        
        // İlk açılışta her şeyi hesapla
        RecalculateAndExpandPools();
    }

    [ContextMenu("Force Recalculate Needs")]
    public void RecalculateAndExpandPools()
    {
        Dictionary<string, (GameObject prefab, int count)> currentNeeds = new Dictionary<string, (GameObject, int)>();
        
        // 1. Evleri Tara
        NpcHousing[] allHouses = FindObjectsOfType<NpcHousing>();
        foreach (NpcHousing house in allHouses) AddNeedsFromData(currentNeeds, house.GetHousingData());
        
        // 2. Siloları Tara
        SiloController[] allSilos = FindObjectsOfType<SiloController>();
        foreach (SiloController silo in allSilos) AddNeedsFromData(currentNeeds, silo.GetSiloData());

        // 3. Marketleri Tara (YENİ EKLENDİ)
        SimpleMarketController[] allMarkets = FindObjectsOfType<SimpleMarketController>();
        foreach (SimpleMarketController market in allMarkets) AddNeedsFromData(currentNeeds, market.GetMarketData());

        // --- HESAPLAMA VE ÜRETİM ---
        foreach (var need in currentNeeds)
        {
            string tag = need.Key;
            GameObject prefab = need.Value.prefab;
            int requiredTotal = need.Value.count;

            int currentTotal = poolCapacities.ContainsKey(tag) ? poolCapacities[tag] : 0;

            if (requiredTotal > currentTotal)
            {
                int amountMissing = requiredTotal - currentTotal;
                Debug.Log($"NpcPooler: '{tag}' için {amountMissing} ek personel üretiliyor.");
                CreatePool(tag, prefab, amountMissing);
            }
        }
    }

    // --- YARDIMCI METOTLAR ---
    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, NpcHousingData data) {
        if (data != null && data.genericNpcPrefab != null) AddToNeedsList(needs, data.genericNpcPrefab.name, data.genericNpcPrefab, data.populationCount);
    }
    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, SiloData data) {
        if (data != null && data.genericNpcPrefab != null) AddToNeedsList(needs, data.genericNpcPrefab.name, data.genericNpcPrefab, data.populationCount);
    }
    // Market Verisi Okuma (YENİ)
    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, SimpleMarketData data) {
        // Marketin 1 tane daimi işçiye ihtiyacı vardır.
        if (data != null && data.workerPrefab != null) AddToNeedsList(needs, data.workerPoolTag, data.workerPrefab.gameObject, 1);
    }

    private void AddToNeedsList(Dictionary<string, (GameObject, int)> needs, string tag, GameObject prefab, int count) {
        if (!needs.ContainsKey(tag)) needs.Add(tag, (prefab, count));
        else { int current = needs[tag].Item2; needs[tag] = (prefab, current + count); }
    }

    public void CreatePool(string tag, GameObject prefab, int size)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            poolDictionary.Add(tag, new Queue<FriendlyNpcAI>());
            Transform t = new GameObject(tag + " Pool").transform;
            t.SetParent(poolParent);
        }
        
        if (!poolCapacities.ContainsKey(tag)) poolCapacities.Add(tag, 0);

        Transform specificPoolParent = poolParent.Find(tag + " Pool");
        if (specificPoolParent == null) specificPoolParent = poolParent;

        for (int i = 0; i < size; i++)
        {
            GameObject npcGO = Instantiate(prefab, specificPoolParent);
            FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
            if (ai != null)
            {
                npcGO.SetActive(false);
                poolDictionary[tag].Enqueue(ai);
                poolCapacities[tag]++; 
            }
            else Destroy(npcGO);
        }
    }

    public FriendlyNpcAI SpawnFromPool(string poolTag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolTag) || poolDictionary[poolTag].Count == 0) return null;

        FriendlyNpcAI npc = poolDictionary[poolTag].Dequeue();
        npc.transform.position = position;
        npc.transform.rotation = rotation;
        npc.gameObject.SetActive(true);
        
        if(npc is IPooledNpc p) p.OnNpcSpawned();
        return npc;
    }

    public void ReturnToPool(string poolTag, FriendlyNpcAI npc)
    {
        if (!poolDictionary.ContainsKey(poolTag)) { Destroy(npc.gameObject); return; }
        npc.gameObject.SetActive(false);
        poolDictionary[poolTag].Enqueue(npc);
    }
}