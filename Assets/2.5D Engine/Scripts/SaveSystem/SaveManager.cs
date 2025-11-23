using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System; // Exception yakalamak için gerekli

namespace IndianOceanAssets.Engine2_5D
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        
        // Dosya Yolları
        private string saveFilePath;
        private string tempFilePath;   // Geçici dosya (Yazma sırasındaki risk alanı)
        private string backupFilePath; // Yedek dosya (Eskisi bozulursa cankurtaran)

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
            
            // Dosya yollarını belirle
            string basePath = Path.Combine(Application.persistentDataPath, "savegame");
            saveFilePath = basePath + ".json";
            tempFilePath = basePath + ".tmp"; // .tmp uzantılı geçici dosya
            backupFilePath = basePath + ".bak"; // .bak uzantılı yedek dosya
        }

        private void Start()
        {
            LoadGame();
        }

        [ContextMenu("Save Game")]
        public void SaveGame()
        {
            // 1. Verileri Topla (Hafızada)
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
            SaveDataCollection collection = new SaveDataCollection();

            foreach (var saveable in saveables)
            {
                MonoBehaviour mb = saveable as MonoBehaviour;
                SaveableEntity idComponent = mb.GetComponent<SaveableEntity>();
                
                if (idComponent == null)
                {
                    Debug.LogWarning($"SaveManager: '{mb.name}' üzerinde SaveableEntity yok, atlanıyor.");
                    continue;
                }

                collection.ids.Add(idComponent.ID);
                collection.jsonDatas.Add(JsonUtility.ToJson(saveable.CaptureState()));
            }

            // JSON'a çevir
            string jsonContent = JsonUtility.ToJson(collection, true);

            // 2. GÜVENLİ KAYIT İŞLEMİ (ATOMIC SAVE)
            try
            {
                // A. Önce geçici dosyaya yaz (Eğer burada hata olursa asıl dosya zarar görmez)
                File.WriteAllText(tempFilePath, jsonContent);
                
                // B. Asıl dosya varsa, onu yedeğe çek (.bak)
                if (File.Exists(saveFilePath))
                {
                    File.Copy(saveFilePath, backupFilePath, true);
                }

                // C. Geçici dosyayı asıl dosyanın yerine taşı (Bu işlem milisaniyeler sürer, risk çok azdır)
                File.Copy(tempFilePath, saveFilePath, true);
                
                // D. Geçici dosyayı temizle
                File.Delete(tempFilePath);

                Debug.Log("Oyun GÜVENLİ Şekilde Kaydedildi!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Kayıt sırasında kritik hata oluştu! Veriler korunuyor. Hata: {e.Message}");
                // Hata olsa bile eski saveFilePath hala duruyor, veri kaybı yok.
            }
        }

        [ContextMenu("Load Game")]
        public void LoadGame()
        {
            // Önce normal dosyayı yüklemeyi dene
            if (LoadFile(saveFilePath))
            {
                Debug.Log("Oyun Yüklendi.");
                return;
            }
            
            // Eğer normal dosya bozuksa veya yoksa, yedeği dene
            Debug.LogWarning("Asıl kayıt dosyası yüklenemedi veya yok. Yedek (.bak) kontrol ediliyor...");
            
            if (LoadFile(backupFilePath))
            {
                Debug.LogWarning("Oyun YEDEK dosyasından kurtarıldı!");
            }
            else
            {
                Debug.Log("Kayıt dosyası bulunamadı. Yeni oyun başlatılıyor.");
            }
        }

        // Yardımcı Yükleme Fonksiyonu
        private bool LoadFile(string path)
        {
            if (!File.Exists(path)) return false;

            try
            {
                string fileJson = File.ReadAllText(path);
                
                // Veri bütünlüğünü kontrol et (Boş veya bozuk mu?)
                if (string.IsNullOrEmpty(fileJson)) return false;

                SaveDataCollection collection = JsonUtility.FromJson<SaveDataCollection>(fileJson);
                if (collection == null) return false;

                // Dağıtım Sözlüğü
                Dictionary<string, string> saveMap = new Dictionary<string, string>();
                for (int i = 0; i < collection.ids.Count; i++)
                {
                    if(i < collection.jsonDatas.Count)
                        saveMap[collection.ids[i]] = collection.jsonDatas[i];
                }

                // Sahnedeki objelere dağıt
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
                
                return true; // Başarılı
            }
            catch (Exception e)
            {
                Debug.LogError($"Dosya yüklenirken hata: {path}\n{e.Message}");
                return false; // Yükleme başarısız
            }
        }
    }
}