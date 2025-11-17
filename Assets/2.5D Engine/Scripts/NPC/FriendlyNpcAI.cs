/*
 * DOST NPC YAPAY ZEKASI (MOTOR) - v1.6 (Performans Optimizasyonu)
 *
 * * DEĞİŞİKLİKLER (v1.6):
 * - PERFORMANS SORUNU: 'Update()' içindeki 'Vector3.Distance' metodu,
 * her frame yavaş bir "karekök" (Square Root) işlemi yapar.
 * Bu, 200 NPC olduğunda CPU'yu yorar.
 *
 * - ÇÖZÜM:
 * - 'sqrArrivalDistanceThreshold' adında yeni bir private float eklendi.
 * - 'Awake()' içinde bu değişken, '0.1f * 0.1f' (yani 0.01f)
 * olarak BİR KEZ hesaplanıp saklanır.
 * - 'Update()' içindeki mesafe kontrolü,
 * '(transform.position - targetPositionOnGround).sqrMagnitude'
 * (mesafenin karesi) ile değiştirildi.
 *
 * - SONUÇ: Yeni sistem (sqrMagnitude), karekök işlemi yapmadığı için
 * 'Vector3.Distance' kullanmaktan ÇOK DAHA HIZLIDIR ve mobil
 * performans için kritiktir.
 */

using UnityEngine;

// Havuzlama (Pooling) kodu isteğiniz üzerine kaldırılmıştı (v1.3)
public class FriendlyNpcAI : MonoBehaviour
{
    private enum State
    {
        Idle,
        GoingToWork,
        ReturningHome
    }
    
    [Header("Bileşen Referansları (Zorunlu)")]
    [SerializeField] private SpriteRenderer spriteRenderer; 
    
    // Anlık (Runtime) Veriler
    private FriendlyNpcData npcData;
    private Transform homeTransform;
    private Transform workSpotTransform;
    
    private State currentState = State.Idle;
    private Transform currentTarget;

    // --- DEĞİŞİKLİK BAŞLANGICI (v1.6 - Optimizasyon) ---
    // Hedefe vardığımızı anlamak için gereken mesafenin "karesi".
    // 'Awake' içinde hesaplanır.
    private float sqrArrivalDistanceThreshold;
    private const float ARRIVAL_DISTANCE_THRESHOLD = 0.1f; // 0.1f mesafe
    // --- DEĞİŞİKLİK SONU ---

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("FriendlyNpcAI: 'Sprite Renderer' alanı Inspector'dan atanmamış!", this);
        }
        
        // --- DEĞİŞİKLİK BAŞLANGICI (v1.6) ---
        // Eşik değerin karesini BİR KEZ hesapla ve sakla.
        // Bu, 'Update' içinde her frame (0.1f * 0.1f) yapmaktan daha iyidir.
        sqrArrivalDistanceThreshold = ARRIVAL_DISTANCE_THRESHOLD * ARRIVAL_DISTANCE_THRESHOLD; // (0.01f)
        // --- DEĞİŞİKLİK SONU ---
    }

    /// <summary>
    /// 'NpcHousing' (Ev) tarafından NPC 'Instantiate' edildikten
    /// hemen sonra çağrılır.
    /// </summary>
    public void Initialize(FriendlyNpcData data, Transform home, Transform workSpot)
    {
        this.npcData = data;
        if (this.npcData == null)
        {
            Debug.LogError("FriendlyNpcAI.Initialize() 'FriendlyNpcData' null olarak çağrıldı.", this);
            gameObject.SetActive(false); 
            return;
        }
        
        this.homeTransform = home;
        this.workSpotTransform = workSpot;
        this.currentState = State.Idle;

        InitializeVisuals();
        GoToWork();
    }

    private void InitializeVisuals()
    {
        if (npcData == null || spriteRenderer == null) return;
        if (npcData.characterSprite != null)
        {
            spriteRenderer.sprite = npcData.characterSprite;
        }
        if (npcData.scale != Vector3.one && npcData.scale != Vector3.zero)
        {
            transform.localScale = npcData.scale;
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }
    
    private void Update()
    {
        if (currentState == State.Idle || currentTarget == null || npcData == null)
        {
            return;
        }

        // 2.5D Hareket Mantığı (Y-Eksenini kilitle)
        Vector3 targetPositionOnGround = new Vector3(
            currentTarget.position.x, 
            transform.position.y,
            currentTarget.position.z
        );
        
        if (targetPositionOnGround.x > transform.position.x)
            FlipSprite(false); 
        else if (targetPositionOnGround.x < transform.position.x)
            FlipSprite(true); 
            
        // Hareket (MoveTowards) zaten hızlıdır, bu kalabilir.
        transform.position = Vector3.MoveTowards(
            transform.position, 
            targetPositionOnGround, 
            Time.deltaTime * npcData.speed
        );

        // --- DEĞİŞİKLİK BAŞLANGICI (v1.6 - Optimize Edilmiş Kontrol) ---
        // Hedefe ulaşıldı mı kontrol et
        // 'Vector3.Distance(A, B)' yerine (A - B).sqrMagnitude kullanıyoruz.
        // Bu, yavaş olan karekök (sqrt) işlemini yapmaz.
        if ((transform.position - targetPositionOnGround).sqrMagnitude < sqrArrivalDistanceThreshold)
        {
            // Orijinal yavaş kod:
            // if (Vector3.Distance(transform.position, targetPositionOnGround) < ARRIVAL_DISTANCE_THRESHOLD)
            
            ArrivedAtTarget();
        }
        // --- DEĞİŞİKLİK SONU ---
    }
    
    private void ArrivedAtTarget()
    {
        State previousState = currentState;
        currentState = State.Idle; // Hareketi durdur
        
        if (previousState == State.GoingToWork)
        {
            OnArrivedAtWork?.Invoke(this);
        }
        else if (previousState == State.ReturningHome)
        {
            OnArrivedAtHome?.Invoke(this);
        }
    }

    // --- Komut Metotları (Event'ler) ---
    public event System.Action<FriendlyNpcAI> OnArrivedAtWork;
    public event System.Action<FriendlyNpcAI> OnArrivedAtHome;
    
    public void GoToWork()
    {
        currentState = State.GoingToWork;
        currentTarget = workSpotTransform;
    }

    public void ReturnHome()
    {
        currentState = State.ReturningHome;
        currentTarget = homeTransform;
    }
    
    // Scale-uyumlu Flip metodu
    private void FlipSprite(bool faceLeft)
    {
        Vector3 baseScale = Vector3.one;
        if (npcData != null) { baseScale = npcData.scale; }
        if (faceLeft)
        {
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
        else
        {
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
    }
}