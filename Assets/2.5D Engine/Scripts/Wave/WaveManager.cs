/*
 * WAVE MANAGER (YÖNETİCİ MODELİ - v5.0 Data-Driven Refactor)
 * * DEĞİŞİKLİKLER:
 * - YENİ ALAN: 'genericEnemyPrefab'. Artık tüm düşmanlar için
 * spawn edilecek TEK prefab budur.
 * - 'dynamicEnemyPools' (Dictionary) kaldırıldı, yerine
 * 'totalEnemyPoolSize' (int) geldi.
 * - 'CalculatePoolRequirements()' METODU GÜNCELLENDİ:
 * - Artık 'spawnEvent.enemyDataToSpawn'ı okuyor.
 * - Farklı prefab'lar için havuz hesaplamak yerine, spawn olacak
 * TOPLAM düşman sayısını hesaplıyor.
 * - Ölüm efekti havuzunu 'enemyData.deathEffectPrefab' üzerinden
 * hesaplamaya devam ediyor (BU ÇOK ÖNEMLİ).
 * - 'Start()' METODU GÜNCELLENDİ:
 * - Artık 'CreateDynamicPools'u çağırmıyor.
 * - 'ObjectPooler.Instance.CreatePool'u DOĞRUDAN çağırarak,
 * 'genericEnemyPrefab'den 'totalEnemyPoolSize' adet içeren
 * TEK BİR havuz oluşturuyor.
 * - 'SpawnBurst()' METODU GÜNCELLENDİ:
 * - Spawn edeceği 'poolTag' artık 'genericEnemyPrefab.name'dir.
 * - Spawn ettiği objeye 'GetComponent<EnemyAI>()' yapar.
 * - 'enemyAI.Initialize()' metodunu, 'spawnEvent.enemyDataToSpawn' verisi,
 * 'playerTarget' ve (varsa) 'path' ile çağırır.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IndianOceanAssets.Engine2_5D
{
    [DefaultExecutionOrder(-10)]
    public class WaveManager : MonoBehaviour
    {
        #region Singleton
        public static WaveManager Instance { get; private set; }
        #endregion

        // --- DEĞİŞİKLİK BAŞLANGICI (Tek Prefab) ---
        [Header("Havuz Ayarları")]
        [Tooltip("Spawn edilecek TÜM düşmanlar için kullanılacak olan " +
                 "tek (generic) düşman prefab'ı. Üzerinde EnemyAI ve HealthSystem olmalı.")]
        [SerializeField] private GameObject genericEnemyPrefab;
        
        // Bu artık 'genericEnemyPrefab'dan kaç tane gerektiğini tutacak
        private int totalEnemyPoolSize = 0; 
        
        // private Dictionary<GameObject, int> dynamicEnemyPools; // <-- SİLİNDİ
        // --- DEĞİŞİKLİK SONU ---
        
        // Ölüm efektleri için bu sözlüğe hala ihtiyacımız var
        private Dictionary<GameObject, int> dynamicEffectPools;
        
        [Header("References")]
        [SerializeField] private RoundManager roundManager;
        
        [Header("Wave Data")]
        [SerializeField] private WaveProfile currentRoundProfile; 
        
        // Sahne referansları (Değişiklik yok)
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();
        private Dictionary<int, EnemyPath> enemyPaths = new Dictionary<int, EnemyPath>();
        
        private List<float> nextEventTriggerTimes;
        private Transform playerTarget;
        private bool waveActive = false;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Sahnedeki yolları ve spawn noktalarını bul
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
            enemyPaths = FindObjectsOfType<EnemyPath>().ToDictionary(path => path.pathID);
        }

        private void Start()
        {
            // 1. Referansları Kontrol Et
            if (roundManager == null)
            {
                Debug.LogError("WaveManager: Sahnede 'RoundManager' bulunamadı!");
                return;
            }
            if (currentRoundProfile == null)
            {
                Debug.LogError("WaveManager: 'Current Round Profile' atanmamış!");
                return;
            }
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            if (genericEnemyPrefab == null)
            {
                 Debug.LogError("WaveManager: 'Generic Enemy Prefab' atanmamış! " +
                                "Düşman spawn edilemez.");
                return;
            }
            // --- DEĞİŞİKLİK SONU ---
            
            // 2. Oyuncu hedefini bul
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) { playerTarget = playerGO.transform; }
            else { Debug.LogError("WaveManager: 'Player' etiketli oyuncu bulunamadı!"); }

            // 3. RoundManager'ı Başlat
            roundManager.InitializeRound(
                currentRoundProfile.roundDuration, 
                currentRoundProfile.victoryDelay
            );

            // 4. Havuz ihtiyacını hesapla
            CalculatePoolRequirements();

            // --- DEĞİŞİKLİK BAŞLANGICI (Tek Havuz Oluşturma) ---
            // 5. Dinamik Düşman Havuzunu Oluştur (SADECE BİR KEZ)
            Debug.Log($"--- WaveManager: Düşman Havuzu Oluşturuluyor... ---");
            ObjectPooler.Instance.CreatePool(
                genericEnemyPrefab.name, // "Enemy" (veya prefab'ın adı)
                genericEnemyPrefab,      // Prefab'ın kendisi
                totalEnemyPoolSize       // Hesaplanan toplam sayı
            );
            
            // 6. Dinamik Efekt Havuzlarını Oluştur
            // (Bu eski metodu efektler için kullanmaya devam ediyoruz)
            CreateDynamicPools(dynamicEffectPools, "Efekt");
            // --- DEĞİŞİKLİK SONU ---
            
            // 7. Dalgaları Başlat
            StartNextWave();
        }

        // 'CreateDynamicPools' metodu artık SADECE efektler için kullanılıyor
        private void CreateDynamicPools(Dictionary<GameObject, int> poolDict, string poolType)
        {
            if (poolDict != null && poolDict.Count > 0)
            {
                Debug.Log($"--- WaveManager: {poolType} Havuzları Oluşturuluyor... ({poolDict.Count} tip) ---");
                foreach (var entry in poolDict)
                {
                    if (entry.Key == null) continue;
                    ObjectPooler.Instance.CreatePool(entry.Key.name, entry.Key, entry.Value);
                }
            }
        }
        
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Havuz Hesaplaması) ---
        /// <summary>
        /// 'currentRoundProfile'ı analiz eder ve havuz ihtiyaçlarını hesaplar.
        /// (v5.0 - Tek prefab havuzu için güncellendi)
        /// </summary>
        private void CalculatePoolRequirements()
        {
            // Sözlükleri sıfırla
            totalEnemyPoolSize = 0;
            dynamicEffectPools = new Dictionary<GameObject, int>();

            if (currentRoundProfile == null || currentRoundProfile.spawnEvents.Count == 0)
            {
                Debug.LogWarning("WaveManager: 'Current Round Profile' boş.");
                return;
            }

            float roundDuration = roundManager.RoundDuration;
            
            foreach (SpawnEvent spawnEvent in currentRoundProfile.spawnEvents)
            {
                // Artık 'enemyPrefab' yerine 'enemyDataToSpawn'ı okuyoruz
                EnemyData enemyData = spawnEvent.enemyDataToSpawn;
                
                if (enemyData == null)
                {
                    Debug.LogWarning($"WaveProfile ({currentRoundProfile.name}) içinde 'Enemy Data To Spawn' " +
                                     "atanmamış bir SpawnEvent bulundu. Bu olay atlanıyor.");
                    continue; 
                }

                int countForThisEvent = 0;
                
                // Spawn tekrarı hesaplaması (Değişiklik yok)
                if (spawnEvent.isPeriodic)
                {
                    if (spawnEvent.repeatInterval < 0.1f) { countForThisEvent = spawnEvent.count; }
                    else
                    {
                        float effectiveEndTime = roundDuration; 
                        if (spawnEvent.hasFiniteDuration && spawnEvent.endTime < effectiveEndTime)
                        {
                            effectiveEndTime = spawnEvent.endTime;
                        }
                        float activeDuration = effectiveEndTime - spawnEvent.triggerTime;
                        
                        if (activeDuration > 0)
                        {
                            int repetitions = Mathf.FloorToInt(activeDuration / spawnEvent.repeatInterval) + 1;
                            countForThisEvent = spawnEvent.count * repetitions;
                        }
                    }
                }
                else
                {
                    if(spawnEvent.triggerTime <= roundDuration) { countForThisEvent = spawnEvent.count; }
                }
                
                if (countForThisEvent == 0) continue; 

                // 1. Düşman havuzunu güncelle (Artık sadece toplamı sayıyoruz)
                totalEnemyPoolSize += countForThisEvent; 

                // 2. Ölüm efekti havuzunu güncelle (Artık 'EnemyData'dan okuyoruz)
                GameObject deathEffectPrefab = enemyData.deathEffectPrefab;
                if (deathEffectPrefab != null)
                {
                    if (!dynamicEffectPools.ContainsKey(deathEffectPrefab))
                    {
                        dynamicEffectPools.Add(deathEffectPrefab, 0); 
                    }
                    dynamicEffectPools[deathEffectPrefab] += countForThisEvent; 
                }
            } 
            
            Debug.Log($"--- WaveManager: Havuz Hesaplaması Tamamlandı ---");
            Debug.Log($"Gereken Toplam Düşman Havuz Boyutu: {totalEnemyPoolSize}");
        }
        // --- DEĞİŞİKLİK SONU ---
        
        
        public void StopWaveSpawning()
        {
            waveActive = false;
            StopAllCoroutines();
            Debug.Log("WaveManager: Yeni düşman spawn'ı durduruldu.");
        }

        public void KillAllActiveEnemies()
        {
            GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in activeEnemies)
            {
                HealthSystem hs = enemy.GetComponent<HealthSystem>();
                if (hs != null) hs.Die();
                else Destroy(enemy);
            }
        }
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Havuz Temizleme) ---
        public void CleanupDynamicPools()
        {
            if (ObjectPooler.Instance == null) return;
            Debug.Log("--- WaveManager: Dinamik Havuzlar Temizleniyor... ---");

            // 1. Tekil düşman havuzunu temizle
            if (genericEnemyPrefab != null)
            {
                ObjectPooler.Instance.DestroyPool(genericEnemyPrefab.name);
            }

            // 2. Efekt havuzlarını temizle
            CleanupPoolDictionary(dynamicEffectPools);
        }
        
        private void CleanupPoolDictionary(Dictionary<GameObject, int> poolDict)
        {
            if (poolDict != null)
            {
                foreach (var entry in poolDict)
                {
                    if (entry.Key == null) continue;
                    ObjectPooler.Instance.DestroyPool(entry.Key.name);
                }
                poolDict.Clear(); 
            }
        }
        // --- DEĞİŞİKLİK SONU ---


        private void Update()
        {
            // ... (Bu metotta değişiklik yok) ...
            #region No Change in Update
            if (!waveActive || !roundManager.IsRoundActive || currentRoundProfile == null)
            {
                return;
            }
            
            float currentTime = roundManager.TimeElapsed;
            float roundDuration = roundManager.RoundDuration;

            for (int i = 0; i < currentRoundProfile.spawnEvents.Count; i++)
            {
                if (nextEventTriggerTimes[i] == Mathf.Infinity) continue;
                
                if (currentTime >= nextEventTriggerTimes[i])
                {
                    SpawnEvent currentEvent = currentRoundProfile.spawnEvents[i];
                    StartCoroutine(SpawnBurst(currentEvent)); 

                    if (currentEvent.isPeriodic)
                    {
                        float nextSpawnTime = nextEventTriggerTimes[i] + currentEvent.repeatInterval;
                        float effectiveEndTime = roundDuration; 
                        
                        if (currentEvent.hasFiniteDuration && currentEvent.endTime < effectiveEndTime)
                        {
                            effectiveEndTime = currentEvent.endTime;
                        }

                        if (nextSpawnTime <= effectiveEndTime) { nextEventTriggerTimes[i] = nextSpawnTime; }
                        else { nextEventTriggerTimes[i] = Mathf.Infinity; }
                    }
                    else
                    {
                        nextEventTriggerTimes[i] = Mathf.Infinity;
                    }
                }
            }
            #endregion
        }

        public void StartNextWave()
        {
            // ... (Bu metotta değişiklik yok) ...
            #region No Change in StartNextWave
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
            #endregion
        }
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Data-Driven SpawnBurst) ---
        /// <summary>
        /// Bir spawn olayını (burst) gerçekleştirir.
        /// (v5.0 - Tek prefab ve EnemyData enjeksiyonu için güncellendi)
        /// </summary>
        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            // 1. Hangi DATAYI spawn edeceğimizi al
            EnemyData dataToSpawn = spawnEvent.enemyDataToSpawn;
            if (dataToSpawn == null)
            {
                Debug.LogError("SpawnEvent'te 'Enemy Data To Spawn' atanmamış!");
                yield break;
            }
            
            // 2. Nerede spawn edeceğimizi bul
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı!");
                yield break; 
            }
            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];
            
            // 3. Hangi YOLU takip edeceğimizi bul
            Transform[] waypointsToFollow = null; 
            if (spawnEvent.pathID != -1)
            {
                if (enemyPaths.ContainsKey(spawnEvent.pathID))
                {
                    waypointsToFollow = enemyPaths[spawnEvent.pathID].waypoints;
                }
                else
                {
                    Debug.LogWarning($"WaveManager: 'WaveProfile' {spawnEvent.pathID} ID'li bir yol " +
                                     $"istedi ancak bu ID sahnede bulunamadı.", this);
                }
            }
            
            // 4. Hangi HAVUZU kullanacağımızı belirle (Artık hep aynı)
            string poolTag = genericEnemyPrefab.name; 

            for (int i = 0; i < spawnEvent.count; i++)
            {
                if (!waveActive) yield break;

                // 5. TEK (GENERIC) PREFAB'ı havuzdan al
                GameObject spawnedEnemy = ObjectPooler.Instance.SpawnFromPool(poolTag, spawnPoint.transform.position, Quaternion.identity);
                
                if (spawnedEnemy == null)
                {
                     Debug.LogError($"'{poolTag}' havuzu boşaldı! Hesaplama yetersiz. Spawn durduruldu.");
                     yield break;
                }
                
                // 6. Havuz etiketini ayarla
                IPooledObject pooledObj = spawnedEnemy.GetComponent<IPooledObject>();
                if (pooledObj != null)
                {
                    pooledObj.PoolTag = poolTag;
                }
                
                // 7. Düşman motorunu (AI) bul ve VERİYİ ENJEKTE ET
                EnemyAI enemyAI = spawnedEnemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    // Düşmana statlarını (data), hedefini (Player) ve yolunu (varsa) ver
                    enemyAI.Initialize(dataToSpawn, playerTarget, waypointsToFollow);
                }
                
                if (spawnEvent.spawnInterval > 0)
                {
                    yield return new WaitForSeconds(spawnEvent.spawnInterval);
                }
            }
        }
        // --- DEĞİŞİKLİK SONU ---
    }
}