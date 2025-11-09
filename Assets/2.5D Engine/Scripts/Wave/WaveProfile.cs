using UnityEngine;
// Listeleri kullanabilmek için bu satır gerekli.
using System.Collections.Generic; 

// Bu sınıf, bir dalga içindeki tek bir spawn olayını tanımlar.
// [System.Serializable] sayesinde bu sınıfın değişkenlerini Unity Inspector'da görebileceğiz.
[System.Serializable]
public class SpawnEvent
{
    [Tooltip("Bu olayda hangi düşman prefab'ının spawn olacağı. (Şu an için 'enemy' tag'i kullanılıyor, ileride bu prefab'a özel spawn eklenebilir)")]
    public GameObject enemyPrefab;

    [Tooltip("Bu olayın hangi Spawn Point ID'sinde gerçekleşeceği.")]
    public int spawnPointID;

    // --- DEĞİŞİKLİK BAŞLANGICI ---
    // 'startDelay' değişkenini daha net bir isimlendirme olan 'triggerTime' ile değiştirdik.
    [Tooltip("Bu olayın İLK defa tetikleneceği saniye (Round başından itibaren).")]
    public float triggerTime; // Eski adı: startDelay

    [Tooltip("Bu olay periyodik olarak tekrarlanacak mı? (İşaretlenmezse, 'triggerTime'da sadece 1 kez çalışır).")]
    public bool isPeriodic; // YENİ: Tek seferlik olaylar için eklendi.

    [Tooltip("EĞER periyodik ise, kaç saniyede bir tekrarlanacağı. (isPeriodic false ise bu dikkate alınmaz).")]
    public float repeatInterval; // YENİ: Periyodik olayların aralığı için eklendi.
    // --- DEĞİŞİKLİK SONU ---

    [Tooltip("Bu olayda (her tetiklendiğinde) toplam kaç düşman spawn edileceği.")]
    public int count;

    [Tooltip("Her bir düşmanın spawn olması arasında geçecek saniye.")]
    public float spawnInterval;
}

// Bu ScriptableObject, bir saldırı dalgasının tamamını tanımlar.
// CreateAssetMenu, Unity'nin Assets > Create menüsüne yeni bir seçenek ekler.
[CreateAssetMenu(fileName = "New Wave Profile", menuName = "Wave System/Wave Profile")]
public class WaveProfile : ScriptableObject
{
    [Tooltip("Bu dalgada gerçekleşecek tüm spawn olaylarının listesi.")]
    public List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
}