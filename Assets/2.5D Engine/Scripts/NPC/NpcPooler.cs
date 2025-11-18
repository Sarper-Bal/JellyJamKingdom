/*
 * NPC HAVUZ YÖNETİCİSİ (NPC POOLER) - v1.4 (Market Desteği)
 * * DEĞİŞİKLİKLER (v1.4):
 * - 'CalculateTotalNeeds' metoduna 'MarketController' taraması eklendi.
 * - 'AddNeedsFromData' metoduna 'MarketData' overload'ı eklendi.
 */

using UnityEngine;
using System.Collections.Generic;

public class NpcPooler : MonoBehaviour
{
    public static NpcPooler Instance { get; private set; }

    private Dictionary<string, Queue<FriendlyNpcAI>> poolDictionary;
    private Transform poolParent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        poolDictionary = new Dictionary<string, Queue<FriendlyNpcAI>>();
        poolParent = new GameObject("NpcPool").transform;
        CalculateTotalNeeds();
    }

    private void CalculateTotalNeeds()
    {
        Dictionary<string, (GameObject, int)> needs = new Dictionary<string, (GameObject, int)>();

        // 1. Evler
        NpcHousing[] allHouses = FindObjectsOfType<NpcHousing>();
        foreach (NpcHousing house in allHouses) AddNeedsFromData(needs, house.GetHousingData());
        
        // 2. Silolar
        SiloController[] allSilos = FindObjectsOfType<SiloController>();
        foreach (SiloController silo in allSilos) AddNeedsFromData(needs, silo.GetSiloData());
        
        // --- DEĞİŞİKLİK: 3. Marketler ---
        MarketController[] allMarkets = FindObjectsOfType<MarketController>();
        foreach (MarketController market in allMarkets) AddNeedsFromData(needs, market.GetMarketData());
        // --------------------------------

        PrewarmPools(needs);
    }

    // --- Overloads ---
    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, NpcHousingData data)
    {
        if (data == null || data.genericNpcPrefab == null) return;
        AddToNeedsList(needs, data.genericNpcPrefab.name, data.genericNpcPrefab, data.populationCount);
    }

    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, SiloData data)
    {
        if (data == null || data.genericNpcPrefab == null) return;
        AddToNeedsList(needs, data.genericNpcPrefab.name, data.genericNpcPrefab, data.populationCount);
    }

    // --- DEĞİŞİKLİK: MarketData Overload ---
    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, MarketData data)
    {
        if (data == null || data.genericNpcPrefab == null) return;
        AddToNeedsList(needs, data.genericNpcPrefab.name, data.genericNpcPrefab, data.populationCount);
    }
    // ---------------------------------------

    private void AddToNeedsList(Dictionary<string, (GameObject, int)> needs, string tag, GameObject prefab, int count)
    {
        if (!needs.ContainsKey(tag)) needs.Add(tag, (prefab, count));
        else
        {
            int currentCount = needs[tag].Item2;
            needs[tag] = (prefab, currentCount + count);
        }
    }
    
    // (SpawnFromPool, ReturnToPool, PrewarmPools - Değişiklik yok)
    #region Existing Methods (No Change)
    private void PrewarmPools(Dictionary<string, (GameObject prefab, int count)> needs)
    {
        foreach (var entry in needs)
        {
            string poolTag = entry.Key;
            GameObject prefab = entry.Value.prefab;
            int count = entry.Value.count;
            Transform prefabParent = new GameObject(poolTag + " Pool").transform;
            prefabParent.SetParent(poolParent);
            Queue<FriendlyNpcAI> npcQueue = new Queue<FriendlyNpcAI>();
            for (int i = 0; i < count; i++)
            {
                GameObject npcGO = Instantiate(prefab, prefabParent);
                FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
                if (ai != null) { npcGO.SetActive(false); npcQueue.Enqueue(ai); }
                else { Destroy(npcGO); }
            }
            poolDictionary.Add(poolTag, npcQueue);
        }
    }

    public FriendlyNpcAI SpawnFromPool(string poolTag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolTag) || poolDictionary[poolTag].Count == 0) return null;
        FriendlyNpcAI npcToSpawn = poolDictionary[poolTag].Dequeue();
        npcToSpawn.transform.position = position;
        npcToSpawn.transform.rotation = rotation;
        npcToSpawn.gameObject.SetActive(true);
        if(npcToSpawn is IPooledNpc pooledNpc) pooledNpc.OnNpcSpawned();
        return npcToSpawn;
    }

    public void ReturnToPool(string poolTag, FriendlyNpcAI npc)
    {
        if (!poolDictionary.ContainsKey(poolTag)) { Destroy(npc.gameObject); return; }
        npc.gameObject.SetActive(false);
        Transform specificPoolParent = poolParent.Find(poolTag + " Pool");
        if (specificPoolParent != null) npc.transform.SetParent(specificPoolParent);
        poolDictionary[poolTag].Enqueue(npc);
    }
    #endregion
}