/*
 * PROJECTILE SHOOTER (VERİ AKTARICI)
 * * DEĞİŞİKLİKLER:
 * - 'FireAtPoint' metodu güncellendi.
 * - 'projectileScript.Initialize' çağrısına artık 3. parametre olarak
 * 'playerStats.CurrentProjectileDamage' (anlık hasar) değeri gönderiliyor.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    [RequireComponent(typeof(PlayerStats))] // PlayerStats component'ini zorunlu kıl
    public class ProjectileShooter : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject projectilePrefab; 

        // Player'ın anlık stat'larını yöneten component'e referans
        private PlayerStats playerStats; 

        private void Awake()
        {
            // Gerekli PlayerStats component'ini al
            playerStats = GetComponent<PlayerStats>();
        }

        /// <summary>
        /// Belirtilen noktaya doğru bir mermi ateşler.
        /// (AutoAttack.cs tarafından çağrılır)
        /// </summary>
        public void FireAtPoint(Vector3 targetPoint)
        {
            // Mermiyi havuzdan "projectile" etiketiyle spawn et
            GameObject projectileGO = ObjectPooler.Instance.SpawnFromPool("projectile", transform.position, Quaternion.identity);

            if (projectileGO != null)
            {
                Projectile projectileScript = projectileGO.GetComponent<Projectile>();
                
                // --- DEĞİŞİKLİK BAŞLANGICI ---
                if (playerStats != null)
                {
                    // Mermiyi fırlatmadan önce stat'larını ayarla:
                    // 1. Hız, 2. Yarıçap, 3. Hasar
                    projectileScript.Initialize(
                        playerStats.CurrentProjectileSpeed, 
                        playerStats.CurrentProjectileRadius,
                        playerStats.CurrentProjectileDamage // YENİ: Hasar parametresi eklendi
                    );
                }
                else
                {
                    // GÜVENLİK ÖNLEMİ: PlayerStats yoksa varsayılan değerleri kullan
                    Debug.LogWarning("ProjectileShooter üzerinde 'PlayerStats' referansı eksik! Varsayılan değerler kullanılıyor.");
                    projectileScript.Initialize(10f, 1f, 5); // Varsayılan değerler (5 hasar)
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