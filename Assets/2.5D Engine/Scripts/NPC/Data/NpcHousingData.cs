/*
 * NPC HOUSING DATA - v5.0 (Üretim Tarifi)
 * * DEĞİŞİKLİKLER (v5.0):
 * - 'requiresConversion' (bool): Bu ev bir fabrika mı?
 * - 'conversionRate' (int): 1 ürün için kaç hammadde lazım?
 * - 'conversionTime' (float): Üretim süresi.
 * - 'producedResource' (ResourceData): Çıktı (Ürün) zaten vardı, aynen kullanıyoruz.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewHousingData", menuName = "Stats/NPC Housing Data")]
public class NpcHousingData : ScriptableObject
{
    [Header("Spawn Ayarları")]
    public GameObject genericNpcPrefab;
    public FriendlyNpcData npcDataToSpawn;
    public int populationCount = 3;
    public float spawnInterval = 1.5f;

    [Header("Davranış Ayarları")]
    public float restDuration = 3.0f;
    
    [Header("Ekonomi ve Üretim")]
    [Tooltip("Bu evin ürettiği SON ÜRÜN (Çıktı).")]
    public ResourceData producedResource; 
    
    // --- DEĞİŞİKLİK BAŞLANGICI ---
    [Header("Dönüşüm / Fabrika Ayarları")]
    [Tooltip("Eğer işaretliyse, bu ev topladığı kaynakları işleyerek 'Produced Resource'a dönüştürür.")]
    public bool requiresConversion = false;

    [Tooltip("1 adet son ürün üretmek için kaç adet hammadde (NPC'lerin topladığı) gerekiyor?")]
    [Min(1)]
    public int conversionRate = 3; 

    [Tooltip("Bir adet ürünü dönüştürmek için gereken süre (saniye).")]
    public float conversionTime = 2.0f;
    // --- DEĞİŞİKLİK SONU ---
}