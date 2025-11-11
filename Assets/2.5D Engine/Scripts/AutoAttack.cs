using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // Bu script'in çalışması için diğer Player bileşenlerine ihtiyaç duyduğunu belirtiyoruz.
    // PlayerStats component'ini zorunlu kılıyoruz.
    [RequireComponent(typeof(PlayerController), typeof(ProjectileShooter), typeof(PlayerStats))]
    public class AutoAttack : MonoBehaviour
    {
        // --- DEĞİŞİKLİK BAŞLANGICI (AttackRange) ---
        // 'attackRange' değişkeni buradan kaldırıldı. Artık PlayerStats'tan dinamik olarak okunacak.
        // [SerializeField] private float attackRange = 10f; // ESKİ
        // --- DEĞİŞİKLİK SONU ---

        // 'fireRate' değişkeni de bir önceki adımda kaldırılmıştı.
        
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
            
            // PlayerStats component'ini alıyoruz
            playerStats = GetComponent<PlayerStats>();
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
                
                // Bir sonraki ateş etme zamanını PlayerStats'tan gelen anlık saldırı hızına göre ayarla.
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

            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            
            // --- DEĞİŞİKLİK BAŞLANGICI (AttackRange) ---
            // Eğer en yakın düşman menzil içindeyse, onu hedefimiz yap.
            // Artık sabit 'attackRange' değişkeni yerine, PlayerStats'tan gelen anlık
            // 'CurrentAttackRange' değerini kullanıyoruz.
            if (nearestEnemy != null && closestDistance <= playerStats.CurrentAttackRange)
            // --- DEĞİŞİKLİK SONU ---
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