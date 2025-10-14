using UnityEngine;
using System.Collections;

namespace IndianOceanAssets.Engine2_5D
{
    // Ensures the GameObject has these essential components
    [RequireComponent(typeof(HealthSystem), typeof(SwordAttack), typeof(ProjectileShooter))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;         // Speed at which the player moves
        [SerializeField] private float rollForce = 8f;         // Force applied during roll
        [SerializeField] private float rollCooldown = 1f;      // Cooldown time between rolls

        private Rigidbody rb;                                  // Rigidbody reference for physics
        private Animator animator;                             // Animator reference for animations
        private Vector2 inputDirection;                        // Stores player input direction
        private float lastRollTime;                            // Timestamp of last roll
        private bool isRolling;                                // Is player currently rolling?

        [SerializeField] private KeyCode rollKeyCode;          // Key to trigger roll

        // Enum to switch between sword or projectile attack types
        [SerializeField] private enum AttackType
        {
            SwordSlash, ProjectileShoot
        }

        [SerializeField] private AttackType attackType;        // Current selected attack type

        private void Start()
        {
            Application.targetFrameRate = 60; // Set target frame rate

            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();

            // Enable only the selected attack script
            if (attackType == AttackType.SwordSlash)
                GetComponent<SwordAttack>().enabled = true;
            else
                GetComponent<ProjectileShooter>().enabled = true;
        }

        private void Update()
        {
            HandleInput(); // Read movement input

            // Animate movement only when not rolling
            if (!isRolling)
                AnimateMovement();

            // Trigger roll if key is pressed and cooldown passed
            if (Input.GetKeyDown(rollKeyCode) && Time.time > lastRollTime + rollCooldown)
                StartCoroutine(PerformRoll());
        }

        private void FixedUpdate()
        {
            // Move only if not rolling
            if (!isRolling)
                Move();
        }

        // Reads directional input from keyboard
        private void HandleInput()
        {
            inputDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        }

        // Applies movement to the Rigidbody based on input
        private void Move()
        {
            Vector3 movement = new Vector3(inputDirection.x, 0f, inputDirection.y) * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }

        // Sets the "Run" animation based on movement input
        private void AnimateMovement()
        {
            animator.SetBool("Run", inputDirection != Vector2.zero);
        }

        // Coroutine to perform roll movement and animation
        private IEnumerator PerformRoll()
        {
            isRolling = true;
            lastRollTime = Time.time;

            float timer = 0.2f;
            Vector3 rollDir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;

            // Move the player during the roll duration
            while (timer > 0f)
            {
                rb.MovePosition(rb.position + rollDir * rollForce * Time.fixedDeltaTime);
                timer -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            isRolling = false;
        }
    }
}
