/*
 * CAMERA FOLLOW (YÖNETİCİ MODELİ)
 * * DEĞİŞİKLİKLER (v1.4 - Joystick Kontrollü Serbest Dolaşım):
 * - 'CameraMode' enum'una 'FreeMove' modu eklendi.
 * - 'playerInputHandler' (PlayerInputHandler) referansı eklendi.
 * (Bu, "mevcut oyuncu joystick'ini" okumamızı sağlar)
 * - 'freeMoveSpeed' (float) değişkeni eklendi.
 * - 'Awake()' metoduna 'playerInputHandler' için bir 'FindObjectOfType' güvenlik
 * kontrolü eklendi.
 * - 'Update()' metodu, 3 modu da yönetebilmek için 'switch' yapısı kullanacak
 * şekilde yeniden düzenlendi.
 * - 'FreeMove' modunu yönetecek 'HandleFreeMove()' metodu eklendi.
 * - 'SetFreeMove()' adında yeni bir public metot eklendi.
 * - 'CurrentMode' değişkeni zaten public olduğu için diğer script'ler
 * (PlayerController gibi) tarafından okunabilir durumdadır.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Yeni Enum Değeri) ---
        public enum CameraMode
        {
            /// <summary>
            /// 'target' olarak atanan objeyi 'offset' ile takip eder.
            /// </summary>
            FollowTarget,
            
            /// <summary>
            /// Kamera tüm takip etme davranışlarını durdurur.
            /// </summary>
            Independent,
            
            /// <summary>
            /// Kamera, 'playerInputHandler'dan gelen joystick verisi ile serbestçe hareket eder.
            /// </summary>
            FreeMove 
        }
        // --- DEĞİŞİKLİK SONU ---

        [Header("Runtime Settings")]
        [Tooltip("Kameranın o anki modu. Oyun başlamadan önce veya oyun sırasında değiştirilerek test edilebilir.")]
        [SerializeField] public CameraMode CurrentMode = CameraMode.FollowTarget;
        
        [Header("Target Settings")]
        [Tooltip("Kameranın 'FollowTarget' modunda takip edeceği hedef (Genellikle Oyuncu).")]
        [SerializeField] private Transform target;
        
        [Tooltip("Takip ederken hedeften ne kadar uzakta duracağı (pozisyonel fark).")]
        [SerializeField] private Vector3 offset;

        [Header("Movement Settings")]
        [Tooltip("'FollowTarget' modundaki takip yumuşaklığı (Lerp hızı).")]
        [SerializeField] private float followSpeed = 8f;
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Yeni Değişkenler) ---
        [Header("Free Move Settings (Strateji Modu)")]
        [Tooltip("Oyuncunun 'FloatingJoystick' verisini okumak için 'PlayerInputHandler' referansı." + 
                 " (Genellikle Oyuncu objesinden sürükleyin)")]
        [SerializeField] private PlayerInputHandler playerInputHandler;
        
        [Tooltip("'FreeMove' modundaki kameranın hareket hızı.")]
        [SerializeField] private float freeMoveSpeed = 10f;
        // --- DEĞİŞİKLİK SONU ---
        
        
        private void Awake()
        {
            // Singleton kurulumu
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Sahnede birden fazla CameraFollow bulundu. Bu kopya yok ediliyor.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // --- DEĞİŞİKLİK BAŞLANGICI (Güvenlik Kontrolü) ---
            // 'playerInputHandler' Inspector'dan atanmamışsa,
            // sahnede bulmayı dene. Bu, modülerliği artırır.
            if (playerInputHandler == null)
            {
                playerInputHandler = FindObjectOfType<PlayerInputHandler>();
                if (playerInputHandler == null)
                {
                    Debug.LogWarning("CameraFollow: 'FreeMove' modu için 'PlayerInputHandler' referansı " +
                                     "ne Inspector'dan atandı ne de sahnede bulundu. " +
                                     "FreeMove modu çalışmayabilir.");
                }
            }
            // --- DEĞİŞİKLİK SONU ---
        }

        // --- DEĞİŞİKLİK BAŞLANGICI: 'Update' metodu 'switch' yapısına geçirildi ---
        private void Update()
        {
            // O anki moda göre ilgili fonksiyonu çalıştır
            switch (CurrentMode)
            {
                case CameraMode.FollowTarget:
                    HandleFollowTarget();
                    break;
                
                case CameraMode.FreeMove:
                    HandleFreeMove();
                    break;
                    
                case CameraMode.Independent:
                    // Independent modda hiçbir şey yapma
                    break;
            }
        }

        /// <summary>
        /// 'FollowTarget' modunun mantığını yönetir.
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
        }

        /// <summary>
        /// YENİ METOT: 'FreeMove' modunun mantığını yönetir.
        /// </summary>
        private void HandleFreeMove()
        {
            // 1. Input Handler referansı var mı diye kontrol et
            if (playerInputHandler == null)
            {
                return; // Input kaynağı yoksa hareket etme
            }

            // 2. PlayerInputHandler'dan (yani oyuncu joystick'inden) input'u oku
            Vector2 input = playerInputHandler.MoveInput;

            // 3. Sıfırdan farklı bir hareket var mı kontrol et
            if (input == Vector2.zero)
            {
                return; // Hareket yoksa işlem yapma
            }
            
            // 4. Hareket vektörü oluştur (Joystick Y -> Dünya Z)
            Vector3 movement = new Vector3(input.x, 0f, input.y);

            // 5. Kameranın pozisyonunu güncelle
            transform.position += movement * freeMoveSpeed * Time.deltaTime;
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

        // --- DEĞİŞİKLİK BAŞLANGICI (Yeni Public Metot) ---
        /// <summary>
        /// Kamerayı 'FreeMove' (Serbest Dolaşım) moduna alır.
        /// (Bu metodu UI'daki bir butona bağlayabilirsiniz)
        /// </summary>
        public void SetFreeMove()
        {
            SetMode(CameraMode.FreeMove);
        }
        // --- DEĞİŞİKLİK SONU ---

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}