using UnityEngine;

// YANLIŞ OLAN "Engine2_D" İSİMLENDİRMESİ DÜZELTİLDİ
namespace IndianOceanAssets.Engine2_5D
{
    // --- YENİ EKLENTİ: PlayerStats component'ini zorunlu kıl ---
    [RequireComponent(typeof(PlayerStats))]
    public class ProjectileShooter : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject projectilePrefab; // Hangi mermi prefab'ını spawn edeceğimizi bilmemiz gerekiyor.

        // --- DEĞİŞİKLİK BAŞLANGICI ---
        [Header("Stats Data")]
        // [SerializeField] private PlayerStatsData statsData; // ESKİ: Kaldırıldı
        
        // YENİ: Player'ın anlık stat'larını yöneten component'e referans
        private PlayerStats playerStats; 
        // --- DEĞİŞİKLİK SONU ---

        // --- YENİ EKLENTİ: Awake metodu ---
        private void Awake()
        {
            // Gerekli PlayerStats component'ini al
            playerStats = GetComponent<PlayerStats>();
        }
        // --- EKLENTİ SONU ---

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
                if (playerStats != null)
                {
                    // 'statsData' yerine 'playerStats' component'inden okuduğumuz anlık (Current) değerleri mermiye gönder.
                    projectileScript.Initialize(playerStats.CurrentProjectileSpeed, playerStats.CurrentProjectileRadius);
                }
                else
                {
                    // GÜVENLİK ÖNLEMİ
                    Debug.LogWarning("ProjectileShooter üzerinde 'PlayerStats' component'i bulunamadı! Varsayılan değerler kullanılıyor.");
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