/*
 * WAVE PROFILE (VERİ KATMANI) - v1.2
 * * DEĞİŞİKLİKLER:
 * - 'SpawnEvent' sınıfına 'pathID' (int) alanı eklendi.
 * - Bu sayede her spawn olayı için, 'spawnPointID'ye ek olarak,
 * (opsiyonel) bir 'pathID' de belirleyebiliriz.
 * - Bu, WaveManager'ın hangi spawn'ın hangi yolu izleyeceğini
 * bilmesini sağlar ve "TypeMismatch" hatasını çözer.
 */

using UnityEngine;
using System.Collections.Generic; 

/// <summary>
/// Bir dalga içindeki tek bir spawn olayını tanımlar.
/// </summary>
[System.Serializable]
public class SpawnEvent
{
    [Tooltip("Bu olayda hangi düşman prefab'ının spawn olacağı.")]
    public GameObject enemyPrefab; 

    [Tooltip("Bu olayın hangi Spawn Point ID'sinde gerçekleşeceği.")]
    public int spawnPointID;

    // --- YENİ EKLENEN KISIM (v1.2) ---
    [Tooltip("EĞER düşman 'FollowPath' modundaysa, takip edeceği 'EnemyPath' objesinin ID'si. " +
             "(-1 = Yol yok, 'ChasePlayer' gibi davran)")]
    public int pathID = -1; // <-- BU ALAN EKLENDİ
    // --- DEĞİŞİKLİK SONU ---

    [Tooltip("Bu olayın İLK defa tetikleneceği saniye (Round başından itibaren).")]
    public float triggerTime; 

    [Tooltip("Bu olay periyodik olarak tekrarlanacak mı?")]
    public bool isPeriodic; 

    // v1.1 Değişiklikleri (Bunlar sizde zaten vardı)
    [Tooltip("EĞER periyodik ise, kaç saniyede bir tekrarlanacağı.")]
    public float repeatInterval = 1f; 

    [Tooltip("EĞER periyodik ise, bu seçenek spawn'ın 'endTime'da durmasını sağlar.")]
    public bool hasFiniteDuration;

    [Tooltip("EĞER 'hasFiniteDuration' true ise, bu olayın periyodik spawn'ı bu saniyede durur.")]
    public float endTime;
    // ---

    [Tooltip("Bu olayda (her tetiklendiğinde) toplam kaç düşman spawn edileceği.")]
    public int count = 1;

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