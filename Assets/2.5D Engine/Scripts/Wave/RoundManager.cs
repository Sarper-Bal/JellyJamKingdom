using UnityEngine;
using TMPro; // TextMeshPro kütüphanesini kullanabilmek için bu satır gerekli.

namespace IndianOceanAssets.Engine2_5D
{
    public class RoundManager : MonoBehaviour
    {
        [Header("Round Settings")]
        [Tooltip("Turun toplam süresi (saniye cinsinden).")]
        [SerializeField] private float roundDuration = 60f;

        [Header("UI")]
        [Tooltip("Kalan süreyi gösterecek olan TextMeshPro objesi.")]
        [SerializeField] private TextMeshProUGUI timerText;

        // Diğer script'lerin oyunun ne kadar süredir çalıştığını bilmesi için.
        public float TimeElapsed { get; private set; }

        public bool IsRoundActive { get; private set; }

        private void Start()
        {
            // Oyun başında zamanı ve durumu başlat.
            TimeElapsed = 0f;
            IsRoundActive = true;
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
                    IsRoundActive = false;
                    // Burada tur bittiğinde ne olacağını yazabiliriz (örn: "Kazandın!" ekranı).
                    Debug.Log("Tur Bitti!");
                }

                // Sayaç metnini güncelle.
                UpdateTimerUI(timeLeft);
            }
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