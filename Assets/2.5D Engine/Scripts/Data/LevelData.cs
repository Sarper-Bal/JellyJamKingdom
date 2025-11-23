using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Engine 2.5D/Level Data")]
    public class LevelData : ScriptableObject
    {
        // --- YENİ: EDİTÖR İÇİN SAHNE REFERANSI ---
#if UNITY_EDITOR
        [Header("Editör Ayarları")]
        [Tooltip("Sahne dosyasını (.unity) buraya sürükleyin. İsim otomatik alınır.")]
        public UnityEditor.SceneAsset sceneAsset;
#endif
        // -----------------------------------------

        [Header("Sahne Ayarı (Otomatik)")]
        [Tooltip("Yukarıya sahne atadığında burası otomatik dolar. Elle değiştirmene gerek yok.")]
        public string sceneName; 

        [Header("Kahraman Verisi")]
        public PlayerStatsData heroStats; 

        [Header("Kule Verileri")]
        public List<PlayerStatsData> towerStats; 

        [Header("Bölüm Ödülleri")]
        [Tooltip("Bu bölümde çıkabilecek Upgrade Kartlarını buraya ekle.")]
        public List<UpgradeData> availableUpgrades;

        [Header("Düşman Dalgası")]
        public WaveSequence levelWaves; 

        // --- OTOMATİK İSİM ALMA SİSTEMİ ---
        // Sen Inspector'da bir şey değiştirdiğinde bu fonksiyon çalışır
        private void OnValidate()
        {
#if UNITY_EDITOR
            if (sceneAsset != null)
            {
                // Sahne dosyasının adını alıp string değişkene yazar
                string newName = sceneAsset.name;
                
                if (sceneName != newName)
                {
                    sceneName = newName;
                    // Değişikliği kaydetmesi için objeyi "Kirli" (Dirty) işaretle
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
#endif
        }
    }
}