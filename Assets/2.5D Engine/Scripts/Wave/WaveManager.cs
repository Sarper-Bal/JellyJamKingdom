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

        [Header("Sistem")]
        public bool autoStart = false;

        [Header("Havuz")]
        [SerializeField] private GameObject genericEnemyPrefab;
        
        [Header("Referanslar")]
        [SerializeField] private RoundManager roundManager;
        
        [Header("Veri")]
        [SerializeField] private WaveProfile currentRoundProfile; 
        
        // --- YENİ: MUHASEBE DEĞİŞKENLERİ ---
        private int totalEnemiesToSpawn = 0; // Bu dalgada çıkacak TOPLAM düşman
        private int spawnedEnemiesCount = 0; // Şu ana kadar doğanlar
        private int activeEnemyCount = 0;    // Şu an sahnede canlı olanlar
        // -----------------------------------
        
        private Dictionary<GameObject, int> dynamicEffectPools;
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();
        private Dictionary<int, EnemyPath> enemyPaths = new Dictionary<int, EnemyPath>();
        private List<float> nextEventTriggerTimes;
        private Transform playerTarget;
        private bool waveActive = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
            enemyPaths = FindObjectsOfType<EnemyPath>().ToDictionary(path => path.pathID);
        }

        private void Start()
        {
            if (roundManager == null) Debug.LogError("WaveManager: RoundManager eksik!");
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) playerTarget = playerGO.transform;
            if (autoStart && currentRoundProfile != null) LoadAndStartWave(currentRoundProfile);
        }

        // --- YENİ: DÜŞMAN ÖLDÜ BİLDİRİMİ ---
        public void OnEnemyKilled()
        {
            if (!waveActive) return;

            activeEnemyCount--;
            
            // Güvenlik kontrolü (Negatif sayı olmasın)
            if (activeEnemyCount < 0) activeEnemyCount = 0;

            CheckEarlyWinCondition();
        }

        private void CheckEarlyWinCondition()
        {
            // Kural: Tüm düşmanlar doğduysa (spawned >= total) VE hiç canlı kalmadıysa (active == 0)
            if (spawnedEnemiesCount >= totalEnemiesToSpawn && activeEnemyCount == 0)
            {
                Debug.Log("WaveManager: Erken Zafer! (Early Win)");
                if (roundManager != null) roundManager.ForceEndRound();
            }
        }
        // ------------------------------------

        public void LoadAndStartWave(WaveProfile profile)
        {
            if (profile == null) return;
            CleanupDynamicPools();
            currentRoundProfile = profile;
            StartWaveRoutine(profile);
        }

        private void StartWaveRoutine(WaveProfile profile)
        {
            // Sayaçları Sıfırla
            spawnedEnemiesCount = 0;
            activeEnemyCount = 0;
            totalEnemiesToSpawn = 0;

            roundManager.InitializeRound(profile.roundDuration, profile.victoryDelay);
            
            // Toplam düşmanı hesapla (totalEnemiesToSpawn burada dolacak)
            CalculatePoolRequirements(); 

            if (totalEnemiesToSpawn > 0) // totalEnemiesToSpawn aynı zamanda havuz boyutu
                ObjectPooler.Instance.CreatePool(genericEnemyPrefab.name, genericEnemyPrefab, totalEnemiesToSpawn);
            
            CreateDynamicPools(dynamicEffectPools);
            StartNextWave();
        }

        public void ForceClearWave()
        {
            StopWaveSpawning();
            GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in activeEnemies)
            {
                if (enemy.activeInHierarchy)
                {
                    HealthSystem hs = enemy.GetComponent<HealthSystem>();
                    if (hs != null) hs.Die(); 
                    else enemy.SetActive(false);
                }
            }
        }

        private void CalculatePoolRequirements()
        {
            totalEnemiesToSpawn = 0; // Sıfırla
            dynamicEffectPools = new Dictionary<GameObject, int>();

            if (currentRoundProfile == null) return;
            float roundDuration = roundManager.RoundDuration;
            
            foreach (SpawnEvent spawnEvent in currentRoundProfile.spawnEvents)
            {
                // ... (Hesaplama mantığı aynı, sadece totalEnemiesToSpawn'a ekleme yapıyoruz) ...
                EnemyData enemyData = spawnEvent.enemyDataToSpawn;
                if (enemyData == null) continue;

                int countForThisEvent = 0;
                if (spawnEvent.isPeriodic)
                {
                    float effectiveEndTime = (spawnEvent.hasFiniteDuration && spawnEvent.endTime < roundDuration) 
                                             ? spawnEvent.endTime : roundDuration;
                    float activeDuration = effectiveEndTime - spawnEvent.triggerTime;
                    
                    if (activeDuration > 0 && spawnEvent.repeatInterval >= 0.1f)
                    {
                        int repetitions = Mathf.FloorToInt(activeDuration / spawnEvent.repeatInterval) + 1;
                        countForThisEvent = spawnEvent.count * repetitions;
                    }
                    else countForThisEvent = spawnEvent.count;
                }
                else
                {
                    if(spawnEvent.triggerTime <= roundDuration) countForThisEvent = spawnEvent.count;
                }
                
                if (countForThisEvent == 0) continue; 

                totalEnemiesToSpawn += countForThisEvent; // TOPLAM SAYIYI TUTUYORUZ

                if (enemyData.deathEffectPrefab != null)
                {
                    if (!dynamicEffectPools.ContainsKey(enemyData.deathEffectPrefab))
                        dynamicEffectPools.Add(enemyData.deathEffectPrefab, 0);
                    dynamicEffectPools[enemyData.deathEffectPrefab] += countForThisEvent; 
                }
            }
            Debug.Log($"WaveManager: Bu dalga için Toplam Beklenen Düşman: {totalEnemiesToSpawn}");
        }

        private void CreateDynamicPools(Dictionary<GameObject, int> poolDict)
        {
            if (poolDict == null) return;
            foreach (var entry in poolDict) if(entry.Key != null) ObjectPooler.Instance.CreatePool(entry.Key.name, entry.Key, entry.Value);
        }
        public void StopWaveSpawning() { waveActive = false; StopAllCoroutines(); }
        public void CleanupDynamicPools() {
            if (ObjectPooler.Instance == null) return;
            if (genericEnemyPrefab != null) ObjectPooler.Instance.DestroyPool(genericEnemyPrefab.name);
            if (dynamicEffectPools != null) {
                foreach (var entry in dynamicEffectPools) if (entry.Key != null) ObjectPooler.Instance.DestroyPool(entry.Key.name);
                dynamicEffectPools.Clear();
            }
        }
        private void Update() {
            if (!waveActive || !roundManager.IsRoundActive || currentRoundProfile == null) return;
            float currentTime = roundManager.RoundDuration - GetRemainingTime(); // Basit zaman hesabı
            // ... (Update mantığı aynı, zamanlayıcıları kontrol eder) ...
             for (int i = 0; i < currentRoundProfile.spawnEvents.Count; i++)
            {
                if (nextEventTriggerTimes[i] == Mathf.Infinity) continue;
                // RoundManager zamanı geriye sayıyor, biz geçen zamanı (Duration - CurrentTimer) bulabiliriz
                // Veya RoundManager'a TimeElapsed eklemiştik, onu kullanalım:
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i])
                {
                    SpawnEvent currentEvent = currentRoundProfile.spawnEvents[i];
                    StartCoroutine(SpawnBurst(currentEvent)); 

                    if (currentEvent.isPeriodic)
                    {
                        float nextSpawnTime = nextEventTriggerTimes[i] + currentEvent.repeatInterval;
                        float effectiveEndTime = (currentEvent.hasFiniteDuration && currentEvent.endTime < roundManager.RoundDuration) 
                                                 ? currentEvent.endTime : roundManager.RoundDuration;

                        if (nextSpawnTime <= effectiveEndTime) nextEventTriggerTimes[i] = nextSpawnTime;
                        else nextEventTriggerTimes[i] = Mathf.Infinity;
                    }
                    else nextEventTriggerTimes[i] = Mathf.Infinity;
                }
            }
        }
        private float GetRemainingTime() { return 0; /* Placeholder */ } // Kullanılmıyor, RoundManager.TimeElapsed kullanıyoruz.

        private void StartNextWave() {
            if (currentRoundProfile != null && currentRoundProfile.spawnEvents.Count > 0) {
                nextEventTriggerTimes = new List<float>();
                foreach (var spawnEvent in currentRoundProfile.spawnEvents) nextEventTriggerTimes.Add(spawnEvent.triggerTime);
                waveActive = true;
            } else waveActive = false;
        }
        
        private IEnumerator SpawnBurst(SpawnEvent spawnEvent) {
            EnemyData data = spawnEvent.enemyDataToSpawn;
            if (data != null && spawnPoints.ContainsKey(spawnEvent.spawnPointID)) {
                EnemySpawnPoint sp = spawnPoints[spawnEvent.spawnPointID];
                Transform[] path = (spawnEvent.pathID != -1 && enemyPaths.ContainsKey(spawnEvent.pathID)) ? enemyPaths[spawnEvent.pathID].waypoints : null;
                string tag = genericEnemyPrefab.name;
                
                for (int i = 0; i < spawnEvent.count; i++) {
                    if (!waveActive) yield break;
                    GameObject obj = ObjectPooler.Instance.SpawnFromPool(tag, sp.transform.position, Quaternion.identity);
                    if (obj != null) {
                        // --- YENİ: SAYIM YAP ---
                        spawnedEnemiesCount++;
                        activeEnemyCount++;
                        // -----------------------
                        
                        obj.GetComponent<EnemyAI>()?.Initialize(data, playerTarget, path);
                        obj.GetComponent<IPooledObject>().PoolTag = tag;
                    }
                    if (spawnEvent.spawnInterval > 0) yield return new WaitForSeconds(spawnEvent.spawnInterval);
                }
            }
        }
    }
}