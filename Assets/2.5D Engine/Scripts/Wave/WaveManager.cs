/*
 * WAVE MANAGER (YÖNETİCİ MODELİ - v4.4)
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
        [Tooltip("Bu seviyede oynanacak dalga profili.")]
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
            CreateDynamicPools(dynamicEnemyPools, "Düşman");
            
            // 6. Dinamik Efekt Havuzlarını Oluştur
            CreateDynamicPools(dynamicEffectPools, "Efekt");
            
            // 7. Dalgaları Başlat
            StartNextWave();
        }

        /// <summary>
        /// ObjectPooler'a havuz oluşturma komutlarını gönderen yardımcı metot.
        /// </summary>
        private void CreateDynamicPools(Dictionary<GameObject, int> poolDict, string poolType)
        {
            if (poolDict != null && poolDict.Count > 0)
            {
                Debug.Log($"--- WaveManager: {poolType} Havuzları Oluşturuluyor... ({poolDict.Count} tip) ---");
                foreach (var entry in poolDict)
                {
                    if (entry.Key == null)
                    {
                        Debug.LogWarning($"WaveManager: {poolType} havuzunda 'null' prefab bulundu. Atlanıyor.");
                        continue;
                    }
                    ObjectPooler.Instance.CreatePool(entry.Key.name, entry.Key, entry.Value);
                }
            }
            else
            {
                Debug.LogWarning($"WaveManager: Hesaplama sonucunda spawn edilecek {poolType} bulunamadı.");
            }
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

            // Turun toplam süresini al (bu, varsayılan bitiş zamanıdır)
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
                    if (spawnEvent.repeatInterval < 0.1f) 
                    {
                        Debug.LogWarning($"WaveProfile ({currentRoundProfile.name}) içindeki bir periyodik olayın 'repeatInterval' değeri çok düşük. Tek seferlik olarak hesaplanacak.");
                        countForThisEvent = spawnEvent.count; 
                    }
                    else
                    {
                        // 1. Geçerli bitiş zamanını belirle:
                        float effectiveEndTime = roundDuration; 
                        
                        if (spawnEvent.hasFiniteDuration && spawnEvent.endTime < effectiveEndTime)
                        {
                            effectiveEndTime = spawnEvent.endTime;
                        }

                        // 2. Aktif süreyi hesapla
                        float activeDuration = effectiveEndTime - spawnEvent.triggerTime;
                        
                        // 3. Tekrar sayısını hesapla
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
        /// Sahnede o anda aktif olan TÜM "Enemy" tag'li objeleri bulur ve öldürür.
        /// </summary>
        public void KillAllActiveEnemies()
        {
            GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            Debug.Log($"WaveManager: Tur bitti. Sahnede kalan {activeEnemies.Length} adet düşman öldürülüyor...");
            foreach (GameObject enemy in activeEnemies)
            {
                HealthSystem hs = enemy.GetComponent<HealthSystem>();
                if (hs != null) hs.Die();
                else Destroy(enemy);
            }
        }

        /// <summary>
        /// 'Start'ta oluşturulan tüm dinamik havuzları (düşman ve efekt) TEMİZLER.
        /// </summary>
        public void CleanupDynamicPools()
        {
            if (ObjectPooler.Instance == null) return;
            Debug.Log("--- WaveManager: Dinamik Havuzlar Temizleniyor... ---");

            CleanupPoolDictionary(dynamicEnemyPools);
            CleanupPoolDictionary(dynamicEffectPools);
        }

        /// <summary>
        /// 'CleanupDynamicPools' için yardımcı metot.
        /// </summary>
        private void CleanupPoolDictionary(Dictionary<GameObject, int> poolDict)
        {
            if (poolDict != null)
            {
                foreach (var entry in poolDict)
                {
                    if (entry.Key == null) continue; // Güvenlik
                    ObjectPooler.Instance.DestroyPool(entry.Key.name);
                }
                poolDict.Clear(); 
            }
        }


        private void Update()
        {
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }
            
            if (currentRoundProfile == null) return; 

            float currentTime = roundManager.TimeElapsed;
            float roundDuration = roundManager.RoundDuration; // Turun sonunu bilmemiz gerek

            for (int i = 0; i < currentRoundProfile.spawnEvents.Count; i++)
            {
                // Eğer olay bittiyse (Infinity), atla
                if (nextEventTriggerTimes[i] == Mathf.Infinity)
                {
                    continue;
                }

                // Tetiklenme zamanı geldi mi?
                if (currentTime >= nextEventTriggerTimes[i])
                {
                    SpawnEvent currentEvent = currentRoundProfile.spawnEvents[i];
                    StartCoroutine(SpawnBurst(currentEvent)); // Düşmanları spawn et

                    // --- Sonraki Spawn Zamanını Planlama ---
                    if (currentEvent.isPeriodic)
                    {
                        // 1. Bir sonraki spawn zamanını hesapla
                        float nextSpawnTime = nextEventTriggerTimes[i] + currentEvent.repeatInterval;
                        
                        // 2. Geçerli bitiş zamanını belirle
                        // Varsayılan bitiş, tur süresidir (roundDuration).
                        float effectiveEndTime = roundDuration; 
                        
                        if (currentEvent.hasFiniteDuration && currentEvent.endTime < effectiveEndTime)
                        {
                            effectiveEndTime = currentEvent.endTime;
                        }

                        // 3. Kontrol et: Bir sonraki spawn, bitiş zamanından önce mi?
                        if (nextSpawnTime <= effectiveEndTime)
                        {
                            // Evet, bir sonraki spawn'ı planla
                            nextEventTriggerTimes[i] = nextSpawnTime;
                        }
                        else
                        {
                            // Hayır, bu olayın son spawn'ıydı. Olayı bitir.
                            nextEventTriggerTimes[i] = Mathf.Infinity;
                        }
                    }
                    else
                    {
                        // Tek seferlik olaydı, olayı bitir.
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
            if (prefabToSpawn == null) yield break;
            
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı!");
                yield break; 
            }

            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];
            string poolTag = prefabToSpawn.name; 

            for (int i = 0; i < spawnEvent.count; i++)
            {
                if (!waveActive) yield break;

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
                
                if (spawnEvent.spawnInterval > 0)
                {
                    yield return new WaitForSeconds(spawnEvent.spawnInterval);
                }
            }
        }
    }
}