/*
 * WAVE MANAGER (YÖNETİCİ MODELİ - v4.1)
 * * DEĞİŞİKLİKLER (Hata Düzeltmesi CS1671):
 * - '[DefaultExecutionOrder(-10)]' özniteliği, 'namespace' bloğunun
 * dışından, 'public class WaveManager' bildiriminin hemen üzerine,
 * 'namespace' bloğunun İÇİNE taşındı.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// --- HATA DÜZELTMESİ: Attribute (öznitelik) namespace'in İÇİNE taşındı ---
namespace IndianOceanAssets.Engine2_5D
{
    /// <summary>
    /// Bu script'in 'Start' metodunun, diğer tüm script'lerden (özellikle RoundManager'dan)
    /// önce çalışmasını garantilemek için bu attribute'u ekliyoruz.
    /// </summary>
    [DefaultExecutionOrder(-10)]
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
        [Tooltip("Bu seviyede oynanacak dalga profili. Turun süresi, gecikmesi ve " +
                 "tüm düşman spawn olayları bu asset'ten okunur.")]
        [SerializeField] private WaveProfile currentRoundProfile; 
        
        
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();
        private List<float> nextEventTriggerTimes;
        
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
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            // 1. RoundManager referansını bul
            if (roundManager == null)
            {
                // RoundManager'ı bulmaya çalış
                roundManager = FindObjectOfType<RoundManager>();
                if (roundManager == null)
                {
                    Debug.LogError("WaveManager: Sahnede 'RoundManager' component'i bulunamadı! " +
                                   "Oyun başlatılamıyor.");
                    return; // RoundManager yoksa devam etme
                }
            }

            // 2. Gerekli WaveProfile'ın atandığını kontrol et
            if (currentRoundProfile == null)
            {
                Debug.LogError("WaveManager üzerinde 'Current Round Profile' atanmamış! " +
                               "Oyun başlatılamıyor.");
                return;
            }

            // 3. RoundManager'ı Başlat (Initialize et)
            // ('DefaultExecutionOrder' sayesinde bu 'Start', 'RoundManager.Start'tan önce çalışır)
            roundManager.InitializeRound(
                currentRoundProfile.roundDuration, 
                currentRoundProfile.victoryDelay
            );

            // 4. Havuz ihtiyacını hesapla
            CalculatePoolRequirements();

            // 5. Dinamik Düşman Havuzlarını Oluştur
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
            
            // 6. Dinamik Efekt Havuzlarını Oluştur
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
            
            // 7. Dalgaları Başlat
            StartNextWave();
        }
        
        /// <summary>
        /// 'currentRoundProfile'ı analiz eder ve havuz ihtiyaçlarını hesaplar.
        /// </summary>
        private void CalculatePoolRequirements()
        {
            dynamicEnemyPools = new Dictionary<GameObject, int>();
            dynamicEffectPools = new Dictionary<GameObject, int>();

            // RoundManager kontrolü Start() içinde yapıldığı için burada tekrar gerekmez.
            
            if (currentRoundProfile == null || currentRoundProfile.spawnEvents.Count == 0)
            {
                Debug.LogWarning("WaveManager'a atanmış 'Current Round Profile' yok veya 'Spawn Events' listesi boş.");
                return;
            }

            float roundDuration = roundManager.RoundDuration;
            
            // Dalgadaki bütün olayları tara
            foreach (SpawnEvent spawnEvent in currentRoundProfile.spawnEvents)
            {
                GameObject enemyPrefab = spawnEvent.enemyPrefab;
                if (enemyPrefab == null)
                {
                    Debug.LogWarning($"WaveProfile ({currentRoundProfile.name}) içinde 'enemyPrefab' atanmamış bir SpawnEvent bulundu. Bu olay atlanıyor.");
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
            
            Debug.Log("--- WaveManager: Havuz Hesaplaması Tamamlandı ---");
        }
        
        
        /// <summary>
        /// Yeni düşman spawn etmeyi DURDURUR.
        /// </summary>
        public void StopWaveSpawning()
        {
            waveActive = false;
            StopAllCoroutines();
            Debug.Log("WaveManager: Yeni düşman spawn'ı durduruldu.");
        }

        /// <summary>
        /// Sahnede o anda aktif olan TÜM "Enemy" tag'li objeleri bulur
        /// ve onları 'Die()' metodunu çağırarak öldürür.
        /// </summary>
        public void KillAllActiveEnemies()
        {
            GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            
            Debug.Log($"WaveManager: Tur bitti. Sahnede kalan {activeEnemies.Length} adet düşman öldürülüyor...");

            foreach (GameObject enemy in activeEnemies)
            {
                HealthSystem hs = enemy.GetComponent<HealthSystem>();
                if (hs != null)
                {
                    hs.Die();
                }
                else
                {
                    Destroy(enemy);
                }
            }
        }

        /// <summary>
        /// 'Start'ta oluşturulan tüm dinamik havuzları (düşman ve efekt) TEMİZLER.
        /// </summary>
        public void CleanupDynamicPools()
        {
            if (ObjectPooler.Instance == null) return;
            Debug.Log("--- WaveManager: Dinamik Havuzlar Temizleniyor... ---");

            if (dynamicEnemyPools != null)
            {
                foreach (var entry in dynamicEnemyPools)
                {
                    ObjectPooler.Instance.DestroyPool(entry.Key.name);
                }
                dynamicEnemyPools.Clear(); 
            }

            if (dynamicEffectPools != null)
            {
                foreach (var entry in dynamicEffectPools)
                {
                    ObjectPooler.Instance.DestroyPool(entry.Key.name);
                }
                dynamicEffectPools.Clear(); 
            }
        }


        private void Update()
        {
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }
            
            if (currentRoundProfile == null) return; 

            for (int i = 0; i < currentRoundProfile.spawnEvents.Count; i++)
            {
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i])
                {
                    SpawnEvent currentEvent = currentRoundProfile.spawnEvents[i];
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

        /// <summary>
        /// Dalga olaylarını başlatır.
        /// </summary>
        public void StartNextWave()
        {
            if (currentRoundProfile != null && currentRoundProfile.spawnEvents.Count > 0)
            {
                Debug.Log($"WaveManager: '{currentRoundProfile.name}' profili başlatılıyor!");

                nextEventTriggerTimes = new List<float>();
                foreach (var spawnEvent in currentRoundProfile.spawnEvents)
                {
                    nextEventTriggerTimes.Add(spawnEvent.triggerTime);
                }

                waveActive = true;
            }
            else
            {
                Debug.LogWarning($"WaveManager: '{currentRoundProfile.name}' profilinde hiç 'Spawn Event' bulunamadı!");
                waveActive = false;
            }
        }

        /// <summary>
        /// Bir spawn olayını (burst) gerçekleştirir.
        /// </summary>
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
            string poolTag = prefabToSpawn.name; 

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