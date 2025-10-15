using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IndianOceanAssets.Engine2_5D
{
    public class WaveManager : MonoBehaviour
    {
        [Tooltip("Bu seviyede oynanacak dalga profillerinin listesi.")]
        [SerializeField] private List<WaveProfile> waves;

        [SerializeField] private RoundManager roundManager;

        [Header("Dynamic Pooling Settings")]
        [Tooltip("Otomatik oluşturulan havuzların başlangıç boyutu.")]
        [SerializeField] private int defaultPoolSize = 10;

        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();
        private List<float> nextEventTriggerTimes;
        private int currentWaveIndex = 0;
        private bool waveActive = false;

        private void Awake()
        {
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }

            StartNextWave();
        }

        private void Update()
        {
            if (!waveActive || !roundManager.IsRoundActive || waves.Count == 0)
            {
                return;
            }

            WaveProfile currentWave = waves[currentWaveIndex - 1];
            for (int i = 0; i < currentWave.spawnEvents.Count; i++)
            {
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i])
                {
                    StartCoroutine(SpawnBurst(currentWave.spawnEvents[i]));
                    nextEventTriggerTimes[i] += currentWave.spawnEvents[i].startDelay;
                }
            }
        }

        public void StartNextWave()
        {
            if (waves != null && waves.Count > currentWaveIndex)
            {
                Debug.Log($"Dalga {currentWaveIndex + 1} hazırlanıyor ve başlıyor!");
                WaveProfile currentWave = waves[currentWaveIndex];

                // YENİ MANTIK: Dalgayı başlatmadan önce havuzları hazırla.
                PreparePoolsForWave(currentWave);

                nextEventTriggerTimes = new List<float>();
                foreach (var spawnEvent in currentWave.spawnEvents)
                {
                    nextEventTriggerTimes.Add(spawnEvent.startDelay);
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

        // YENİ FONKSİYON: Bir dalga için gerekli havuzları otomatik olarak oluşturan fonksiyon.
        private void PreparePoolsForWave(WaveProfile wave)
        {
            foreach (var spawnEvent in wave.spawnEvents)
            {
                // Gerekli bilgiler var mı kontrol et.
                if (spawnEvent.enemyPrefab != null && !string.IsNullOrEmpty(spawnEvent.poolTag))
                {
                    // ObjectPooler'a git ve bu havuzu oluşturmasını iste.
                    // Havuz zaten varsa, ObjectPooler'daki mantığımız sayesinde tekrar oluşturulmayacak.
                    ObjectPooler.Instance.CreatePool(spawnEvent.poolTag, spawnEvent.enemyPrefab, defaultPoolSize, true);
                }
            }
        }

        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı!");
                yield break;
            }

            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];

            for (int i = 0; i < spawnEvent.count; i++)
            {
                ObjectPooler.Instance.SpawnFromPool(spawnEvent.poolTag, spawnPoint.transform.position, Quaternion.identity);
                yield return new WaitForSeconds(spawnEvent.spawnInterval);
            }
        }
    }
}