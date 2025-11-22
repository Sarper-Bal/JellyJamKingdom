using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IndianOceanAssets.Engine2_5D
{
    public class RoundManager : MonoBehaviour
    {
        [Header("Görsel Ayarlar")]
        [SerializeField] private Image timerBarImage;
        [SerializeField] private TextMeshProUGUI timerText;

        public event System.Action OnRoundEnded;

        public bool IsRoundActive { get; private set; }
        public float RoundDuration { get; private set; }
        public float TimeElapsed { get; private set; } // WaveManager için gerekli
        
        private float timer;
        private bool roundEndedTriggered = false; 
        
        // --- OPTİMİZASYON: Önceki saniyeyi tutan değişken ---
        private int lastSecond = -1; 
        // ---------------------------------------------------

        public void InitializeRound(float duration, float victoryDelay)
        {
            RoundDuration = duration;
            timer = duration;
            TimeElapsed = 0f;
            IsRoundActive = true;
            roundEndedTriggered = false; 
            
            // Yeni turda sayacı sıfırla
            lastSecond = -1; 

            if (timerBarImage != null) timerBarImage.fillAmount = 1f;
            UpdateTimerText(); // Başlangıçta bir kez yaz
        }

        public void ForceEndRound()
        {
            if (!IsRoundActive) return;
            Debug.Log("<color=green>RoundManager: Tüm düşmanlar temizlendi! Tur erken bitiriliyor.</color>");
            timer = 0; 
            EndRound(); 
        }

        private void Update()
        {
            if (!IsRoundActive) return;

            timer -= Time.deltaTime;
            TimeElapsed += Time.deltaTime; // WaveManager için zaman sayacı

            // 1. Bar Güncellemesi (Mecburen her frame, akıcı olması için)
            if (timerBarImage != null && RoundDuration > 0)
                timerBarImage.fillAmount = Mathf.Clamp01(timer / RoundDuration);

            // 2. Metin Güncellemesi (OPTİMİZE EDİLDİ)
            // Sadece saniye tam sayı olarak değiştiyse metni güncelle
            int currentSecondInt = Mathf.CeilToInt(timer);
            if (currentSecondInt != lastSecond)
            {
                UpdateTimerText();
                lastSecond = currentSecondInt;
            }

            if (timer <= 0) EndRound();
        }

        private void UpdateTimerText()
        {
            if (timerText != null) 
            {
                // Negatif sayı göstermemesi için Clamp
                float displayTime = Mathf.Max(0, timer);
                timerText.text = Mathf.CeilToInt(displayTime).ToString();
            }
        }

        private void EndRound()
        {
            IsRoundActive = false;
            if (timerBarImage != null) timerBarImage.fillAmount = 0f;
            if (timerText != null) timerText.text = "0";

            if (!roundEndedTriggered)
            {
                roundEndedTriggered = true;
                OnRoundEnded?.Invoke();
            }
        }
    }
}