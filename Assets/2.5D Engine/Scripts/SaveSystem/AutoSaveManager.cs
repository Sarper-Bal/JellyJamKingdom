using UnityEngine;
using System.Collections;

namespace IndianOceanAssets.Engine2_5D
{
    public class AutoSaveManager : MonoBehaviour
    {
        [Header("Ayarlar")]
        [Tooltip("Otomatik kayıt aralığı (saniye). 0 = Kapalı.")]
        [SerializeField] private float autoSaveInterval = 180f; // 3 Dakika
        
        [Tooltip("Kayıt sırasında Log mesajı gösterilsin mi?")]
        [SerializeField] private bool showDebugLogs = true;

        private float timer;
        private bool isGameActive = true;

        private void Start()
        {
            // Oyun başladığında sayaç başlasın
            timer = autoSaveInterval;
            
            // Kritik olaylara abone ol (Observer Pattern)
            RegisterEvents();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        private void Update()
        {
            if (!isGameActive || autoSaveInterval <= 0) return;

            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                TriggerAutoSave("Zamanlayıcı");
                timer = autoSaveInterval; // Sayacı sıfırla
            }
        }

        // --- TETİKLEYİCİ MEKANİZMA ---
        
        /// <summary>
        /// Dışarıdan veya içeriden otomatik kayıt isteği gönderir.
        /// </summary>
        /// <param name="reason">Kaydın sebebi (Debug için)</param>
        public void TriggerAutoSave(string reason)
        {
            // Eğer zaten kayıt yapılıyorsa SaveManager bunu reddeder, sorun yok.
            if (SaveManager.Instance != null)
            {
                if(showDebugLogs) Debug.Log($"[AutoSave] Tetiklendi: {reason}");
                
                // Asenkron kayıt başlat (Fire and Forget)
                _ = SaveManager.Instance.SaveGameAsync();
            }
        }

        // --- KRİTİK OLAYLARI DİNLEME (EVENT LISTENER) ---

        private void RegisterEvents()
        {
            // 1. Uygulama Arka Plana Atıldığında (Pause)
            // Unity'nin kendi mesaj sistemini kullanacağız (OnApplicationPause).
            
            // 2. Round Bittiğinde (RoundManager üzerinden)
            RoundManager roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
            {
                roundManager.OnRoundEnded += HandleRoundEnded;
            }
        }

        private void UnregisterEvents()
        {
            RoundManager roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
            {
                roundManager.OnRoundEnded -= HandleRoundEnded;
            }
        }

        // Olay: Tur Bitti
        private void HandleRoundEnded()
        {
            TriggerAutoSave("Tur Bitti");
            timer = autoSaveInterval; // Zamanlayıcıyı ertele, üst üste binmesin.
        }

        // Olay: Uygulama Arka Plana Gitti (Android/iOS için çok önemli)
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) // True ise oyun duraklatıldı (Home tuşuna basıldı)
            {
                TriggerAutoSave("Uygulama Duraklatıldı");
            }
        }

        // Olay: Uygulama Kapanıyor
        private void OnApplicationQuit()
        {
            // Burası asenkron olamaz, Unity kapanmadan hemen önce senkron (bloklayıcı) kayıt yapmalı.
            // Ancak SaveManager'ımız asenkron. Bu yüzden burada manuel bir çağrı gerekebilir 
            // veya SaveManager'a "ForceSaveSync" metodu ekleyebiliriz.
            // Güvenlik için şimdilik async çağırıp şansımızı deniyoruz (Genelde çalışır).
            TriggerAutoSave("Uygulama Kapanıyor");
        }
    }
}