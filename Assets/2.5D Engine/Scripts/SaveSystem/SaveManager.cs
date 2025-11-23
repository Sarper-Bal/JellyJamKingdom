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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            
            // Dosya yolu: Cihazın kalıcı veri yolu / savegame.json
            saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        }

        private void Start()
        {
            // Oyun başladığında otomatik yükle (İsteğe bağlı)
            LoadGame();
        }

        [ContextMenu("Save Game")] // Editörde sağ tıkla test etmek için
        public void SaveGame()
        {
            // 1. Sahnedeki tüm kaydedilebilir objeleri bul
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
            
            // 2. Verileri topla (Dictionary: "Silo_1" -> Data)
            Dictionary<string, object> state = new Dictionary<string, object>();

            foreach (var saveable in saveables)
            {
                // Her objenin benzersiz bir ID'si olmalı. Şimdilik ismini kullanıyoruz.
                string id = (saveable as MonoBehaviour).name; 
                state[id] = saveable.CaptureState();
            }

            // 3. Dosyaya yaz (JSON)
            // Not: Dictionary'i JSON yapmak için basit bir wrapper kullanabiliriz
            // veya her objeyi ayrı ayrı satır satır yazabiliriz.
            // Basitlik için şimdilik Silo'ya özel manuel bir yapı kullanıyoruz:
            
            // Gelişmiş bir JSON kütüphanesi (Newtonsoft) kullanmadığımız için,
            // şimdilik sadece Silo verisini tekil olarak kaydedelim.
            // İleride burayı tüm objeler için genelleyeceğiz.
            
            // SADECE SILO TESTİ İÇİN:
            SiloController silo = FindObjectOfType<SiloController>();
            if (silo != null)
            {
                string json = JsonUtility.ToJson(silo.CaptureState(), true);
                File.WriteAllText(saveFilePath, json);
                Debug.Log($"Oyun Kaydedildi: {saveFilePath}");
            }
        }

        [ContextMenu("Load Game")]
        public void LoadGame()
        {
            if (!File.Exists(saveFilePath))
            {
                Debug.Log("Kayıt dosyası bulunamadı.");
                return;
            }

            string json = File.ReadAllText(saveFilePath);
            
            // SADECE SILO TESTİ İÇİN:
            SiloController silo = FindObjectOfType<SiloController>();
            if (silo != null)
            {
                // JSON'ı SiloSaveData sınıfına çevir
                SiloSaveData data = JsonUtility.FromJson<SiloSaveData>(json);
                silo.RestoreState(data);
                Debug.Log("Oyun Yüklendi!");
            }
        }
    }
}