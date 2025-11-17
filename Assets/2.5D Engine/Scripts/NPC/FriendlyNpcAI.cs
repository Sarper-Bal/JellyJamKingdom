/*
 * DOST NPC YAPAY ZEKASI (MOTOR) - v1.4 (Event-Driven)
 *
 * * DEĞİŞİKLİKLER (v1.4):
 * - Bu script artık "düşünen" taraf değil, sadece "yapan" taraf (Motor).
 * - 'ArrivedAtTarget' metodu artık kendi kendine karar vermiyor.
 * - 'OnArrivedAtWork' ve 'OnArrivedAtHome' adında iki YENİ event (olay) eklendi.
 * - 'ArrivedAtTarget' metodu, 'currentState'i 'Idle' yapar ve ilgili event'i
 * tetikler ('NpcHousing'in duyması için).
 * - 'GoToWork' ve 'ReturnHome' metotları 'NpcHousing'in
 * komut verebilmesi için 'public' yapıldı.
 */

using UnityEngine;

public class FriendlyNpcAI : MonoBehaviour
{
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.4 - Event'ler) ---
    /// <summary>
    /// NPC iş yerine (workSpot) ulaştığında tetiklenir.
    /// 'NpcHousing' bu olayı dinler.
    /// </summary>
    public event System.Action<FriendlyNpcAI> OnArrivedAtWork;
    
    /// <summary>
    /// NPC evine (homeTransform) ulaştığında tetiklenir.
    /// 'NpcHousing' bu olayı dinler.
    /// </summary>
    public event System.Action<FriendlyNpcAI> OnArrivedAtHome;
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
    
    // Not: Havuzlama (Pooling) kodları isteğiniz üzerine kaldırılmıştı.

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("FriendlyNpcAI: 'Sprite Renderer' alanı Inspector'dan atanmamış!", this);
        }
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

        // Görselleri ayarla
        InitializeVisuals();
        
        // Hareketi başlat (İlk komut)
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
        // Sadece hareket durumlarındayken çalış
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

        if (Vector3.Distance(transform.position, targetPositionOnGround) < 0.1f)
        {
            // Hedefe ulaşıldı
            ArrivedAtTarget();
        }
    }
    
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.4 - Olay Tetikleyici) ---
    /// <summary>
    /// Hedefe ulaşıldığında çağrılır.
    /// Artık kendi kendine karar vermiyor, sadece "Beyin"e (NpcHousing)
    /// rapor veriyor.
    /// </summary>
    private void ArrivedAtTarget()
    {
        State previousState = currentState;
        currentState = State.Idle; // Hareketi durdur
        
        if (previousState == State.GoingToWork)
        {
            // "İş yerine vardım!" diye event tetikle
            OnArrivedAtWork?.Invoke(this);
        }
        else if (previousState == State.ReturningHome)
        {
            // "Eve vardım!" diye event tetikle
            OnArrivedAtHome?.Invoke(this);
        }
    }

    // --- Komut Metotları (Artık 'public') ---
    
    /// <summary>
    /// NpcHousing'den "İşe Git" komutu alır
    /// </summary>
    public void GoToWork()
    {
        currentState = State.GoingToWork;
        currentTarget = workSpotTransform;
    }

    /// <summary>
    /// NpcHousing'den "Eve Dön" komutu alır
    /// </summary>
    public void ReturnHome()
    {
        currentState = State.ReturningHome;
        currentTarget = homeTransform;
    }
    // --- DEĞİŞİKLİK SONU ---
    
    // Scale-uyumlu Flip metodu (Değişiklik yok)
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