using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.Engine2_5D
{
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // --- YENİ: EDİTÖR DOSTU SAHNE SEÇİMİ ---
#if UNITY_EDITOR
        [Header("Sahne Ayarları")]
        [Tooltip("Köy sahnesini (.unity dosyası) buraya sürükleyin.")]
        public UnityEditor.SceneAsset villageSceneAsset;
#endif

        [Tooltip("Otomatik dolar. Elle yazmanıza gerek yok.")]
        [SerializeField] private string villageSceneName; // Inspector'da görünür ama private
        // ---------------------------------------

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

        // --- OTOMATİK İSİM GÜNCELLEME ---
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

        public void LoadLevel(LevelData data)
        {
            if (data == null)
            {
                Debug.LogError("GameManager: LevelData boş!");
                return;
            }

            PendingLevelData = data;
            Debug.Log($"GameManager: {data.sceneName} sahnesi yükleniyor...");
            SceneManager.LoadScene(data.sceneName);
        }

        // --- GÜNCELLENMİŞ DÖNÜŞ METODU ---
        public void ReturnToVillage()
        {
            if (string.IsNullOrEmpty(villageSceneName))
            {
                Debug.LogError("GameManager: Köy sahnesi atanmamış! Lütfen Inspector'dan 'Village Scene Asset'i atayın.");
                return;
            }

            Debug.Log("Savaş bitti, köye dönülüyor...");
            SceneManager.LoadScene(villageSceneName);
        }
    }
}