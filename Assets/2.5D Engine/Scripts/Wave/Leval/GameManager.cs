using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.Engine2_5D
{
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

#if UNITY_EDITOR
        [Header("Sahne Ayarları")]
        [Tooltip("Köy sahnesini (.unity dosyası) buraya sürükleyin.")]
        public UnityEditor.SceneAsset villageSceneAsset;
#endif

        [Tooltip("Otomatik dolar. Elle yazmanıza gerek yok.")]
        [SerializeField] private string villageSceneName; 

        public LevelData PendingLevelData { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (villageSceneAsset != null)
            {
                string assetName = villageSceneAsset.name;
                if (villageSceneName != assetName)
                {
                    villageSceneName = assetName;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
#endif
        }

        // --- SAHNE YÜKLEME METOTLARI ---

        public void LoadLevel(LevelData data)
        {
            if (data == null)
            {
                Debug.LogError("GameManager: LevelData boş!");
                return;
            }

            PendingLevelData = data;
            
            // --- YENİ: SAHNEYE GİTMEDEN ÖNCE KAYDET ---
            TriggerSceneTransitionSave($"Bölüm Başlangıcı: {data.sceneName}");
            // -------------------------------------------

            Debug.Log($"GameManager: {data.sceneName} sahnesi yükleniyor...");
            SceneManager.LoadScene(data.sceneName);
        }

        public void ReturnToVillage()
        {
            if (string.IsNullOrEmpty(villageSceneName))
            {
                Debug.LogError("GameManager: Köy sahnesi atanmamış!");
                return;
            }

            // --- YENİ: KÖYE DÖNMEDEN ÖNCE KAYDET ---
            TriggerSceneTransitionSave("Köye Dönüş");
            // ---------------------------------------

            Debug.Log("Savaş bitti, köye dönülüyor...");
            SceneManager.LoadScene(villageSceneName);
        }

        // --- YARDIMCI METOT ---
        private void TriggerSceneTransitionSave(string context)
        {
            // Sahnede bir AutoSaveManager var mı diye bak
            AutoSaveManager autoSave = FindObjectOfType<AutoSaveManager>();
            if (autoSave != null)
            {
                // Varsa, akıllı sistem üzerinden kaydet (Loglama vb. için)
                autoSave.TriggerAutoSave(context);
            }
            else if (SaveManager.Instance != null)
            {
                // Yoksa, direkt SaveManager'ı kullan (Yedek plan)
                Debug.Log($"[GameManager] Sahne geçişi kaydı başlatılıyor: {context}");
                _ = SaveManager.Instance.SaveGameAsync();
            }
        }
    }
}