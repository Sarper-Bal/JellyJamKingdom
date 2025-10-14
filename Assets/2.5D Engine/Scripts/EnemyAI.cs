using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // Controls enemy behavior: follows player and handles collision.
    public class EnemyAI : MonoBehaviour
    {
        private Transform target; // Reference to the player
        [SerializeField] private float speed; // Movement speed
        [SerializeField] private int damageAmount; // Damage dealt to player
        [SerializeField] private SpriteRenderer spriteRenderer; // For flipping sprite

        // Called on start
        public void Start()
        {
            Invoke("AssignPlayer", 1f); // Delay to ensure player exists
        }

        // Finds and assigns the player as target
        public void AssignPlayer()
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }

        // Called every frame
        public void Update()
        {
            if (target)
            {
                // Flip sprite based on direction to player
                if (target.position.x > transform.position.x)
                    spriteRenderer.flipX = false;
                else
                    spriteRenderer.flipX = true;

                // Move towards player
                transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * speed);
            }
        }

        // Handles collision with player
        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                // Damage player and kill self
                collision.collider.GetComponent<HealthSystem>().Damage(damageAmount);
                GetComponent<HealthSystem>().Die();
            }
        }
    }
}