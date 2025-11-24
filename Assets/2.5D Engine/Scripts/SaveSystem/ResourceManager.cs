using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    public static class ResourceManager
    {
        // Önbellek (Hız için)
        private static Dictionary<string, NpcHousingData> housingCache;

        /// <summary>
        /// Verilen isimdeki HousingData'yı Resources klasörünün neresinde olursa olsun bulur.
        /// </summary>
        public static NpcHousingData LoadHousingData(string dataName)
        {
            // 1. Önbellek boşsa doldur (Oyun başında bir kez çalışır)
            if (housingCache == null)
            {
                housingCache = new Dictionary<string, NpcHousingData>();
                // Tüm Resources klasörünü tara ve yükle
                var allData = Resources.LoadAll<NpcHousingData>(""); 
                foreach (var data in allData)
                {
                    if (!housingCache.ContainsKey(data.name))
                    {
                        housingCache.Add(data.name, data);
                    }
                }
                Debug.Log($"[ResourceManager] {housingCache.Count} adet bina verisi indekslendi.");
            }

            // 2. İsmi sözlükte ara
            if (housingCache.TryGetValue(dataName, out NpcHousingData result))
            {
                return result;
            }

            Debug.LogError($"[ResourceManager] HATA: '{dataName}' isimli veri Resources klasöründe bulunamadı!");
            return null;
        }
    }
}