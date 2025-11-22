using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    public class EnvironmentRotator : MonoBehaviour
    {
        public enum MotionType
        {
            Continuous, // Pervane, Tekerlek (Sürekli döner)
            Oscillate   // Vinç, Tabela, Fener (Sallanır)
        }

        [Header("Hareket Tipi")]
        [SerializeField] private MotionType motionType = MotionType.Continuous;

        [Header("Sürekli Dönüş (Pervane)")]
        [Tooltip("Saniyedeki dönüş hızı. (Örn: Z ekseni için 0, 0, 90)")]
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 0, 100);

        [Header("Sallanma / Sarkaç (Vinç)")]
        [Tooltip("Hangi eksende sallanacak? (Örn: Y ekseni için 0, 1, 0)")]
        [SerializeField] private Vector3 oscillateAxis = new Vector3(0, 1, 0);
        
        [Tooltip("Açı sınırı. (Örn: 30 ise, -30 ile +30 derece arasında gider gelir).")]
        [SerializeField] private float angleLimit = 30f;
        
        [Tooltip("Sallanma hızı. Daha düşük = Daha yavaş.")]
        [SerializeField] private float oscillateSpeed = 2.0f;

        [Header("Genel")]
        [Tooltip("Açık olursa hepsi senkronize hareket etmez, doğal durur.")]
        [SerializeField] private bool randomizeStart = true;

        private Transform _transform;
        private Quaternion _initialRotation; // Başlangıç duruşunu sakla
        private float _randomOffset;         // Rastgele zaman farkı

        private void Awake()
        {
            _transform = transform;
            _initialRotation = transform.localRotation; // Orijinal duruşu kaydet

            if (randomizeStart)
            {
                if (motionType == MotionType.Continuous)
                {
                    // Sürekli modda rastgele bir yöne çevirip başlat
                    Vector3 randomRot = new Vector3(
                        rotationSpeed.x != 0 ? Random.Range(0, 360) : 0,
                        rotationSpeed.y != 0 ? Random.Range(0, 360) : 0,
                        rotationSpeed.z != 0 ? Random.Range(0, 360) : 0
                    );
                    _transform.Rotate(randomRot);
                }
                else
                {
                    // Sarkaç modunda harekete "zamanın farklı bir yerinden" başlat
                    _randomOffset = Random.Range(0f, 10f);
                }
            }
        }

        private void Update()
        {
            if (motionType == MotionType.Continuous)
            {
                // --- MOD 1: SÜREKLİ DÖNÜŞ ---
                // Basit ve en performanslı yöntem
                _transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
            }
            else
            {
                // --- MOD 2: SARKAÇ (OSCILLATE) ---
                // Mathf.Sinüs kullanıyoruz. Bu fonksiyon -1 ile 1 arasında pürüzsüzce gidip gelir.
                // Asla takılmaz, asla sıçrama yapmaz.
                
                float time = Time.time * oscillateSpeed + _randomOffset;
                float sineValue = Mathf.Sin(time); // -1...0...+1

                // Açıyı hesapla (-30 ... +30)
                float angle = sineValue * angleLimit;

                // Hesaplanan açıyı, objenin "Başlangıç Duruşu"nun üzerine ekle.
                // Bu sayede obje asla rotasını şaşırmaz, hep başladığı yerin etrafında sallanır.
                Quaternion swing = Quaternion.AngleAxis(angle, oscillateAxis);
                _transform.localRotation = _initialRotation * swing;
            }
        }
    }
}