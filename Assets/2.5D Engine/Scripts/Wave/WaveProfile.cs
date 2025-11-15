/*
 * WAVE PROFILE (VERİ KATMANI) - v2.0 (Data-Driven Refactor)
 * * DEĞİŞİKLİKLER:
 * - 'SpawnEvent' sınıfındaki 'public GameObject enemyPrefab' alanı,
 * 'public EnemyData enemyDataToSpawn' alanı ile DEĞİŞTİRİLDİ.
 * - Bu, artık dalga planlarken prefab değil, 'EnemyData' (stat)
 * asset'i seçmemizi sağlar.
 */

using UnityEngine;
using System.Collections.Generic; 

[System.Serializable]
public class SpawnEvent
{
    // --- DEĞİŞİKLİK BAŞLANGICI ---
    // 'enemyPrefab' alanını 'EnemyData' ile değiştirdik.
    [Tooltip("Bu olayda hangi düşman VERİSİNİN (statlarının) spawn olacağı.")]
    public EnemyData enemyDataToSpawn; // <-- BU DEĞİŞTİ
    // public GameObject enemyPrefab; // <-- BU SİLİNDİ
    // --- DEĞİŞİKLİK SONU ---

    [Tooltip("Bu olayın hangi Spawn Point ID'sinde gerçekleşeceği.")]
    public int spawnPointID;
    
    [Tooltip("EĞER düşman 'FollowPath' modundaysa, takip edeceği 'EnemyPath' objesinin ID'si.")]
    public int pathID = -1; 

    [Tooltip("Bu olayın İLK defa tetikleneceği saniye.")]
    public float triggerTime; 

    [Tooltip("Bu olay periyodik olarak tekrarlanacak mı?")]
    public bool isPeriodic; 

    [Tooltip("EĞER periyodik ise, kaç saniyede bir tekrarlanacağı.")]
    public float repeatInterval = 1f; 

    [Tooltip("EĞER periyodik ise, spawn'ın 'endTime'da durmasını sağlar.")]
    public bool hasFiniteDuration;

    [Tooltip("EĞER 'hasFiniteDuration' true ise, spawn bu saniyede durur.")]
    public float endTime;

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
    [Min(1)] 
    public float roundDuration = 60f;
    [Min(0)]
    public float victoryDelay = 3f;
    
    
    [Header("Spawn Events")]
    [Tooltip("Bu dalgada gerçekleşecek tüm spawn olaylarının listesi.")]
    public List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
}