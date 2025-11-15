/*
 * WAVE MANAGER (YÖNETİCİ MODELİ - v4.6 Düzeltme)
 * * DEĞİŞİKLİKLER:
 * - 'EnemyPath' ve 'EnemySpawnPoint' bağlılığı TAMAMEN AYRILDI.
 * - 'enemyPaths' adında yeni bir 'Dictionary<int, EnemyPath>' eklendi.
 * - 'Awake()' metodu artık sahnedeki tüm 'EnemyPath' objelerini
 * bulup 'pathID'lerine göre bu sözlüğe ekliyor.
 * - 'SpawnBurst()' metodu DÜZELTİLDİ:
 * - Artık 'spawnPoint.GetComponent<EnemyPath>()' ÇAĞIRMIYOR.
 * - Bunun yerine, 'spawnEvent.pathID'yi okuyor. (v1.2'den gelen)
 * - Eğer 'pathID' geçerliyse, yolu 'enemyPaths' sözlüğünden alıyor.
 * - Bu, 'FollowPath' modu için alınan 'null path' hatasını düzeltir.
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

        // Prefab'a göre ayrılmış havuz boyutları
        private Dictionary<GameObject, int> dynamicEnemyPools;
        private Dictionary<GameObject, int> dynamicEffectPools;
        
        [Header("References")]
        [SerializeField] private RoundManager roundManager;
        
        [Header("Wave Data")]
        [SerializeField] private WaveProfile currentRoundProfile; 
        
        // Sahnedeki tüm spawn noktalarını ve yolları ID'ye göre saklar
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();
        private Dictionary<int, EnemyPath> enemyPaths = new Dictionary<int, EnemyPath>(); // <-- YENİ (v4.6)
        
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
            
            // --- DEĞİŞİKLİK (v4.6) ---
            // Sahnedeki tüm spawn noktalarını ve yolları ID'lerine göre
            // oyun başlarken BİR KEZ bulup sözlüğe kaydet (Optimize)
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
            enemyPaths = FindObjectsOfType<EnemyPath>().ToDictionary(path => path.pathID);
            
            Debug.Log($"WaveManager: {spawnPoints.Count} adet Spawn Noktası, " +
                      $"{enemyPaths.Count} adet Düşman Yolu bulundu.");
            // --- DEĞİŞİKLİK SONU ---
        }

        private void Start()
        {
            // 1. Referansları Kontrol Et
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
                if (roundManager == null)
                {
                    Debug.LogError("WaveManager: Sahnede 'RoundManager' component'i bulunamadı!");
                    return;
                }
            }

            if (currentRoundProfile == null)
            {
                Debug.LogError("WaveManager üzerinde 'Current Round Profile' atanmamış!");
                return;
            }
            
            // 2. Oyuncu hedefini BİR KEZ bul ve sakla (Optimize)
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                playerTarget = playerGO.transform;
            }
            else
            {
                Debug.LogError("WaveManager: Sahnede 'Player' etiketli oyuncu bulunamadı!");
            }

            // 3. RoundManager'ı Başlat
            roundManager.InitializeRound(
                currentRoundProfile.roundDuration, 
                currentRoundProfile.victoryDelay
            );

            // 4. Havuz ihtiyacını hesapla
            CalculatePoolRequirements();

            // 5. Dinamik Havuzları Oluştur
            CreateDynamicPools(dynamicEnemyPools, "Düşman");
            CreateDynamicPools(dynamicEffectPools, "Efekt");
            
            // 6. Dalgaları Başlat
            StartNextWave();
        }
        
        // ... (Değişiklik Olmayan Metotlar) ...
        #region Bu Metotlarda Değişiklik Yok
        
        private void CreateDynamicPools(Dictionary<GameObject, int> poolDict, string poolType)
        {
            if (poolDict != null && poolDict.Count > 0)
            {
                foreach (var entry in poolDict)
                {
                    if (entry.Key == null) continue;
                    ObjectPooler.Instance.CreatePool(entry.Key.name, entry.Key, entry.Value);
                }
            }
        }
        
        private void CalculatePoolRequirements()
        {
            dynamicEnemyPools = new Dictionary<GameObject, int>();
            dynamicEffectPools = new Dictionary<GameObject, int>();

            if (currentRoundProfile == null || currentRoundProfile.spawnEvents.Count == 0) return;

            float roundDuration = roundManager.RoundDuration;
            
            foreach (SpawnEvent spawnEvent in currentRoundProfile.spawnEvents)
            {
                GameObject enemyPrefab = spawnEvent.enemyPrefab;
                if (enemyPrefab == null) continue; 

                int countForThisEvent = 0;
                
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

                if (!dynamicEnemyPools.ContainsKey(enemyPrefab)) { dynamicEnemyPools.Add(enemyPrefab, 0); }
                dynamicEnemyPools[enemyPrefab] += countForThisEvent; 

                HealthSystem hs = enemyPrefab.GetComponent<HealthSystem>();
                if (hs != null)
                {
                    GameObject deathEffectPrefab = hs.GetDeathEffectPrefab(); 
                    if (deathEffectPrefab != null)
                    {
                        if (!dynamicEffectPools.ContainsKey(deathEffectPrefab)) { dynamicEffectPools.Add(deathEffectPrefab, 0); }
                        dynamicEffectPools[deathEffectPrefab] += countForThisEvent; 
                    }
                }
            } 
        }
        
        public void StopWaveSpawning()
        {
            waveActive = false;
            StopAllCoroutines();
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
        
        public void CleanupDynamicPools()
        {
            if (ObjectPooler.Instance == null) return;
            CleanupPoolDictionary(dynamicEnemyPools);
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

        private void Update()
        {
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
        }

        public void StartNextWave()
        {
            if (currentRoundProfile != null && currentRoundProfile.spawnEvents.Count > 0)
            {
                nextEventTriggerTimes = new List<float>();
                foreach (var spawnEvent in currentRoundProfile.spawnEvents)
                {
                    nextEventTriggerTimes.Add(spawnEvent.triggerTime);
                }
                waveActive = true;
            }
        }
        
        #endregion

        /// <summary>
        /// Bir spawn olayını (burst) gerçekleştirir.
        /// (v4.6 - PathID düzeltmesi ile güncellendi)
        /// </summary>
        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            GameObject prefabToSpawn = spawnEvent.enemyPrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogError("SpawnEvent'te 'enemyPrefab' atanmamış!");
                yield break;
            }
            
            // 1. Spawn Noktasını Bul
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı!");
                yield break; 
            }
            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];
            
            string poolTag = prefabToSpawn.name; 
            
            // --- DEĞİŞİKLİK (v4.6) ---
            // 2. Takip Edilecek Yolu Bul
            Transform[] waypointsToFollow = null; // Varsayılan olarak yol yok
            
            // Bu event için özel bir 'pathID' (-1 değil) atanmış mı?
            if (spawnEvent.pathID != -1)
            {
                // Atanmış. 'Awake'te bulduğumuz 'enemyPaths' sözlüğünde bu ID var mı?
                if (enemyPaths.ContainsKey(spawnEvent.pathID))
                {
                    // Harika! Yolu (waypoint dizisini) al.
                    waypointsToFollow = enemyPaths[spawnEvent.pathID].waypoints;
                }
                else
                {
                    // 'WaveProfile' bir ID istiyor ama sahnede o ID'ye sahip
                    // bir 'EnemyPath' objesi yok.
                    Debug.LogWarning($"WaveManager: 'WaveProfile' {spawnEvent.pathID} ID'li bir yol " +
                                     $"istedi ancak bu ID sahnede bulunamadı.", this);
                    // 'waypointsToFollow' null olarak kalacak
                }
            }
            // 'playerTarget' zaten 'Start()' içinde bulundu ve saklandı.
            // --- DEĞİŞİKLİK SONU ---

            for (int i = 0; i < spawnEvent.count; i++)
            {
                if (!waveActive) yield break;

                // 3. Düşmanı havuzdan al
                GameObject spawnedEnemy = ObjectPooler.Instance.SpawnFromPool(poolTag, spawnPoint.transform.position, Quaternion.identity);
                
                if (spawnedEnemy == null)
                {
                     Debug.LogError($"'{poolTag}' havuzu boşaldı! Spawn durduruldu.");
                     yield break;
                }
                
                // 4. Havuz etiketini ayarla
                IPooledObject pooledObj = spawnedEnemy.GetComponent<IPooledObject>();
                if (pooledObj != null)
                {
                    pooledObj.PoolTag = poolTag;
                }
                
                // 5. Düşman motorunu (AI) bul ve başlat
                EnemyAI enemyAI = spawnedEnemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    // Düşmana hedefini (Player) ve (eğer bulunduysa) yolu (Waypoints) ver
                    // 'EnemyAI.Initialize' metodu, 'waypointsToFollow' null gelse bile
                    // 'FollowPath' modunda hata vermemesi için yazdığımız uyarıyı gösterecek.
                    enemyAI.Initialize(playerTarget, waypointsToFollow);
                }
                
                if (spawnEvent.spawnInterval > 0)
                {
                    yield return new WaitForSeconds(spawnEvent.spawnInterval);
                }
            }
        }
    }
}