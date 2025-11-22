using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.Engine2_5D
{
    [DefaultExecutionOrder(-50)] // Diğer scriptlerden önce çalışsın
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // Bir sonraki sahneye taşınacak veri paketi
        public LevelData PendingLevelData { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Sahne değişse de yok olma!
            }
            else
            {
                Destroy(gameObject); // Çift oluşursa kendini yok et
            }
        }

        /// <summary>
        /// Köydeki binaya tıklanınca çağrılır. Veriyi alır ve sahneyi yükler.
        /// </summary>
        public void LoadLevel(LevelData data)
        {
            if (data == null)
            {
                Debug.LogError("GameManager: Yüklenmek istenen LevelData boş!");
                return;
            }

            PendingLevelData = data; // Veriyi bavula koy
            Debug.Log($"GameManager: {data.sceneName} sahnesi yükleniyor...");
            SceneManager.LoadScene(data.sceneName); // Sahneyi aç
        }

        /// <summary>
        /// Savaş bitince köye dönüş için (İleride kullanacağız).
        /// </summary>
        public void ReturnToVillage()
        {
            SceneManager.LoadScene("VillageScene"); // Köy sahnesinin adı neyse onu yaz
        }
    }
}