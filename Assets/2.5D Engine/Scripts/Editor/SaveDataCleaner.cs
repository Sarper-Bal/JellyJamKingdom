#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveDataCleaner : EditorWindow
{
    [MenuItem("Tools/Clear Save Data (RESET)")]
    public static void ClearSaveData()
    {
        // Dosya yolunu bul (SaveManager'daki yol ile aynı olmalı)
        string basePath = Path.Combine(Application.persistentDataPath, "savegame");
        
        string[] filesToDelete = new string[]
        {
            basePath + ".json", // Asıl dosya
            basePath + ".tmp",  // Geçici dosya
            basePath + ".bak"   // Yedek dosya
        };

        bool deletedAny = false;

        foreach (var path in filesToDelete)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    Debug.Log($"Silindi: {path}");
                    deletedAny = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Dosya silinemedi: {path}\nHata: {e.Message}");
                }
            }
        }

        if (deletedAny)
        {
            EditorUtility.DisplayDialog("İşlem Tamam", "Tüm kayıt dosyaları başarıyla silindi.\nOyun sıfırlandı.", "Tamam");
        }
        else
        {
            EditorUtility.DisplayDialog("Bilgi", "Silinecek kayıt dosyası bulunamadı.\nZaten temiz.", "Tamam");
        }
        
        // PlayerPrefs temizliği (Eğer kullanıyorsan bunu da açabilirsin)
        // PlayerPrefs.DeleteAll();
    }
    
    [MenuItem("Tools/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        // Kayıt dosyasının olduğu klasörü Windows Gezgini'nde açar
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
#endif