/*
 * ROUND MANAGER (TETİKLEYİCİ - v2)
 * * DEĞİŞİKLİKLER (Akıllı Temizlik):
 * - 'EndRound()' metodu: Artık 'StopAndCleanupWaves' ÇAĞIRMIYOR.
 * Bunun yerine SADECE 'StopWaveSpawning' çağırarak yeni düşman gelişini durduruyor.
 * - 'ShowVictoryScreen()' Coroutine'i:
 * 'victoryDelay' kadar bekledikten SONRA,
 * ve 'victoryPanel'i aktif etmeden ÖNCE,
 * 'WaveManager.Instance.CleanupDynamicPools()' komutunu çağırarak
 * havuzların gecikmeli olarak temizlenmesini sağlıyor.
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
        [Header("Round Settings")]
        [Tooltip("Turun toplam süresi (saniye cinsinden).")]
        [SerializeField] private float roundDuration = 60f;

        [Tooltip("Tur bittikten sonra (spawn'lar durduktan sonra) " +
                 "Zafer ekranı gelene kadar oyuncuya tanınan 'lütuf zamanı' (grace period).")]
        [SerializeField] private float victoryDelay = 3f;

        [Header("UI")]
        [Tooltip("Kalan süreyi gösterecek olan TextMeshPro objesi.")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Tooltip("Tur bittiğinde gösterilecek olan 'Kazandın!' UI paneli.")]
        [SerializeField] private GameObject victoryPanel; 

        public float TimeElapsed { get; private set; }
        public float RoundDuration => roundDuration; 
        public bool IsRoundActive { get; private set; }

        private void Start()
        {
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
                float timeLeft = roundDuration - TimeElapsed;

                if (timeLeft <= 0)
                {
                    timeLeft = 0;
                    EndRound(); // Tur bitti, kazanma sürecini başlat
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

            // --- DEĞİŞİKLİK BAŞLANGICI ---
            
            // 1. WaveManager'a YENİ spawn'ları HEMEN durdurma komutu ver.
            //    (Artık havuzları TEMİZLEMİYORUZ)
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StopWaveSpawning();
            }
            
            // 2. Gecikmeli olarak Zafer Ekranını ve Havuz Temizliğini tetikle.
            StartCoroutine(ShowVictoryScreen());
            
            // --- DEĞİŞİKLİK SONU ---
        }

        /// <summary>
        /// Gecikmeli olarak havuzları temizler ve "Kazanma" ekranını gösterir.
        /// </summary>
        private IEnumerator ShowVictoryScreen()
        {
            // 1. Lütuf zamanı (grace period) kadar bekle.
            //    Bu sırada oyuncu kalan düşmanları öldürebilir ve efektler çalışır.
            yield return new WaitForSeconds(victoryDelay);

            // --- YENİ EKLENEN KISIM BAŞLANGICI ---
            
            // 2. Lütuf zamanı bitti. Havuzları TEMİZLE.
            //    Bu andan itibaren ölen düşmanların efekti görünmez
            //    (ama 'ReturnToPool' metodumuz buna karşı güvende).
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.CleanupDynamicPools();
            }
            
            // --- YENİ EKLENEN KISIM SONU ---

            // 3. Zafer ekranını göster.
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Sahneyi yeniden yükler (Örn: "Tekrar Oyna" butonu veya oyuncu ölünce)
        /// </summary>
        public void ReloadScene()
        {
            // (Oyuncu öldüğünde HealthSystem burayı çağırdığında,
            // sahne yeniden yüklendiği için havuzlar otomatik olarak temizlenir.
            // Bu yüzden 'Cleanup' çağırmaya gerek yok.)
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