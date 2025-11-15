/*
 * CAMERA FOLLOW (YÖNETİCİ MODELİ - v1.6 Teyit Edilmiş)
 * * PERFORMANS ANALİZİ:
 * - Update() içindeki 'CurrentMode != previousMode' kontrolü,
 * bir 'int' karşılaştırmasıdır ve maliyeti ihmal edilebilir.
 * - 'switch' bloğu yüksek performanslıdır.
 * - 'HandleFreeMove()' ve 'HandleFollowTarget()' metotları, temel matematik
 * işlemleri ve 'struct' (Vector, Quaternion) atamaları kullanır.
 * - Bu script, Update() döngüsünde HİÇBİRYERDE 'heap' üzerinde yeni nesne
 * (class, string, vb.) oluşturmaz.
 * - SONUÇ: Bu yapı SIFIR (ZERO) çöp (Garbage) üretir ve mobil
 * cihazlar için son derece optimize bir çözümdür.
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
            
            // Input Handler için güvenlik kontrolü (eğer Inspector'dan atanmadıysa)
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
            ApplyModeChange(CurrentMode, (CameraMode)(-1)); // 'oldMode'u geçersiz yaparak ilk ayarı zorla
        }
        
        private void Update()
        {
            // 1. Mod Değişikliğini Algıla (Çok düşük maliyetli 'int' karşılaştırması)
            if (CurrentMode != previousMode)
            {
                // Mod değiştiyse, geçiş mantığını (snap) uygula (Sadece 1 frame çalışır)
                ApplyModeChange(CurrentMode, previousMode);
                previousMode = CurrentMode; // Modu güncelle
            }

            // 2. Mevcut Modu Uygula (Her Frame - Düşük maliyetli 'switch')
            switch (CurrentMode)
            {
                case CameraMode.FollowTarget:
                    HandleFollowTarget();
                    break;
                case CameraMode.FreeMove:
                    HandleFreeMove();
                    break;
                case CameraMode.Independent:
                    // Hiçbir şey yapma (En optimize durum)
                    break;
            }
        }
        
        /// <summary>
        /// Mod değişikliği algılandığında *ANINDA* (snap) yapılması gereken
        /// pozisyon/rotasyon ayarlarını yapar.
        /// Bu fonksiyon sadece mod değişim anında 1 kez çalışır.
        /// </summary>
        private void ApplyModeChange(CameraMode newMode, CameraMode oldMode)
        {
            // 'FreeMove' moduna GİRİYORSAK:
            if (newMode == CameraMode.FreeMove)
            {
                // Kamerayı anında (snap) stratejik pozisyona ve rotasyona ayarla.
                transform.position = freeMoveStartPosition;
                transform.rotation = Quaternion.Euler(freeMoveStartRotation);
            }
            
            // 'FollowTarget' moduna GİRİYORSAK:
            else if (newMode == CameraMode.FollowTarget)
            {
                // Rotasyonu anında (snap) varsayılana döndür.
                transform.rotation = followTargetRotation;
                // Not: Pozisyon, 'HandleFollowTarget' içinde hedefe yumuşakça (Lerp) zaten kayacak.
            }
        }

        /// <summary>
        /// 'FollowTarget' modunun her frame çalışan mantığı. (Düşük maliyet)
        /// </summary>
        private void HandleFollowTarget()
        {
            if (target != null)
            {
                // Pozisyonu yumuşakça (Lerp) takip et (Optimize, GC üretmez)
                transform.position = Vector3.Lerp(
                    transform.position, 
                    target.position + offset, 
                    Time.deltaTime * followSpeed
                );
            }
            
            // Rotasyonu ANINDA varsayılana ayarla (Optimize, GC üretmez)
            transform.rotation = followTargetRotation;
        }

        /// <summary>
        /// 'FreeMove' modunun her frame çalışan mantığı. (Düşük maliyet)
        /// </summary>
        private void HandleFreeMove()
        {
            if (playerInputHandler == null) return; 

            Vector2 input = playerInputHandler.MoveInput;
            if (input == Vector2.zero) return;
            
            // Temel matematik işlemleri (Optimize, GC üretmez)
            Vector3 movement = new Vector3(input.x, 0f, input.y);
            transform.position += movement * freeMoveSpeed * Time.deltaTime;
        }
        
        
        // --- PUBLIC API (Dışarıdan Komutlar) ---
        
        /// <summary>
        /// Kameranın modunu değiştirir (Dışarıdan komut için).
        /// </summary>
        public void SetMode(CameraMode newMode)
        {
            // Sadece değişkeni ayarla. 'Update' döngüsü değişikliği algılayacaktır.
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