/*
 * ROUND MANAGER (TETİKLEYİCİ)
 * * DEĞİŞİKLİKLER:
 * - 'EndRound()' (tur kazanıldığında) fonksiyonuna, 'WaveManager'ı çağırarak
 * havuz temizliğini tetikleyen 'StopAndCleanupWaves()' komutu eklendi.
 * - 'HealthSystem'e dokunulmadı, oyuncu öldüğünde sahne eskisi gibi yeniden yüklenecek
 * ve bu da havuzun dolaylı olarak temizlenmesini sağlayacak.
 */

using UnityEngine;
using TMPro; 
using System.Collections;
using UnityEngine.SceneManagement; 

// --- YENİ EKLENTİ ---
// WaveManager'a komut verebilmek için bu kütüphane eklendi.
using IndianOceanAssets.Engine2_5D;
// --- YENİ EKLENTİ SONU ---

namespace IndianOceanAssets.Engine2_5D
{
    public class RoundManager : MonoBehaviour
    {
        [Header("Round Settings")]
        [Tooltip("Turun toplam süresi (saniye cinsinden).")]
        [SerializeField] private float roundDuration = 60f;

        [Tooltip("Tur bittikten sonra kazanma ekranına geçmeden önceki bekleme süresi.")]
        [SerializeField] private float victoryDelay = 3f;

        [Header("UI")]
        [Tooltip("Kalan süreyi gösterecek olan TextMeshPro objesi.")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Tooltip("Tur bittiğinde gösterilecek olan 'Kazandın!' UI paneli.")]
        [SerializeField] private GameObject victoryPanel; // Inspector'dan atanmalı

        // Diğer script'lerin oyunun ne kadar süredir çalıştığını bilmesi için.
        public float TimeElapsed { get; private set; }
        
        // Diğer script'lerin (WaveManager gibi) turun toplam süresini okuyabilmesi için.
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
        /// Düşman spawn'ını durdurur, havuzu temizler ve Zafer ekranını gösterir.
        /// </summary>
        private void EndRound()
        {
            // Bu fonksiyonun 'Update' içinde tekrar tekrar çağrılmasını engelle
            if (!IsRoundActive) return; 
            
            IsRoundActive = false;
            Debug.Log("Tur Bitti! (Kazanıldı). Temizlik başlıyor.");

            // --- YENİ EKLENEN KISIM BAŞLANGICI (Temizlik) ---
            
            // 1. WaveManager'a spawn'ı durdurma ve "enemy" havuzunu yok etme komutu ver.
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StopAndCleanupWaves();
            }
            
            // --- YENİ EKLENEN KISIM SONU ---

            // 2. Zafer ekranını gecikmeli olarak göster
            StartCoroutine(ShowVictoryScreen());
        }

        /// <summary>
        /// Gecikmeli olarak "Kazanma" ekranını gösterir.
        /// </summary>
        private IEnumerator ShowVictoryScreen()
        {
            yield return new WaitForSeconds(victoryDelay);

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Sahneyi yeniden yükler (Örn: "Tekrar Oyna" butonu için)
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