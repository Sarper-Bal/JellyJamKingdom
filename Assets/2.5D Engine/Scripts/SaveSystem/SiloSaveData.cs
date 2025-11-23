using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [System.Serializable]
    public class SiloSaveData
    {
        // JSON sadece Listeleri sever, Dictionary sevmez.
        public List<InventoryEntry> inventory = new List<InventoryEntry>();

        [System.Serializable]
        public class InventoryEntry
        {
            public string resourceID; // Kaynağın dosya adı (Örn: "Wood")
            public int amount;        // Miktar
        }
    }
}