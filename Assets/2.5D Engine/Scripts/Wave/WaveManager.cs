/*
 * WAVE MANAGER (YÖNETİCİ MODELİ - v3.1)
 * * DEĞİŞİKLİKLER (Akıllı Temizlik):
 * - 'StopAndCleanupWaves' metodu ikiye bölündü:
 * 1. 'StopWaveSpawning()': Sadece spawn'ı durdurur. Tur biter bitmez çağrılır.
 * 2. 'CleanupDynamicPools()': Sadece havuzları yok eder. Zafer ekranı gelmeden
 * hemen önce 'RoundManager' tarafından gecikmeli çağrılır.
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
        public static WaveManager Instance { get; private set; }
        #endregion

        // Prefab'a göre ayrılmış havuz boyutları
        private Dictionary<GameObject, int> dynamicEnemyPools;
        private Dictionary<GameObject, int> dynamicEffectPools;
        
        [Header("References")]
        [Tooltip("Turun süresi gibi bilgileri almak için RoundManager referansı.")]
        [SerializeField] private RoundManager roundManager;
        
        [Header("Wave Data")]
        [Tooltip("Bu seviyede oynanacak dalga profillerinin listesi.")]
        [SerializeField] private List<WaveProfile> waves;
        
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();
        private List<float> nextEventTriggerTimes;
        private int currentWaveIndex = 0;
        private bool waveActive = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }

            // 1. Havuz ihtiyacını hesapla
            CalculatePoolRequirements();

            // 2. Dinamik Düşman Havuzlarını Oluştur
            if (dynamicEnemyPools.Count > 0)
            {
                Debug.Log($"--- WaveManager: Düşman Havuzları Oluşturuluyor... ({dynamicEnemyPools.Count} tip) ---");
                foreach (var entry in dynamicEnemyPools)
                {
                    ObjectPooler.Instance.CreatePool(entry.Key.name, entry.Key, entry.Value);
                }
            }
            else
            {
                Debug.LogWarning("WaveManager: Hesaplama sonucunda spawn edilecek DÜŞMAN bulunamadı.");
            }
            
            // 3. Dinamik Efekt Havuzlarını Oluştur
            if (dynamicEffectPools.Count > 0)
            {
                Debug.Log($"--- WaveManager: Efekt Havuzları Oluşturuluyor... ({dynamicEffectPools.Count} tip) ---");
                foreach (var entry in dynamicEffectPools)
                {
                    ObjectPooler.Instance.CreatePool(entry.Key.name, entry.Key, entry.Value);
                }
            }
            else
            {
                Debug.LogWarning("WaveManager: Hesaplama sonucunda spawn edilecek ÖLÜM EFEKTİ bulunamadı.");
            }
            
            // 4. Dalgaları Başlat
            StartNextWave();
        }
        
        /// <summary>
        /// WaveProfile'ları tarayarak havuz ihtiyaçlarını hesaplar.
        /// </summary>
        private void CalculatePoolRequirements()
        {
            dynamicEnemyPools = new Dictionary<GameObject, int>();
            dynamicEffectPools = new Dictionary<GameObject, int>();

            if (roundManager == null)
            {
                Debug.LogError("WaveManager, RoundManager referansını bulamadı!");
                return;
            }
            if (waves == null || waves.Count == 0)
            {
                Debug.LogWarning("WaveManager'a hiç dalga profili (WaveProfile) atanmamış.");
                return;
            }

            float roundDuration = roundManager.RoundDuration;
            
            foreach (WaveProfile wave in waves)
            {
                if (wave == null) continue; 
                
                foreach (SpawnEvent spawnEvent in wave.spawnEvents)
                {
                    GameObject enemyPrefab = spawnEvent.enemyPrefab;
                    if (enemyPrefab == null)
                    {
                        Debug.LogWarning($"WaveProfile ({wave.name}) içinde 'enemyPrefab' atanmamış bir SpawnEvent bulundu. Bu olay atlanıyor.");
                        continue; 
                    }

                    int countForThisEvent = 0;
                    if (spawnEvent.isPeriodic) // Periyodik
                    {
                        if (spawnEvent.repeatInterval <= 0.1f) 
                        {
                            countForThisEvent = spawnEvent.count; 
                        }
                        else
                        {
                            float activeDuration = roundDuration - spawnEvent.triggerTime;
                            if (activeDuration > 0)
                            {
                                int repetitions = Mathf.FloorToInt(activeDuration / spawnEvent.repeatInterval) + 1;
                                countForThisEvent = spawnEvent.count * repetitions;
                            }
                        }
                    }
                    else // Tek seferlik
                    {
                        if(spawnEvent.triggerTime <= roundDuration)
                        {
                            countForThisEvent = spawnEvent.count;
                        }
                    }
                    
                    if (countForThisEvent == 0) continue; 

                    // Düşman havuzunu güncelle
                    if (!dynamicEnemyPools.ContainsKey(enemyPrefab))
                    {
                        dynamicEnemyPools.Add(enemyPrefab, 0); 
                    }
                    dynamicEnemyPools[enemyPrefab] += countForThisEvent; 

                    // Ölüm efekti havuzunu güncelle
                    HealthSystem hs = enemyPrefab.GetComponent<HealthSystem>();
                    if (hs != null)
                    {
                        GameObject deathEffectPrefab = hs.GetDeathEffectPrefab(); 
                        if (deathEffectPrefab != null)
                        {
                            if (!dynamicEffectPools.ContainsKey(deathEffectPrefab))
                            {
                                dynamicEffectPools.Add(deathEffectPrefab, 0); 
                            }
                            dynamicEffectPools[deathEffectPrefab] += countForThisEvent; 
                        }
                    }
                } 
            } 
            
            Debug.Log("--- WaveManager: Havuz Hesaplaması Tamamlandı ---");
        }
        
        
        // --- DEĞİŞİKLİK BAŞLANGICI: Metot Bölme ---

        /// <summary>
        /// Yeni düşman spawn etmeyi DURDURUR.
        /// 'RoundManager' tarafından tur biter bitmez çağrılır.
        /// </summary>
        public void StopWaveSpawning()
        {
            // 1. Update() içindeki spawn döngüsünü durdur
            waveActive = false;
            
            // 2. Halen çalışmakta olan SpawnBurst Coroutine'lerini durdur
            StopAllCoroutines();
            
            Debug.Log("WaveManager: Yeni düşman spawn'ı durduruldu.");
        }

        /// <summary>
        /// 'Start'ta oluşturulan tüm dinamik havuzları (düşman ve efekt) TEMİZLER.
        /// 'RoundManager' tarafından zafer ekranı gelmeden hemen önce çağrılır.
        /// </summary>
        public void CleanupDynamicPools()
        {
            if (ObjectPooler.Instance == null) return;
            
            Debug.Log("--- WaveManager: Dinamik Havuzlar Temizleniyor... ---");

            // 'dynamicEnemyPools' sözlüğünü döngüye al ve tüm havuzları yok et
            if (dynamicEnemyPools != null)
            {
                foreach (var entry in dynamicEnemyPools)
                {
                    ObjectPooler.Instance.DestroyPool(entry.Key.name);
                }
                dynamicEnemyPools.Clear(); // Sözlüğü temizle
            }

            // 'dynamicEffectPools' sözlüğünü döngüye al ve tüm havuzları yok et
            if (dynamicEffectPools != null)
            {
                foreach (var entry in dynamicEffectPools)
                {
                    ObjectPooler.Instance.DestroyPool(entry.Key.name);
                }
                dynamicEffectPools.Clear(); // Sözlüğü temizle
            }
        }
        // --- DEĞİŞİKLİK SONU ---


        private void Update()
        {
            // waveActive = false ise (StopWaveSpawning çağrıldıysa) spawn etme
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }

            WaveProfile currentWave = waves[currentWaveIndex - 1];
            if (currentWave == null) return; 

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
            GameObject prefabToSpawn = spawnEvent.enemyPrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogError("SpawnEvent'te 'enemyPrefab' (null) olduğu için spawn işlemi yapılamadı.");
                yield break;
            }
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı! Düşman spawn edilemiyor.");
                yield break; 
            }

            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];
            string poolTag = prefabToSpawn.name; // Prefab'ın adı = Havuzun tag'i

            for (int i = 0; i < spawnEvent.count; i++)
            {
                GameObject spawnedEnemy = ObjectPooler.Instance.SpawnFromPool(poolTag, spawnPoint.transform.position, Quaternion.identity);
                
                if (spawnedEnemy == null)
                {
                     Debug.LogError($"'{poolTag}' havuzu boşaldı! Hesaplama yetersiz kalmış olabilir. Spawn işlemi durduruldu.");
                     yield break;
                }
                
                IPooledObject pooledObj = spawnedEnemy.GetComponent<IPooledObject>();
                if (pooledObj != null)
                {
                    pooledObj.PoolTag = poolTag;
                }
                
                yield return new WaitForSeconds(spawnEvent.spawnInterval);
            }
        }
    }
}