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

        // Verileri paketlemek için kullanılan iç sınıf
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
            // 1. Sahnedeki tüm kaydedilebilir objeleri bul
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
            SaveDataCollection collection = new SaveDataCollection();

            foreach (var saveable in saveables)
            {
                MonoBehaviour mb = saveable as MonoBehaviour;
                // Her objenin benzersiz bir ID'si olmalı (İsimleri ID olarak kullanıyoruz)
                // Dikkat: Sahnede aynı isimde iki obje olmamalı (Örn: Market_1, Market_2 yapın).
                string id = mb.name; 
                
                // Objeden veriyi al ve JSON string'e çevir
                object dataObject = saveable.CaptureState();
                string json = JsonUtility.ToJson(dataObject);

                collection.ids.Add(id);
                collection.jsonDatas.Add(json);
            }

            // 2. Dosyaya yaz
            string fileJson = JsonUtility.ToJson(collection, true);
            File.WriteAllText(saveFilePath, fileJson);
            
            Debug.Log($"Oyun Kaydedildi! ({collection.ids.Count} obje)");
        }

        [ContextMenu("Load Game")]
        public void LoadGame()
        {
            if (!File.Exists(saveFilePath))
            {
                Debug.Log("Kayıt dosyası yok. Yeni oyun.");
                return;
            }

            // 1. Dosyayı oku
            string fileJson = File.ReadAllText(saveFilePath);
            SaveDataCollection collection = JsonUtility.FromJson<SaveDataCollection>(fileJson);

            if (collection == null) return;

            // 2. Sözlüğe çevir (Hızlı erişim için)
            Dictionary<string, string> saveMap = new Dictionary<string, string>();
            for (int i = 0; i < collection.ids.Count; i++)
            {
                if(i < collection.jsonDatas.Count)
                    saveMap[collection.ids[i]] = collection.jsonDatas[i];
            }

            // 3. Sahnedeki objelere dağıt
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
            foreach (var saveable in saveables)
            {
                string id = (saveable as MonoBehaviour).name;
                if (saveMap.ContainsKey(id))
                {
                    // JSON string'i olduğu gibi objeye gönder, o kendi açsın
                    saveable.RestoreState(saveMap[id]);
                }
            }
            
            Debug.Log("Oyun Yüklendi!");
        }
    }
}