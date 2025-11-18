/*
 * NPC HAVUZ YÖNETİCİSİ (NPC POOLER)
 * GÖREVİ:
 * 1. Bir Singleton'dır (Merkezi Erişim Noktası).
 * 2. 'Awake()'te, sahnedeki TÜM 'NpcHousing' (Ev) script'lerini bulur
 * ve onlardan ihtiyaç listesini ('NpcHousingData') toplar.
 * 3. 'Start()' metodunda, toplanan bu ihtiyaçlara göre
 * (kaç Goblin, kaç Köylü) tüm havuzları 'Instantiate' ederek
 * yaratır ve 'SetActive(false)' yapar.
 * 4. 'NpcHousing' script'lerinin kullanması için 'SpawnFromPool'
 * metodu sunar.
 * 5. Bu sistem 'Enemy' 'ObjectPooler'ından tamamen bağımsızdır.
 */

using UnityEngine;
using System.Collections.Generic;

public class NpcPooler : MonoBehaviour
{
    public static NpcPooler Instance { get; private set; }

    // Havuzları saklamak için bir yapı
    // Key = Prefab'ın adı (veya 'poolTag')
    // Value = O prefab'dan yaratılmış ve şu an pasif olan NPC'lerin Kuyruğu
    private Dictionary<string, Queue<FriendlyNpcAI>> poolDictionary;
    
    // NPC'lerin Hiyerarşi'de düzenli durması için
    private Transform poolParent;

    private void Awake()
    {
        // Singleton Kurulumu
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        poolDictionary = new Dictionary<string, Queue<FriendlyNpcAI>>();
        poolParent = new GameObject("NpcPool").transform;
        
        // Gerekli sayıda NPC'yi hesapla
        CalculateTotalNeeds();
    }

    /// <summary>
    /// Sahnedeki tüm 'NpcHousing' evlerini bularak
    /// toplam havuz ihtiyacını hesaplar.
    /// </summary>
    private void CalculateTotalNeeds()
    {
        // Geçici bir sözlük (Dictionary) kullanarak hangi prefab'dan
        // kaç tane gerektiğini hesaplayalım.
        Dictionary<string, (GameObject prefab, int count)> needs = 
            new Dictionary<string, (GameObject, int)>();

        // 1. Sahnedeki tüm Evleri bul
        NpcHousing[] allHouses = FindObjectsOfType<NpcHousing>();
        
        Debug.Log($"NpcPooler: {allHouses.Length} adet Ev bulundu. İhtiyaçlar hesaplanıyor...");

        foreach (NpcHousing house in allHouses)
        {
            NpcHousingData data = house.GetHousingData();
            if (data == null || data.genericNpcPrefab == null)
            {
                 Debug.LogWarning($"Ev ({house.name}) 'Housing Data'sı veya 'Prefab'ı atanmamış.", house);
                 continue;
            }

            string poolTag = data.genericNpcPrefab.name;
            int count = data.populationCount;

            if (!needs.ContainsKey(poolTag))
            {
                // Bu prefab (örn: 'GenericNpc.prefab')
                // listeye ilk defa ekleniyor
                needs.Add(poolTag, (data.genericNpcPrefab, count));
            }
            else
            {
                // Bu prefab zaten listedeydi (örn: başka bir ev de
                // 'GenericNpc.prefab' kullanıyor), sayısını artır
                needs[poolTag] = (data.genericNpcPrefab, needs[poolTag].count + count);
            }
        }
        
        // 2. Havuzları Ön-Yükle (Prewarm)
        // 'Start()' içinde değil 'Awake' sonunda yapıyoruz ki,
        // Evler 'Start()' dediğinde havuz hazır olsun.
        PrewarmPools(needs);
    }

    /// <summary>
    /// Hesaplanan ihtiyaçlara göre tüm NPC'leri 'Instantiate' eder
    /// ve pasif olarak havuza atar.
    /// </summary>
    private void PrewarmPools(Dictionary<string, (GameObject prefab, int count)> needs)
    {
        foreach (var entry in needs)
        {
            string poolTag = entry.Key;
            GameObject prefab = entry.Value.prefab;
            int count = entry.Value.count;
            
            Debug.Log($"NpcPooler: '{poolTag}' havuzu {count} adet NPC ile yaratılıyor...");

            // Havuz için bir alt-obje oluştur (daha düzenli)
            Transform prefabParent = new GameObject(poolTag + " Pool").transform;
            prefabParent.SetParent(poolParent);
            
            // Havuz kuyruğunu (Queue) oluştur
            Queue<FriendlyNpcAI> npcQueue = new Queue<FriendlyNpcAI>();

            for (int i = 0; i < count; i++)
            {
                GameObject npcGO = Instantiate(prefab, prefabParent);
                FriendlyNpcAI ai = npcGO.GetComponent<FriendlyNpcAI>();
                
                // (Emin olmak için havuz arayüzünü uygulayıp uygulamadığını kontrol edelim)
                if (ai == null || !(ai is IPooledNpc))
                {
                     Debug.LogError($"'{prefab.name}' prefab'ı 'FriendlyNpcAI' " +
                                      "script'ini içermiyor veya 'IPooledNpc' arayüzünü uygulamıyor!", prefab);
                     Destroy(npcGO);
                     continue;
                }
                
                npcGO.SetActive(false); // Pasif olarak havuza ekle
                npcQueue.Enqueue(ai);
            }
            
            // Dolu kuyruğu ana sözlüğe ekle
            poolDictionary.Add(poolTag, npcQueue);
        }
    }

    /// <summary>
    /// Havuzdan bir NPC'yi çeker, aktive eder ve döndürür.
    /// </summary>
    public FriendlyNpcAI SpawnFromPool(string poolTag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolTag))
        {
            Debug.LogError($"NpcPooler: '{poolTag}' adında bir havuz bulunamadı!");
            return null;
        }

        if (poolDictionary[poolTag].Count == 0)
        {
            Debug.LogError($"NpcPooler: '{poolTag}' havuzu boşaldı! " +
                             "Daha fazla NPC yaratılmalı (Dinamik büyüme henüz eklenmedi).");
            return null;
        }

        // 1. Havuzdan çek
        FriendlyNpcAI npcToSpawn = poolDictionary[poolTag].Dequeue();
        
        // 2. Aktive et
        npcToSpawn.gameObject.SetActive(true);
        npcToSpawn.transform.position = position;
        npcToSpawn.transform.rotation = rotation;
        
        // 3. NPC'nin kendi 'Spawn' olayını tetikle
        (npcToSpawn as IPooledNpc).OnNpcSpawned();
        
        return npcToSpawn;
    }

    /// <summary>
    /// Bir NPC'yi havuza geri döndürür.
    /// (Not: v2.1 yapısında bu metot henüz çağrılmıyor,
    /// ama ileride sistem durdurulursa diye hazır)
    /// </summary>
    public void ReturnToPool(string poolTag, FriendlyNpcAI npc)
    {
        if (!poolDictionary.ContainsKey(poolTag))
        {
            Debug.LogWarning($"NpcPooler: '{poolTag}' havuzuna dönme isteği geldi " +
                             "ancak böyle bir havuz yok.");
            Destroy(npc.gameObject); // Havuz yoksa yok et
            return;
        }
        
        npc.gameObject.SetActive(false);
        npc.transform.SetParent(poolParent.Find(poolTag + " Pool")); // Hiyerarşide düzenle
        poolDictionary[poolTag].Enqueue(npc);
    }
}