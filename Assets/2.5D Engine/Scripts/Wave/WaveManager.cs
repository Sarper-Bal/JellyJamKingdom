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

        [Header("Havuz")]
        [SerializeField] private GameObject genericEnemyPrefab;
        
        [Header("Referanslar")]
        [SerializeField] private RoundManager roundManager;
        
        private WaveProfile currentRoundProfile; 
        
        // Muhasebe
        private int totalEnemiesToSpawn = 0; 
        private int spawnedEnemiesCount = 0; 
        private int activeEnemyCount = 0;    
        
        // --- OPTİMİZASYON: AKTİF DÜŞMAN LİSTESİ ---
        // Sahneyi taramak yerine, canlı olanları burada tutacağız.
        private List<GameObject> activeEnemiesList = new List<GameObject>();
        // ------------------------------------------

        private int totalEnemyPoolSize = 0;
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
            if (genericEnemyPrefab == null) Debug.LogError("WaveManager: Generic Enemy Prefab eksik!");
            
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) playerTarget = playerGO.transform;
        }

        public void LoadAndStartWave(WaveProfile profile)
        {
            if (profile == null) return;
            CleanupDynamicPools();
            currentRoundProfile = profile; 
            StartWaveRoutine(profile);
        }

        // --- GÜNCELLENMİŞ ÖLÜM BİLDİRİMİ ---
        public void OnEnemyKilled(GameObject enemyObj)
        {
            if (!waveActive) return;
            
            // Listeden çıkar
            if (activeEnemiesList.Contains(enemyObj))
            {
                activeEnemiesList.Remove(enemyObj);
            }

            activeEnemyCount--;
            if (activeEnemyCount < 0) activeEnemyCount = 0;
            CheckEarlyWinCondition();
        }
        // -----------------------------------

        private void CheckEarlyWinCondition()
        {
            if (spawnedEnemiesCount >= totalEnemiesToSpawn && activeEnemyCount == 0)
            {
                Debug.Log("WaveManager: Erken Zafer! (Early Win)");
                if (roundManager != null) roundManager.ForceEndRound();
            }
        }

        private void StartWaveRoutine(WaveProfile profile)
        {
            spawnedEnemiesCount = 0;
            activeEnemyCount = 0;
            totalEnemiesToSpawn = 0;
            
            // Listeyi güvenli temizle
            activeEnemiesList.Clear(); 

            roundManager.InitializeRound(profile.roundDuration, profile.victoryDelay);
            CalculatePoolRequirements(); 

            if (totalEnemiesToSpawn > 0)
                ObjectPooler.Instance.CreatePool(genericEnemyPrefab.name, genericEnemyPrefab, totalEnemiesToSpawn);
            
            CreateDynamicPools(dynamicEffectPools);
            StartNextWave();
        }

        // --- OPTİMİZE EDİLMİŞ TEMİZLİK ---
        public void ForceClearWave()
        {
            StopWaveSpawning();
            
            Debug.Log($"WaveManager: {activeEnemiesList.Count} aktif düşman temizleniyor.");

            // Listeyi tersten dönerek güvenli silme yapıyoruz
            for (int i = activeEnemiesList.Count - 1; i >= 0; i--)
            {
                GameObject enemy = activeEnemiesList[i];
                if (enemy != null && enemy.activeInHierarchy)
                {
                    HealthSystem hs = enemy.GetComponent<HealthSystem>();
                    if (hs != null) hs.Die(); // Die metodu havuza yollar ve OnEnemyKilled çağırır
                    else enemy.SetActive(false);
                }
            }
            
            activeEnemiesList.Clear();
            activeEnemyCount = 0;
        }
        // ---------------------------------

        #region Core Logic
        // ... (CalculatePoolRequirements, CreateDynamicPools, CleanupDynamicPools, StopWaveSpawning, Update, StartNextWave AYNI) ...
        // Kod kalabalığı olmasın diye buraları kısalttım, eski lojiklerin aynısı kalacak.
        // Sadece SpawnBurst içinde listeye ekleme yapacağız.

        private void CalculatePoolRequirements()
        {
            totalEnemiesToSpawn = 0; 
            dynamicEffectPools = new Dictionary<GameObject, int>();

            if (currentRoundProfile == null) return;
            float roundDuration = roundManager.RoundDuration;
            
            foreach (SpawnEvent spawnEvent in currentRoundProfile.spawnEvents)
            {
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

                totalEnemiesToSpawn += countForThisEvent; 

                if (enemyData.deathEffectPrefab != null)
                {
                    if (!dynamicEffectPools.ContainsKey(enemyData.deathEffectPrefab))
                        dynamicEffectPools.Add(enemyData.deathEffectPrefab, 0);
                    dynamicEffectPools[enemyData.deathEffectPrefab] += countForThisEvent; 
                }
            }
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
            for (int i = 0; i < currentRoundProfile.spawnEvents.Count; i++) {
                if (nextEventTriggerTimes[i] == Mathf.Infinity) continue;
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i]) {
                    SpawnEvent currentEvent = currentRoundProfile.spawnEvents[i];
                    StartCoroutine(SpawnBurst(currentEvent)); 
                    if (currentEvent.isPeriodic) {
                        float nextSpawnTime = nextEventTriggerTimes[i] + currentEvent.repeatInterval;
                        float effectiveEndTime = (currentEvent.hasFiniteDuration && currentEvent.endTime < roundManager.RoundDuration) 
                                                 ? currentEvent.endTime : roundManager.RoundDuration;
                        if (nextSpawnTime <= effectiveEndTime) nextEventTriggerTimes[i] = nextSpawnTime; else nextEventTriggerTimes[i] = Mathf.Infinity;
                    } else nextEventTriggerTimes[i] = Mathf.Infinity;
                }
            }
        }
        private void StartNextWave() {
            if (currentRoundProfile != null && currentRoundProfile.spawnEvents.Count > 0) {
                nextEventTriggerTimes = new List<float>();
                foreach (var spawnEvent in currentRoundProfile.spawnEvents) nextEventTriggerTimes.Add(spawnEvent.triggerTime);
                waveActive = true;
            } else waveActive = false;
        }

        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            EnemyData data = spawnEvent.enemyDataToSpawn;
            if (data != null && spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                EnemySpawnPoint sp = spawnPoints[spawnEvent.spawnPointID];
                Transform[] path = (spawnEvent.pathID != -1 && enemyPaths.ContainsKey(spawnEvent.pathID)) ? enemyPaths[spawnEvent.pathID].waypoints : null;
                string tag = genericEnemyPrefab.name;
                
                for (int i = 0; i < spawnEvent.count; i++)
                {
                    if (!waveActive) yield break;
                    GameObject obj = ObjectPooler.Instance.SpawnFromPool(tag, sp.transform.position, Quaternion.identity);
                    if (obj != null)
                    {
                        // --- LİSTEYE EKLEME ---
                        activeEnemiesList.Add(obj);
                        // ---------------------
                        
                        spawnedEnemiesCount++;
                        activeEnemyCount++;
                        
                        obj.GetComponent<EnemyAI>()?.Initialize(data, playerTarget, path);
                        obj.GetComponent<IPooledObject>().PoolTag = tag;
                    }
                    if (spawnEvent.spawnInterval > 0) yield return new WaitForSeconds(spawnEvent.spawnInterval);
                }
            }
        }
        #endregion
    }
}