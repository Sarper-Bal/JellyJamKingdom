using UnityEngine;
using System.Collections;

namespace IndianOceanAssets.Engine2_5D
{
    public class WaveSequenceController : MonoBehaviour
    {
        [Header("Veri Kaynağı")]
        [SerializeField] private WaveSequence waveSequence;

        [Header("Referanslar")]
        [SerializeField] private RoundManager roundManager;

        private int currentWaveIndex = 0;
        private bool sequenceStarted = false;

private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();

            if (roundManager == null) roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null) roundManager.OnRoundEnded += HandleRoundEnded;

            // DİKKAT: Burada artık LoadWave(0) çağırmıyoruz!
            // Çünkü verinin BattleInitializer'dan gelmesini bekleyeceğiz.
        }

        // YENİ METOT: Dışarıdan Başlatma
        public void InitializeFromExternal(WaveSequence sequence)
        {
            this.waveSequence = sequence; // Dışarıdan gelen veriyi al
            
            if (this.waveSequence != null && this.waveSequence.waves.Count > 0)
            {
                Debug.Log("WaveSequenceController: Dışarıdan veri alındı, savaş başlıyor!");
                sequenceStarted = true;
                LoadWave(0); // Ve başlat
            }
            else
            {
                Debug.LogError("WaveSequenceController: Geçersiz WaveSequence verisi!");
            }
        }

        private void OnDestroy()
        {
            if (roundManager != null)
                roundManager.OnRoundEnded -= HandleRoundEnded;
        }

        private void LoadWave(int index)
        {
            if (index >= waveSequence.waves.Count)
            {
                if (waveSequence.loopSequence) index = 0;
                else return;
            }

            currentWaveIndex = index;
            WaveProfile profileToPlay = waveSequence.waves[currentWaveIndex];

            Debug.Log($"--- SIRADAKİ DALGA YÜKLENİYOR: {profileToPlay.name} ---");

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.LoadAndStartWave(profileToPlay);
            }
        }

        // Tur bittiğinde burası çalışır
        private void HandleRoundEnded()
        {
            if (!sequenceStarted) return;

            // 1. ÖNCE SAHNEYİ TEMİZLE (Kullanıcı İsteği)
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ForceClearWave();
            }

            Debug.Log($"Tur Bitti. {waveSequence.delayBetweenWaves} saniye bekleniyor...");
            
            // 2. SONRA BEKLE VE YENİSİNE GEÇ
            StartCoroutine(WaitAndStartNext());
        }

        private IEnumerator WaitAndStartNext()
        {
            // Ayarlanan süre kadar bekle (Sahne şu an boş)
            yield return new WaitForSeconds(waveSequence.delayBetweenWaves);
            
            // Yeni dalgayı başlat
            LoadWave(currentWaveIndex + 1);
        }
    }
}