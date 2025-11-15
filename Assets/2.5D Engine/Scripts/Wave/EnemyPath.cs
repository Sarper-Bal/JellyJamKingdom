/*
 * DÜŞMAN YOLU (ENEMY PATH) - v1.1
 * GÖREVİ:
 * Sahnedeki belirli bir yolu (waypoint dizisi) ve bu yolun kimliğini (ID)
 * tutan bir component.
 *
 * DEĞİŞİKLİK:
 * - 'pathID' eklendi. 'WaveManager' bu objeyi 'FindObjectOfType' ile
 * bulacak ve 'pathID' ile bir sözlüğe (Dictionary) kaydedecek.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    public class EnemyPath : MonoBehaviour
    {
        // --- BU SİZDE EKSİK OLAN ALANDI ---
        [Tooltip("Bu yolun benzersiz kimlik (ID) numarası. " +
                 "'WaveProfile' asset'indeki 'SpawnEvent' bu ID'yi kullanacak.")]
        public int pathID = 0; // <-- YENİ EKLENDİ (v1.1)
        // --- ---
        
        [Tooltip("Bu yolu oluşturan Transform (boş obje) noktalarının sıralı listesi.")]
        public Transform[] waypoints;
    }
}