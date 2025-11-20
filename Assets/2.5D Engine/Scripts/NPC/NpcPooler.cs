using UnityEngine;
using System.Collections.Generic;

public class NpcPooler : MonoBehaviour
{
    public static NpcPooler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        poolDictionary = new Dictionary<string, Queue<FriendlyNpcAI>>();
        poolParent = new GameObject("NpcPool").transform;
        
        // Eski sistemler için otomatik tarama
        CalculateTotalNeeds();
    }

    private Dictionary<string, Queue<FriendlyNpcAI>> poolDictionary;
    private Transform poolParent;

    // --- GEREKLİ YARDIMCI METOTLAR (Eski sistemin çalışması için) ---
    private void CalculateTotalNeeds()
    {
        Dictionary<string, (GameObject, int)> needs = new Dictionary<string, (GameObject, int)>();
        // Evleri ve Siloları tara
        NpcHousing[] allHouses = FindObjectsOfType<NpcHousing>();
        foreach (NpcHousing house in allHouses) AddNeedsFromData(needs, house.GetHousingData());
        SiloController[] allSilos = FindObjectsOfType<SiloController>();
        foreach (SiloController silo in allSilos) AddNeedsFromData(needs, silo.GetSiloData());
        
        PrewarmPools(needs);
    }
    
    // Data okuma yardımcıları
    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, NpcHousingData data) {
        if (data != null && data.genericNpcPrefab != null) AddToNeedsList(needs, data.genericNpcPrefab.name, data.genericNpcPrefab, data.populationCount);
    }
    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, SiloData data) {
        if (data != null && data.genericNpcPrefab != null) AddToNeedsList(needs, data.genericNpcPrefab.name, data.genericNpcPrefab, data.populationCount);
    }
    private void AddToNeedsList(Dictionary<string, (GameObject, int)> needs, string tag, GameObject prefab, int count) {
        if (!needs.ContainsKey(tag)) needs.Add(tag, (prefab, count));
        else { int current = needs[tag].Item2; needs[tag] = (prefab, current + count); }
    }
    private void PrewarmPools(Dictionary<string, (GameObject prefab, int count)> needs) {
        foreach (var entry in needs) CreatePool(entry.Key, entry.Value.prefab, entry.Value.count);
    }

    // --- KRİTİK METOT: CREATE POOL (MARKETİN ÇAĞIRDIĞI) ---
    public void CreatePool(string tag, GameObject prefab, int size)
    {
        // 1. Havuz yoksa oluştur
        if (!poolDictionary.ContainsKey(tag))
        {
            poolDictionary.Add(tag, new Queue<FriendlyNpcAI>());
            // Parent düzeni
            Transform t = new GameObject(tag + " Pool").transform;
            t.SetParent(poolParent);
        }
        
        // Parent'ı bul (NpcPool -> Tag Pool)
        Transform specificPoolParent = poolParent.Find(tag + " Pool");
        if (specificPoolParent == null) specificPoolParent = poolParent;

        // 2. Havuza EKLEME yap (Mevcut sayıyı umursamaz, +size kadar ekler)
        for (int i = 0; i < size; i++)
        {
            GameObject npcGO = Instantiate(prefab, specificPoolParent);
            FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
            if (ai != null)
            {
                npcGO.SetActive(false);
                poolDictionary[tag].Enqueue(ai);
            }
            else Destroy(npcGO);
        }
        // Debug.Log($"NpcPooler: {tag} havuzuna {size} NPC eklendi.");
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