using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro kütüphanesi eklendi

namespace IndianOceanAssets.Engine2_5D
{
    public class RoundManager : MonoBehaviour
    {
        [Header("Görsel Ayarlar (İsteğe Bağlı)")]
        [Tooltip("Dolup boşalan bar efekti için 'Image' objesi (Image Type: Filled olmalı).")]
        [SerializeField] private Image timerBarImage;

        [Tooltip("Seri numara sayacı için TextMeshPro objesi (Örn: 60, 59...).")]
        [SerializeField] private TextMeshProUGUI timerText;

        // Event
        public event System.Action OnRoundEnded;

        public bool IsRoundActive { get; private set; }
        public float TimeElapsed { get; private set; }
        public float RoundDuration { get; private set; }

        private float timer;
        private bool roundEndedTriggered = false; 

        public void InitializeRound(float duration, float victoryDelay)
        {
            RoundDuration = duration;
            timer = duration;
            TimeElapsed = 0f;
            IsRoundActive = true;
            roundEndedTriggered = false; 

            // Barı fulle (Varsa)
            if (timerBarImage != null) 
                timerBarImage.fillAmount = 1f;
            
            // Yazıyı güncelle (Varsa)
            UpdateTimerText();
        }

        private void Update()
        {
            if (!IsRoundActive) return;

            // Geri sayım
            timer -= Time.deltaTime;
            TimeElapsed += Time.deltaTime;

            // 1. Bar Güncellemesi (Varsa)
            if (timerBarImage != null && RoundDuration > 0)
            {
                timerBarImage.fillAmount = Mathf.Clamp01(timer / RoundDuration);
            }

            // 2. Yazı Güncellemesi (Varsa)
            if (timerText != null)
            {
                UpdateTimerText();
            }

            // Süre bitti mi?
            if (timer <= 0)
            {
                EndRound();
            }
        }

        private void UpdateTimerText()
        {
            if (timerText != null)
            {
                // Sayıyı yukarı yuvarla (59.1 -> 60 gözüksün)
                timerText.text = Mathf.CeilToInt(timer).ToString();
            }
        }

        private void EndRound()
        {
            IsRoundActive = false;
            
            // Görselleri sıfırla
            if (timerBarImage != null) timerBarImage.fillAmount = 0f;
            if (timerText != null) timerText.text = "0";

            if (!roundEndedTriggered)
            {
                roundEndedTriggered = true;
                Debug.Log("RoundManager: Tur Bitti! Event tetikleniyor.");
                OnRoundEnded?.Invoke();
            }
        }
    }
}