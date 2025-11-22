using UnityEngine;
using System.Collections;

namespace IndianOceanAssets.Engine2_5D
{
    public class WaveSequenceController : MonoBehaviour
    {
        [Header("Veri Kaynağı")]
        [SerializeField] private WaveSequence waveSequence;

        [Header("Referanslar")]
        [SerializeField] private RoundManager roundManager;

        private int currentWaveIndex = 0;
        private bool sequenceStarted = false;

        private IEnumerator Start()
        {
            // Başlangıçta diğer sistemlerin hazır olmasını bekle
            yield return new WaitForEndOfFrame();

            if (roundManager == null) roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
            {
                roundManager.OnRoundEnded += HandleRoundEnded;
            }
            
            // Not: Artık otomatik başlamıyor, BattleInitializer'dan emir bekliyor.
        }

        /// <summary>
        /// BattleInitializer tarafından çağrılır ve savaşı başlatır.
        /// </summary>
        public void InitializeFromExternal(WaveSequence sequence)
        {
            this.waveSequence = sequence;
            
            if (this.waveSequence != null && this.waveSequence.waves.Count > 0)
            {
                Debug.Log("WaveSequenceController: Dışarıdan veri alındı, savaş başlıyor!");
                sequenceStarted = true;
                LoadWave(0); // İlk dalgayı yükle
            }
            else
            {
                Debug.LogError("WaveSequenceController: Geçersiz WaveSequence verisi!");
            }
        }

        private void OnDestroy()
        {
            if (roundManager != null)
                roundManager.OnRoundEnded -= HandleRoundEnded;
        }

        private void LoadWave(int index)
        {
            // --- 1. LİSTE SONU KONTROLÜ (BİTİŞ VEYA DÖNGÜ) ---
            if (index >= waveSequence.waves.Count)
            {
                if (waveSequence.loopSequence)
                {
                    Debug.Log("WaveSequenceController: Liste bitti, başa dönülüyor (Loop).");
                    index = 0;
                }
                else
                {
                    Debug.Log("WaveSequenceController: Tüm dalgalar bitti. Savaş kazanıldı! Köye dönülüyor...");
                    
                    // --- YENİ EKLENEN KISIM: KÖYE DÖNÜŞ ---
                    if (GameManager.Instance != null)
                    {
                        // Köy sahnesini yükle
                        GameManager.Instance.ReturnToVillage();
                    }
                    else
                    {
                        Debug.LogWarning("WaveSequenceController: GameManager bulunamadı, köye dönülemiyor!");
                    }
                    // ---------------------------------------
                    
                    return; // Fonksiyondan çık, yeni dalga yükleme
                }
            }

            // --- 2. YENİ DALGAYI YÜKLE ---
            currentWaveIndex = index;
            WaveProfile profileToPlay = waveSequence.waves[currentWaveIndex];

            Debug.Log($"--- SIRADAKİ DALGA YÜKLENİYOR: {profileToPlay.name} ---");

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.LoadAndStartWave(profileToPlay);
            }
        }

        // Bir tur bittiğinde (Süre doldu veya erken zafer)
        private void HandleRoundEnded()
        {
            if (!sequenceStarted) return;

            // 1. Sahneyi temizle
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ForceClearWave();
            }

            Debug.Log($"Tur Bitti. {waveSequence.delayBetweenWaves} saniye bekleniyor...");
            
            // 2. Bekle ve bir sonraki kararı ver (Yeni dalga mı, Köye dönüş mü?)
            StartCoroutine(WaitAndStartNext());
        }

        private IEnumerator WaitAndStartNext()
        {
            // İki dalga arasındaki bekleme süresi
            yield return new WaitForSeconds(waveSequence.delayBetweenWaves);
            
            // Sıradaki dalgayı yüklemeyi dene (LoadWave içinde bitiş kontrolü yapılacak)
            LoadWave(currentWaveIndex + 1);
        }
    }
}