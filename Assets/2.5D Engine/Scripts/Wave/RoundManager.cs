/*
 * ROUND MANAGER (TETİKLEYİCİ - v3.1)
 * * DEĞİŞİKLİKLER (Güvenli Temizlik):
 * - 'ShowVictoryScreen()' Coroutine'i güncellendi.
 * - 'victoryDelay' bittikten sonraki sıralama değiştirildi:
 * 1. WaveManager.KillAllActiveEnemies() (Sahnede kalanları öldür)
 * 2. WaveManager.CleanupDynamicPools() (Havuzları temizle)
 * 3. victoryPanel.SetActive(true) (Ekranı göster)
 * - Bu, zafer ekranı göründüğünde sahnede düşman kalmamasını ve
 * efekt havuzu hatalarını engeller.
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
        // Inspector'dan atanan değişkenler kaldırıldı.
        private float currentRoundDuration;
        private float currentVictoryDelay;
        
        [Header("UI")]
        [Tooltip("Kalan süreyi gösterecek olan TextMeshPro objesi.")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Tooltip("Tur bittiğinde gösterilecek olan 'Kazandın!' UI paneli.")]
        [SerializeField] private GameObject victoryPanel; 

        public float TimeElapsed { get; private set; }
        public float RoundDuration => currentRoundDuration; 
        public bool IsRoundActive { get; private set; }

        
        /// <summary>
        /// RoundManager'ı, WaveProfile'dan gelen ayarlarla başlatır.
        /// </summary>
        public void InitializeRound(float duration, float delay)
        {
            this.currentRoundDuration = duration;
            this.currentVictoryDelay = delay;
            Debug.Log($"RoundManager: Tur Süresi {duration}s, Zafer Gecikmesi {delay}s olarak ayarlandı.");
        }
        

        private void Start()
        {
            // Güvenlik kontrolü
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
        /// Düşman spawn'ını HEMEN durdurur ve GECİKMELİ temizlik sürecini başlatır.
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
            // 1. Lütuf zamanı (grace period) kadar bekle.
            yield return new WaitForSeconds(currentVictoryDelay);

            // --- DEĞİŞİKLİK BAŞLANGICI: Güvenli Temizlik Sıralaması ---
            if (WaveManager.Instance != null)
            {
                // 2. Sahnede kalan tüm aktif düşmanları ÖLDÜR
                //    (Onlar da havuzlarına geri dönecek)
                WaveManager.Instance.KillAllActiveEnemies();
                
                // 3. Artık içi dolu olan dinamik havuzları TEMİZLE
                WaveManager.Instance.CleanupDynamicPools();
            }
            // --- DEĞİŞİKLİK SONU ---

            // 4. Zafer ekranını göster.
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