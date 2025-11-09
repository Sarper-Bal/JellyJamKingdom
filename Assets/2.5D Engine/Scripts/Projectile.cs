using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // YENİ: IPooledObject arayüzünü uyguluyoruz.
    public class Projectile : MonoBehaviour, IPooledObject
    {
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // Merminin kendi stat değişkenlerini kaldırıyoruz.
        // public float speed = 10f; // ESKİ: Kaldırıldı
        // public float radius = 1f; // ESKİ: Kaldırıldı

        // YENİ: Merminin bu "yaşam döngüsündeki" hız ve yarıçap değerleri.
        // Bu değerler ProjectileShooter tarafından atanacak.
        private float currentSpeed;
        private float currentRadius;
        // --- DEĞİŞİKLİK SONU ---

        private Vector3 target;

        [Header("Collision Layers")]
        public LayerMask whatIsEnemy;
        public LayerMask whatIsPlant;

        // YENİ: IPooledObject'ten gelen özellikler.
        public string PoolTag { get; set; }
        public void OnObjectSpawn()
        {
            // Mermi her spawn olduğunda yapılacak bir şey varsa buraya yazılır.
            // Stat'lar 'Initialize' ile ayarlandığı için burası şimdilik boş.
        }

        // --- YENİ FONKSİYON ---
        // Bu fonksiyon, mermiyi spawn eden 'ProjectileShooter' tarafından çağırılır.
        // Mermiye bu yaşam döngüsünde hangi stat'ları kullanacağını söyler.
        public void Initialize(float speed, float radius)
        {
            this.currentSpeed = speed;
            this.currentRadius = radius;
        }

        public void SetTarget(Vector3 point)
        {
            target = point;
            // Hedefe doğru dön (sadece Y ekseninde)
            transform.LookAt(new Vector3(point.x, transform.position.y, point.z));
        }

        void Update()
        {
            // --- DEĞİŞİKLİK: 'speed' yerine 'currentSpeed' kullan ---
            transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);

            // Hedefe yeterince yaklaştıysak patla
            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                Explode();
            }
        }

        void Explode()
        {
            // Patlama efektini havuzdan çağır
            ObjectPooler.Instance.SpawnFromPool("explosion", transform.position, Quaternion.identity);

            // --- DEĞİŞİKLİK: 'radius' yerine 'currentRadius' kullan ---
            // 'currentRadius' içindeki Düşmanları bul
            Collider[] enemyColliders = Physics.OverlapSphere(transform.position, currentRadius, whatIsEnemy);
            if (enemyColliders != null)
            {
                foreach (Collider C in enemyColliders)
                {
                    // Düşmanlara 'Die' (Öl) komutu ver
                    C.GetComponent<HealthSystem>().Die();
                }
            }

            // --- DEĞİŞİKLİK: 'radius' yerine 'currentRadius' kullan ---
            // 'currentRadius' içindeki Bitkileri bul
            Collider[] plantationColliders = Physics.OverlapSphere(transform.position, currentRadius, whatIsPlant);
            if (plantationColliders != null)
            {
                foreach (Collider C in plantationColliders)
                {
                    C.GetComponent<Plantation>().Cut();
                }
            }

            // YENİ: Kendini yok etmek yerine, kendi etiketiyle havuza geri dön.
            ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
        }
    }
}