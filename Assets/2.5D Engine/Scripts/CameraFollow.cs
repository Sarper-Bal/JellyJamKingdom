/*
 * CAMERA FOLLOW (YÖNETİCİ MODELİ)
 * * DEĞİŞİKLİKLER (v1.7 - Serbest Dolaşım Sınırları):
 * - 'FreeMove Settings' altına 'freeMoveMinBounds' ve 'freeMoveMaxBounds'
 * (Vector2) eklendi. (X ve Z eksenleri için).
 * - 'ApplyModeChange()' metodu, 'FreeMove' moduna geçerken ayarlanan
 * 'freeMoveStartPosition' değerini bu sınırlara 'Clamp' (sınırlama)
 * yapacak şekilde güncellendi.
 * - 'HandleFreeMove()' metodu, joystick ile hareket ettirilen pozisyonu
 * 'transform.position'a atamadan önce 'Mathf.Clamp()' kullanarak
 * bu sınırlara kısıtlayacak şekilde güncellendi.
 * - Bu yapı, Update() içinde GC (çöp) üretmez ve son derece optimizedir.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }
        
        public enum CameraMode
        {
            FollowTarget,
            Independent,
            FreeMove 
        }

        [Header("Runtime Settings")]
        [Tooltip("Kameranın o anki modu.")]
        [SerializeField] public CameraMode CurrentMode = CameraMode.FollowTarget;
        
        [Header("Target Settings")]
        [Tooltip("Kameranın 'FollowTarget' modunda takip edeceği hedef (Genellikle Oyuncu).")]
        [SerializeField] private Transform target;
        
        [Tooltip("Takip ederken hedeften ne kadar uzakta duracağı (pozisyonel fark).")]
        [SerializeField] private Vector3 offset;

        [Header("Movement Settings")]
        [Tooltip("'FollowTarget' modundaki takip yumuşaklığı (Lerp hızı).")]
        [SerializeField] private float followSpeed = 8f;
        
        [Header("Free Move Settings (Strateji Modu)")]
        [Tooltip("Oyuncunun 'FloatingJoystick' verisini okumak için 'PlayerInputHandler' referansı.")]
        [SerializeField] private PlayerInputHandler playerInputHandler;
        
        [Tooltip("'FreeMove' modundaki kameranın hareket hızı.")]
        [SerializeField] private float freeMoveSpeed = 10f;
        
        [Space]
        [Tooltip("'FreeMove' moduna geçerken kameranın anında ışınlanacağı DÜNYA POZİSYONU.")]
        [SerializeField] private Vector3 freeMoveStartPosition = new Vector3(0, 15, -10);

        [Tooltip("'FreeMove' moduna geçerken kameranın anında alacağı DÜNYA ROTASYONU (Euler Açıları).")]
        [SerializeField] private Vector3 freeMoveStartRotation = new Vector3(45, 0, 0);
        
        // --- DEĞİŞİKLİK BAŞLANGICI (v1.7 - Sınır Değişkenleri) ---
        [Space]
        [Header("Free Move Boundaries (XZ Plane)")]
        [Tooltip("Serbest dolaşım alanının minimum (Sol-Alt) XZ koordinatları. (Vector2'nin Y'si, Z ekseni içindir)")]
        [SerializeField] private Vector2 freeMoveMinBounds = new Vector2(-50, -50);

        [Tooltip("Serbest dolaşım alanının maksimum (Sağ-Üst) XZ koordinatları. (Vector2'nin Y'si, Z ekseni içindir)")]
        [SerializeField] private Vector2 freeMoveMaxBounds = new Vector2(50, 50);
        // --- DEĞİŞİKLİK SONU ---
        
        // 'FollowTarget' modu için varsayılan rotasyon
        private Quaternion followTargetRotation;
        // Bir önceki frame'deki modu izlemek için
        private CameraMode previousMode;
        
        
        private void Awake()
        {
            // Singleton kurulumu
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Kameranın varsayılan rotasyonunu kaydet
            followTargetRotation = transform.rotation;
            
            // Input Handler için güvenlik kontrolü
            if (playerInputHandler == null)
            {
                playerInputHandler = FindObjectOfType<PlayerInputHandler>();
                if (playerInputHandler == null)
                {
                    Debug.LogWarning("CameraFollow: 'FreeMove' modu için 'PlayerInputHandler' referansı " +
                                     "bulunamadı. FreeMove modu çalışmayabilir.");
                }
            }
            
            // 'previousMode'u mevcut mod ile başlat
            previousMode = CurrentMode;
            // Kameranın oyuna başlarken doğru pozisyon/rotasyona
            // anında (snap) geçmesini garantilemek için geçiş fonksiyonunu çağır.
            ApplyModeChange(CurrentMode, (CameraMode)(-1));
        }
        
        private void Update()
        {
            // 1. Mod Değişikliğini Algıla
            if (CurrentMode != previousMode)
            {
                // Mod değiştiyse, geçiş mantığını (snap) uygula
                ApplyModeChange(CurrentMode, previousMode);
                previousMode = CurrentMode;
            }

            // 2. Mevcut Modu Uygula (Her Frame)
            switch (CurrentMode)
            {
                case CameraMode.FollowTarget:
                    HandleFollowTarget();
                    break;
                case CameraMode.FreeMove:
                    HandleFreeMove();
                    break;
                case CameraMode.Independent:
                    break;
            }
        }
        
        /// <summary>
        /// Mod değişikliği algılandığında *ANINDA* (snap) yapılması gereken
        /// pozisyon/rotasyon ayarlarını yapar.
        /// </summary>
        private void ApplyModeChange(CameraMode newMode, CameraMode oldMode)
        {
            // 'FreeMove' moduna GİRİYORSAK:
            if (newMode == CameraMode.FreeMove)
            {
                // --- DEĞİŞİKLİK BAŞLANGICI (v1.7 - Başlangıç Sınır Kontrolü) ---
                
                // Inspector'dan ayarlanan başlangıç pozisyonunu al
                Vector3 startPos = freeMoveStartPosition;
                
                // Başlangıç pozisyonunun sınırlar içinde olduğundan emin ol.
                // Bu, Inspector'a yanlış değer girilmesini engeller.
                startPos.x = Mathf.Clamp(startPos.x, freeMoveMinBounds.x, freeMoveMaxBounds.x);
                // (Vector2'nin 'y' alanı, bizim dünyamızın 'z' eksenidir)
                startPos.z = Mathf.Clamp(startPos.z, freeMoveMinBounds.y, freeMoveMaxBounds.y);
                // Not: 'startPos.y' (yükseklik) kasten kelepçelenmez.

                // Kamerayı anında (snap) bu (gerekirse düzeltilmiş)
                // stratejik pozisyona ve rotasyona ayarla.
                transform.position = startPos;
                transform.rotation = Quaternion.Euler(freeMoveStartRotation);
                // --- DEĞİŞİKLİK SONU ---
            }
            
            // 'FollowTarget' moduna GİRİYORSAK:
            else if (newMode == CameraMode.FollowTarget)
            {
                // Rotasyonu anında (snap) varsayılana döndür.
                transform.rotation = followTargetRotation;
            }
        }

        /// <summary>
        /// 'FollowTarget' modunun her frame çalışan mantığı.
        /// </summary>
        private void HandleFollowTarget()
        {
            if (target != null)
            {
                transform.position = Vector3.Lerp(
                    transform.position, 
                    target.position + offset, 
                    Time.deltaTime * followSpeed
                );
            }
            transform.rotation = followTargetRotation;
        }

        // --- DEĞİŞİKLİK BAŞLANGICI (v1.7 - Hareket Sınır Kontrolü) ---
        /// <summary>
        /// 'FreeMove' modunun her frame çalışan mantığı.
        /// Kamerayı joystick ile sınırlar (bounds) içinde kaydırır.
        /// </summary>
        private void HandleFreeMove()
        {
            if (playerInputHandler == null) return; 

            Vector2 input = playerInputHandler.MoveInput;
            if (input == Vector2.zero) return;
            
            // 1. Hareket vektörünü hesapla (GC üretmez)
            Vector3 movement = new Vector3(input.x, 0f, input.y) * (freeMoveSpeed * Time.deltaTime);
            
            // 2. Yeni *hedef* pozisyonu hesapla
            Vector3 newPosition = transform.position + movement;

            // 3. Yeni pozisyonu X ve Z eksenlerinde Sınırla (Clamp)
            // (Bu, 'Mathf.Clamp' kullandığı için çok performanslıdır ve GC üretmez)
            newPosition.x = Mathf.Clamp(newPosition.x, freeMoveMinBounds.x, freeMoveMaxBounds.x);
            // (Vector2'nin 'y' alanı, bizim dünyamızın 'z' eksenidir)
            newPosition.z = Mathf.Clamp(newPosition.z, freeMoveMinBounds.y, freeMoveMaxBounds.y);
            // Not: newPosition.y (yükseklik) değişmez, çünkü 'movement.y' zaten sıfırdır.
            
            // 4. Sınırlanmış pozisyonu kameraya ata
            transform.position = newPosition;
        }
        // --- DEĞİŞİKLİK SONU ---
        
        
        // --- PUBLIC API (Dışarıdan Komutlar) ---
        
        public void SetMode(CameraMode newMode)
        {
            CurrentMode = newMode;
        }
        
        public void SetIndependent()
        {
            SetMode(CameraMode.Independent);
        }

        public void FollowTarget()
        {
            SetMode(CameraMode.FollowTarget);
        }
        
        public void SetFreeMove()
        {
            SetMode(CameraMode.FreeMove);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}