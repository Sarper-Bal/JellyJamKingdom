using UnityEngine;
using System.Collections.Generic; // Listeleri kullanabilmek için bu satır gerekli.

// Bu sınıf, bir dalga içindeki tek bir spawn olayını tanımlar.
// [System.Serializable] sayesinde bu sınıfın değişkenlerini Unity Inspector'da görebileceğiz.
[System.Serializable]
public class SpawnEvent
{
    [Tooltip("Bu olayda hangi düşman prefab'ının spawn olacağı.")]
    public GameObject enemyPrefab;

    [Tooltip("Bu olayın hangi Spawn Point ID'sinde gerçekleşeceği.")]
    public int spawnPointID;

    [Tooltip("Bu olayın periyodik olarak tetiklenme aralığı (saniye). İlk tetiklenme de bu saniyede olur.")]
    public float startDelay;

    [Tooltip("Bu olayda toplam kaç düşman spawn edileceği.")]
    public int count;

    [Tooltip("Her bir düşmanın spawn olması arasında geçecek saniye.")]
    public float spawnInterval;
    [Tooltip("ObjectPooler'da bu düşmanı çağırmak için kullanılacak etiket.")]
    public string poolTag;
}

// Bu ScriptableObject, bir saldırı dalgasının tamamını tanımlar.
// CreateAssetMenu, Unity'nin Assets > Create menüsüne yeni bir seçenek ekler.
[CreateAssetMenu(fileName = "New Wave Profile", menuName = "Wave System/Wave Profile")]
public class WaveProfile : ScriptableObject
{
    [Tooltip("Bu dalgada gerçekleşecek tüm spawn olaylarının listesi.")]
    public List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
}