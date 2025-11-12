/*
 * WAVE MANAGER (YÖNETİCİ MODELİ - v2)
 * * DEĞİŞİKLİKLER:
 * - 'enemyPrefab' referansına ek olarak 'enemyDeathEffectPrefab' referansı eklendi.
 * - 'Start()' metodu: Artık 'enemy' havuzunu oluştururken, 'enemyDeath' havuzunu da
 * aynı 'CalculatedEnemyPoolSize' ile birlikte oluşturuyor.
 * - 'StopAndCleanupWaves()' metodu: Artık 'enemy' havuzunu yok ederken,
 * 'enemyDeath' havuzunu da (eğer varsa) yok ediyor.
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
        [SerializeField] private GameObject enemyPrefab; 

        // --- YENİ EKLENEN KISIM BAŞLANGICI ---
        [Tooltip("Düşman öldüğünde 'HealthSystem' tarafından kullanılacak ÖLÜM EFEKTİ prefab'ı.")]
        [SerializeField] private GameObject enemyDeathEffectPrefab; // YENİ: Inspector'dan atanmalı
        // --- YENİ EKLENEN KISIM SONU ---

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
            // 1. RoundManager referansını al
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }

            // 2. Havuz boyutunu hesapla
            CalculateWorstCaseEnemyPoolSize();
            
            // 3. Gerekli prefab'ların atandığını kontrol et
            if (enemyPrefab == null)
            {
                Debug.LogError("WaveManager üzerinde 'Enemy Prefab' atanmamış! " +
                               "Havuz oluşturulamıyor ve dalgalar başlatılamıyor.");
                return;
            }
            // --- YENİ KONTROL EKLENDİ ---
            if (enemyDeathEffectPrefab == null)
            {
                // Bu bir hata değil, uyarı. Belki ölüm efekti olmayan bir düşman istiyoruzdur.
                Debug.LogWarning("WaveManager üzerinde 'Enemy Death Effect Prefab' atanmamış. " +
                                 "'enemyDeath' havuzu oluşturulmayacak.");
            }
            // --- YENİ KONTROL SONU ---


            // 4. ObjectPooler servisine DİNAMİK havuzları oluşturması için komut ver
            
            // Düşman havuzunu oluştur
            ObjectPooler.Instance.CreatePool("enemy", enemyPrefab, CalculatedEnemyPoolSize);

            // --- YENİ EKLENEN KISIM BAŞLANGICI ---
            // Düşman Ölüm Efekti havuzunu oluştur (eğer prefab atandıysa)
            if (enemyDeathEffectPrefab != null)
            {
                // 'enemyDeath' havuzunu da 'enemy' havuzuyla AYNI BOYUTTA oluşturuyoruz.
                // Çünkü en kötü senaryoda her düşman için bir ölüm efekti gerekir.
                ObjectPooler.Instance.CreatePool("enemyDeath", enemyDeathEffectPrefab, CalculatedEnemyPoolSize);
            }
            // --- YENİ EKLENEN KISIM SONU ---


            // 5. Her şey hazır olduğuna göre, ilk dalgayı başlat.
            StartNextWave();
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
                if (wave == null) continue; 
                
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
                Debug.Log($"ObjectPooler için hesaplanan dinamik havuz boyutu (enemy & enemyDeath): {CalculatedEnemyPoolSize}");
            }
        }
        
        
        /// <summary>
        /// Düşman spawn etmeyi durdurur ve dinamik havuzları ('enemy', 'enemyDeath')
        /// temizlemesi için ObjectPooler'a komut verir.
        /// </summary>
        public void StopAndCleanupWaves()
        {
            waveActive = false;
            StopAllCoroutines();
            
            if (ObjectPooler.Instance != null)
            {
                // "enemy" havuzunu yok et
                ObjectPooler.Instance.DestroyPool("enemy");
                
                // --- YENİ EKLENEN KISIM BAŞLANGICI ---
                // "enemyDeath" havuzunu da yok et
                // (Eğer 'enemyDeathEffectPrefab' null idiyse, bu havuz hiç oluşmamıştı
                // ve 'DestroyPool' metodu uyarı verip güvenli bir şekilde çıkacaktır.
                // Bu yüzden 'if' kontrolüne gerek yok.)
                ObjectPooler.Instance.DestroyPool("enemyDeath");
                // --- YENİ EKLENEN KISIM SONU ---
            }
            
            Debug.Log("WaveManager: Dinamik dalga havuzları (enemy, enemyDeath) durduruldu ve temizlendi.");
        }


        private void Update()
        {
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }

            WaveProfile currentWave = waves[currentWaveIndex - 1];
            if (currentWave == null) return; // Güvenlik kontrolü

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