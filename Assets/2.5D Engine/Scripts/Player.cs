using UnityEngine;
using System.Collections;
using DG.Tweening;

namespace IndianOceanAssets.Engine2_5D
{
    // HATA 1 DÜZELTMESİ: Her bileşen için ayrı bir RequireComponent satırı eklendi.
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(SwordAttack))]
    [RequireComponent(typeof(ProjectileShooter))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rollForce = 8f;
        [SerializeField] private float rollCooldown = 1f;

        private Rigidbody rb;
        private Animator animator;
        private PlayerInputHandler inputHandler;

        private Vector2 inputDirection;
        private float lastRollTime;
        private bool isRolling;
        public bool IsMoving { get; private set; }

        // Enum tanımı başlık olmadan, kendi başına durmalı.
        private enum AttackType
        {
            SwordSlash, ProjectileShoot
        }

        // HATA 2 DÜZELTMESİ: Header, ait olduğu değişkenin hemen üzerine taşındı.
        [Header("Attack Settings")]
        [SerializeField] private AttackType attackType;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            inputHandler = GetComponent<PlayerInputHandler>();
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            if (attackType == AttackType.SwordSlash)
                GetComponent<SwordAttack>().enabled = true;
            else
                GetComponent<ProjectileShooter>().enabled = true;
        }

        private void Update()
        {
            inputDirection = inputHandler.MoveInput;

            // YENİ EKLENEN SATIR: Hareket girdisi varsa IsMoving true, yoksa false olacak.
            IsMoving = inputDirection != Vector2.zero;

            if (!isRolling)
            {
                // AnimateMovement içindeki kontrolü değiştirdiğimiz için artık buna gerek yok.
                // animator.SetBool("Run", IsMoving); // Bu satırı silebiliriz veya yorum yapabiliriz.
                AnimateMovement();
                FlipCharacter();
            }
        }

        private void FixedUpdate()
        {
            if (!isRolling)
                Move();
        }

        private void Move()
        {
            Vector3 movement = new Vector3(inputDirection.x, 0f, inputDirection.y) * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }

        private void AnimateMovement()
        {
            //animator.SetBool("Run", inputDirection != Vector2.zero);
            animator.SetBool("Run", IsMoving);
        }

        private void FlipCharacter()
        {
            if (Mathf.Abs(inputDirection.x) > 0.1f)
            {
                float newScaleX = Mathf.Sign(inputDirection.x);
                transform.localScale = new Vector3(newScaleX, 1, 1);
            }
        }

        private IEnumerator PerformRoll()
        {
            isRolling = true;
            lastRollTime = Time.time;
            Vector3 rollDir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;

            if (rollDir == Vector3.zero)
            {
                rollDir = new Vector3(transform.localScale.x, 0, 0);
            }

            float rollDuration = 0.3f;
            rb.DOMove(rb.position + rollDir * rollForce, rollDuration).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(rollDuration);

            isRolling = false;
        }
    }
}
