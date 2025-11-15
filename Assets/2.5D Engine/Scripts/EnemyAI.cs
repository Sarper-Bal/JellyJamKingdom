/*
 * DÜŞMAN YAPAY ZEKASI (ENEMY AI) - MODÜLER YAPI (v2.0)
 *
 * BU SCRİPT'İN YENİ GÖREVLERİ:
 * 1. HAREKET MODÜLERLİĞİ: Artık 'MovementType' enum'u sayesinde düşmanın nasıl
 * hareket edeceğini (Oyuncu Takibi, Yol Takibi, Sabit Yön) Inspector'dan
 * seçebiliyoruz.
 * 2. OPTİMİZASYON (HAVUZLAMA UYUMU):
 * - 'Start()' metodu, 'OnEnable()' olarak değiştirildi. Bu sayede düşman
 * ObjectPooler'dan her çağrıldığında (spawn olduğunda) hedef ataması
 * güvenilir bir şekilde YENİDEN yapılır.
 * - 'Invoke' kaldırıldı, hedef ataması (gerekiyorsa) anında yapılıyor.
 * - 'OnDisable()' eklendi. Düşman havuza geri döndüğünde (öldüğünde veya
 * deaktif olduğunda) mevcut hedefini ('target') ve yol takibi verisini
 * (currentWaypointIndex) sıfırlar. Bu, bir sonraki spawn için kritik
 * önemdedir.
 * 3. YÖNLENDİRİCİ (ROUTER) YAPI:
 * - 'Update()' metodu artık bir 'switch' bloğu kullanarak bir yönlendirici
 * görevi görüyor ve seçili olan 'movementType'a göre ilgili
 * 'Handle...Movement()' fonksiyonunu çağırıyor. Bu, kodun temiz,
 * okunabilir ve genişletilebilir (modüler) olmasını sağlar.
 * 4. YENİ HAREKET METOTLARI:
 * - HandleChasePlayerMovement(): Sizin eski 'Update' metodunuzdaki mantığı
 * içerir.
 * - HandleFollowPathMovement(): Inspector'dan atanan 'waypoints' dizisini
 * takip eder.
 * - HandleFixedDirectionMovement(): Inspector'dan atanan 'fixedDirection'
 * yönünde sabit olarak ilerler.
 *
 * NOT: Bu güncelleme, sizin "şimdilik stat sistemini (PlayerStats)
 * entegre etmeyelim" talebinize uymuştur. Mevcut 'speed' ve 'damageAmount'
 * değişkenleri korunmuştur.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // Controls enemy behavior: follows player and handles collision.
    public class EnemyAI : MonoBehaviour
    {
        // --- DEĞİŞİKLİK BAŞLANGICI (Modüler Hareket Sistemi) ---

        /// <summary>
        /// Düşmanın hangi hareket mantığını kullanacağını belirler.
        /// Bu, Unity Inspector'ından bir dropdown menü olarak seçilebilir.
        /// </summary>
        public enum MovementType
        {
            /// <summary>
            /// 'Player' etiketli hedefi aktif olarak arar ve takip eder.
            /// (Sizin mevcut sisteminiz)
            /// </summary>
            ChasePlayer,
            
            /// <summary>
            /// Inspector'dan atanan 'waypoints' dizisini sırayla takip eder.
            /// (Kule savunma oyunlarındaki gibi)
            /// </summary>
            FollowPath,
            
            /// <summary>
            /// 'fixedDirection' değişkeninde belirtilen sabit yöne doğru ilerler.
            /// (Flappy Bird'deki borular veya bir mermi gibi)
            /// </summary>
            FixedDirection
        }

        [Header("Hareket Ayarları")]
        [Tooltip("Bu düşmanın kullanacağı yapay zeka hareket tipi.")]
        [SerializeField] private MovementType movementType = MovementType.ChasePlayer;

        [Tooltip("Düşmanın hareket hızı. (Tüm modlar tarafından kullanılır)")]
        [SerializeField] private float speed;
        
        [Header("Yol Takibi Ayarları (FollowPath)")]
        [Tooltip("EĞER 'Movement Type = FollowPath' ise, düşmanın takip edeceği " +
                 "Transform (boş obje) noktalarının sıralı listesi.")]
        [SerializeField] private Transform[] waypoints;
        
        [Tooltip("Yolun sonuna gelindiğinde başa dönsün mü?")]
        [SerializeField] private bool loopPath = true;
        
        // Takip edilen mevcut yol noktasının indeksi
        private int currentWaypointIndex = 0;
        
        [Header("Sabit Yön Ayarları (FixedDirection)")]
        [Tooltip("EĞER 'Movement Type = FixedDirection' ise, düşmanın " +
                 "ilerleyeceği yön. (Normalize edilmesine gerek yok, kod içinde yapılır)")]
        [SerializeField] private Vector3 fixedDirection = new Vector3(0, 0, -1); // Varsayılan: Z'de aşağı
        
        // --- DEĞİŞİKLİK SONU ---

        
        [Header("Genel Ayarlar")]
        [Tooltip("Oyuncuya çarptığında vereceği hasar miktarı.")]
        [SerializeField] private int damageAmount;
        
        [Tooltip("Yön değiştirdiğinde dönecek olan Sprite Renderer.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        // Düşmanın o an takip ettiği hedef (Player veya Waypoint olabilir)
        private Transform target; 
        
        
        // --- DEĞİŞİKLİK BAŞLANGICI (Optimize Edilmiş Başlatma) ---
        
        /// <summary>
        /// 'Start()' yerine 'OnEnable()' kullanıyoruz.
        /// Bu metot, obje her 'SetActive(true)' yapıldığında (yani ObjectPooler
        /// tarafından her spawn edildiğinde) çalışır. Bu, havuzlama için
        /// kritik bir düzeltmedir.
        /// </summary>
        private void OnEnable()
        {
            // Eğer hareket tipimiz 'ChasePlayer' olarak ayarlandıysa,
            // hedefimizi (Player'ı) bulmayı dene.
            if (movementType == MovementType.ChasePlayer)
            {
                // 'Invoke' kaldırıldı. Artık 1 saniye beklemiyoruz.
                AssignPlayer();
            }
            // Diğer modlar (FollowPath, FixedDirection) hedef olarak
            // 'Player'ı aramadığı için 'AssignPlayer'ı boşuna çağırmayız.
            // Bu da bir optimizasyondur.
        }

        /// <summary>
        /// Bu metot, obje 'SetActive(false)' yapıldığında (yani havuza
        /// geri döndüğünde) çalışır.
        /// </summary>
        private void OnDisable()
        {
            // Düşman havuza geri dönerken, bir sonraki kullanım için
            // geçici verilerini sıfırlamalıyız.
            target = null;
            currentWaypointIndex = 0;
        }

        // 'Start()' metodu 'OnEnable()' olarak değiştirildi.
        /*
        public void Start()
        {
            Invoke("AssignPlayer", 1f); // ESKİ KOD
        }
        */

        // 'AssignPlayer' metodu korundu ancak artık 'Invoke' ile çağrılmıyor.
        public void AssignPlayer()
        {
            // Bu fonksiyon yavaştır, ancak 'OnEnable'da sadece 1 KEZ
            // çağrıldığı için 'Update' içinde çağırmaktan çok daha performanslıdır.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                // Oyuncu bulunamadıysa (test sahnesi vb.), hata vermemesi için
                // 'Update' döngüsünü durdur.
                Debug.LogWarning("EnemyAI: 'Player' etiketli hedef bulunamadı. " +
                                 "'ChasePlayer' modu çalışmayacak.");
            }
        }
        
        // --- DEĞİŞİKLİK SONU ---


        // --- DEĞİŞİKLİK BAŞLANGICI (Modüler Update Yönlendiricisi) ---
        
        /// <summary>
        /// 'Update' artık bir yönlendirici (router) görevi görüyor.
        /// İçinde hareket hesaplaması yapmaz, sadece seçilen moda göre
        /// ilgili 'Handle...Movement()' fonksiyonunu çağırır.
        /// </summary>
        public void Update()
        {
            // Hangi hareket tipi seçiliyse o fonksiyona git
            switch (movementType)
            {
                case MovementType.ChasePlayer:
                    HandleChasePlayerMovement();
                    break;

                case MovementType.FollowPath:
                    HandleFollowPathMovement();
                    break;

                case MovementType.FixedDirection:
                    HandleFixedDirectionMovement();
                    break;
            }
        }

        /// <summary>
        /// MOD 1: Oyuncuyu Takip Etme Hareketi
        /// (Sizin eski 'Update' kodunuz buraya taşındı)
        /// </summary>
        private void HandleChasePlayerMovement()
        {
            // 'OnEnable/AssignPlayer' içinde hedef atandıysa
            if (target) 
            {
                // Sprite'ı hedefin X pozisyonuna göre çevir
                if (target.position.x > transform.position.x)
                    spriteRenderer.flipX = false;
                else
                    spriteRenderer.flipX = true;

                // Hedefe doğru 'speed' hızıyla hareket et
                transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * speed);
            }
            // 'target' yoksa (örn: AssignPlayer başarısız olduysa)
            // hiçbir şey yapma, bekle.
        }

        /// <summary>
        /// MOD 2: Yolu Takip Etme Hareketi (Yeni)
        /// </summary>
        private void HandleFollowPathMovement()
        {
            // Waypoint listesi atanmamışsa veya boşsa, hata vermemesi
            // için hiçbir şey yapma.
            if (waypoints == null || waypoints.Length == 0)
            {
                // (Bu uyarıyı OnEnable'da vermek daha performanslıdır ama
                // basitlik için burada tutabiliriz)
                // Debug.LogWarning("MovementType 'FollowPath' seçili ancak 'waypoints' dizisi boş!");
                return;
            }
            
            // 1. Hedef waypoint'i belirle
            // 'currentWaypointIndex'in dizi sınırları içinde olduğundan emin ol
            if (currentWaypointIndex >= waypoints.Length)
            {
                // Yolun sonuna gelmişiz demektir
                if (loopPath)
                {
                    // Döngüye izin verildiyse, başa dön
                    currentWaypointIndex = 0;
                }
                else
                {
                    // Döngü yoksa, hareketi durdur (fonksiyondan çık)
                    // Düşman son noktada bekleyecektir.
                    return; 
                }
            }
            
            Transform targetWaypoint = waypoints[currentWaypointIndex];
            if (targetWaypoint == null) return; // Güvenlik (Nokta silinmişse)
            
            // 2. Sprite yönünü ayarla
            if (targetWaypoint.position.x > transform.position.x)
                spriteRenderer.flipX = false;
            else
                spriteRenderer.flipX = true;
            
            // 3. Hedef waypoint'e doğru hareket et
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, Time.deltaTime * speed);

            // 4. Hedefe ulaşıp ulaşmadığımızı kontrol et
            // (Küçük bir eşik değer (0.1f) kullanmak, tam 0'ı beklemekten daha güvenlidir)
            if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
            {
                // Hedefe ulaştıysak, bir sonraki hedefe geç
                currentWaypointIndex++;
            }
        }

        /// <summary>
        /// MOD 3: Sabit Yönde İlerleme Hareketi (Yeni)
        /// </summary>
        private void HandleFixedDirectionMovement()
        {
            // Sprite'ı sabit yönün X eksenine göre ayarla
            // (Eğer X yönü 0 ise, mevcut 'flipX' durumunu koru)
            if (fixedDirection.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (fixedDirection.x < -0.01f)
                spriteRenderer.flipX = true;
            
            // Belirlenen yönde 'speed' hızıyla durmadan ilerle
            // '.normalized' kullanmak, Inspector'dan (1,0,0) veya (100,0,0)
            // girilse bile hızın 'speed' ile aynı kalmasını sağlar.
            transform.position += fixedDirection.normalized * speed * Time.deltaTime;
        }
        
        // --- DEĞİŞİKLİK SONU ---


        /// <summary>
        /// Çarpışma mantığı (Bu kısım değişmedi).
        /// Bu mantık, hangi hareket modu seçilirse seçilsin çalışmaya
        /// devam eder, bu da sistemi modüler yapar.
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                // Oyuncuya hasar ver
                collision.collider.GetComponent<HealthSystem>().Damage(damageAmount);
                // Kendini yok et (Aslında havuzuna geri dön [Bkz: HealthSystem.cs])
                GetComponent<HealthSystem>().Die();
            }
        }
    }
}