using UnityEngine;

// YANLIŞ OLAN "Engine2_D" İSİMLENDİRMESİ DÜZELTİLDİ
namespace IndianOceanAssets.Engine2_5D
{
    public class ProjectileShooter : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject projectilePrefab; // Hangi mermi prefab'ını spawn edeceğimizi bilmemiz gerekiyor.

        // --- DEĞİŞİKLİK BAŞLANGICI ---
        [Header("Stats Data")]
        [Tooltip("Mermiye atanacak stat'ları (hız, yarıçap) bu veriden çeker.")]
        [SerializeField] private PlayerStatsData statsData; // YENİ: Inspector'dan sürüklenecek
        // --- DEĞİŞİKLİK SONU ---

        public void FireAtPoint(Vector3 targetPoint)
        {
            // YENİ: Mermiyi havuzdan "projectile" etiketiyle spawn et
            GameObject projectileGO = ObjectPooler.Instance.SpawnFromPool("projectile", transform.position, Quaternion.identity);

            if (projectileGO != null)
            {
                // Merminin script'ine eriş
                Projectile projectileScript = projectileGO.GetComponent<Projectile>();
                
                // --- DEĞİŞİKLİK BAŞLANGICI ---
                // Mermiyi fırlatmadan önce stat'larını ayarla
                if (statsData != null)
                {
                    // 'statsData' asset'inden okuduğumuz değerleri mermiye gönder.
                    projectileScript.Initialize(statsData.projectileSpeed, statsData.projectileRadius);
                }
                else
                {
                    // GÜVENLİK ÖNLEMİ: Eğer 'statsData' atanmamışsa,
                    // oyunu çökertmek yerine varsayılan değerlerle başlat ve uyarı ver.
                    Debug.LogWarning("ProjectileShooter üzerinde 'StatsData' referansı eksik! Varsayılan değerler kullanılıyor.");
                    projectileScript.Initialize(10f, 1f); // Varsayılan değerler
                }
                // --- DEĞİŞİKLİK SONU ---

                // Mermiye hedefini söyle
                projectileScript.SetTarget(targetPoint);
                
                // Karakterin yönünü hedefe doğru çevir (Sprite flip)
                FlipTowards(targetPoint);
            }
        }

        // Karakterin sprite'ını hedefe göre çevirir
        private void FlipTowards(Vector3 target)
        {
            if (target.x > transform.position.x)
                transform.localScale = new Vector3(1, 1, 1);
            else
                transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}