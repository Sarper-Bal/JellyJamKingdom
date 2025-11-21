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

        [Header("Sistem Ayarları")]
        public bool autoStart = false; // Controller kullandığımız için kapalı kalsın

        [Header("Havuz Ayarları")]
        [SerializeField] private GameObject genericEnemyPrefab;
        private int totalEnemyPoolSize = 0; 
        private Dictionary<GameObject, int> dynamicEffectPools;
        
        [Header("Referanslar")]
        [SerializeField] private RoundManager roundManager;
        
        [Header("Varsayılan Veri")]
        [SerializeField] private WaveProfile currentRoundProfile; 
        
        // Runtime Veriler
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

            if (autoStart && currentRoundProfile != null)
            {
                LoadAndStartWave(currentRoundProfile);
            }
        }

        // --- YENİ: ANINDA TEMİZLİK METODU ---
        public void ForceClearWave()
        {
            Debug.Log("WaveManager: Sahne temizleniyor...");
            
            // 1. Spawn işlemini durdur
            StopWaveSpawning();

            // 2. Aktif düşmanları öldür/yok et
            GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in activeEnemies)
            {
                // Havuza döndürmek yerine direkt kapatıyoruz/yok ediyoruz
                // ki bir sonraki dalga temiz başlasın.
                if (enemy.activeInHierarchy)
                {
                    HealthSystem hs = enemy.GetComponent<HealthSystem>();
                    if (hs != null) hs.Die(); // Die metodu havuza döndürür
                    else enemy.SetActive(false);
                }
            }

            // 3. Efektleri veya yerdeki kalıntıları temizlemek istersen buraya ekle
        }
        // ------------------------------------

        public void LoadAndStartWave(WaveProfile profile)
        {
            if (profile == null) return;

            // Önceki havuzları temizle ve yenisine hazırlan
            CleanupDynamicPools();
            
            currentRoundProfile = profile;
            StartWaveRoutine(profile);
        }

        private void StartWaveRoutine(WaveProfile profile)
        {
            roundManager.InitializeRound(profile.roundDuration, profile.victoryDelay);
            CalculatePoolRequirements();

            if (totalEnemyPoolSize > 0)
                ObjectPooler.Instance.CreatePool(genericEnemyPrefab.name, genericEnemyPrefab, totalEnemyPoolSize);
            
            CreateDynamicPools(dynamicEffectPools);
            StartNextWave();
        }

        // ... (CalculatePoolRequirements, CreateDynamicPools, CleanupDynamicPools, StopWaveSpawning AYNI) ...
        // Kod tekrarı olmasın diye buraları kısa geçiyorum, önceki dosyadaki lojikler geçerli.
        // Ancak CalculatePoolRequirements vb. metotların silinmemesi gerekiyor. 
        // Eğer tam dosya istersen tekrar yazabilirim ama sadece ForceClearWave eklendi.

        #region Core Logic (Hidden for brevity - same as before)
        private void CalculatePoolRequirements() {
             totalEnemyPoolSize = 0; dynamicEffectPools = new Dictionary<GameObject, int>();
             if (currentRoundProfile == null) return;
             float roundDuration = roundManager.RoundDuration;
             foreach (SpawnEvent spawnEvent in currentRoundProfile.spawnEvents) {
                 EnemyData enemyData = spawnEvent.enemyDataToSpawn;
                 if (enemyData == null) continue;
                 int count = spawnEvent.count; 
                 if (spawnEvent.isPeriodic) {
                     float end = (spawnEvent.hasFiniteDuration && spawnEvent.endTime < roundDuration) ? spawnEvent.endTime : roundDuration;
                     float dur = end - spawnEvent.triggerTime;
                     if (dur > 0 && spawnEvent.repeatInterval > 0.1f) count = spawnEvent.count * (Mathf.FloorToInt(dur/spawnEvent.repeatInterval) + 1);
                 } else if (spawnEvent.triggerTime > roundDuration) continue;
                 
                 totalEnemyPoolSize += count;
                 if (enemyData.deathEffectPrefab != null) {
                     if(!dynamicEffectPools.ContainsKey(enemyData.deathEffectPrefab)) dynamicEffectPools.Add(enemyData.deathEffectPrefab, 0);
                     dynamicEffectPools[enemyData.deathEffectPrefab] += count;
                 }
             }
        }
        private void CreateDynamicPools(Dictionary<GameObject, int> poolDict) {
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
            float currentTime = roundManager.TimeElapsed;
            float roundDuration = roundManager.RoundDuration;
            for (int i = 0; i < currentRoundProfile.spawnEvents.Count; i++) {
                if (nextEventTriggerTimes[i] == Mathf.Infinity) continue;
                if (currentTime >= nextEventTriggerTimes[i]) {
                    SpawnEvent currentEvent = currentRoundProfile.spawnEvents[i];
                    StartCoroutine(SpawnBurst(currentEvent)); 
                    if (currentEvent.isPeriodic) {
                        float nextTime = nextEventTriggerTimes[i] + currentEvent.repeatInterval;
                        float endTime = (currentEvent.hasFiniteDuration && currentEvent.endTime < roundDuration) ? currentEvent.endTime : roundDuration;
                        if (nextTime <= endTime) nextEventTriggerTimes[i] = nextTime; else nextEventTriggerTimes[i] = Mathf.Infinity;
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