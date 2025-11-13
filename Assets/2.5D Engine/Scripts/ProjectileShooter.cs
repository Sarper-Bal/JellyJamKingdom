/*
 * PROJECTILE SHOOTER (VERİ AKTARICI)
 * * DEĞİŞİKLİKLER (v1.1 - Boyut Hatası Düzeltmesi):
 * - 'FlipTowards' metodu, 'transform.localScale = new Vector3(1, 1, 1)'
 * kullanarak karakterin boyutunu EZECEK şekilde hatalı çalışıyordu.
 * - Metot, artık 'currentScale.x' değerini 'Mathf.Abs' kullanarak
 * mevcut boyutu (scale) koruyacak ve sadece yönünü (+ veya -)
 * değiştirecek şekilde güncellendi.
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
            // (ObjectPooler'daki statik listeye 'projectile' eklediğinizden emin olun)
            GameObject projectileGO = ObjectPooler.Instance.SpawnFromPool("projectile", transform.position, Quaternion.identity);

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

        // --- HATA DÜZELTMESİ BURADA ---
        /// <summary>
        /// Karakterin sprite'ını (veya GFX objesini) hedefe göre çevirir.
        /// 'localScale' değerini ezmeden, sadece 'x' ekseninin yönünü değiştirir.
        /// </summary>
        /// <param name="target">Hedefin pozisyonu</param>
        private void FlipTowards(Vector3 target)
        {
            // 1. Mevcut scale değerini oku (örn: (0.8, 0.8, 1))
            Vector3 currentScale = transform.localScale;

            // 2. Mevcut X scale'inin büyüklüğünü (mutlak değerini) al
            //    (Eğer scale -0.8 ise, 'scaleMagnitudeX' 0.8 olur)
            float scaleMagnitudeX = Mathf.Abs(currentScale.x);
            
            // 3. Hedefin konumuna göre sadece 'x' değerini güncelle
            if (target.x > transform.position.x)
            {
                // Hedef sağda: x yönünü pozitif yap (örn: 0.8)
                currentScale.x = scaleMagnitudeX;
            }
            else
            {
                // Hedef solda: x yönünü negatif yap (örn: -0.8)
                currentScale.x = -scaleMagnitudeX;
            }

            // 4. Sadece 'x' yönü güncellenmiş olan scale'i geri ata.
            //    'y' ve 'z' boyutları korunmuş olur.
            transform.localScale = currentScale;
        }
        // --- HATA DÜZELTMESİ SONU ---
    }
}