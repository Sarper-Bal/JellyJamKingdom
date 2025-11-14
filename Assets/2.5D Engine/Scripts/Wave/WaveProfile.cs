using UnityEngine;
// Listeleri kullanabilmek için bu satır gerekli.
using System.Collections.Generic; 

// Bu sınıf, bir dalga içindeki tek bir spawn olayını tanımlar.
// [System.Serializable] sayesinde bu sınıfın değişkenlerini Unity Inspector'da görebileceğiz.
[System.Serializable]
public class SpawnEvent
{
    [Tooltip("Bu olayda hangi düşman prefab'ının spawn olacağı.")]
    public GameObject enemyPrefab; // Düşman prefab'ı buradan okunacak

    [Tooltip("Bu olayın hangi Spawn Point ID'sinde gerçekleşeceği.")]
    public int spawnPointID;

    [Tooltip("Bu olayın İLK defa tetikleneceği saniye (Round başından itibaren).")]
    public float triggerTime; 

    [Tooltip("Bu olay periyodik olarak tekrarlanacak mı? (İşaretlenmezse, 'triggerTime'da sadece 1 kez çalışır).")]
    public bool isPeriodic; 

    [Tooltip("EĞER periyodik ise, kaç saniyede bir tekrarlanacağı. (isPeriodic false ise bu dikkate alınmaz).")]
    public float repeatInterval; 

    [Tooltip("Bu olayda (her tetiklendiğinde) toplam kaç düşman spawn edileceği.")]
    public int count;

    [Tooltip("Her bir düşmanın spawn olması arasında geçecek saniye.")]
    public float spawnInterval;
}

// Bu ScriptableObject, bir saldırı dalgasının (veya tüm bir turun) tamamını tanımlar.
[CreateAssetMenu(fileName = "New Wave Profile", menuName = "Wave System/Wave Profile")]
public class WaveProfile : ScriptableObject
{
    // --- YENİ EKLENEN KISIM BAŞLANGICI ---
    [Header("Round Settings")]
    [Tooltip("Bu dalganın (turun) toplam süresi (saniye cinsinden).")]
    [Min(1)] // Sürenin en az 1 saniye olmasını zorunlu kıl
    public float roundDuration = 60f;

    [Tooltip("Tur bittikten sonra (kazanma) 'Victory Panel'in gösterilmesi için beklenecek süre (saniye).")]
    [Min(0)]
    public float victoryDelay = 3f;
    // --- YENİ EKLENEN KISIM SONU ---
    
    
    [Header("Spawn Events")]
    [Tooltip("Bu dalgada gerçekleşecek tüm spawn olaylarının listesi.")]
    public List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
}