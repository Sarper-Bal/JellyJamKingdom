/*
 * CAMERA FOLLOW (YÖNETİCİ MODELİ)
 * * DEĞİŞİKLİKLER (v1.2 - Başlangıç Modu Düzeltmesi):
 * - 'Awake()' metodunun sonundaki 'CurrentMode = CameraMode.FollowTarget;' satırı
 * YORUM SATIRI HALİNE GETİRİLDİ (veya silebilirsiniz).
 * - Bu sayede, oyun başlamadan önce Inspector'dan 'Independent' seçilirse,
 * oyun başladığında bu ayar korunur ve 'Awake' tarafından ezilmez.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // This class makes the camera follow a target transform with a specified offset.
    public class CameraFollow : MonoBehaviour
    {
        /// <summary>
        /// Sahnedeki tek CameraFollow script'ine statik erişim noktası.
        /// </summary>
        public static CameraFollow Instance { get; private set; }
        
        /// <summary>
        /// Kameranın anlık davranış modlarını tanımlar.
        /// </summary>
        public enum CameraMode
        {
            /// <summary>
            /// 'target' olarak atanan objeyi 'offset' ile takip eder. (Varsayılan)
            /// </summary>
            FollowTarget,
            
            /// <summary>
            /// Kamera tüm takip etme davranışlarını durdurur.
            /// </summary>
            Independent 
        }

        [Header("Runtime Settings")]
        [Tooltip("Kameranın o anki modu. Oyun başlamadan önce veya oyun sırasında değiştirilerek test edilebilir.")]
        [SerializeField] public CameraMode CurrentMode = CameraMode.FollowTarget; // Varsayılan değeri burada atamak daha sağlıklıdır.
        
        [Header("Target Settings")]
        [Tooltip("Kameranın takip edeceği varsayılan hedef (Genellikle Oyuncu).")]
        [SerializeField] private Transform target;
        
        [Tooltip("Takip ederken hedeften ne kadar uzakta duracağı (pozisyonel fark).")]
        [SerializeField] private Vector3 offset;

        [Header("Movement Settings")]
        [Tooltip("Kameranın hedefi takip etme yumuşaklığı (Lerp hızı).")]
        [SerializeField] private float followSpeed = 8f;
        
        
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
            
            // --- DEĞİŞİKLİK BAŞLANGICI ---
            // Bu satır, Inspector'da yaptığınız ayarı eziyordu.
            // Değişkenin varsayılan değeri artık yukarıda, tanımlandığı satırda veriliyor.
            // CurrentMode = CameraMode.FollowTarget; // <-- BU SATIR KALDIRILDI/YORUMA ALINDI
            // --- DEĞİŞİKLİK SONU ---
        }


        // Called once per frame.
        private void Update()
        {
            // 1. Kameranın modu 'Independent' (Bağımsız) ise, HİÇBİR ŞEY YAPMA.
            if (CurrentMode == CameraMode.Independent)
            {
                return; // Takip etme mantığını atla
            }

            // 2. Eğer mod 'FollowTarget' ise ve hedef varsa, takip et.
            if (target != null)
            {
                transform.position = Vector3.Lerp(
                    transform.position, 
                    target.position + offset, 
                    Time.deltaTime * followSpeed
                );
            }
        }
        
        // --- PUBLIC API (Dışarıdan Komutlar) ---
        
        /// <summary>
        /// Kameranın modunu değiştirir (FollowTarget veya Independent).
        /// </summary>
        public void SetMode(CameraMode newMode)
        {
            CurrentMode = newMode;
        }
        
        /// <summary>
        /// Kamerayı 'Independent' (Bağımsız) moda alır ve takibi durdurur.
        /// </summary>
        public void SetIndependent()
        {
            SetMode(CameraMode.Independent);
        }

        /// <summary>
        /// Kamerayı 'FollowTarget' (Hedef Takip) moduna alır.
        /// </summary>
        public void FollowTarget()
        {
            SetMode(CameraMode.FollowTarget);
        }

        /// <summary>
        /// Kameranın takip ettiği hedefi anlık olarak değiştirir.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}