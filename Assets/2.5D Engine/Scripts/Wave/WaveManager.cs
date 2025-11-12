using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IndianOceanAssets.Engine2_5D
{
    public class WaveManager : MonoBehaviour
    {
        // --- YENİ EKLENEN KISIM BAŞLANGICI (Singleton) ---
        /// <summary>
        /// WaveManager'a dışarıdan erişim için statik referans (Singleton).
        /// </summary>
        public static WaveManager Instance { get; private set; }
        
        /// <summary>
        /// Tüm dalgalar boyunca spawn olacak toplam düşman sayısı.
        /// ObjectPooler bu sayıyı okuyarak 'enemy' havuzunu oluşturur.
        /// </summary>
        public int CalculatedEnemyPoolSize { get; private set; }
        // --- YENİ EKLENEN KISIM SONU ---

        [Tooltip("Bu seviyede oynanacak dalga profillerinin listesi.")]
        [SerializeField] private List<WaveProfile> waves;

        [SerializeField] private RoundManager roundManager;

        // Sahnedeki spawn noktalarını ID ile hızlıca bulmak için bir sözlük (Dictionary).
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();

        // Her bir spawn olayının bir sonraki tetiklenme zamanını takip eden liste.
        private List<float> nextEventTriggerTimes;

        private int currentWaveIndex = 0;
        private bool waveActive = false;

        private void Awake()
        {
            // --- YENİ EKLENEN KISIM BAŞLANGICI (Singleton ve Hesaplama) ---
            
            // Singleton kurulumu
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Sahnede birden fazla WaveManager bulundu. Bu kopya yok ediliyor.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // RoundManager referansını al (Start'tan taşındı)
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }

            // ObjectPooler'ın 'Start' metodundan önce çalışarak
            // ihtiyaç duyulan havuz boyutunu hesapla.
            CalculateWorstCaseEnemyPoolSize();
            
            // --- YENİ EKLENEN KISIM SONU ---

            // Sahnedeki tüm spawn noktalarını bul ve ID'lerini anahtar olarak kullanarak sözlüğe ekle.
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            // (RoundManager kontrolü Awake'e taşındı)
            
            // İlk dalgayı başlat.
            StartNextWave();
        }
        
        // --- YENİ EKLENEN FONKSİYON BAŞLANGICI ---
        /// <summary>
        /// 'waves' listesindeki tüm WaveProfile'ları analiz eder ve
        /// 'roundDuration' süresi boyunca spawn olacak toplam düşman sayısını hesaplar.
        /// Bu, ObjectPooler için en kötü durum (worst-case) senaryosudur.
        /// </summary>
        private void CalculateWorstCaseEnemyPoolSize()
        {
            // Gerekli referanslar yoksa hesaplama yapma
            if (roundManager == null)
            {
                Debug.LogError("WaveManager, havuz boyutunu hesaplamak için RoundManager referansını bulamadı!");
                CalculatedEnemyPoolSize = 20; // Hata durumunda varsayılan boyut
                return;
            }
            if (waves == null || waves.Count == 0)
            {
                Debug.LogWarning("WaveManager'a hiç dalga profili (WaveProfile) atanmamış. Havuz boyutu 20 olarak ayarlandı.");
                CalculatedEnemyPoolSize = 20; // Hata durumunda varsayılan boyut
                return;
            }

            float roundDuration = roundManager.RoundDuration;
            int totalEnemies = 0;

            // Atanan tüm dalga profillerini döngüye al
            foreach (WaveProfile wave in waves)
            {
                // Dalga içindeki tüm spawn olaylarını döngüye al
                foreach (SpawnEvent spawnEvent in wave.spawnEvents)
                {
                    if (spawnEvent.isPeriodic)
                    {
                        // Bu periyodik (tekrarlanan) bir olay
                        
                        // Hata önleme: repeatInterval çok küçükse (veya 0 ise) sonsuz döngüye girer.
                        // Bunu tek seferlik gibi kabul et.
                        if (spawnEvent.repeatInterval <= 0.1f) 
                        {
                            totalEnemies += spawnEvent.count;
                        }
                        else
                        {
                            // Olayın aktif olacağı süreyi hesapla (tur süresi - başlama zamanı)
                            float activeDuration = roundDuration - spawnEvent.triggerTime;
                            
                            if (activeDuration > 0)
                            {
                                // Kaç kez tekrarlanacağını hesapla (+1, triggerTime anındaki ilk spawn'ı da saymak için)
                                int repetitions = Mathf.FloorToInt(activeDuration / spawnEvent.repeatInterval) + 1;
                                
                                // Toplam düşman = (tekrar sayısı) * (her tekrardaki düşman sayısı)
                                totalEnemies += spawnEvent.count * repetitions;
                            }
                            // else: Olay, tur bittikten sonra başlıyor, o yüzden hiç spawn olmayacak.
                        }
                    }
                    else
                    {
                        // Bu tek seferlik bir olay
                        totalEnemies += spawnEvent.count;
                    }
                }
            }

            // Hesaplanan değeri public değişkene ata
            CalculatedEnemyPoolSize = totalEnemies;
            
            // Güvenlik önlemi: Eğer hesaplanan sayı 0 ise (örn: profiller boşsa)
            // en azından küçük bir havuz oluştur.
            if (CalculatedEnemyPoolSize == 0)
            {
                Debug.LogWarning("Dalga profilleri analiz edildi ancak spawn olacak hiç düşman bulunamadı. Havuz boyutu 20 olarak ayarlandı.");
                CalculatedEnemyPoolSize = 20;
            }
            else
            {
                Debug.Log($"ObjectPooler için hesaplanan 'enemy' havuz boyutu: {CalculatedEnemyPoolSize}");
            }
        }
        // --- YENİ EKLENEN FONKSİYON SONU ---


        private void Update()
        {
            // Tur veya dalga aktif değilse hiçbir şey yapma.
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }

            // Mevcut dalga profilini al.
            WaveProfile currentWave = waves[currentWaveIndex - 1];

            // Aktif dalganın içindeki her bir olayı kontrol et.
            for (int i = 0; i < currentWave.spawnEvents.Count; i++)
            {
                // Ana saat (TimeElapsed), bu olayın sıradaki tetiklenme zamanını (nextEventTriggerTimes[i]) geçti mi?
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i])
                {
                    // Zamanı gelen olayın referansını al
                    SpawnEvent currentEvent = currentWave.spawnEvents[i];

                    // Geçtiyse: Bir düşman "patlaması" (burst) başlat.
                    StartCoroutine(SpawnBurst(currentEvent));

                    // Şimdi bir sonraki tetiklenme zamanını hesapla
                    if (currentEvent.isPeriodic)
                    {
                        // EĞER BU PERİYODİK BİR OLAYSA:
                        nextEventTriggerTimes[i] += currentEvent.repeatInterval;
                    }
                    else
                    {
                        // EĞER BU TEK SEFERLİK BİR OLAYSA:
                        // Bir daha tetiklenmemesi için ulaşılamaz bir değere ayarla.
                        nextEventTriggerTimes[i] = Mathf.Infinity;
                    }
                }
            }
        }

        public void StartNextWave()
        {
            if (waves != null && waves.Count > currentWaveIndex)
            {
                Debug.Log($"Dalga {currentWaveIndex + 1} başlıyor!");
                WaveProfile currentWave = waves[currentWaveIndex];

                // Zaman takip listesini sıfırla ve ilk tetiklenme zamanlarını ayarla.
                nextEventTriggerTimes = new List<float>();

                foreach (var spawnEvent in currentWave.spawnEvents)
                {
                    // Her olayın ilk tetiklenme zamanı 'triggerTime' olacak.
                    nextEventTriggerTimes.Add(spawnEvent.triggerTime);
                }

                currentWaveIndex++;
                waveActive = true;
            }
            else
            {
                Debug.Log("Tüm dalgalar tamamlandı!");
                waveActive = false;
            }
        }

        // Bir "patlama" (burst) şeklinde düşman spawn eden Coroutine.
        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı! Düşman spawn edilemiyor.");
                yield break; // Coroutine'i sonlandır.
            }

            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];

            for (int i = 0; i < spawnEvent.count; i++)
            {
                // ObjectPooler'dan "enemy" etiketli bir düşman çağır.
                GameObject spawnedEnemy = ObjectPooler.Instance.SpawnFromPool("enemy", spawnPoint.transform.position, Quaternion.identity);
                
                // --- Güvenlik Kontrolü ---
                // Eğer havuz boşaldıysa (hesaplamaya rağmen bir sorun olduysa)
                // SpawnFromPool null dönebilir. Bu durumda Coroutine'i durdur.
                if (spawnedEnemy == null)
                {
                     Debug.LogError($"'enemy' havuzu boşaldı! Hesaplanan boyut ({CalculatedEnemyPoolSize}) yetersiz kalmış olabilir. Spawn işlemi durduruldu.");
                     yield break;
                }
                
                yield return new WaitForSeconds(spawnEvent.spawnInterval);
            }
        }
    }
}