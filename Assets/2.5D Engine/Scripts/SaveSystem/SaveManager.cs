using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace IndianOceanAssets.Engine2_5D
{
    [DefaultExecutionOrder(-100)]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        
        private string saveFilePath;
        private string tempFilePath;
        private string backupFilePath;
        private bool isSaving = false; 

        [System.Serializable]
        private class SaveDataCollection
        {
            public List<string> ids = new List<string>();
            public List<string> jsonDatas = new List<string>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            // Not: DontDestroyOnLoad kullanmadığımız senaryo (Her sahnede prefab var)
            
            string basePath = Path.Combine(Application.persistentDataPath, "savegame");
            saveFilePath = basePath + ".json";
            tempFilePath = basePath + ".tmp";
            backupFilePath = basePath + ".bak";
        }

        private void Start()
        {
            LoadGame();
        }

        [ContextMenu("Save Game")]
        public void SaveGameTrigger() => _ = SaveGameAsync();

        public async Task SaveGameAsync()
        {
            if (isSaving) return;
            isSaving = true;

            // --- ADIM 1: MEVCUT SAHNEDEKİ VERİLERİ TOPLA ---
            Dictionary<string, string> currentSceneData = new Dictionary<string, string>();
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();

            foreach (var saveable in saveables)
            {
                MonoBehaviour mb = saveable as MonoBehaviour;
                if (mb == null) continue;

                SaveableEntity idComponent = mb.GetComponent<SaveableEntity>();
                if (idComponent != null)
                {
                    // Bu sahnedeki güncel veriyi al
                    currentSceneData[idComponent.ID] = JsonUtility.ToJson(saveable.CaptureState());
                }
            }

            // --- ADIM 2: ESKİ DOSYAYI OKU VE BİRLEŞTİR (ARKA PLAN) ---
            await Task.Run(() => 
            {
                try
                {
                    // A. Eski verileri yükle (Varsa)
                    Dictionary<string, string> finalDataMap = new Dictionary<string, string>();

                    if (File.Exists(saveFilePath))
                    {
                        string existingJson = File.ReadAllText(saveFilePath);
                        if (!string.IsNullOrEmpty(existingJson))
                        {
                            SaveDataCollection existingCollection = JsonUtility.FromJson<SaveDataCollection>(existingJson);
                            if (existingCollection != null)
                            {
                                for (int i = 0; i < existingCollection.ids.Count; i++)
                                {
                                    if (i < existingCollection.jsonDatas.Count)
                                        finalDataMap[existingCollection.ids[i]] = existingCollection.jsonDatas[i];
                                }
                            }
                        }
                    }

                    // B. Yeni sahne verilerini eskilerin üzerine yaz (Merge/Update)
                    foreach (var kvp in currentSceneData)
                    {
                        // Varsa günceller, yoksa yeni ekler.
                        // Silo bu sahnede yoksa, 'finalDataMap' içindeki eski Silo verisi korunur.
                        finalDataMap[kvp.Key] = kvp.Value;
                    }

                    // C. Tekrar Listeye çevir (JSON için)
                    SaveDataCollection finalCollection = new SaveDataCollection();
                    finalCollection.ids = finalDataMap.Keys.ToList();
                    finalCollection.jsonDatas = finalDataMap.Values.ToList();

                    string jsonContent = JsonUtility.ToJson(finalCollection, true);

                    // D. Dosyaya Yaz (Atomik)
                    File.WriteAllText(tempFilePath, jsonContent);
                    if (File.Exists(saveFilePath)) File.Copy(saveFilePath, backupFilePath, true);
                    File.Copy(tempFilePath, saveFilePath, true);
                    File.Delete(tempFilePath);
                }
                catch (Exception e) { Debug.LogError($"Save Error: {e.Message}"); }
            });

            if (this == null) return;
            isSaving = false;
            Debug.Log("Oyun Kaydedildi (Veriler Birleştirildi).");
        }

        [ContextMenu("Load Game")]
        public void LoadGame()
        {
            if (LoadFile(saveFilePath)) { Debug.Log("Veriler Yüklendi."); return; }
            if (LoadFile(backupFilePath)) { Debug.LogWarning("Yedek Yüklendi."); }
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
                    if(i < collection.jsonDatas.Count) saveMap[collection.ids[i]] = collection.jsonDatas[i];

                var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
                foreach (var saveable in saveables)
                {
                    MonoBehaviour mb = saveable as MonoBehaviour;
                    if (mb == null) continue;

                    SaveableEntity idComponent = mb.GetComponent<SaveableEntity>();
                    if (idComponent != null && saveMap.ContainsKey(idComponent.ID))
                        saveable.RestoreState(saveMap[idComponent.ID]);
                }
                return true;
            }
            catch { return false; }
        }
    }
}