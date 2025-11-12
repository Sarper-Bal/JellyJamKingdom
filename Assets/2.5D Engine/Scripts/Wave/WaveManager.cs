/*
 * WAVE MANAGER (YÖNETİCİ MODELİ)
 * * DEĞİŞİKLİKLER:
 * - Artık 'ObjectPooler'a güvenmek yerine ona aktif olarak komut veriyor.
 * - 'Start()' içinde 'ObjectPooler.Instance.CreatePool' çağrısını yapıyor.
 * - 'enemyPrefab' referansını Inspector'dan alıyor (Pooler'a hangi prefab'ı vereceğini bilmek için).
 * - 'Awake' içindeki hesaplama mantığı 'Start'a taşındı (ObjectPooler.Instance'ın hazır olmasını garantilemek için).
 * - 'StopAndCleanupWaves' metodu eklendi. Bu metot, spawn'ı durdurur, Coroutine'leri temizler
 * ve ObjectPooler'a 'DestroyPool' komutunu gönderir.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IndianOceanAssets.Engine2_5D
{
    public class WaveManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>
        /// WaveManager'a dışarıdan erişim için statik referans (Singleton).
        /// </summary>
        public static WaveManager Instance { get; private set; }
        #endregion

        /// <summary>
        /// Tüm dalgalar boyunca spawn olacak toplam düşman sayısı.
        /// </summary>
        public int CalculatedEnemyPoolSize { get; private set; }
        
        [Header("References")]
        [Tooltip("Dalgalarda spawn edilecek DÜŞMAN prefab'ı.")]
        [SerializeField] private GameObject enemyPrefab; // YENİ: Inspector'dan atanmalı

        [Tooltip("Turun süresi gibi bilgileri almak için RoundManager referansı.")]
        [SerializeField] private RoundManager roundManager;
        
        [Header("Wave Data")]
        [Tooltip("Bu seviyede oynanacak dalga profillerinin listesi.")]
        [SerializeField] private List<WaveProfile> waves;


        // Sahnedeki spawn noktalarını ID ile hızlıca bulmak için bir sözlük (Dictionary).
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();

        // Her bir spawn olayının bir sonraki tetiklenme zamanını takip eden liste.
        private List<float> nextEventTriggerTimes;

        private int currentWaveIndex = 0;
        private bool waveActive = false;

        private void Awake()
        {
            // Singleton kurulumu
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Sahnedeki tüm spawn noktalarını bul ve ID'lerini anahtar olarak kullanarak sözlüğe ekle.
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            // --- DEĞİŞİKLİK BAŞLANGICI (Havuz Oluşturma Akışı) ---
            
            // 1. RoundManager referansını al
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }

            // 2. Havuz boyutunu hesapla
            CalculateWorstCaseEnemyPoolSize();
            
            // 3. Gerekli prefab'ın atandığını kontrol et
            if (enemyPrefab == null)
            {
                Debug.LogError("WaveManager üzerinde 'Enemy Prefab' atanmamış! " +
                               "Havuz oluşturulamıyor ve dalgalar başlatılamıyor.");
                return;
            }

            // 4. ObjectPooler servisine 'enemy' havuzunu oluşturması için komut ver
            ObjectPooler.Instance.CreatePool("enemy", enemyPrefab, CalculatedEnemyPoolSize);

            // 5. Her şey hazır olduğuna göre, ilk dalgayı başlat.
            StartNextWave();
            
            // --- DEĞİŞİKLİK SONU ---
        }
        
        /// <summary>
        /// 'waves' listesindeki tüm WaveProfile'ları analiz eder ve
        /// 'roundDuration' süresi boyunca spawn olacak toplam düşman sayısını hesaplar.
        /// </summary>
        private void CalculateWorstCaseEnemyPoolSize()
        {
            if (roundManager == null)
            {
                Debug.LogError("WaveManager, havuz boyutunu hesaplamak için RoundManager referansını bulamadı!");
                CalculatedEnemyPoolSize = 20; // Hata durumunda varsayılan boyut
                return;
            }
            if (waves == null || waves.Count == 0)
            {
                Debug.LogWarning("WaveManager'a hiç dalga profili (WaveProfile) atanmamış. Havuz boyutu 20 olarak ayarlandı.");
                CalculatedEnemyPoolSize = 20; 
                return;
            }

            float roundDuration = roundManager.RoundDuration;
            int totalEnemies = 0;

            foreach (WaveProfile wave in waves)
            {
                if (wave == null) continue; // Atanmamış (null) bir dalga profili varsa atla
                
                foreach (SpawnEvent spawnEvent in wave.spawnEvents)
                {
                    if (spawnEvent.isPeriodic)
                    {
                        if (spawnEvent.repeatInterval <= 0.1f) 
                        {
                            totalEnemies += spawnEvent.count;
                        }
                        else
                        {
                            float activeDuration = roundDuration - spawnEvent.triggerTime;
                            if (activeDuration > 0)
                            {
                                int repetitions = Mathf.FloorToInt(activeDuration / spawnEvent.repeatInterval) + 1;
                                totalEnemies += spawnEvent.count * repetitions;
                            }
                        }
                    }
                    else
                    {
                        // Tek seferlik olay (ve tur süresi içinde tetikleniyorsa)
                        if(spawnEvent.triggerTime <= roundDuration)
                        {
                            totalEnemies += spawnEvent.count;
                        }
                    }
                }
            }

            CalculatedEnemyPoolSize = totalEnemies;
            
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
        
        
        // --- YENİ FONKSİYON BAŞLANGICI ---
        /// <summary>
        /// Düşman spawn etmeyi durdurur ve 'enemy' havuzunu temizlemesi için ObjectPooler'a komut verir.
        /// Bu metot, 'RoundManager' tarafından tur bittiğinde (kazanma) çağrılır.
        /// </summary>
        public void StopAndCleanupWaves()
        {
            // 1. Update() içindeki spawn döngüsünü durdur
            waveActive = false;
            
            // 2. Halen çalışmakta olan SpawnBurst Coroutine'lerini durdur
            //    (Bu, tur biterken yeni düşmanların spawn olmaya devam etmesini engeller)
            StopAllCoroutines();
            
            // 3. ObjectPooler'a 'enemy' havuzunu yok etme komutu ver
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.DestroyPool("enemy");
            }
            
            Debug.Log("WaveManager: Dalgalar durduruldu ve 'enemy' havuzu temizlendi.");
        }
        // --- YENİ FONKSİYON SONU ---


        private void Update()
        {
            // Tur veya dalga aktif değilse (waveActive = false) hiçbir şey yapma.
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }

            // Mevcut dalga profilini al.
            WaveProfile currentWave = waves[currentWaveIndex - 1];

            // Aktif dalganın içindeki her bir olayı kontrol et.
            for (int i = 0; i < currentWave.spawnEvents.Count; i++)
            {
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i])
                {
                    SpawnEvent currentEvent = currentWave.spawnEvents[i];
                    StartCoroutine(SpawnBurst(currentEvent));

                    if (currentEvent.isPeriodic)
                    {
                        nextEventTriggerTimes[i] += currentEvent.repeatInterval;
                    }
                    else
                    {
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

                // currentWave'in null olup olmadığını kontrol et
                if (currentWave == null)
                {
                     Debug.LogError($"Dalga {currentWaveIndex + 1} (index {currentWaveIndex}) 'waves' listesinde atanmamış (null).");
                     waveActive = false;
                     return;
                }

                nextEventTriggerTimes = new List<float>();
                foreach (var spawnEvent in currentWave.spawnEvents)
                {
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

        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı! Düşman spawn edilemiyor.");
                yield break; 
            }

            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];

            for (int i = 0; i < spawnEvent.count; i++)
            {
                // 'enemy' tag'ini kullanan eski prefab referansı yerine,
                // WaveManager'a atadığımız 'enemyPrefab'in tag'ini kullanalım.
                // Not: Pool'u "enemy" tag'i ile oluşturduğumuz için burada "enemy" kullanmak doğrudur.
                GameObject spawnedEnemy = ObjectPooler.Instance.SpawnFromPool("enemy", spawnPoint.transform.position, Quaternion.identity);
                
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