/*
 * NPC EVİ - v5.0 (Üretim ve Dönüşüm Sistemi)
 * * * YENİ AKIŞ:
 * 1. Bu evin NPC'leri dışarı (ResourceTarget) gider, hammadde toplar.
 * 2. Eve döndüklerinde bu hammadde 'inputResourceCount' (Hammadde Deposu) içine eklenir.
 * 3. 'StartProduction' Coroutine'i arka planda çalışır:
 * - Yeterli hammadde (conversionRate) var mı bakar.
 * - Varsa hammaddeyi siler, bekler ve 'tasksCompletedCounter' (Ürün Deposu) artırır.
 * 4. Silo'dan gelen taşıyıcılar, 'DecreaseCounter' ile SADECE üretilmiş ürünleri (Ürün Deposu) alır.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NpcHousing : MonoBehaviour
{
    [Header("Veri")]
    [SerializeField] private NpcHousingData housingData;
    
    [Header("Davranış")]
    [SerializeField] private NpcJobType jobType = NpcJobType.GatherResource; 
    [SerializeField] private WorkSpotInteractable resourceTarget;
    [SerializeField] public NpcHousing houseTarget; 
    
    [Header("Konum")]
    [SerializeField] private Transform spawnPoint; 
    [SerializeField] private NpcPath optionalNpcPath; 
    
    [Header("Stoklar (Sadece İzleme)")]
    // Silo'nun gelip alacağı ÜRETİLMİŞ ÜRÜN stoğu
    [Tooltip("Silo'nun alacağı işlenmiş ürün stoğu.")]
    [SerializeField] private int outputProductCount = 0; // Eski 'tasksCompletedCounter'

    // --- DEĞİŞİKLİK BAŞLANGICI (Hammadde Deposu) ---
    // Kendi NPC'lerimizin toplayıp getirdiği HAMMADDE stoğu
    [Tooltip("NPC'lerin toplayıp getirdiği işlenmemiş hammadde stoğu.")]
    [SerializeField] private int inputRawMaterialCount = 0;
    
    // Üretim durumunu kontrol etmek için
    private bool isProducing = false;
    // --- DEĞİŞİKLİK SONU ---
    
    public enum NpcJobType { GatherResource, TransferResource }
    public event System.Action<FriendlyNpcAI, NpcHousing> OnNpcReadyToWork;
    private List<FriendlyNpcAI> managedNpcs = new List<FriendlyNpcAI>();

    private void Start()
    {
        if (housingData == null) return;
        
        StartCoroutine(SpawnNpcs());
        
        // --- DEĞİŞİKLİK: Üretim Hattını Başlat ---
        if (housingData.requiresConversion)
        {
            StartCoroutine(ProductionRoutine());
        }
        // -----------------------------------------
    }

    // --- YENİ: ÜRETİM DÖNGÜSÜ ---
    private IEnumerator ProductionRoutine()
    {
        while (true)
        {
            // 1. Yeterli hammadde var mı?
            if (inputRawMaterialCount >= housingData.conversionRate)
            {
                isProducing = true;
                
                // 2. Üretim süresi kadar bekle
                yield return new WaitForSeconds(housingData.conversionTime);
                
                // 3. Tekrar kontrol et (Beklerken hammadde çalınmış/silinmiş olabilir mi? Zor ama güvenli olsun)
                if (inputRawMaterialCount >= housingData.conversionRate)
                {
                    // 4. Dönüşümü yap: Hammaddeyi sil -> Ürünü ekle
                    inputRawMaterialCount -= housingData.conversionRate;
                    outputProductCount++; // 1 Ürün üretildi
                    
                    // (Opsiyonel: Buraya bir "Duman Efekti" veya "Ses" ekleyebilirsiniz)
                    // Debug.Log($"FABRİKA ({name}): 1 {housingData.producedResource.name} üretildi! Kalan Hammadde: {inputRawMaterialCount}");
                }
            }
            else
            {
                isProducing = false;
                // Hammadde yoksa biraz bekle, işlemciyi yorma
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
    // -----------------------------

    private IEnumerator SpawnNpcs()
    {
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Transform home = (spawnPoint != null) ? spawnPoint : transform;
        string tag = housingData.genericNpcPrefab.name;

        for (int i = 0; i < housingData.populationCount; i++)
        {
            FriendlyNpcAI ai = NpcPooler.Instance.SpawnFromPool(tag, pos, Quaternion.identity);
            if (ai != null)
            {
                OnNpcReadyToWork?.Invoke(ai, this);
                Transform work = DetermineWorkTarget();
                ai.Activate(housingData.npcDataToSpawn, home, work, optionalNpcPath); 
                
                ai.OnArrivedAtWork -= HandleNpcArrivedAtWork; 
                ai.OnArrivedAtHome -= HandleNpcArrivedAtHome;
                ai.OnArrivedAtWork += HandleNpcArrivedAtWork;
                ai.OnArrivedAtHome += HandleNpcArrivedAtHome;
                
                managedNpcs.Add(ai);
            }
            yield return new WaitForSeconds(housingData.spawnInterval);
        }
    }
    
    private Transform DetermineWorkTarget()
    {
        if (jobType == NpcJobType.GatherResource && resourceTarget != null)
            return (resourceTarget.interactionPoint != null) ? resourceTarget.interactionPoint : resourceTarget.transform;
        else if (jobType == NpcJobType.TransferResource && houseTarget != null)
            return houseTarget.GetSpawnPoint();
        return transform; 
    }
    
    private void HandleNpcArrivedAtWork(FriendlyNpcAI npc)
    {
        FriendlyNpcData data = npc.GetNpcData();
        if (data == null) { npc.ReturnHome(0, null); return; }
        int capacity = data.maxCarryCapacity; 

        if (jobType == NpcJobType.GatherResource)
        {
            // Toplama: Hammaddeyi (ResourceTarget'tan) topluyoruz
            // Not: Burada hangi tip topladığımızı bilmek için ResourceTarget'a da bir ResourceData ekleyebilirdik
            // ama şimdilik 'producedResource'un hammaddesini topladığını varsayıyoruz.
            // İleride WorkSpotInteractable'a da 'resourceType' eklenebilir.
            StartCoroutine(WorkCycle(npc, capacity, null)); 
        }
        else if (jobType == NpcJobType.TransferResource)
        {
            int collected = 0;
            ResourceData resource = null;

            if (houseTarget != null)
            {
                // Transfer: Hedef evden ÜRETİLMİŞ ÜRÜNÜ alıyoruz
                collected = houseTarget.DecreaseCounter(capacity);
                if (collected > 0)
                {
                    resource = houseTarget.GetProducedResource();
                }
            }
            npc.ReturnHome(collected, resource); 
        }
    }
    
    private void HandleNpcArrivedAtHome(FriendlyNpcAI npc, int amount, ResourceData resource)
    {
        if (amount > 0)
        {
            // --- DEĞİŞİKLİK: Hammadde mi, Ürün mü? ---
            if (housingData.requiresConversion)
            {
                // Eğer bu ev bir fabrikaysa, gelen her şey HAMMADDEDİR.
                inputRawMaterialCount += amount;
                // Debug.Log($"Ev ({name}): {amount} hammadde geldi. Depo: {inputRawMaterialCount}");
            }
            else
            {
                // Fabrika değilse (basit toplama), direkt ürün deposuna gider.
                outputProductCount += amount;
                // Debug.Log($"Ev ({name}): {amount} ürün geldi. Toplam: {outputProductCount}");
            }
            // ------------------------------------------
        }
        StartCoroutine(RestCycle(npc, housingData.restDuration));
    }
    
    private IEnumerator WorkCycle(FriendlyNpcAI npc, int capacity, ResourceData resource)
    {
        if (resourceTarget != null) resourceTarget.TriggerInteraction();
        yield return new WaitForSeconds(resourceTarget.workDuration);
        if(npc != null) 
        {
            // Toplama yaparken, henüz "işlenmemiş" olduğu için resource tipini null gönderebiliriz
            // veya housingData'daki tipi gönderebiliriz, ama mantık Ev içinde 'inputRaw'a ekleneceği için fark etmez.
            npc.ReturnHome(capacity, resource);
        }
    }
    
    private IEnumerator RestCycle(FriendlyNpcAI npc, float duration)
    {
        yield return new WaitForSeconds(duration);
        if(npc != null)
        {
            OnNpcReadyToWork?.Invoke(npc, this);
            Transform newWork = DetermineWorkTarget();
            npc.Activate(housingData.npcDataToSpawn, (spawnPoint != null ? spawnPoint : transform), newWork, optionalNpcPath);
        }
    }
    
    public NpcHousingData GetHousingData() { return housingData; }
    
    // Silo buradan "Hazır Ürün" sayısını okur
    public int GetResourceCount() { return outputProductCount; }
    
    public ResourceData GetProducedResource() 
    { 
        return housingData != null ? housingData.producedResource : null; 
    }

    public Transform GetSpawnPoint() { return (spawnPoint != null) ? spawnPoint : transform; }

    // Dışarıdan müdahale (Hile vb.) için
    public void IncreaseCounter(int amount) { outputProductCount += amount; }

    // Silo'nun ürün aldığı metot
    public int DecreaseCounter(int amountToTake)
    {
        if (outputProductCount == 0) { return 0; }
        int actual = Mathf.Min(outputProductCount, amountToTake);
        outputProductCount -= actual;
        return actual;
    }
}