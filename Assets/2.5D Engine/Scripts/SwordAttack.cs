using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    public class SwordAttack : MonoBehaviour
    {
        // Damage dealt by the sword attack
        [Range(10, 40)]
        [SerializeField] private int damageAmount;

        // Radius around the attack position where enemies can be hit
        [Range(0.1f, 5.0f)]
        [SerializeField] private float slashRadius;

        // Particle effect played during slash
        [SerializeField] private ParticleSystem swordSlashParticle;

        // Transform that defines the attack position/origin
        [SerializeField] private Transform attackPos;

        // Sound effect played on slash
        [SerializeField] private AudioSource swordSlashSFX;

        // Layers to detect enemies and plantation objects
        [SerializeField] private LayerMask whatIsEnemy;
        [SerializeField] private LayerMask whatIsPlantation;

        // Key assigned to trigger the attack
        [SerializeField] private KeyCode attackKeyCode;

        private void Update()
        {
            // Flip player sprite based on horizontal input direction
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (input.x > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (input.x < 0)
                transform.localScale = new Vector3(-1, 1, 1);

            // Trigger slash when attack key is pressed
            if (Input.GetKeyDown(attackKeyCode))
            {
                Slash();
            }
        }

        // Executes the sword slash logic
        public void Slash()
        {
            // Play particle and sound effects
            swordSlashParticle.Play();
            swordSlashSFX.Play();

            // Detect and damage enemies within the slash radius
            Collider[] enemies = Physics.OverlapSphere(attackPos.position, slashRadius, whatIsEnemy);
            if (enemies != null)
            {
                foreach (var E in enemies)
                {
                    E.GetComponent<HealthSystem>().Damage(damageAmount);
                }
            }

            // Detect and cut plantation objects (like bushes) within radius
            Collider[] plantation = Physics.OverlapSphere(attackPos.position, slashRadius, whatIsPlantation);
            if (plantation != null)
            {
                foreach (var P in plantation)
                {
                    P.GetComponent<Plantation>().Cut();
                }
            }
        }

        // Draws the slash radius in the editor for visualization
        private void OnDrawGizmosSelected()
        {
            if (attackPos != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPos.position, slashRadius);
            }
        }
    }
}
