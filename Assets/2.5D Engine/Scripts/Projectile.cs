/*
 * PROJECTILE (HASAR UYGULAYICI)
 * * DEĞİŞİKLİKLER:
 * - 'currentDamage' (int) adında yeni bir özel değişken eklendi.
 * - 'Initialize' metodu artık 'damage' parametresi alıyor ve 'currentDamage'i ayarlıyor.
 * - 'Explode' metodu güncellendi:
 * - 'C.GetComponent<HealthSystem>().Die()' komutu,
 * - 'C.GetComponent<HealthSystem>().Damage(currentDamage)' komutu ile değiştirildi.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // IPooledObject arayüzünü uyguluyoruz (Havuzlanabilir obje)
    public class Projectile : MonoBehaviour, IPooledObject
    {
        // Merminin bu "yaşam döngüsündeki" anlık stat'ları
        // Bu değerler ProjectileShooter tarafından 'Initialize' ile atanacak.
        private float currentSpeed;
        private float currentRadius;
        
        // --- YENİ EKLENEN KISIM BAŞLANGICI ---
        private int currentDamage; // Merminin bu atıştaki hasar miktarı
        // --- YENİ EKLENEN KISIM SONU ---

        private Vector3 target;

        [Header("Collision Layers")]
        public LayerMask whatIsEnemy;
        public LayerMask whatIsPlant;

        // IPooledObject arayüzünden gelen özellikler
        public string PoolTag { get; set; }
        public void OnObjectSpawn()
        {
            // Mermi her spawn olduğunda yapılacak bir şey varsa buraya yazılır.
            // (Stat'lar 'Initialize' ile ayarlandığı için burası şimdilik boş)
        }

        // --- DEĞİŞİKLİK BAŞLANGICI ---
        /// <summary>
        /// Bu fonksiyon, mermiyi spawn eden 'ProjectileShooter' tarafından çağırılır.
        /// Mermiye bu yaşam döngüsünde hangi stat'ları kullanacağını söyler.
        /// </summary>
        /// <param name="speed">Anlık mermi hızı</param>
        /// <param name="radius">Anlık patlama yarıçapı</param>
        /// <param name="damage">Anlık mermi hasarı</param>
        public void Initialize(float speed, float radius, int damage) // YENİ: 'int damage' eklendi
        {
            this.currentSpeed = speed;
            this.currentRadius = radius;
            this.currentDamage = damage; // YENİ: Hasarı ayarla
        }
        // --- DEĞİŞİKLİK SONU ---

        /// <summary>
        /// Mermiye hedefini (gideceği yeri) atar.
        /// </summary>
        public void SetTarget(Vector3 point)
        {
            target = point;
            // Hedefe doğru dön (sadece Y ekseninde)
            transform.LookAt(new Vector3(point.x, transform.position.y, point.z));
        }

        void Update()
        {
            // 'currentSpeed' kullanarak hedefe doğru hareket et
            transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);

            // Hedefe yeterince yaklaştıysak patla
            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                Explode();
            }
        }

        /// <summary>
        /// Merminin patlama ve etki alanını (AOE) yönetir.
        /// </summary>
        void Explode()
        {
            // Patlama efektini havuzdan çağır
            ObjectPooler.Instance.SpawnFromPool("explosion", transform.position, Quaternion.identity);

            // --- DEĞİŞİKLİK BAŞLANGICI (Damage vs Die) ---
            // 'currentRadius' içindeki Düşmanları bul
            Collider[] enemyColliders = Physics.OverlapSphere(transform.position, currentRadius, whatIsEnemy);
            if (enemyColliders != null)
            {
                foreach (Collider C in enemyColliders)
                {
                    // Düşmanlara 'Die' (Öl) komutu VERMEK YERİNE,
                    // 'currentDamage' (anlık hasar) miktarında HASAR VER (Damage).
                    C.GetComponent<HealthSystem>().Damage(currentDamage);
                }
            }
            // --- DEĞİŞİKLİK SONU ---

            // 'currentRadius' içindeki Bitkileri bul
            Collider[] plantationColliders = Physics.OverlapSphere(transform.position, currentRadius, whatIsPlant);
            if (plantationColliders != null)
            {
                foreach (Collider C in plantationColliders)
                {
                    // Bitkiler hasar almaz, direkt kesilir (Bu mantık korunuyor)
                    C.GetComponent<Plantation>().Cut();
                }
            }

            // Kendini yok etmek yerine, kendi etiketiyle havuza geri dön.
            ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
        }
    }
}