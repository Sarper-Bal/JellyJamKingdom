/*
 * NPC EV VERİSİ (SCRIPTABLE OBJECT)
 * GÖREVİ:
 * Bu, bir evin "veri" kısmını tanımlar. Hangi NPC'yi,
 * kaç tane ve hangi aralıkla spawn edeceğini belirler.
 *
 * KULLANICI İSTEĞİ: 'JobType' (davranış) bu script'e DEĞİL,
 * 'NpcHousing' (motor) script'ine konulacaktır.
 */

using UnityEngine;

// Unity'nin "Assets/Create" menüsüne yeni bir seçenek ekler.
[CreateAssetMenu(fileName = "NewHousingData", menuName = "Stats/NPC Housing Data")]
public class NpcHousingData : ScriptableObject
{
    [Header("Spawn Ayarları")]
    [Tooltip("Bu evden spawn edilecek 'GenericNpc.prefab' (FriendlyNpcAI script'li).")]
    public GameObject genericNpcPrefab;
    
    [Tooltip("Spawn edilecek NPC'lerin kullanacağı stat verisi (Data_KoylU vb.).")]
    public FriendlyNpcData npcDataToSpawn;

    [Tooltip("Bu evde yaşayan ve spawn edilecek toplam NPC sayısı.")]
    public int populationCount = 3;

    [Tooltip("NPC'lerin evden teker teker çıkması için aradaki saniye farkı.")]
    public float spawnInterval = 1.5f;

    [Header("Davranış Ayarları")]
    [Tooltip("NPC'lerin eve döndükten sonra tekrar işe gitmeden önce dinlenme süresi.")]
    public float restDuration = 3.0f;
}