/*
 * DOST NPC YAPAY ZEKASI (MOTOR) - v1.8 (Kapasite Sistemi)
 *
 * * DEĞİŞİKLİKLER (v1.8):
 * - 'hasResourcePayload' (bool) alanı, 'currentPayloadAmount' (int)
 * olarak değiştirildi. Artık ne kadar kaynak taşıdığını biliyor.
 * - 'ReturnHome()' metodunun imzası 'ReturnHome(int collectedAmount)'
 * olarak değiştirildi. 'NpcHousing' tam olarak kaç kaynak
 * toplandığını bu metoda iletecek.
 * - 'OnArrivedAtHome' event'inin imzası değişti: 'Action<FriendlyNpcAI, int>'.
 * Artık eve vardığında 'NpcHousing'e taşıdığı kaynak miktarını
 * raporlar.
 * - 'ArrivedAtTarget()' metodu, 'OnArrivedAtHome' event'ini
 * 'currentPayloadAmount' ile tetikliyor.
 * - YENİ METOT: 'GetNpcData()' eklendi. 'NpcHousing'in bu NPC'nin
 * 'maxCarryCapacity' bilgisine erişebilmesi için gereklidir.
 */

using UnityEngine;

public class FriendlyNpcAI : MonoBehaviour
{
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.8 - Event İmzası) ---
    /// <summary>
    /// NPC iş yerine (workSpot) ulaştığında tetiklenir.
    /// </summary>
    public event System.Action<FriendlyNpcAI> OnArrivedAtWork;
    
    /// <summary>
    /// NPC evine (homeTransform) ulaştığında tetiklenir.
    /// 'int' parametresi, ne kadar kaynakla döndüğünü belirtir (0 = eli boş).
    /// </summary>
    public event System.Action<FriendlyNpcAI, int> OnArrivedAtHome;
    // --- DEĞİŞİKLİK SONU ---

    private enum State
    {
        Idle,
        GoingToWork,
        ReturningHome
    }
    
    [Header("Bileşen Referansları (Zorunlu)")]
    [SerializeField] private SpriteRenderer spriteRenderer; 
    
    private FriendlyNpcData npcData;
    private Transform homeTransform;
    private Transform workSpotTransform;
    
    private State currentState = State.Idle;
    private Transform currentTarget;

    // --- DEĞİŞİKLİK BAŞLANGICI (v1.8 - Kapasite) ---
    /// <summary>
    /// NPC'nin o an elinde taşıdığı kaynak miktarı.
    /// </summary>
    private int currentPayloadAmount = 0;
    // private bool hasResourcePayload = false; // <-- SİLİNDİ
    // --- DEĞİŞİKLİK SONU ---
    
    private float sqrArrivalDistanceThreshold;
    private const float ARRIVAL_DISTANCE_THRESHOLD = 0.1f;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("FriendlyNpcAI: 'Sprite Renderer' alanı Inspector'dan atanmamış!", this);
        }
        sqrArrivalDistanceThreshold = ARRIVAL_DISTANCE_THRESHOLD * ARRIVAL_DISTANCE_THRESHOLD;
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
        GoToWork(); // İlk komut
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
        
        Vector3 targetPositionOnGround = new Vector3(
            currentTarget.position.x, 
            transform.position.y,
            currentTarget.position.z
        );
        
        if (targetPositionOnGround.x > transform.position.x)
            FlipSprite(false); 
        else if (targetPositionOnGround.x < transform.position.x)
            FlipSprite(true); 
            
        transform.position = Vector3.MoveTowards(
            transform.position, 
            targetPositionOnGround, 
            Time.deltaTime * npcData.speed
        );

        // Optimize edilmiş mesafe kontrolü (v1.6)
        if ((transform.position - targetPositionOnGround).sqrMagnitude < sqrArrivalDistanceThreshold)
        {
            ArrivedAtTarget();
        }
    }
    
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.8 - Olay Tetikleyici) ---
    /// <summary>
    /// Hedefe ulaşıldığında çağrılır.
    /// Eve vardığında yük (payload) miktarını raporlar.
    /// </summary>
    private void ArrivedAtTarget()
    {
        State previousState = currentState;
        currentState = State.Idle; // Hareketi durdur
        
        if (previousState == State.GoingToWork)
        {
            // "İş yerine vardım!"
            OnArrivedAtWork?.Invoke(this);
        }
        else if (previousState == State.ReturningHome)
        {
            // "Eve vardım!" (Taşıdığı miktarı raporla)
            OnArrivedAtHome?.Invoke(this, currentPayloadAmount);
        }
    }

    // --- Komut Metotları (v1.8) ---
    
    /// <summary>
    /// NpcHousing'den "İşe Git" komutu alır
    /// </summary>
    public void GoToWork()
    {
        currentPayloadAmount = 0; // İşe giderken elin boş (0 kaynak)
        currentState = State.GoingToWork;
        currentTarget = workSpotTransform;
    }

    /// <summary>
    /// NpcHousing'den "Eve Dön" komutu alır
    /// </summary>
    /// <param name="collectedAmount">NPC'nin ne kadar kaynak topladığı</param>
    public void ReturnHome(int collectedAmount)
    {
        currentPayloadAmount = collectedAmount; // Kaynak miktarını ayarla (0 olabilir)
        currentState = State.ReturningHome;
        currentTarget = homeTransform;
    }
    
    /// <summary>
    /// 'NpcHousing'in bu NPC'nin 'maxCarryCapacity'
    /// gibi verilerine erişmesini sağlar.
    /// </summary>
    public FriendlyNpcData GetNpcData()
    {
        return npcData;
    }
    // --- DEĞİŞİKLİK SONU ---
    
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