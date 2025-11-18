/*
 * NPC HAVUZ YÖNETİCİSİ (NPC POOLER) - v1.2 (Tuple Hatası Düzeltildi)
 * GÖREVİ:
 * - Sahnedeki Evleri ve Siloları tarar.
 * - İhtiyaç duyulan toplam NPC sayısını hesaplar.
 * - Oyun başında (Awake/Start) tüm NPC'leri yaratıp pasif havuza atar.
 *
 * * HATA DÜZELTMESİ (v1.2):
 * - 'needs[poolTag].count' satırındaki derleme hatası giderildi.
 * - Tuple elemanına erişirken garanti yöntem olan '.Item2' kullanıldı.
 */

using UnityEngine;
using System.Collections.Generic;

public class NpcPooler : MonoBehaviour
{
    public static NpcPooler Instance { get; private set; }

    // Key = Prefab Adı, Value = NPC Kuyruğu
    private Dictionary<string, Queue<FriendlyNpcAI>> poolDictionary;
    private Transform poolParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        poolDictionary = new Dictionary<string, Queue<FriendlyNpcAI>>();
        poolParent = new GameObject("NpcPool").transform;
        
        // İhtiyaçları hesapla ve havuzu doldur
        CalculateTotalNeeds();
    }

    private void CalculateTotalNeeds()
    {
        // (GameObject, int) türünde bir Tuple sözlüğü
        // Item1 = Prefab, Item2 = Sayı
        Dictionary<string, (GameObject, int)> needs = 
            new Dictionary<string, (GameObject, int)>();

        // 1. Evleri (NpcHousing) Tara
        NpcHousing[] allHouses = FindObjectsOfType<NpcHousing>();
        foreach (NpcHousing house in allHouses)
        {
            AddNeedsFromData(needs, house.GetHousingData(), house.name);
        }
        
        // 2. Siloları (SiloController) Tara
        SiloController[] allSilos = FindObjectsOfType<SiloController>();
        foreach (SiloController silo in allSilos)
        {
            AddNeedsFromData(needs, silo.GetHousingData(), silo.name);
        }
        
        // Havuzları oluştur
        PrewarmPools(needs);
    }

    private void AddNeedsFromData(Dictionary<string, (GameObject, int)> needs, NpcHousingData data, string ownerName)
    {
        if (data == null || data.genericNpcPrefab == null)
        {
             return;
        }

        string poolTag = data.genericNpcPrefab.name;
        int count = data.populationCount;

        if (!needs.ContainsKey(poolTag))
        {
            // Listeye yeni ekle
            needs.Add(poolTag, (data.genericNpcPrefab, count));
        }
        else
        {
            // --- DÜZELTME BURADA ---
            // Eski kod: needs[poolTag].count 
            // Yeni kod: needs[poolTag].Item2 (Item2 = int count demektir)
            int currentCount = needs[poolTag].Item2;
            
            needs[poolTag] = (data.genericNpcPrefab, currentCount + count);
        }
    }

    private void PrewarmPools(Dictionary<string, (GameObject prefab, int count)> needs)
    {
        foreach (var entry in needs)
        {
            string poolTag = entry.Key;
            // Tuple elemanlarına burada da Item1 ve Item2 diyebiliriz veya deconstruct edebiliriz
            GameObject prefab = entry.Value.prefab; // veya entry.Value.Item1
            int count = entry.Value.count;          // veya entry.Value.Item2
            
            Transform prefabParent = new GameObject(poolTag + " Pool").transform;
            prefabParent.SetParent(poolParent);
            
            Queue<FriendlyNpcAI> npcQueue = new Queue<FriendlyNpcAI>();

            for (int i = 0; i < count; i++)
            {
                GameObject npcGO = Instantiate(prefab, prefabParent);
                FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
                
                if (ai != null)
                {
                    npcGO.SetActive(false);
                    npcQueue.Enqueue(ai);
                }
                else
                {
                     Destroy(npcGO);
                }
            }
            poolDictionary.Add(poolTag, npcQueue);
        }
    }

    public FriendlyNpcAI SpawnFromPool(string poolTag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolTag) || poolDictionary[poolTag].Count == 0)
        {
            // Eğer havuz boşaldıysa ve acil lazımsa burada yeni yaratılabilir (Opsiyonel)
            return null;
        }

        FriendlyNpcAI npcToSpawn = poolDictionary[poolTag].Dequeue();
        
        npcToSpawn.transform.position = position;
        npcToSpawn.transform.rotation = rotation;
        npcToSpawn.gameObject.SetActive(true);
        
        if(npcToSpawn is IPooledNpc pooledNpc)
        {
            pooledNpc.OnNpcSpawned();
        }
        
        return npcToSpawn;
    }

    public void ReturnToPool(string poolTag, FriendlyNpcAI npc)
    {
        if (!poolDictionary.ContainsKey(poolTag))
        {
            Destroy(npc.gameObject);
            return;
        }
        
        npc.gameObject.SetActive(false);
        // Pool parent'ın altına geri taşı ki hiyerarşi temiz kalsın
        Transform specificPoolParent = poolParent.Find(poolTag + " Pool");
        if (specificPoolParent != null)
            npc.transform.SetParent(specificPoolParent);
            
        poolDictionary[poolTag].Enqueue(npc);
    }
}