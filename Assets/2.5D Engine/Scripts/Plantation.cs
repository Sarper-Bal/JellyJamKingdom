using UnityEngine;
namespace IndianOceanAssets.Engine2_5D
{
    // Handles plantation (plant) interactions and cutting logic
    public class Plantation : MonoBehaviour
    {
        public GameObject plantationCutParticles; // Particle effect prefab for cutting
        private Animator animator; // Animator for plant animations

        // Triggered when another collider enters this object's trigger collider
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // If animator already assigned, trigger a random animation
                if (animator)
                {
                    animator.SetTrigger("Trigger" + Random.Range(1, 4));
                }
                else
                {
                    // Assign animator and trigger a random animation
                    animator = GetComponent<Animator>();
                    animator.SetTrigger("Trigger" + Random.Range(1, 4));
                }
            }
        }

        // Called to cut the plant, spawn particles, and destroy the object
        public void Cut()
        {
            // Instantiate cut particles and set their sprite to match the plant
            Instantiate(plantationCutParticles, transform.position + new Vector3(0f, 0.5f, 0f), Quaternion.identity)
                .GetComponent<ParticleSystem>().textureSheetAnimation.SetSprite(0, GetComponent<SpriteRenderer>().sprite);
            Destroy(gameObject); // Remove plant from scene
        }
    }
}