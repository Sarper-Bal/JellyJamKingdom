using UnityEngine;
using TMPro; // TextMeshPro kütüphanesini kullanabilmek için bu satır gerekli.
using System.Collections; // YENİ: IEnumerator için eklendi (HealthUI'dan taşınan kod için)

// --- HATA DÜZELTMESİ (CS0103) ---
// SceneManager.LoadScene kullanabilmek için bu kütüphane eklendi.
using UnityEngine.SceneManagement; 
// --- HATA DÜZELTMESİ SONU ---
namespace IndianOceanAssets.Engine2_5D
{
    public class RoundManager : MonoBehaviour
    {
        [Header("Round Settings")]
        [Tooltip("Turun toplam süresi (saniye cinsinden).")]
        [SerializeField] private float roundDuration = 60f;

        // --- YENİ EKLENEN KISIM BAŞLANGICI ---
        [Tooltip("Tur bittikten sonra kazanma ekranına geçmeden önceki bekleme süresi.")]
        [SerializeField] private float victoryDelay = 3f;
        // --- YENİ EKLENEN KISIM SONU ---

        [Header("UI")]
        [Tooltip("Kalan süreyi gösterecek olan TextMeshPro objesi.")]
        [SerializeField] private TextMeshProUGUI timerText;

        // --- YENİ EKLENEN KISIM BAŞLANGICI ---
        [Tooltip("Tur bittiğinde gösterilecek olan 'Kazandın!' UI paneli.")]
        [SerializeField] private GameObject victoryPanel; // Inspector'dan atanmalı
        // --- YENİ EKLENEN KISIM SONU ---


        // Diğer script'lerin oyunun ne kadar süredir çalıştığını bilmesi için.
        public float TimeElapsed { get; private set; }

        // Diğer script'lerin (WaveManager gibi) turun toplam süresini okuyabilmesi için.
        public float RoundDuration => roundDuration; // YENİ: Public özellik (property)

        public bool IsRoundActive { get; private set; }

        private void Start()
        {
            // Oyun başında zamanı ve durumu başlat.
            TimeElapsed = 0f;
            IsRoundActive = true;

            // --- YENİ EKLENTİ ---
            // Başlangıçta zafer panelini kapat
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
            // --- YENİ EKLENTİ SONU ---
        }

        private void Update()
        {
            // Eğer tur aktifse, zamanı ilerlet.
            if (IsRoundActive)
            {
                TimeElapsed += Time.deltaTime;

                // Kalan süreyi hesapla.
                float timeLeft = roundDuration - TimeElapsed;

                // Eğer süre bittiyse turu bitir.
                if (timeLeft <= 0)
                {
                    timeLeft = 0;
                    // --- DEĞİŞİKLİK BAŞLANGICI ---
                    // Turu durdur ve kazanma Coroutine'ini başlat
                    EndRound();
                    // --- DEĞİŞİKLİK SONU ---
                }

                // Sayaç metnini güncelle.
                UpdateTimerUI(timeLeft);
            }
        }

        // --- YENİ FONKSİYON ---
        /// <summary>
        /// Turu sonlandırır ve kazanma sürecini başlatır.
        /// </summary>
        private void EndRound()
        {
            IsRoundActive = false;
            Debug.Log("Tur Bitti! (Kazanıldı)");

            // (Opsiyonel: Düşmanların spawn olmasını durdurmak için WaveManager'a haber verilebilir
            // veya WaveManager zaten 'IsRoundActive' bayrağını kontrol ettiği için gerek kalmayabilir)

            // Kazanma panelini gösterme Coroutine'ini başlat
            StartCoroutine(ShowVictoryScreen());
        }

        // --- YENİ FONKSİYON ---
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

            // Zamanı durdur (opsiyonel, istenirse)
            // Time.timeScale = 0f; 
        }

        // --- YENİ FONKSİYON (HealthUI'dan taşındı ve düzenlendi) ---
        /// <summary>
        /// Sahneyi yeniden yükler (Örn: "Tekrar Oyna" butonu için)
        /// </summary>
        public void ReloadScene()
        {
            // (Eğer zaman durdurulduysa aç)
            // Time.timeScale = 1f; 
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Kalan saniyeyi "dakika:saniye" formatında UI'a yazdıran fonksiyon.
        private void UpdateTimerUI(float time)
        {
            if (timerText != null)
            {
                // Zamanı dakika ve saniye olarak ayır.
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);

                // Metni formatla (örn: 1:05, 0:32 gibi).
                timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
            }
        }
    }
}