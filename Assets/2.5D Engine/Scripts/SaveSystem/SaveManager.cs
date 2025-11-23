using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace IndianOceanAssets.Engine2_5D
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        private string saveFilePath;

        [System.Serializable]
        private class SaveDataCollection
        {
            public List<string> ids = new List<string>();
            public List<string> jsonDatas = new List<string>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        }

        private void Start()
        {
            LoadGame();
        }

        [ContextMenu("Save Game")]
        public void SaveGame()
        {
            // 1. Sahnede hem ISaveable olan hem de SaveableEntity taşıyanları bul
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
            SaveDataCollection collection = new SaveDataCollection();

            foreach (var saveable in saveables)
            {
                MonoBehaviour mb = saveable as MonoBehaviour;
                // --- KRİTİK DEĞİŞİKLİK: ID SİSTEMİ ---
                SaveableEntity idComponent = mb.GetComponent<SaveableEntity>();
                
                if (idComponent == null)
                {
                    Debug.LogWarning($"SaveManager: '{mb.name}' objesinde ISaveable var ama 'SaveableEntity' EKSİK! Kaydedilmedi.");
                    continue;
                }

                string id = idComponent.ID; // Artık isim değil, Unique ID kullanıyoruz.
                // -------------------------------------
                
                object dataObject = saveable.CaptureState();
                string json = JsonUtility.ToJson(dataObject);

                collection.ids.Add(id);
                collection.jsonDatas.Add(json);
            }

            string fileJson = JsonUtility.ToJson(collection, true);
            File.WriteAllText(saveFilePath, fileJson);
            
            Debug.Log($"Oyun Kaydedildi! ({collection.ids.Count} obje ID ile koruma altında)");
        }

        [ContextMenu("Load Game")]
        public void LoadGame()
        {
            if (!File.Exists(saveFilePath))
            {
                Debug.Log("Kayıt dosyası yok. Yeni oyun.");
                return;
            }

            string fileJson = File.ReadAllText(saveFilePath);
            SaveDataCollection collection = JsonUtility.FromJson<SaveDataCollection>(fileJson);

            if (collection == null) return;

            // ID -> Data sözlüğünü oluştur
            Dictionary<string, string> saveMap = new Dictionary<string, string>();
            for (int i = 0; i < collection.ids.Count; i++)
            {
                if(i < collection.jsonDatas.Count)
                    saveMap[collection.ids[i]] = collection.jsonDatas[i];
            }

            // Sahneyi tara ve ID'leri eşleştir
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
            foreach (var saveable in saveables)
            {
                MonoBehaviour mb = saveable as MonoBehaviour;
                // --- KRİTİK DEĞİŞİKLİK: ID KONTROLÜ ---
                SaveableEntity idComponent = mb.GetComponent<SaveableEntity>();

                if (idComponent != null && saveMap.ContainsKey(idComponent.ID))
                {
                    // ID eşleşti, veriyi yükle
                    saveable.RestoreState(saveMap[idComponent.ID]);
                }
                // ---------------------------------------
            }
            
            Debug.Log("Oyun Yüklendi (ID Sistemine Göre)!");
        }
    }
}