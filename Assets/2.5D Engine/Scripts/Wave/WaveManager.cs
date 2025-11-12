/*
 * WAVE MANAGER (SÜPER-AKILLI YÖNETİCİ - v3)
 * * DEĞİŞİKLİKLER:
 * - Inspector'daki 'enemyPrefab' ve 'enemyDeathEffectPrefab' referansları kaldırıldı.
 * - Artık 'WaveProfile'lardaki 'spawnEvent.enemyPrefab'ı okuyor.
 * - 'Calculate...' metodu, 'dynamicEnemyPools' (Düşmanlar) ve
 * 'dynamicEffectPools' (Ölüm Efektleri) adında iki sözlük (Dictionary) dolduruyor.
 * - 'Start()' metodu, bu sözlükleri döngüye alarak HER BİR BENZERSİZ prefab için
 * (prefab'ın adını 'tag' olarak kullanarak) dinamik havuzlar oluşturuyor.
 * - 'SpawnBurst()' metodu, artık 'spawnEvent.enemyPrefab.name' tag'ini kullanarak
 * doğru havuzdan (örn: "Goblin" veya "Orc") spawn yapıyor.
 * - 'StopAndCleanupWaves()' metodu, 'Start'ta oluşturduğu tüm dinamik havuzları
 * (hem düşman hem efekt) isimlerini kullanarak temizliyor.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IndianOceanAssets.Engine2_5D
{
    public class WaveManager : MonoBehaviour
    {
        #region Singleton
        public static WaveManager Instance { get; private set; }
        #endregion

        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // Bu iki referans kaldırıldı. Artık veriler 'WaveProfile'lardan okunacak.
        // [SerializeField] private GameObject enemyPrefab; 
        // [SerializeField] private GameObject enemyDeathEffectPrefab; 
        // --- DEĞİŞİKLİK SONU ---

        [Header("References")]
        [Tooltip("Turun süresi gibi bilgileri almak için RoundManager referansı.")]
        [SerializeField] private RoundManager roundManager;
        
        [Header("Wave Data")]
        [Tooltip("Bu seviyede oynanacak dalga profillerinin listesi.")]
        [SerializeField] private List<WaveProfile> waves;
        
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Havuz Hesaplama) ---
        // 'CalculatedEnemyPoolSize' (tek int) yerine, prefab'a göre ayrılmış sözlükler:
        
        /// <summary>
        /// Hangi 'düşman' prefab'ından kaç adet gerektiğini (Hesaplanan Boyut) saklar.
        /// Key: Düşman Prefab'ı (örn: Goblin.prefab)
        /// Value: Gerekli adet (örn: 50)
        /// </summary>
        private Dictionary<GameObject, int> dynamicEnemyPools;
        
        /// <summary>
        /// Hangi 'ölüm efekti' prefab'ından kaç adet gerektiğini (Hesaplanan Boyut) saklar.
        /// Key: Efekt Prefab'ı (örn: GoblinDeathFX.prefab)
        /// Value: Gerekli adet (örn: 50)
        /// </summary>
        private Dictionary<GameObject, int> dynamicEffectPools;
        // --- DEĞİŞİKLİK SONU ---


        // Sahnedeki spawn noktalarını ID ile hızlıca bulmak için bir sözlük (Dictionary).
        private Dictionary<int, EnemySpawnPoint> spawnPoints = new Dictionary<int, EnemySpawnPoint>();

        // Her bir spawn olayının bir sonraki tetiklenme zamanını takip eden liste.
        private List<float> nextEventTriggerTimes;

        private int currentWaveIndex = 0;
        private bool waveActive = false;

        private void Awake()
        {
            // Singleton kurulumu
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Spawn noktalarını bul
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>().ToDictionary(sp => sp.spawnPointID);
        }

        private void Start()
        {
            // 1. RoundManager referansını al
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }

            // --- DEĞİŞİKLİK BAŞLANGICI (Havuz Oluşturma Akışı) ---
            
            // 2. Gerekli tüm dinamik havuzları (düşmanlar ve efektleri) hesapla
            CalculatePoolRequirements();

            // 3. ObjectPooler servisine DİNAMİK havuzları oluşturması için komut ver
            
            // DÜŞMAN havuzlarını oluştur
            if (dynamicEnemyPools.Count > 0)
            {
                Debug.Log($"--- WaveManager: Düşman Havuzları Oluşturuluyor... ({dynamicEnemyPools.Count} tip) ---");
                foreach (var entry in dynamicEnemyPools)
                {
                    GameObject prefab = entry.Key;
                    int size = entry.Value;
                    // Havuz 'tag'i olarak prefab'ın adını kullan (örn: "Enemy_Goblin")
                    ObjectPooler.Instance.CreatePool(prefab.name, prefab, size);
                }
            }
            else
            {
                Debug.LogWarning("WaveManager: Hesaplama sonucunda spawn edilecek DÜŞMAN bulunamadı.");
            }
            
            // EFEKT havuzlarını oluştur
            if (dynamicEffectPools.Count > 0)
            {
                Debug.Log($"--- WaveManager: Efekt Havuzları Oluşturuluyor... ({dynamicEffectPools.Count} tip) ---");
                foreach (var entry in dynamicEffectPools)
                {
                    GameObject prefab = entry.Key;
                    int size = entry.Value;
                    // Havuz 'tag'i olarak prefab'ın adını kullan (örn: "Goblin_Death_FX")
                    ObjectPooler.Instance.CreatePool(prefab.name, prefab, size);
                }
            }
            else
            {
                Debug.LogWarning("WaveManager: Hesaplama sonucunda spawn edilecek ÖLÜM EFEKTİ bulunamadı.");
            }
            
            // 4. Her şey hazır olduğuna göre, ilk dalgayı başlat.
            StartNextWave();
            
            // --- DEĞİŞİKLİK SONU ---
        }
        
        /// <summary>
        /// 'waves' listesindeki tüm 'SpawnEvent'leri analiz eder.
        /// Hangi düşman prefab'ından ve hangi ölüm efekti prefab'ından kaçar adet
        /// spawn olacağını hesaplar ve 'dynamicEnemyPools' ile 'dynamicEffectPools' sözlüklerini doldurur.
        /// </summary>
        private void CalculatePoolRequirements()
        {
            // Sözlükleri başlat
            dynamicEnemyPools = new Dictionary<GameObject, int>();
            dynamicEffectPools = new Dictionary<GameObject, int>();

            if (roundManager == null)
            {
                Debug.LogError("WaveManager, havuz boyutunu hesaplamak için RoundManager referansını bulamadı!");
                return;
            }
            if (waves == null || waves.Count == 0)
            {
                Debug.LogWarning("WaveManager'a hiç dalga profili (WaveProfile) atanmamış.");
                return;
            }

            float roundDuration = roundManager.RoundDuration;
            
            // Bütün dalgaları tara
            foreach (WaveProfile wave in waves)
            {
                if (wave == null) continue; 
                
                // Dalgadaki bütün olayları tara
                foreach (SpawnEvent spawnEvent in wave.spawnEvents)
                {
                    // --- 1. DÜŞMAN PREFAB'INI KONTROL ET ---
                    GameObject enemyPrefab = spawnEvent.enemyPrefab;
                    if (enemyPrefab == null)
                    {
                        Debug.LogWarning($"WaveProfile ({wave.name}) içinde 'enemyPrefab' atanmamış bir SpawnEvent bulundu. Bu olay atlanıyor.");
                        continue; // Bu olayı atla
                    }

                    // --- 2. GEREKLİ SAYIYI HESAPLA ---
                    int countForThisEvent = 0;
                    if (spawnEvent.isPeriodic) // Periyodik spawn
                    {
                        if (spawnEvent.repeatInterval <= 0.1f) 
                        {
                            countForThisEvent = spawnEvent.count; // Hatalı veriyse, tek seferlik kabul et
                        }
                        else
                        {
                            float activeDuration = roundDuration - spawnEvent.triggerTime;
                            if (activeDuration > 0)
                            {
                                int repetitions = Mathf.FloorToInt(activeDuration / spawnEvent.repeatInterval) + 1;
                                countForThisEvent = spawnEvent.count * repetitions;
                            }
                            // else: Tur bittikten sonra başlıyor, sayı = 0
                        }
                    }
                    else // Tek seferlik spawn
                    {
                        if(spawnEvent.triggerTime <= roundDuration)
                        {
                            countForThisEvent = spawnEvent.count;
                        }
                        // else: Tur bittikten sonra başlıyor, sayı = 0
                    }
                    
                    if (countForThisEvent == 0) continue; // Spawn olmayacaksa devam etme

                    // --- 3. DÜŞMAN HAVUZU SÖZLÜĞÜNÜ GÜNCELLE ---
                    if (!dynamicEnemyPools.ContainsKey(enemyPrefab))
                    {
                        dynamicEnemyPools.Add(enemyPrefab, 0); // Sözlüğe yeni prefab'ı ekle
                    }
                    dynamicEnemyPools[enemyPrefab] += countForThisEvent; // Sayıyı arttır

                    // --- 4. ÖLÜM EFEKTİ HAVUZU SÖZLÜĞÜNÜ GÜNCELLE ---
                    HealthSystem hs = enemyPrefab.GetComponent<HealthSystem>();
                    if (hs != null)
                    {
                        GameObject deathEffectPrefab = hs.GetDeathEffectPrefab(); // Prefab'dan efekt bilgisini al
                        if (deathEffectPrefab != null)
                        {
                            if (!dynamicEffectPools.ContainsKey(deathEffectPrefab))
                            {
                                dynamicEffectPools.Add(deathEffectPrefab, 0); // Sözlüğe yeni efekti ekle
                            }
                            dynamicEffectPools[deathEffectPrefab] += countForThisEvent; // Sayıyı arttır
                        }
                    }
                } // End foreach SpawnEvent
            } // End foreach WaveProfile
            
            Debug.Log("--- WaveManager: Havuz Hesaplaması Tamamlandı ---");
        }
        
        
        /// <summary>
        /// Düşman spawn etmeyi durdurur ve 'Start'ta oluşturulan tüm dinamik havuzları temizler.
        /// </summary>
        public void StopAndCleanupWaves()
        {
            waveActive = false;
            StopAllCoroutines();
            
            if (ObjectPooler.Instance == null) return;
            
            Debug.Log("--- WaveManager: Dinamik Havuzlar Temizleniyor... ---");

            // --- DEĞİŞİKLİK BAŞLANGICI ---
            // 'dynamicEnemyPools' sözlüğünü döngüye al ve tüm havuzları yok et
            if (dynamicEnemyPools != null)
            {
                foreach (var entry in dynamicEnemyPools)
                {
                    // Havuz tag'i olarak prefab'ın adını kullanmıştık
                    ObjectPooler.Instance.DestroyPool(entry.Key.name);
                }
                dynamicEnemyPools.Clear(); // Sözlüğü temizle
            }

            // 'dynamicEffectPools' sözlüğünü döngüye al ve tüm havuzları yok et
            if (dynamicEffectPools != null)
            {
                foreach (var entry in dynamicEffectPools)
                {
                    // Havuz tag'i olarak prefab'ın adını kullanmıştık
                    ObjectPooler.Instance.DestroyPool(entry.Key.name);
                }
                dynamicEffectPools.Clear(); // Sözlüğü temizle
            }
            // --- DEĞİŞİKLİK SONU ---
        }


        private void Update()
        {
            if (!waveActive || !roundManager.IsRoundActive)
            {
                return;
            }

            WaveProfile currentWave = waves[currentWaveIndex - 1];
            if (currentWave == null) return; 

            for (int i = 0; i < currentWave.spawnEvents.Count; i++)
            {
                if (roundManager.TimeElapsed >= nextEventTriggerTimes[i])
                {
                    SpawnEvent currentEvent = currentWave.spawnEvents[i];
                    StartCoroutine(SpawnBurst(currentEvent));

                    if (currentEvent.isPeriodic)
                    {
                        nextEventTriggerTimes[i] += currentEvent.repeatInterval;
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
            if (waves != null && waves.Count > currentWaveIndex)
            {
                Debug.Log($"Dalga {currentWaveIndex + 1} başlıyor!");
                WaveProfile currentWave = waves[currentWaveIndex];

                if (currentWave == null)
                {
                     Debug.LogError($"Dalga {currentWaveIndex + 1} (index {currentWaveIndex}) 'waves' listesinde atanmamış (null).");
                     waveActive = false;
                     return;
                }

                nextEventTriggerTimes = new List<float>();
                foreach (var spawnEvent in currentWave.spawnEvents)
                {
                    nextEventTriggerTimes.Add(spawnEvent.triggerTime);
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

        private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
        {
            // --- DEĞİŞİKLİK BAŞLANGICI (Dinamik Spawn) ---
            
            // 1. Olayın prefab'ını al
            GameObject prefabToSpawn = spawnEvent.enemyPrefab;
            if (prefabToSpawn == null)
            {
                // 'CalculatePoolRequirements' zaten uyardı ama bu yine de iyi bir güvenlik kontrolü.
                Debug.LogError("SpawnEvent'te 'enemyPrefab' (null) olduğu için spawn işlemi yapılamadı.");
                yield break;
            }

            // 2. Spawn noktasını al
            if (!spawnPoints.ContainsKey(spawnEvent.spawnPointID))
            {
                Debug.LogWarning($"Spawn Point ID: {spawnEvent.spawnPointID} sahnede bulunamadı! Düşman spawn edilemiyor.");
                yield break; 
            }
            EnemySpawnPoint spawnPoint = spawnPoints[spawnEvent.spawnPointID];
            
            // 3. Havuz 'tag'ini prefab'ın adından al
            string poolTag = prefabToSpawn.name;

            for (int i = 0; i < spawnEvent.count; i++)
            {
                // 4. Doğru 'tag' ile doğru havuzdan spawn et
                GameObject spawnedEnemy = ObjectPooler.Instance.SpawnFromPool(poolTag, spawnPoint.transform.position, Quaternion.identity);
                
                if (spawnedEnemy == null)
                {
                     // Havuzun boşalması kritik bir hata
                     Debug.LogError($"'{poolTag}' havuzu boşaldı! Hesaplama yetersiz kalmış olabilir. Spawn işlemi durduruldu.");
                     yield break;
                }
                
                // 5. IPooledObject'e tag'i ata (HealthSystem'in havuza dönebilmesi için)
                //    (Bu zaten SpawnFromPool içinde yapılıyor, ama biz
                //    PoolTag'in prefab adı olduğundan emin olalım)
                IPooledObject pooledObj = spawnedEnemy.GetComponent<IPooledObject>();
                if (pooledObj != null)
                {
                    pooledObj.PoolTag = poolTag;
                }

                // --- DEĞİŞİKLİK SONU ---
                
                yield return new WaitForSeconds(spawnEvent.spawnInterval);
            }
        }
    }
}