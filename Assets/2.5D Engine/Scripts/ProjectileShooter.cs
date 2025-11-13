/*
 * PROJECTILE SHOOTER (VERİ AKTARICI)
 * * DEĞİŞİKLİKLER (v2.0 - Fire Point):
 * - 'firePoint' adında opsiyonel bir 'Transform' referansı eklendi.
 * - 'FireAtPoint' metodu güncellendi:
 * - Artık mermiyi 'transform.position' yerine 'spawnPosition'dan ateşliyor.
 * - 'spawnPosition' değeri, 'firePoint' atanmışsa 'firePoint.position',
 * atanmamışsa (null ise) 'transform.position' olarak belirleniyor.
 * - Bu sayede script, hem Player (silah ucuyla) hem de Kule (pivot noktasıyla)
 * için esnek bir şekilde kullanılabilir.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    [RequireComponent(typeof(PlayerStats))] // PlayerStats component'ini zorunlu kıl
    public class ProjectileShooter : MonoBehaviour
    {
        [Header("Prefab")]
        [Tooltip("Havuzdan 'projectile' tag'i ile çağrılacak mermi prefab'ı.")]
        [SerializeField] private GameObject projectilePrefab; 

        // --- YENİ EKLENEN KISIM BAŞLANGICI ---
        [Header("Configuration")]
        [Tooltip("(Opsiyonel) Merminin çıkacağı özel nokta (örn: silahın namlu ucu). " +
                 "Eğer boş bırakılırsa, bu objenin kendi 'transform.position'u (pivot noktası) kullanılır.")]
        [SerializeField] private Transform firePoint;
        // --- YENİ EKLENEN KISIM SONU ---

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
            // --- DEĞİŞİKLİK BAŞLANGICI (Spawn Pozisyonu) ---
            
            // 1. Merminin nereden spawn olacağına karar ver.
            //    'firePoint' atanmışsa orayı, atanmamışsa (null ise) bu objenin
            //    kendi pozisyonunu (transform.position) kullan.
            Vector3 spawnPosition = (firePoint != null) ? firePoint.position : transform.position;
            
            // 2. Mermiyi havuzdan bu hesaplanmış 'spawnPosition' ile çağır.
            GameObject projectileGO = ObjectPooler.Instance.SpawnFromPool(
                "projectile", 
                spawnPosition, // 'transform.position' yerine 'spawnPosition' kullan
                Quaternion.identity
            );
            
            // --- DEĞİŞİKLİK SONU ---

            if (projectileGO != null)
            {
                Projectile projectileScript = projectileGO.GetComponent<Projectile>();
                
                if (playerStats != null)
                {
                    // Mermiyi fırlatmadan önce stat'larını ayarla:
                    // 1. Hız, 2. Yarıçap, 3. Hasar
                    projectileScript.Initialize(
                        playerStats.CurrentProjectileSpeed, 
                        playerStats.CurrentProjectileRadius,
                        playerStats.CurrentProjectileDamage 
                    );
                }
                else
                {
                    // GÜVENLİK ÖNLEMİ: PlayerStats yoksa varsayılan değerleri kullan
                    Debug.LogWarning("ProjectileShooter üzerinde 'PlayerStats' referansı eksik! Varsayılan değerler kullanılıyor.");
                    projectileScript.Initialize(10f, 1f, 5); // Varsayılan değerler
                }

                // Mermiye hedefini söyle
                projectileScript.SetTarget(targetPoint);
                
                // Karakterin yönünü (sprite'ını) hedefe doğru çevir
                FlipTowards(targetPoint);
            }
        }

        /// <summary>
        /// Karakterin sprite'ını (veya GFX objesini) hedefe göre çevirir.
        /// 'localScale' değerini ezmeden, sadece 'x' ekseninin yönünü değiştirir.
        /// (Bu, bir önceki "boyut sıfırlanma" hatası için düzeltmeyi içerir)
        /// </summary>
        private void FlipTowards(Vector3 target)
        {
            Vector3 currentScale = transform.localScale;
            float scaleMagnitudeX = Mathf.Abs(currentScale.x);
            
            if (target.x > transform.position.x)
            {
                currentScale.x = scaleMagnitudeX; // Hedef sağda: Yönü pozitif yap
            }
            else
            {
                currentScale.x = -scaleMagnitudeX; // Hedef solda: Yönü negatif yap
            }
            
            // Sadece 'x' yönü güncellenmiş olan scale'i geri ata.
            transform.localScale = currentScale;
        }
    }
}