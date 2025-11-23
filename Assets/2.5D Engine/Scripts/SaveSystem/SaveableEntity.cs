using UnityEngine;
using System;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [ExecuteInEditMode] // Editörde çalışır, oyunu başlatmadan ID üretir.
    public class SaveableEntity : MonoBehaviour
    {
        [Tooltip("Bu objenin benzersiz kimliği. Otomatik oluşturulur, elle değiştirmeyin.")]
        [SerializeField] private string uniqueID = "";

        // Dışarıdan okumak için Property
        public string ID => uniqueID;

        // Sahne genelinde ID çakışmasını önlemek için statik liste (Opsiyonel güvenlik)
        private static Dictionary<string, SaveableEntity> globalLookup = new Dictionary<string, SaveableEntity>();

        private void OnValidate()
        {
            // Eğer oyun çalışmıyorsa (Editör modundaysak) ve ID boşsa oluştur.
            if (!Application.IsPlaying(gameObject))
            {
                if (string.IsNullOrEmpty(uniqueID))
                {
                    GenerateID();
                }
            }
        }

        private void GenerateID()
        {
            uniqueID = Guid.NewGuid().ToString();
            // Unity Editöründe objeyi "kirli" işaretle ki değişiklik kaybolmasın
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // Scripti resetlersen veya yeni eklersen ID üret
        private void Reset()
        {
            GenerateID();
        }
    }
}