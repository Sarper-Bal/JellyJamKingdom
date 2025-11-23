using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks; // <-- ASYNC GÖREVLER İÇİN GEREKLİ

namespace IndianOceanAssets.Engine2_5D
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        
        private string saveFilePath;
        private string tempFilePath;
        private string backupFilePath;
        
        // Aynı anda iki kayıt işleminin çakışmasını önlemek için kilit
        private bool isSaving = false; 

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
            
            string basePath = Path.Combine(Application.persistentDataPath, "savegame");
            saveFilePath = basePath + ".json";
            tempFilePath = basePath + ".tmp";
            backupFilePath = basePath + ".bak";
        }

        private void Start()
        {
            LoadGame();
        }

        // ContextMenu asenkron metotları desteklemez, bu yüzden bir wrapper kullanıyoruz
        [ContextMenu("Save Game")]
        public void SaveGameTrigger()
        {
            _ = SaveGameAsync(); // "Fire and Forget" (Başlat ve unut)
        }

        public async Task SaveGameAsync()
        {
            if (isSaving)
            {
                Debug.LogWarning("Zaten kayıt işlemi devam ediyor...");
                return;
            }

            isSaving = true;
            
            // ADIM 1: VERİ TOPLAMA (MAIN THREAD)
            // Unity objelerine (Transform, GameObject) sadece ana iş parçacığından erişilebilir.
            // Bu yüzden veriyi burada hızlıca topluyoruz.
            SaveDataCollection collection = new SaveDataCollection();
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();

            foreach (var saveable in saveables)
            {
                MonoBehaviour mb = saveable as MonoBehaviour;
                SaveableEntity idComponent = mb.GetComponent<SaveableEntity>();
                
                if (idComponent != null)
                {
                    collection.ids.Add(idComponent.ID);
                    // Veriyi nesne (Object) olarak alıyoruz
                    collection.jsonDatas.Add(JsonUtility.ToJson(saveable.CaptureState()));
                }
            }

            // Veriyi tek bir büyük JSON metnine çevir (Bu işlem hızlıdır ama veri büyükse biraz sürebilir)
            string jsonContent = JsonUtility.ToJson(collection, true);

            // ADIM 2: DOSYAYA YAZMA (BACKGROUND THREAD)
            // İşte SİHİR burada! Diske yazma işlemi arka plana atılıyor.
            // Oyun bu sırada donmaz.
            await Task.Run(() => 
            {
                try
                {
                    // Atomik Kayıt İşlemleri (Ağır İşler)
                    File.WriteAllText(tempFilePath, jsonContent);
                    
                    if (File.Exists(saveFilePath))
                        File.Copy(saveFilePath, backupFilePath, true);

                    File.Copy(tempFilePath, saveFilePath, true);
                    File.Delete(tempFilePath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Arka plan kayıt hatası: {e.Message}");
                }
            });

            isSaving = false;
            Debug.Log("Oyun Asenkron Olarak Kaydedildi! (Takılma yok)");
        }

        [ContextMenu("Load Game")]
        public void LoadGame()
        {
            // Yükleme işlemi genelde oyun başında bir kez yapılır ve "Loading Screen" olur.
            // Bu yüzden Yükleme'nin senkron (bloklayıcı) olması kabul edilebilir.
            // Ancak istersen bunu da async yapabiliriz. Şimdilik güvenli senkron bırakıyorum.
            
            if (LoadFile(saveFilePath))
            {
                Debug.Log("Oyun Yüklendi.");
                return;
            }
            
            if (LoadFile(backupFilePath))
            {
                Debug.LogWarning("Yedek dosya yüklendi.");
            }
            else
            {
                Debug.Log("Kayıt bulunamadı. Yeni oyun.");
            }
        }

        private bool LoadFile(string path)
        {
            if (!File.Exists(path)) return false;

            try
            {
                string fileJson = File.ReadAllText(path);
                if (string.IsNullOrEmpty(fileJson)) return false;

                SaveDataCollection collection = JsonUtility.FromJson<SaveDataCollection>(fileJson);
                if (collection == null) return false;

                Dictionary<string, string> saveMap = new Dictionary<string, string>();
                for (int i = 0; i < collection.ids.Count; i++)
                {
                    if(i < collection.jsonDatas.Count)
                        saveMap[collection.ids[i]] = collection.jsonDatas[i];
                }

                var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
                foreach (var saveable in saveables)
                {
                    MonoBehaviour mb = saveable as MonoBehaviour;
                    SaveableEntity idComponent = mb.GetComponent<SaveableEntity>();

                    if (idComponent != null && saveMap.ContainsKey(idComponent.ID))
                    {
                        saveable.RestoreState(saveMap[idComponent.ID]);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Yükleme hatası: {path}\n{e.Message}");
                return false;
            }
        }
    }
}