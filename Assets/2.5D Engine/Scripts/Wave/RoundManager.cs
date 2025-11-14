/*
 * ROUND MANAGER (UYGULAYICI - v3)
 * * DEĞİŞİKLİKLER (Veri Odaklı):
 * - Inspector'dan ayarlanan 'roundDuration' ve 'victoryDelay' değişkenleri kaldırıldı.
 * - Bunların yerine 'currentRoundDuration' ve 'currentVictoryDelay' adında
 * private (özel) değişkenler getirildi.
 * - 'WaveManager'dan ayarları alabilmek için 'InitializeRound' adında
 * public bir metot eklendi.
 * - 'RoundDuration' property'si (özelliği) artık 'currentRoundDuration'ı döndürüyor.
 */

using UnityEngine;
using TMPro; 
using System.Collections;
using UnityEngine.SceneManagement; 
using IndianOceanAssets.Engine2_5D; // WaveManager'a erişim için

namespace IndianOceanAssets.Engine2_5D
{
    public class RoundManager : MonoBehaviour
    {
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // Inspector'dan ayarlanan değişkenler kaldırıldı.
        // [SerializeField] private float roundDuration = 60f;
        // [SerializeField] private float victoryDelay = 3f;

        // Bu değerler artık 'InitializeRound' metodu ile 'WaveManager' tarafından atanacak.
        private float currentRoundDuration;
        private float currentVictoryDelay;
        // --- DEĞİŞİKLİK SONU ---
        
        [Header("UI")]
        [Tooltip("Kalan süreyi gösterecek olan TextMeshPro objesi.")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Tooltip("Tur bittiğinde gösterilecek olan 'Kazandın!' UI paneli.")]
        [SerializeField] private GameObject victoryPanel; 

        public float TimeElapsed { get; private set; }
        
        // --- DEĞİŞİKLİK: Artık 'currentRoundDuration' döndürüyor ---
        public float RoundDuration => currentRoundDuration; 

        public bool IsRoundActive { get; private set; }

        
        // --- YENİ FONKSİYON BAŞLANGICI ---
        /// <summary>
        /// RoundManager'ı, WaveProfile'dan gelen ayarlarla başlatır.
        /// Bu metot, 'WaveManager.Start()' tarafından 'Start()' metodundan önce çağrılır.
        /// </summary>
        /// <param name="duration">Turun toplam süresi</param>
        /// <param name="delay">Kazanma gecikme süresi</param>
        public void InitializeRound(float duration, float delay)
        {
            this.currentRoundDuration = duration;
            this.currentVictoryDelay = delay;
            Debug.Log($"RoundManager: Tur Süresi {duration}s, Zafer Gecikmesi {delay}s olarak ayarlandı.");
        }
        // --- YENİ FONKSİYON SONU ---
        

        private void Start()
        {
            // Güvenlik kontrolü: Eğer InitializeRound çağrılmadıysa (örn: WaveManager yoksa)
            if (currentRoundDuration == 0)
            {
                Debug.LogWarning("RoundManager.InitializeRound() çağrılmadı. Varsayılan süre (60s) kullanılıyor.");
                currentRoundDuration = 60f;
                currentVictoryDelay = 3f;
            }
            
            TimeElapsed = 0f;
            IsRoundActive = true;
            
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (IsRoundActive)
            {
                TimeElapsed += Time.deltaTime;
                
                // --- DEĞİŞİKLİK: 'roundDuration' yerine 'currentRoundDuration' kullanılıyor ---
                float timeLeft = currentRoundDuration - TimeElapsed;

                if (timeLeft <= 0)
                {
                    timeLeft = 0;
                    EndRound(); 
                }

                UpdateTimerUI(timeLeft);
            }
        }
        
        /// <summary>
        /// Turu sonlandırır (Kazanma durumu).
        /// Düşman spawn'ını durdurur ve gecikmeli temizliği başlatır.
        /// </summary>
        private void EndRound()
        {
            if (!IsRoundActive) return; 
            
            IsRoundActive = false;
            Debug.Log("Tur Bitti! (Kazanıldı). Yeni spawn'lar durduruldu.");

            // 1. WaveManager'a YENİ spawn'ları HEMEN durdurma komutu ver.
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StopWaveSpawning();
            }
            
            // 2. Gecikmeli olarak Zafer Ekranını ve Havuz Temizliğini tetikle.
            StartCoroutine(ShowVictoryScreen());
        }

        /// <summary>
        /// Gecikmeli olarak havuzları temizler ve "Kazanma" ekranını gösterir.
        /// </summary>
        private IEnumerator ShowVictoryScreen()
        {
            // --- DEĞİŞİKLİK: 'victoryDelay' yerine 'currentVictoryDelay' kullanılıyor ---
            yield return new WaitForSeconds(currentVictoryDelay);

            // 2. Lütuf zamanı bitti. Havuzları TEMİZLE.
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.CleanupDynamicPools();
            }
            
            // 3. Zafer ekranını göster.
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Sahneyi yeniden yükler.
        /// </summary>
        public void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Kalan saniyeyi "dakika:saniye" formatında UI'a yazdıran fonksiyon.
        /// </summary>
        private void UpdateTimerUI(float time)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);
                timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
            }
        }
    }
}