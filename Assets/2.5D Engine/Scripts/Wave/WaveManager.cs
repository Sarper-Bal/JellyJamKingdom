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

        // Sahnedeki spawn noktalarını ID ile hızlıca bulmak için bir sözlük (Dictionary).
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();

        // Her bir spawn olayının bir sonraki tetiklenme zamanını takip eden liste.
        private List<float> nextEventTriggerTimes;

        private int currentWaveIndex = 0;
        private bool waveActive = false;

        private void Awake()
        {
            // Sahnedeki tüm spawn noktalarını bul ve ID'lerini anahtar olarak kullanarak sözlüğe ekle.
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            if (roundManager == null)
            {
                // RoundManager'ı sahnede bul (eğer Inspector'dan atanmadıysa).
                roundManager = FindObjectOfType<RoundManager>();
            }

            // İlk dalgayı başlat.
            StartNextWave();
        }

        private void Update()
        {
            // Tur veya dalga aktif değilse hiçbir şey yapma.
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }

            // Mevcut dalga profilini al.
            WaveProfile currentWave = waves[currentWaveIndex - 1];

            // Aktif dalganın içindeki her bir olayı kontrol et.
            for (int i = 0; i < currentWave.spawnEvents.Count; i++)
            {
                // --- DEĞİŞİKLİK BAŞLANGICI: Spawn Tetikleme Mantığı ---

                // Ana saat (TimeElapsed), bu olayın sıradaki tetiklenme zamanını (nextEventTriggerTimes[i]) geçti mi?
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i])
                {
                    // Zamanı gelen olayın referansını al
                    SpawnEvent currentEvent = currentWave.spawnEvents[i];

                    // Geçtiyse: Bir düşman "patlaması" (burst) başlat.
                    StartCoroutine(SpawnBurst(currentEvent));

                    // Şimdi bir sonraki tetiklenme zamanını hesapla
                    if (currentEvent.isPeriodic)
                    {
                        // EĞER BU PERİYODİK BİR OLAYSA (eski sistem gibi):
                        // Bir sonraki tetiklenme zamanını 'repeatInterval' kullanarak ayarla.
                        // (Eğer repeatInterval 0 ise, bir sonraki frame tekrar tetiklenir, dikkatli kullanılmalı)
                        nextEventTriggerTimes[i] += currentEvent.repeatInterval;
                    }
                    else
                    {
                        // EĞER BU TEK SEFERLİK BİR OLAYSA (yeni özellik):
                        // Bir daha tetiklenmemesi için bir sonraki tetiklenme zamanını
                        // ulaşılamaz bir değere (float.MaxValue veya Mathf.Infinity) ayarla.
                        nextEventTriggerTimes[i] = Mathf.Infinity;
                    }
                }
                // --- DEĞİŞİKLİK SONU ---
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

                // --- DEĞİŞİKLİK BAŞLANGICI: İlk Tetiklenme Zamanı Ayarı ---
                foreach (var spawnEvent in currentWave.spawnEvents)
                {
                    // Her olayın ilk tetiklenme zamanı, artık 'startDelay' değil, 'triggerTime' olacak.
                    // Bu sayede olay 5. saniyede de başlasa, 20. saniyede de başlasa doğru zamanda tetiklenecek.
                    nextEventTriggerTimes.Add(spawnEvent.triggerTime);
                }
                // --- DEĞİŞİKLİK SONU ---

                currentWaveIndex++;
                waveActive = true;
            }
            else
            {
                Debug.Log("Tüm dalgalar tamamlandı!");
                waveActive = false;
            }
        }

        // Bir "patlama" (burst) şeklinde düşman spawn eden Coroutine.
        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            // Belirtilen ID'de bir spawn noktası var mı kontrol et.
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı! Düşman spawn edilemiyor.");
                yield break; // Coroutine'i sonlandır.
            }

            // Doğru spawn noktasını sözlükten al.
            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];

            // Olayın 'count' (adet) değeri kadar döngüye gir.
            for (int i = 0; i < spawnEvent.count; i++)
            {
                // ObjectPooler'dan "enemy" etiketli bir düşman çağır.
                // NOT: Gelecekte, farklı düşman türleri için `spawnEvent.enemyPrefab`'ı kullanarak
                // ObjectPooler'dan "enemy_boss", "enemy_fast" gibi farklı tag'ler isteyebiliriz.
                // Şimdilik mevcut "enemy" tag'ini kullanan sistemi koruyoruz.
                ObjectPooler.Instance.SpawnFromPool("enemy", spawnPoint.transform.position, Quaternion.identity);
                
                // İki düşman arasında 'spawnInterval' kadar bekle (eğer 0 değilse).
                yield return new WaitForSeconds(spawnEvent.spawnInterval);
            }
        }
    }
}