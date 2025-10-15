using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IndianOceanAssets.Engine2_5D
{
    // Bu yardımcı sınıf, bir SpawnEvent'in durumunu (kaç tane spawn oldu, bir sonraki ne zaman vb.)
    // takip etmek için kullanılır.
    public class SpawnEventTracker
    {
        public SpawnEvent EventData { get; private set; }
        public int SpawnedCount { get; set; }
        public float NextSpawnTime { get; set; }
        public bool IsFinished { get { return SpawnedCount >= EventData.count; } }

        public SpawnEventTracker(SpawnEvent eventData)
        {
            EventData = eventData;
            SpawnedCount = 0;
            // İlk spawn zamanını, olayın başlama gecikmesine ayarlıyoruz.
            NextSpawnTime = eventData.startDelay;
        }
    }

    public class WaveManager : MonoBehaviour
    {
        [Tooltip("Bu seviyede oynanacak dalga profillerinin listesi.")]
        [SerializeField] private List<WaveProfile> waves;

        // YENİ: Sahnedeki RoundManager'a referans.
        [SerializeField] private RoundManager roundManager;

        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();
        private List<SpawnEventTracker> activeEventTrackers = new List<SpawnEventTracker>();
        private int currentWaveIndex = 0;
        private bool waveStarted = false;

        private void Awake()
        {
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            // Eğer RoundManager atanmamışsa, sahnede bulmaya çalış.
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }
        }

        // COROUTINE'LERİ TAMAMEN SİLİP YERİNE UPDATE KULLANIYORUZ.
        private void Update()
        {
            // Tur aktif değilse veya dalgalar bittiyse bir şey yapma.
            if (roundManager == null || !roundManager.IsRoundActive || currentWaveIndex > waves.Count)
            {
                return;
            }

            // Dalga henüz başlamadıysa ve başlatma zamanı geldiyse...
            if (!waveStarted)
            {
                StartNextWave();
                waveStarted = true;
            }

            // Eğer dalga başladıysa, tüm spawn olaylarını ana saate göre kontrol et.
            if (waveStarted)
            {
                ProcessActiveEvents();
            }
        }

        public void StartNextWave()
        {
            if (waves != null && waves.Count > currentWaveIndex)
            {
                Debug.Log($"Dalga {currentWaveIndex + 1} başlıyor!");
                WaveProfile currentWave = waves[currentWaveIndex];
                activeEventTrackers.Clear(); // Önceki dalganın takipçilerini temizle.

                // Dalga profilindeki her bir olay için bir takipçi oluştur.
                foreach (var spawnEvent in currentWave.spawnEvents)
                {
                    activeEventTrackers.Add(new SpawnEventTracker(spawnEvent));
                }
                currentWaveIndex++;
            }
            else
            {
                Debug.Log("Tüm dalgalar tamamlandı!");
            }
        }

        private void ProcessActiveEvents()
        {
            // Aktif olan her bir spawn olayını gözden geçir.
            foreach (var tracker in activeEventTrackers)
            {
                // Eğer bu olay zaten bittiyse (gerekli sayıda düşman spawn olduysa) atla.
                if (tracker.IsFinished)
                {
                    continue;
                }

                // Ana saat, bu düşmanın sıradaki spawn zamanını geçti mi?
                if (roundManager.TimeElapsed >= tracker.NextSpawnTime)
                {
                    // Geçtiyse, bir düşman spawn et.
                    if (spawnPoints.ContainsKey(tracker.EventData.spawnPointID))
                    {
                        EnemySpawnPoint spawnPoint = spawnPoints[tracker.EventData.spawnPointID];
                        Instantiate(tracker.EventData.enemyPrefab, spawnPoint.transform.position, Quaternion.identity);

                        // Takipçi bilgilerini güncelle.
                        tracker.SpawnedCount++;
                        tracker.NextSpawnTime += tracker.EventData.spawnInterval;
                    }
                    else
                    {
                        Debug.LogWarning($"Spawn Point ID: {tracker.EventData.spawnPointID} sahnede bulunamadı!");
                    }
                }
            }
        }
    }
}