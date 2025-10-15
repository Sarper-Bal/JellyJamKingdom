using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Bu satır, spawn noktalarını kolayca bulmamız için gerekli.

namespace IndianOceanAssets.Engine2_5D
{
    public class WaveManager : MonoBehaviour
    {
        [Tooltip("Bu seviyede oynanacak dalga profillerinin listesi. Sırayla oynanacak.")]
        [SerializeField] private List<WaveProfile> waves;

        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();
        private int currentWaveIndex = 0;

        private void Awake()
        {
            // Sahnedeki tüm spawn noktalarını bul ve ID'lerine göre bir sözlüğe (dictionary) kaydet.
            // Bu sayede "ID'si 1 olan spawn noktası" diye aradığımızda anında bulabiliriz.
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            // Oyun başlar başlamaz ilk dalgayı başlat.
            StartNextWave();
        }

        public void StartNextWave()
        {
            if (waves != null && waves.Count > currentWaveIndex)
            {
                // Sıradaki dalgayı başlat ve index'i bir artır.
                StartCoroutine(ProcessWave(waves[currentWaveIndex]));
                currentWaveIndex++;
            }
            else
            {
                Debug.Log("Tüm dalgalar tamamlandı!");
            }
        }

        // Bir dalga profilini işleyen ana Coroutine.
        private IEnumerator ProcessWave(WaveProfile wave)
        {
            // Dalganın içindeki her bir spawn olayı için ayrı bir coroutine başlat.
            foreach (var spawnEvent in wave.spawnEvents)
            {
                // ID'si eşleşen bir spawn noktası var mı kontrol et.
                if (spawnPoints.ContainsKey(spawnEvent.spawnPointID))
                {
                    StartCoroutine(ProcessSpawnEvent(spawnEvent));
                }
                else
                {
                    Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı!");
                }
            }
            yield return null; // Bu coroutine'in görevi diğerlerini başlatmaktı, bu yüzden hemen biter.
        }

        // Tek bir spawn olayını (örneğin 5 tane goblin spawn etme) yöneten Coroutine.
        private IEnumerator ProcessSpawnEvent(SpawnEvent spawnEvent)
        {
            // 1. Başlangıç gecikmesi kadar bekle.
            yield return new WaitForSeconds(spawnEvent.startDelay);

            // 2. Belirtilen sayıda düşmanı, belirtilen aralıklarla spawn et.
            for (int i = 0; i < spawnEvent.count; i++)
            {
                // Doğru spawn noktasını al.
                EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];

                // Düşmanı spawn et.
                Instantiate(spawnEvent.enemyPrefab, spawnPoint.transform.position, Quaternion.identity);

                // 3. Bir sonraki düşmanı spawn etmeden önce bekle.
                yield return new WaitForSeconds(spawnEvent.spawnInterval);
            }
        }
    }
}