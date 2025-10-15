using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // YENİ: IPooledObject arayüzünü uyguluyoruz.
    public class Projectile : MonoBehaviour, IPooledObject
    {
        public float speed = 10f;
        private Vector3 target;

        [Range(.2f, 3f)]
        public float radius = 1f;
        public LayerMask whatIsEnemy;
        public LayerMask whatIsPlant;

        // YENİ: IPooledObject'ten gelen özellikler.
        public string PoolTag { get; set; }
        public void OnObjectSpawn()
        {
            // Mermi her spawn olduğunda yapılacak bir şey varsa buraya yazılır.
            // Şimdilik boş bırakabiliriz.
        }

        public void SetTarget(Vector3 point)
        {
            target = point;
            transform.LookAt(new Vector3(point.x, transform.position.y, point.z));
        }

        void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                Explode();
            }
        }

        void Explode()
        {
            ObjectPooler.Instance.SpawnFromPool("explosion", transform.position, Quaternion.identity);

            Collider[] enemyColliders = Physics.OverlapSphere(transform.position, radius, whatIsEnemy);
            if (enemyColliders != null)
            {
                foreach (Collider C in enemyColliders)
                {
                    C.GetComponent<HealthSystem>().Die();
                }
            }

            Collider[] plantationColliders = Physics.OverlapSphere(transform.position, radius, whatIsPlant);
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