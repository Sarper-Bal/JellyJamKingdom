/*
 * AUTO ATTACK (TETİKLEYİCİ)
 * * DEĞİŞİKLİKLER (v4 - Burst Fire):
 * - 'Update()' metodu güncellendi:
 * - Atış zamanı geldiğinde ('nextFireTime') 'PlayerStats'tan 'CurrentProjectilesPerShot' okunur.
 * - Eğer sayı 1 ise, 'ProjectileShooter.FireAtPoint()' doğrudan çağrılır (eski sistem).
 * - Eğer sayı > 1 ise, 'StartCoroutine(PerformBurstFire(...))' çağrılır (yeni sistem).
 * - 'PerformBurstFire' adında yeni bir private Coroutine eklendi.
 * - Bu Coroutine, mermileri 'CurrentBurstFireDelay' saniye aralıklarla arka arkaya ateşler.
 * - 'ProjectileShooter.cs' script'inde HİÇBİR değişiklik gerekmemiştir.
 */

using UnityEngine;
// --- YENİ EKLENTİ ---
// Coroutine (IEnumerator) kullanabilmek için bu kütüphane eklendi.
using System.Collections;
// --- YENİ EKLENTİ SONU ---

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
        private PlayerStats playerStats;

        // Zamanlayıcılar
        private float nextFireTime;
        private float nextTargetSearchTime;
        
        // --- YENİ EKLENTİ ---
        // Zaten bir burst atışı yapılırken yenisini tetiklememek için kontrol bayrağı
        private bool isFiringBurst = false; 
        // --- YENİ EKLENTİ SONU ---

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            projectileShooter = GetComponent<ProjectileShooter>();
            playerStats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            // PlayerStats'tan anlık 'hareketliyken ateş etme' ayarını oku.
            bool canFireWhileMoving = playerStats.CurrentCanFireWhileMoving;

            // Eğer karakter hareket ediyorsa VE hareketliyken ateş edemiyorsa, saldırıyı durdur.
            if (playerController.IsMoving && !canFireWhileMoving)
            {
                currentTarget = null; // Hedefi unut
                return; // Saldırı yapma
            }
            
            // 1. Hedef bulma zamanı geldiyse yeni hedef ara.
            if (Time.time > nextTargetSearchTime)
            {
                FindNearestEnemy();
                nextTargetSearchTime = Time.time + (1f / targetSearchFrequency);
            }

            // --- DEĞİŞİKLİK BAŞLANGICI (Burst Fire Mantığı) ---
            
            // 2. Eğer geçerli bir hedefimiz varsa, atış zamanı geldiyse VE
            //    halihazırda bir burst atışı yapmıyorsak...
            if (currentTarget != null && Time.time > nextFireTime && !isFiringBurst)
            {
                // Ana saldırı döngüsünün bekleme süresini (Cooldown) AYARLA.
                // Bir sonraki 'burst' saldırısı ancak bu süre dolduktan sonra başlayabilir.
                float currentAttackCooldown = 1f / playerStats.CurrentAttackSpeed;
                nextFireTime = Time.time + currentAttackCooldown;
                
                // Stat'lardan mermi sayısını oku
                int projectilesToFire = playerStats.CurrentProjectilesPerShot;

                if (projectilesToFire <= 1)
                {
                    // Mermi sayısı 1 ise: Coroutine'e gerek yok, doğrudan ateşle (Eski sistem)
                    projectileShooter.FireAtPoint(currentTarget.position);
                }
                else
                {
                    // Mermi sayısı 1'den fazla ise: Burst Coroutine'ini başlat (Yeni sistem)
                    float burstDelay = playerStats.CurrentBurstFireDelay;
                    StartCoroutine(PerformBurstFire(projectilesToFire, burstDelay));
                }
            }
            // --- DEĞİŞİKLİK SONU ---
        }

        /// <summary>
        /// Belirlenen sayıda mermiyi, belirlenen gecikme ile arka arkaya ateşler.
        /// </summary>
        /// <param name="count">Atılacak toplam mermi sayısı</param>
        /// <param name="delay">Mermiler arasındaki saniye cinsinden gecikme</param>
        private IEnumerator PerformBurstFire(int count, float delay)
        {
            isFiringBurst = true; // Burst atışını başlat, 'Update' tekrar girmesin.

            for (int i = 0; i < count; i++)
            {
                // Hedefin hala geçerli olup olmadığını KONTROL ET
                // (Eğer düşman menzilden çıktıysa veya öldüyse burst'ü yarıda kes)
                if (currentTarget == null)
                {
                    break; // Döngüyü kır
                }
                
                // Hedef hala geçerliyse, ateş et
                projectileShooter.FireAtPoint(currentTarget.position);
                
                // Son mermi değilse, 'delay' kadar bekle
                if (i < count - 1)
                {
                    yield return new WaitForSeconds(delay);
                }
            }
            
            isFiringBurst = false; // Burst atışı bitti, 'Update' artık yeni bir saldırı başlatabilir.
        }

        /// <summary>
        /// En yakın düşmanı bulan optimize edilmiş fonksiyon.
        /// </summary>
        private void FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float closestDistance = Mathf.Infinity;
            GameObject nearestEnemy = null;
            
            // PlayerStats'tan anlık saldırı menzilini oku
            float currentAttackRange = playerStats.CurrentAttackRange;

            foreach (GameObject enemy in enemies)
            {
                // Güvenlik: Düşman hala aktif mi? (Ölmüş ve havuza dönmüş olabilir)
                if (!enemy.activeInHierarchy) continue;
                
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