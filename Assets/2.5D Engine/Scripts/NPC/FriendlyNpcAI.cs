/*
 * DOST NPC YAPAY ZEKASI (MOTOR) - v1.7 (Payload Sistemi)
 *
 * * DEĞİŞİKLİKLER (v1.7):
 * - YENİ ALAN: 'hasResourcePayload' (bool) eklendi. NPC'nin elinde
 * kaynak olup olmadığını tutar.
 * - 'GoToWork()' metodu, 'hasResourcePayload'u 'false' yaparak
 * NPC'nin işe "eli boş" gitmesini sağlar.
 * - 'ReturnHome()' metodunun imzası değişti: 'ReturnHome(bool didCollectResource)'.
 * 'NpcHousing' bu metodu çağırarak NPC'ye yükü verip vermediğini söyler.
 * - 'OnArrivedAtHome' event'inin imzası değişti: 'Action<FriendlyNpcAI, bool>'.
 * Artık eve vardığında 'NpcHousing'e yük durumunu (payload) raporlar.
 * - 'ArrivedAtTarget()' metodu, 'OnArrivedAtHome' event'ini
 * bu yeni imzayla ('hasResourcePayload' ile) tetikleyecek şekilde güncellendi.
 */

using UnityEngine;

public class FriendlyNpcAI : MonoBehaviour
{
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.7 - Event İmzası) ---
    /// <summary>
    /// NPC iş yerine (workSpot) ulaştığında tetiklenir.
    /// </summary>
    public event System.Action<FriendlyNpcAI> OnArrivedAtWork;
    
    /// <summary>
    /// NPC evine (homeTransform) ulaştığında tetiklenir.
    /// 'bool' parametresi, 'true' ise elinde kaynakla döndüğünü belirtir.
    /// </summary>
    public event System.Action<FriendlyNpcAI, bool> OnArrivedAtHome;
    // --- DEĞİŞİKLİK SONU ---

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

    // --- DEĞİŞİKLİK BAŞLANGICI (v1.7 - Payload) ---
    /// <summary>
    /// NPC'nin o an elinde kaynak taşıyıp taşımadığını belirtir.
    /// </summary>
    private bool hasResourcePayload = false;
    // --- DEĞİŞİKLİK SONU ---
    
    // Optimize edilmiş mesafe kontrolü için (v1.6)
    private float sqrArrivalDistanceThreshold;
    private const float ARRIVAL_DISTANCE_THRESHOLD = 0.1f;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("FriendlyNpcAI: 'Sprite Renderer' alanı Inspector'dan atanmamış!", this);
        }
        // Optimize edilmiş mesafe eşiğini hesapla
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
    
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.7 - Olay Tetikleyici) ---
    /// <summary>
    /// Hedefe ulaşıldığında çağrılır.
    /// Eve vardığında yük (payload) durumunu raporlar.
    /// </summary>
    private void ArrivedAtTarget()
    {
        State previousState = currentState;
        currentState = State.Idle; // Hareketi durdur
        
        if (previousState == State.GoingToWork)
        {
            // "İş yerine vardım!" (Yük her zaman 'false' olur)
            OnArrivedAtWork?.Invoke(this);
        }
        else if (previousState == State.ReturningHome)
        {
            // "Eve vardım!" (Yük 'true' veya 'false' olabilir)
            OnArrivedAtHome?.Invoke(this, hasResourcePayload);
        }
    }

    // --- Komut Metotları (v1.7) ---
    
    /// <summary>
    /// NpcHousing'den "İşe Git" komutu alır
    /// </summary>
    public void GoToWork()
    {
        hasResourcePayload = false; // İşe giderken elin boş
        currentState = State.GoingToWork;
        currentTarget = workSpotTransform;
    }

    /// <summary>
    /// NpcHousing'den "Eve Dön" komutu alır
    /// </summary>
    /// <param name="didCollectResource">NPC'nin kaynak alıp almadığı</param>
    public void ReturnHome(bool didCollectResource)
    {
        hasResourcePayload = didCollectResource; // Kaynak durumunu ayarla
        currentState = State.ReturningHome;
        currentTarget = homeTransform;
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