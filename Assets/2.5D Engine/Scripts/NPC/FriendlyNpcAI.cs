/*
 * DOST NPC YAPAY ZEKASI (MOTOR) - v1.3 (Havuzlama OLMADAN)
 *
 * * DEĞİŞİKLİKLER (v1.3):
 * - ': IPooledObject' arayüzü kaldırıldı.
 * - 'PoolTag' propertysi kaldırıldı.
 * - 'OnObjectSpawn()' metodu kaldırıldı.
 * - Başlatma mantığı ('InitializeVisuals' ve 'GoToWork')
 * zaten 'Initialize' metodu içindeydi (v1.2'deki düzeltme),
 * bu yüzden 'Start()' metodunda da çalışacaktır.
 */

using UnityEngine;

// ': IPooledObject' kaldırıldı
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

    // --- IPooledObject Arayüzü KALDIRILDI ---
    // public string PoolTag { get; set; }
    // public void OnObjectSpawn() { }
    // --- ---

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
        // 1. Veriyi al
        this.npcData = data;
        if (this.npcData == null)
        {
            Debug.LogError("FriendlyNpcAI.Initialize() 'FriendlyNpcData' null olarak çağrıldı.", this);
            gameObject.SetActive(false); 
            return;
        }
        
        // 2. Hedefleri al
        this.homeTransform = home;
        this.workSpotTransform = workSpot;
        this.currentState = State.Idle;

        // 3. Görselleri ayarla
        InitializeVisuals();
        
        // 4. Hareketi başlat
        GoToWork();
    }

    /// <summary>
    /// Veriye göre görselleri ayarlar
    /// </summary>
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
            
        transform.position = Vector3.MoveTowards(
            transform.position, 
            targetPositionOnGround, 
            Time.deltaTime * npcData.speed
        );

        if (Vector3.Distance(transform.position, targetPositionOnGround) < 0.1f)
        {
            ArrivedAtTarget();
        }
    }
    
    private void ArrivedAtTarget()
    {
        if (currentState == State.GoingToWork)
        {
            ReturnHome();
        }
        else if (currentState == State.ReturningHome)
        {
            // Şimdilik basit döngü: Eve varınca tekrar işe git
            GoToWork();
        }
    }

    // --- Komut Metotları ---
    private void GoToWork()
    {
        currentState = State.GoingToWork;
        currentTarget = workSpotTransform;
    }

    private void ReturnHome()
    {
        currentState = State.ReturningHome;
        currentTarget = homeTransform;
    }
    
    // Scale-uyumlu Flip metodu
    private void FlipSprite(bool faceLeft)
    {
        Vector3 baseScale = Vector3.one;
        if (npcData != null)
        {
            baseScale = npcData.scale;
        }

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