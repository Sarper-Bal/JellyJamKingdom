/*
 * AUTO ATTACK (MODÜLER YAPI)
 * * DEĞİŞİKLİKLER:
 * - 'RequireComponent(typeof(PlayerController))' bağımlılığı kaldırıldı.
 * - 'private PlayerController playerController;' referansı,
 * 'private IAttackStateProvider attackStateProvider;' (arayüz) referansı ile değiştirildi.
 * - 'Awake()' metodu artık 'GetComponent<IAttackStateProvider>()' çağırıyor.
 * - 'Update()' metodundaki 'playerController.IsMoving' kontrolü,
 * 'if (!attackStateProvider.CanAttack())' kontrolü ile değiştirildi.
 * - Bu script artık Player, Kule, Pet vb. 'IAttackStateProvider' arayüzünü
 * uygulayan her şey üzerinde çalışabilir.
 */

using UnityEngine;
using System.Collections;

namespace IndianOceanAssets.Engine2_5D
{
    // --- DEĞİŞİKLİK BAŞLANGICI ---
    // PlayerController bağımlılığı kaldırıldı.
    [RequireComponent(typeof(ProjectileShooter), typeof(PlayerStats))]
    // --- DEĞİŞİKLİK SONU ---
    public class AutoAttack : MonoBehaviour
    {
        [Header("Optimization")]
        [Tooltip("Saniyede kaç kez hedef aranacağını belirler. Performans için önemlidir.")]
        [SerializeField] private float targetSearchFrequency = 4f;

        // Gerekli bileşenlere referanslar
        private ProjectileShooter projectileShooter;
        private Transform currentTarget;
        private PlayerStats playerStats;
        
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // 'PlayerController' referansı, 'IAttackStateProvider' arayüzü ile değiştirildi.
        private IAttackStateProvider attackStateProvider;
        // --- DEĞİŞİKLİK SONU ---

        // Zamanlayıcılar
        private float nextFireTime;
        private float nextTargetSearchTime;
        
        // Burst atışı kontrol bayrağı
        private bool isFiringBurst = false; 

        private void Awake()
        {
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            // 'GetComponent<PlayerController>()' yerine 'GetComponent<IAttackStateProvider>()' al.
            // Bu, component'in 'PlayerController' da 'TowerAttackState' de olabileceği anlamına gelir.
            attackStateProvider = GetComponent<IAttackStateProvider>();
            // --- DEĞİŞİKLİK SONU ---
            
            projectileShooter = GetComponent<ProjectileShooter>();
            playerStats = GetComponent<PlayerStats>();
            
            // Güvenlik kontrolü
            if (attackStateProvider == null)
            {
                Debug.LogError("AutoAttack: Bu objede 'IAttackStateProvider' (PlayerController veya TowerAttackState gibi) " +
                               "arayüzünü uygulayan bir component bulunamadı! AutoAttack çalışmayacak.");
            }
        }

        private void Update()
        {
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            // Güvenlik kontrolü
            if (attackStateProvider == null) return;
            
            // Arayüze "Saldırabilir miyim?" diye sor.
            // PlayerController: Hareket durumuna göre 'true'/'false' döner.
            // TowerAttackState: Her zaman 'true' döner.
            if (!attackStateProvider.CanAttack())
            {
                currentTarget = null; // Hedefi unut
                return; // Saldırı yapma
            }
            // --- DEĞİŞİKLİK SONU ---
            
            // 1. Hedef bulma zamanı geldiyse yeni hedef ara.
            if (Time.time > nextTargetSearchTime)
            {
                FindNearestEnemy();
                nextTargetSearchTime = Time.time + (1f / targetSearchFrequency);
            }
            
            // 2. Saldırı (Burst veya Tekli)
            if (currentTarget != null && Time.time > nextFireTime && !isFiringBurst)
            {
                float currentAttackCooldown = 1f / playerStats.CurrentAttackSpeed;
                nextFireTime = Time.time + currentAttackCooldown;
                
                int projectilesToFire = playerStats.CurrentProjectilesPerShot;

                if (projectilesToFire <= 1)
                {
                    // Tekli atış
                    projectileShooter.FireAtPoint(currentTarget.position);
                }
                else
                {
                    // Burst atış
                    float burstDelay = playerStats.CurrentBurstFireDelay;
                    StartCoroutine(PerformBurstFire(projectilesToFire, burstDelay));
                }
            }
        }

        private IEnumerator PerformBurstFire(int count, float delay)
        {
            isFiringBurst = true; 
            for (int i = 0; i < count; i++)
            {
                if (currentTarget == null)
                {
                    break; 
                }
                projectileShooter.FireAtPoint(currentTarget.position);
                
                if (i < count - 1)
                {
                    yield return new WaitForSeconds(delay);
                }
            }
            isFiringBurst = false; 
        }

        private void FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float closestDistance = Mathf.Infinity;
            GameObject nearestEnemy = null;
            
            float currentAttackRange = playerStats.CurrentAttackRange;

            foreach (GameObject enemy in enemies)
            {
                if (!enemy.activeInHierarchy) continue;
                
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            if (nearestEnemy != null && closestDistance <= currentAttackRange)
            {
                currentTarget = nearestEnemy.transform;
            }
            else
            {
                currentTarget = null;
            }
        }
    }
}