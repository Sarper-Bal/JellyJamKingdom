/*
 * SILO VERİSİ (SCRIPTABLE OBJECT)
 * GÖREVİ:
 * Silo binasına özel verileri tutar.
 * Başlangıçta 'NpcHousingData' ile aynı yapıdadır, ancak ayrıştırılmıştır.
 * İleride 'MaxStorageCapacity' gibi Silo'ya özel statlar buraya eklenebilir.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewSiloData", menuName = "Stats/Silo Data")]
public class SiloData : ScriptableObject
{
    [Header("Spawn Ayarları")]
    [Tooltip("Silo'dan spawn edilecek taşıyıcı NPC prefab'ı.")]
    public GameObject genericNpcPrefab;
    
    [Tooltip("Spawn edilecek NPC'lerin kullanacağı stat verisi (Hız vb.).")]
    public FriendlyNpcData npcDataToSpawn;

    [Tooltip("Silo'da çalışan toplam taşıyıcı NPC sayısı.")]
    public int populationCount = 5;

    [Tooltip("NPC'lerin teker teker çıkması için aradaki saniye farkı.")]
    public float spawnInterval = 1.0f;

    [Header("Davranış Ayarları")]
    [Tooltip("NPC'lerin işten döndükten sonra tekrar çıkmadan önce dinlenme süresi.")]
    public float restDuration = 2.0f;
}