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

        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();

        // YENİ: Her bir spawn olayının bir sonraki tetiklenme zamanını takip eden liste.
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
            // Tur veya dalga aktif değilse hiçbir şey yapma.
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }

            // Aktif dalganın içindeki her bir olayı kontrol et.
            WaveProfile currentWave = waves[currentWaveIndex - 1];
            for (int i = 0; i < currentWave.spawnEvents.Count; i++)
            {
                // Ana saat, bu olayın sıradaki tetiklenme zamanını geçti mi?
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i])
                {
                    // Geçtiyse: Bir düşman "patlaması" başlat.
                    StartCoroutine(SpawnBurst(currentWave.spawnEvents[i]));

                    // VE en önemlisi: Bir sonraki tetiklenme zamanını şimdi hesapla.
                    nextEventTriggerTimes[i] += currentWave.spawnEvents[i].startDelay;
                }
            }
        }

        public void StartNextWave()
        {
            if (waves != null && waves.Count > currentWaveIndex)
            {
                Debug.Log($"Dalga {currentWaveIndex + 1} başlıyor!");
                WaveProfile currentWave = waves[currentWaveIndex];

                // Zaman takip listesini sıfırla ve ilk tetiklenme zamanlarını ayarla.
                nextEventTriggerTimes = new List<float>();
                foreach (var spawnEvent in currentWave.spawnEvents)
                {
                    // Her olayın ilk tetiklenme zamanı, kendi gecikmesidir.
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

        // YENİ: Bir "patlama" (burst) şeklinde düşman spawn eden Coroutine.
        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı!");
                yield break; // Coroutine'i sonlandır.
            }

            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];

            for (int i = 0; i < spawnEvent.count; i++)
            {
                ObjectPooler.Instance.SpawnFromPool("enemy", spawnPoint.transform.position, Quaternion.identity);
                yield return new WaitForSeconds(spawnEvent.spawnInterval);
            }
        }
    }
}