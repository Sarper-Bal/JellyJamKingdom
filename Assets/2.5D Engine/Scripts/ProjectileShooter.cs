using UnityEngine;
namespace IndianOceanAssets.Engine2_5D
{
    // Handles shooting projectiles towards mouse click position
    public class ProjectileShooter : MonoBehaviour
    {
        [SerializeField] private GameObject projectilePrefab; // Prefab for the projectile to shoot
        [SerializeField] private KeyCode attackKeyCode; // Key to trigger shooting
        [SerializeField] private LayerMask groundLayer; // Layer to detect ground for aiming

        // Called once per frame
        void Update()
        {
            // Check if attack key is pressed
            if (Input.GetKeyDown(attackKeyCode)) // Left-click
            {
                // Create a ray from the camera to the mouse position
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                // Raycast to ground layer to find target point
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
                {
                    Vector3 targetPoint = hit.point;
                    // Instantiate projectile at shooter's position
                    GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                    // Set projectile's target point
                    projectile.GetComponent<Projectile>().SetTarget(targetPoint);

                    // Flip shooter sprite based on target direction
                    if (hit.point.x > transform.position.x)
                        transform.localScale = new Vector3(1, 1, 1);
                    else
                        transform.localScale = new Vector3(-1, 1, 1);
                }
            }
        }
    }
}