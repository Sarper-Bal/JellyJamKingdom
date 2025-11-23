using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [CreateAssetMenu(fileName = "NewUpgradeCard", menuName = "Engine 2.5D/Upgrade Card")]
    public class UpgradeData : ScriptableObject
    {
        [Header("Card Visuals")]
        public string upgradeName;          // Kartın oyunda görünen adı (Örn: "Hermes Boots")
        [TextArea(3,5)]
        public string description;          // Açıklama (Örn: "Increases movement speed.")
        public Sprite icon;                 // Kartın ikonu
        
        [Header("Effects")]
        [Tooltip("Bu kart alındığında uygulanacak tüm geliştirmelerin listesi.")]
        public List<StatBonus> bonuses;     // Bir kart birden fazla özellik verebilir.
    }

    /// <summary>
    /// Tek bir stat değişikliğini tanımlayan yardımcı sınıf.
    /// </summary>
    [System.Serializable]
    public class StatBonus
    {
        [Tooltip("Geliştirilecek özellik")]
        public StatType statType;

        [Tooltip("Değer (Örn: 10 veya 0.1)")]
        public float value;

        [Tooltip("Artış türü (Flat: Düz Ekleme, PercentAdd: Yüzdesel)")]
        public StatModType modType;
    }
}