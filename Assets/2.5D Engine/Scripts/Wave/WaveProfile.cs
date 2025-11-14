using UnityEngine;
using System.Collections.Generic; 

/*
 * WAVE PROFILE (VERİ KATMANI)
 * * DEĞİŞİKLİKLER (v1.1 - Sınırlı Süre):
 * - 'SpawnEvent' sınıfına 'hasFiniteDuration' (bool) ve 'endTime' (float) eklendi.
 * - Bu, periyodik olayların tur sonundan önce bitmesini sağlayacak.
 */

/// <summary>
/// Bir dalga içindeki tek bir spawn olayını tanımlar.
/// [System.Serializable] sayesinde bu sınıfın değişkenlerini Unity Inspector'da görebileceğiz.
/// </summary>
[System.Serializable]
public class SpawnEvent
{
    [Tooltip("Bu olayda hangi düşman prefab'ının spawn olacağı.")]
    public GameObject enemyPrefab; 

    [Tooltip("Bu olayın hangi Spawn Point ID'sinde gerçekleşeceği.")]
    public int spawnPointID;

    [Tooltip("Bu olayın İLK defa tetikleneceği saniye (Round başından itibaren).")]
    public float triggerTime; 

    [Tooltip("Bu olay periyodik olarak tekrarlanacak mı? (İşaretlenmezse, 'triggerTime'da sadece 1 kez çalışır).")]
    public bool isPeriodic; 

    // --- DEĞİŞİKLİK BAŞLANGICI ---
    // Bu alanlar artık 'SpawnEventDrawer.cs' (Editor script'i) tarafından
    // 'isPeriodic' true ise gösterilecek.

    [Tooltip("EĞER periyodik ise, kaç saniyede bir tekrarlanacağı.")]
    public float repeatInterval = 1f; // (Varsayılan değer 0 olmamalı, 1f olarak güncellendi)

    [Tooltip("EĞER periyodik ise, bu seçenek spawn'ın 'endTime'da durmasını sağlar. " +
             "İşaretlenmezse, tur sonuna kadar devam eder.")]
    public bool hasFiniteDuration;

    [Tooltip("EĞER 'hasFiniteDuration' true ise, bu olayın periyodik spawn'ı bu saniyede durur.")]
    public float endTime;
    // --- DEĞİŞİKLİK SONU ---

    [Tooltip("Bu olayda (her tetiklendiğinde) toplam kaç düşman spawn edileceği.")]
    public int count = 1; // (Varsayılan değer 0 olmamalı, 1 olarak güncellendi)

    [Tooltip("Her bir düşmanın spawn olması arasında geçecek saniye.")]
    public float spawnInterval;
}

/// <summary>
/// Bir saldırı dalgasının (veya tüm bir turun) tamamını tanımlar.
/// </summary>
[CreateAssetMenu(fileName = "New Wave Profile", menuName = "Wave System/Wave Profile")]
public class WaveProfile : ScriptableObject
{
    [Header("Round Settings")]
    [Tooltip("Bu dalganın (turun) toplam süresi (saniye cinsinden).")]
    [Min(1)] 
    public float roundDuration = 60f;

    [Tooltip("Tur bittikten sonra (kazanma) 'Victory Panel'in gösterilmesi için beklenecek süre (saniye).")]
    [Min(0)]
    public float victoryDelay = 3f;
    
    
    [Header("Spawn Events")]
    [Tooltip("Bu dalgada gerçekleşecek tüm spawn olaylarının listesi.")]
    public List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
}