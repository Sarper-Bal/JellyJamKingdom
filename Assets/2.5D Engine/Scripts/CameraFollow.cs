/*
 * CAMERA FOLLOW - v2.0 (Strategy Pan Modu Eklendi)
 * YENİLİKLER:
 * - 'StrategyPan' modu eklendi (Mobil uyumlu harita kaydırma).
 * - New Input System entegrasyonu (Pointer.current) ile dokunmatik kontrol.
 * - 'Ground Plane' matematiği ile optimize edilmiş pürüzsüz kaydırma.
 */

using UnityEngine;
using UnityEngine.InputSystem; // Yeni Input Sistemi için şart
using UnityEngine.EventSystems; // UI engellemesi için

namespace IndianOceanAssets.Engine2_5D
{
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }
        
        public enum CameraMode
        {
            FollowTarget,
            Independent,
            FreeMove,
            StrategyPan // <-- YENİ MOD
        }

        [Header("Runtime Settings")]
        [Tooltip("Kameranın o anki modu.")]
        [SerializeField] public CameraMode CurrentMode = CameraMode.FollowTarget;
        
        [Header("Target Settings")]
        [Tooltip("FollowTarget modunda takip edilecek hedef.")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset;

        [Header("Movement Settings")]
        [SerializeField] private float followSpeed = 8f;
        
        [Header("Free Move Settings (Joystick)")]
        [SerializeField] private PlayerInputHandler playerInputHandler;
        [SerializeField] private float freeMoveSpeed = 10f;
        
        [Header("Strategy Pan Settings (Touch)")]
        [Tooltip("StrategyPan moduna geçerken kameranın alacağı pozisyon.")]
        [SerializeField] private Vector3 strategyStartPosition = new Vector3(0, 15, -10);
        [Tooltip("StrategyPan moduna geçerken kameranın alacağı açı.")]
        [SerializeField] private Vector3 strategyStartRotation = new Vector3(60, 0, 0);
        
        // Hareket hesaplaması için matematiksel zemin (Y = 0 düzlemi)
        private Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        private Vector3 dragOrigin; // Sürükleme başlangıç noktası
        private bool isDragging = false;

        [Space]
        [Header("Map Boundaries (Ortak Sınırlar)")]
        [SerializeField] private Vector2 mapMinBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 mapMaxBounds = new Vector2(50, 50);
        
        // State Tracking
        private Quaternion followTargetRotation;
        private CameraMode previousMode;
        private Camera mainCamera;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null) mainCamera = Camera.main;

            followTargetRotation = transform.rotation;
            
            if (playerInputHandler == null) playerInputHandler = FindObjectOfType<PlayerInputHandler>();
            
            previousMode = CurrentMode;
            ApplyModeChange(CurrentMode, (CameraMode)(-1));
        }
        
        private void Update()
        {
            // 1. Mod Değişikliği Algıla
            if (CurrentMode != previousMode)
            {
                ApplyModeChange(CurrentMode, previousMode);
                previousMode = CurrentMode;
            }

            // 2. Mod Mantığını Çalıştır
            switch (CurrentMode)
            {
                case CameraMode.FollowTarget:
                    HandleFollowTarget();
                    break;
                case CameraMode.FreeMove:
                    HandleFreeMove();
                    break;
                case CameraMode.StrategyPan: // <-- YENİ
                    HandleStrategyPan();
                    break;
                case CameraMode.Independent:
                    break;
            }
        }
        
        private void ApplyModeChange(CameraMode newMode, CameraMode oldMode)
        {
            if (newMode == CameraMode.FreeMove)
            {
                // FreeMove başlangıç ayarları (Eski kod korundu, sadece bounds ismi güncellendi)
                Vector3 startPos = strategyStartPosition; // Veya freeMoveStartPos (senin tercihine göre)
                transform.position = ClampPosition(startPos);
                transform.rotation = Quaternion.Euler(strategyStartRotation);
            }
            else if (newMode == CameraMode.StrategyPan)
            {
                // StrategyPan için ideal açıya geç
                Vector3 startPos = strategyStartPosition;
                // Mevcut X/Z konumunu koruyup sadece yüksekliği mi ayarlayalım? 
                // Hayır, belirlenen stratejik noktaya gitsin.
                transform.position = ClampPosition(startPos);
                transform.rotation = Quaternion.Euler(strategyStartRotation);
                
                isDragging = false; // Reset
            }
            else if (newMode == CameraMode.FollowTarget)
            {
                transform.rotation = followTargetRotation;
            }
        }

        // --- YENİ: STRATEJİ MODU (TOUCH PAN) ---
        private void HandleStrategyPan()
        {
            // Input System kontrolü (Pointer var mı?)
            if (Pointer.current == null) return;

            // 1. Dokunma Başladı mı? (Press Down)
            if (Pointer.current.press.wasPressedThisFrame)
            {
                // UI üzerine mi tıkladı? (Öyleyse haritayı kaydırma)
                if (IsPointerOverUI()) return;

                Vector2 screenPos = Pointer.current.position.ReadValue();
                
                // Zemine ışın at ve tuttuğumuz noktayı kaydet
                Ray ray = mainCamera.ScreenPointToRay(screenPos);
                float entry;
                if (groundPlane.Raycast(ray, out entry))
                {
                    dragOrigin = ray.GetPoint(entry);
                    isDragging = true;
                }
            }

            // 2. Sürükleme Devam Ediyor mu? (Is Pressed)
            if (Pointer.current.press.isPressed && isDragging)
            {
                Vector2 screenPos = Pointer.current.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(screenPos);
                float entry;
                
                if (groundPlane.Raycast(ray, out entry))
                {
                    Vector3 currentHitPoint = ray.GetPoint(entry);
                    
                    // Matematik: (Tuttuğumuz Yer - Şu Anki Yer) farkı kadar kamerayı kaydır.
                    // Bu sayede harita parmağımızın altında sabit kalır (Google Maps hissi).
                    Vector3 difference = dragOrigin - currentHitPoint;
                    
                    // Yeni pozisyonu hesapla ve sınırla
                    Vector3 newPos = transform.position + difference;
                    transform.position = ClampPosition(newPos);
                }
            }

            // 3. Bıraktı mı?
            if (Pointer.current.press.wasReleasedThisFrame)
            {
                isDragging = false;
            }
        }

        // --- YARDIMCI METOTLAR ---

        // Pozisyonu sınırlayan yardımcı metot (Kod tekrarını önler)
        private Vector3 ClampPosition(Vector3 targetPos)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, mapMinBounds.x, mapMaxBounds.x);
            targetPos.z = Mathf.Clamp(targetPos.z, mapMinBounds.y, mapMaxBounds.y);
            return targetPos;
        }

        // UI Tıklamasını Kontrol Et
        private bool IsPointerOverUI()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
            // Mobil ID kontrolü gerekirse buraya eklenebilir ama Pointer.current genelde yeterlidir.
            return false;
        }

        // --- ESKİ MODLAR (KORUNDU) ---

        private void HandleFollowTarget()
        {
            if (target != null)
            {
                transform.position = Vector3.Lerp(transform.position, target.position + offset, Time.deltaTime * followSpeed);
            }
            transform.rotation = followTargetRotation;
        }

        private void HandleFreeMove()
        {
            if (playerInputHandler == null) return; 
            Vector2 input = playerInputHandler.MoveInput;
            if (input == Vector2.zero) return;
            
            Vector3 movement = new Vector3(input.x, 0f, input.y) * (freeMoveSpeed * Time.deltaTime);
            transform.position = ClampPosition(transform.position + movement);
        }
        
        // --- PUBLIC API ---
        
        public void SetStrategyMode() => CurrentMode = CameraMode.StrategyPan;
        public void SetFollowMode() => CurrentMode = CameraMode.FollowTarget;
        public void SetTarget(Transform newTarget) => target = newTarget;
    }
}