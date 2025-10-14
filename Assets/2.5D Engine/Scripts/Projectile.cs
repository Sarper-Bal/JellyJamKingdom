using UnityEngine;
namespace IndianOceanAssets.Engine2_5D
{
    // Handles projectile movement, explosion, and effects
    public class Projectile : MonoBehaviour
    {
        public float speed = 10f; // Projectile speed
        public GameObject explosionEffect; // Effect prefab
        private Vector3 target; // Target position

        [Range(.2f, 3f)]
        public float radius = 1f; // Explosion radius
        public LayerMask whatIsEnemy; // Layer for enemies
        public LayerMask whatIsPlant; // Layer for plants

        // Sets the target point and rotates projectile to face it
        public void SetTarget(Vector3 point)
        {
            target = point;
            transform.LookAt(new Vector3(point.x, transform.position.y, point.z)); // Optional: face target
        }

        // Moves projectile towards target each frame
        void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            // Explode if close to target
            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                Explode();
            }
        }

        // Handles explosion logic and effects
        void Explode()
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

            // Damage enemies in radius
            Collider[] enemyColliders = Physics.OverlapSphere(transform.position, radius, whatIsEnemy);

            if (enemyColliders != null)
            {
                foreach (Collider C in enemyColliders)
                {
                    C.GetComponent<HealthSystem>().Die();
                }
            }

            // Affect plants in radius
            Collider[] plantationColliders = Physics.OverlapSphere(transform.position, radius, whatIsPlant);

            if (plantationColliders != null)
            {
                foreach (Collider C in plantationColliders)
                {
                    C.GetComponent<Plantation>().Cut();
                }
            }
            Destroy(gameObject); // Destroy projectile
        }
    }
}