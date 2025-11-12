/*
 * AUTO ATTACK (TÜKETİCİ)
 * * DEĞİŞİKLİKLER:
 * - 'Update()' metodunun başındaki 'IsMoving' kontrolü güncellendi.
 * - Artık: "Eğer hareket ediyorsam VE playerStats'ım hareketliyken ateş edemez
 * (CurrentCanFireWhileMoving == false) diyorsa" saldırıyı durdur.
 * - Eğer 'CurrentCanFireWhileMoving' true ise, hareket etse bile saldırıya devam eder.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // Gerekli bileşenleri (PlayerStats dahil) zorunlu kılıyoruz.
    [RequireComponent(typeof(PlayerController), typeof(ProjectileShooter), typeof(PlayerStats))]
    public class AutoAttack : MonoBehaviour
    {
        [Header("Optimization")]
        [Tooltip("Saniyede kaç kez hedef aranacağını belirler. Performans için önemlidir.")]
        [SerializeField] private float targetSearchFrequency = 4f;

        // Gerekli bileşenlere referanslar
        private PlayerController playerController;
        private ProjectileShooter projectileShooter;
        private Transform currentTarget;
        
        // Player'ın anlık stat'larını tutan component'e referans
        private PlayerStats playerStats;

        // Zamanlayıcılar
        private float nextFireTime;
        private float nextTargetSearchTime;

        private void Awake()
        {
            // Gerekli bileşenleri en başta alıyoruz.
            playerController = GetComponent<PlayerController>();
            projectileShooter = GetComponent<ProjectileShooter>();
            playerStats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            
            // PlayerStats'tan anlık 'hareketliyken ateş etme' ayarını oku.
            bool canFireWhileMoving = playerStats.CurrentCanFireWhileMoving;

            // Eğer karakter hareket ediyorsa (IsMoving == true)
            // VE
            // hareketliyken ateş etme yeteneği YOKSA (canFireWhileMoving == false)
            // ...o zaman saldırıyı durdur.
            if (playerController.IsMoving && !canFireWhileMoving)
            {
                currentTarget = null; // Hedefi unut
                return; // Saldırı yapma
            }
            
            // Eğer duruyorsa (IsMoving == false) VEYA
            // hareketliyken ateş etme yeteneği VARSA (canFireWhileMoving == true),
            // kod buradan aşağıya normal şekilde devam eder.
            
            // --- DEĞİŞİKLİK SONU ---


            // Eğer karakter duruyorsa (veya hareketliyken ateş edebiliyorsa):
            
            // 1. Hedef bulma zamanı geldiyse yeni hedef ara.
            if (Time.time > nextTargetSearchTime)
            {
                FindNearestEnemy();
                // Bir sonraki arama zamanını ayarla.
                nextTargetSearchTime = Time.time + (1f / targetSearchFrequency);
            }

            // 2. Eğer geçerli bir hedefimiz varsa ve ateş etme zamanı geldiyse...
            if (currentTarget != null && Time.time > nextFireTime)
            {
                // Ateş et!
                projectileShooter.FireAtPoint(currentTarget.position);
                
                // Bir sonraki ateş etme zamanını ayarla
                float currentFireRate = 1f / playerStats.CurrentAttackSpeed;
                nextFireTime = Time.time + currentFireRate;
            }
        }

        // En yakın düşmanı bulan optimize edilmiş fonksiyon.
        private void FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float closestDistance = Mathf.Infinity;
            GameObject nearestEnemy = null;
            
            // PlayerStats'tan anlık saldırı menzilini oku
            float currentAttackRange = playerStats.CurrentAttackRange;

            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            // Eğer en yakın düşman anlık menzil içindeyse, onu hedefimiz yap.
            if (nearestEnemy != null && closestDistance <= currentAttackRange)
            {
                currentTarget = nearestEnemy.transform;
            }
            else
            {
                // Menzil içinde düşman yoksa, hedefi unut.
                currentTarget = null;
            }
        }
    }
}