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
        
        // --- DÜZELTME: TimeElapsed GERİ EKLENDİ ---
        public float TimeElapsed { get; private set; }
        // ------------------------------------------
        
        private float timer;
        private bool roundEndedTriggered = false; 

        public void InitializeRound(float duration, float victoryDelay)
        {
            RoundDuration = duration;
            timer = duration;
            
            // --- DÜZELTME: Sıfırlama ---
            TimeElapsed = 0f;
            // ---------------------------
            
            IsRoundActive = true;
            roundEndedTriggered = false; 

            if (timerBarImage != null) timerBarImage.fillAmount = 1f;
            UpdateTimerText();
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
            
            // --- DÜZELTME: Sayacı Artır ---
            TimeElapsed += Time.deltaTime;
            // ------------------------------

            if (timerBarImage != null && RoundDuration > 0)
                timerBarImage.fillAmount = Mathf.Clamp01(timer / RoundDuration);

            if (timerText != null) UpdateTimerText();

            if (timer <= 0) EndRound();
        }

        private void UpdateTimerText()
        {
            if (timerText != null) timerText.text = Mathf.CeilToInt(timer).ToString();
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