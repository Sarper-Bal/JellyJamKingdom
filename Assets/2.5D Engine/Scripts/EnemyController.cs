using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    [RequireComponent(typeof(HealthSystem), typeof(SpriteRenderer))]
    public class EnemyController : MonoBehaviour, IPooledObject
    {
        [SerializeField] private EnemyProfile profile;

        private Transform target;
        private SpriteRenderer spriteRenderer;
        private HealthSystem healthSystem;

        private float currentSpeed;
        private int currentDamageAmount;

        #region IPooledObject Implementation
        public string PoolTag { get; set; }

        public void OnObjectSpawn()
        {
            // CASUS 1: Bu fonksiyonun çağrılıp çağrılmadığını kontrol edelim.
            Debug.Log(gameObject.name + " havuzdan spawn oldu! OnObjectSpawn() çalıştı.");

            AssignPlayer();

            if (profile != null)
            {
                // CASUS 2: Profile'ın içindeki verileri okuyup okumadığını görelim.
                Debug.Log("Profile bulundu: " + profile.name + ". Hız ayarlanıyor: " + profile.speed);

                currentSpeed = profile.speed;
                currentDamageAmount = profile.damageAmount;
                healthSystem.Initialize(profile.maxHealth);
                spriteRenderer.sprite = profile.sprite;
            }
            else
            {
                // CASUS 3: Eğer Profile boşsa, bu mesajı göreceğiz.
                Debug.LogError("HATA: " + gameObject.name + " üzerinde EnemyProfile atanmamış!");
            }
        }
        #endregion

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            healthSystem = GetComponent<HealthSystem>();
        }

        private void AssignPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                // CASUS 4: Hedefin başarıyla bulunup bulunmadığını kontrol edelim.
                Debug.Log(gameObject.name + " hedefi buldu: " + target.name);
            }
            else
            {
                Debug.LogWarning("Sahnede 'Player' tag'ine sahip bir obje bulunamadı!");
            }
        }

        private void Update()
        {
            if (target != null)
            {
                // Düşman hareket etmiyorsa, hızının 0 olup olmadığını kontrol edelim.
                if (currentSpeed <= 0)
                {
                    // Bu mesajı sürekli görüyorsak, hız atanmasında sorun var demektir.
                    // Debug.LogWarning(gameObject.name + " hareket edemiyor çünkü hızı sıfır!");
                    return;
                }

                if (target.position.x > transform.position.x)
                    transform.localScale = new Vector3(1, 1, 1);
                else
                    transform.localScale = new Vector3(-1, 1, 1);

                transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);
            }
            // Eğer hedefi yoksa, bu mesajı görebiliriz.
            // else { Debug.LogWarning(gameObject.name + " hedefi olmadığı için hareket etmiyor."); }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                collision.collider.GetComponent<HealthSystem>().Damage(currentDamageAmount);
                healthSystem.Die();
            }
        }
    }
}