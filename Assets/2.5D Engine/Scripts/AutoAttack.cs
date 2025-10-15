using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // Bu script'in çalışması için diğer Player bileşenlerine ihtiyaç duyduğunu belirtiyoruz.
    [RequireComponent(typeof(PlayerController), typeof(ProjectileShooter))]
    public class AutoAttack : MonoBehaviour
    {
        [Header("Attack Settings")]
        [Tooltip("Ateş etme menzili")]
        [SerializeField] private float attackRange = 10f;

        [Tooltip("İki atış arasındaki saniye cinsinden bekleme süresi")]
        [SerializeField] private float fireRate = 0.5f;

        [Header("Optimization")]
        [Tooltip("Saniyede kaç kez hedef aranacağını belirler. Performans için önemlidir.")]
        [SerializeField] private float targetSearchFrequency = 4f;

        // Gerekli bileşenlere referanslar
        private PlayerController playerController;
        private ProjectileShooter projectileShooter;
        private Transform currentTarget;

        // Zamanlayıcılar
        private float nextFireTime;
        private float nextTargetSearchTime;

        private void Awake()
        {
            // Gerekli bileşenleri en başta alıyoruz.
            playerController = GetComponent<PlayerController>();
            projectileShooter = GetComponent<ProjectileShooter>();
        }

        private void Update()
        {
            // Eğer karakter hareket ediyorsa, hiçbir şey yapma ve hedefi unut.
            if (playerController.IsMoving)
            {
                currentTarget = null;
                return;
            }

            // Eğer karakter duruyorsa:
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
                // Bir sonraki ateş etme zamanını ayarla.
                nextFireTime = Time.time + fireRate;
            }
        }

        // En yakın düşmanı bulan optimize edilmiş fonksiyon.
        private void FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float closestDistance = Mathf.Infinity;
            GameObject nearestEnemy = null;

            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            // Eğer en yakın düşman menzil içindeyse, onu hedefimiz yap.
            if (nearestEnemy != null && closestDistance <= attackRange)
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