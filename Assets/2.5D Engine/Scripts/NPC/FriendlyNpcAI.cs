/*
 * DOST NPC YAPAY ZEKASI (MOTOR) - v1.9 (Opsiyonel Yol Takibi)
 *
 * * DEĞİŞİKLİKLER (v1.9):
 * - Artık opsiyonel bir 'NpcPath' altyapısını destekliyor.
 * - YENİ ALANLAR: 'currentPath', 'currentWaypointIndex', 'isMovingOnPath'.
 * - 'Initialize()' metodu artık 4. parametre olarak 'NpcPath' alıyor.
 * - 'GoToWork()' metodu güncellendi:
 * - Eğer 'path' varsa, 'currentTarget'ı yolun ilk noktası (index 0) yapar.
 * - Eğer 'path' yoksa, 'currentTarget'ı 'workSpot' yapar (eski sistem).
 * - 'ReturnHome()' metodu güncellendi:
 * - Eğer 'path' varsa, 'currentTarget'ı yolun SON noktası yapar (tersine).
 * - Eğer 'path' yoksa, 'currentTarget'ı 'home' yapar (eski sistem).
 * - 'ArrivedAtTarget()' metodu güncellendi:
 * - Artık bir ara noktaya mı (waypoint) yoksa asıl hedefe mi
 * vardığını kontrol ediyor.
 * - Ara noktaya vardıysa, 'currentWaypointIndex'i artırır/azaltır
 * ve bir sonraki noktayı hedefler.
 * - Yol bittiğinde, asıl hedefi ('workSpot' veya 'home') hedefler.
 * - Asıl hedefe vardığında event tetikler (eski sistem).
 */

using UnityEngine;

public class FriendlyNpcAI : MonoBehaviour
{
    // Event'ler (v1.8 - Değişiklik yok)
    public event System.Action<FriendlyNpcAI> OnArrivedAtWork;
    public event System.Action<FriendlyNpcAI, int> OnArrivedAtHome;

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
    private Transform currentTarget; // O an hareket edilen hedef
    private int currentPayloadAmount = 0;

    // --- DEĞİŞİKLİK BAŞLANGICI (v1.9 - Yol Takibi) ---
    private NpcPath currentPath = null;    // Atanmışsa, takip edilecek yol
    private int currentWaypointIndex = -1;   // Yoldaki mevcut index
    private bool isMovingOnPath = false;  // Bir ara noktaya mı gidiyor?
    // --- DEĞİŞİKLİK SONU ---
    
    // Optimize mesafe kontrolü (v1.6 - Değişiklik yok)
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

    // --- DEĞİŞİKLİK BAŞLANGICI (v1.9 - Yeni Initialize) ---
    /// <summary>
    /// 'NpcHousing' (Ev) tarafından NPC 'Instantiate' edildikten
    /// hemen sonra çağrılır.
    /// </summary>
    /// <param name="path"> (Opsiyonel) Takip edilecek yol. 'null' ise direkt gider.</param>
    public void Initialize(FriendlyNpcData data, Transform home, Transform workSpot, NpcPath path)
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
        this.currentPath = path; // Opsiyonel yolu sakla
        this.currentState = State.Idle;

        InitializeVisuals();
        GoToWork(); // İlk komut
    }
    // --- DEĞİŞİKLİK SONU ---

    private void InitializeVisuals()
    {
        // ... (Değişiklik yok)
        if (npcData == null || spriteRenderer == null) return;
        if (npcData.characterSprite != null) { spriteRenderer.sprite = npcData.characterSprite; }
        if (npcData.scale != Vector3.one && npcData.scale != Vector3.zero) { transform.localScale = npcData.scale; }
        else { transform.localScale = Vector3.one; }
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

        if ((transform.position - targetPositionOnGround).sqrMagnitude < sqrArrivalDistanceThreshold)
        {
            ArrivedAtTarget();
        }
    }
    
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.9 - Yol Mantığı) ---
    /// <summary>
    /// Bir hedefe ulaşıldığında çağrılır.
    /// Bu hedef bir ara nokta (waypoint) veya asıl hedef (ev/iş) olabilir.
    /// </summary>
    private void ArrivedAtTarget()
    {
        // 1. Bir ara noktayı mı takip ediyorduk?
        if (isMovingOnPath)
        {
            if (currentState == State.GoingToWork)
            {
                // İşe giderken bir ara noktaya vardık
                currentWaypointIndex++; // Bir sonraki indekse geç
                
                // Takip edecek daha fazla nokta var mı?
                if (currentWaypointIndex < currentPath.waypoints.Length)
                {
                    // Evet, bir sonraki noktayı hedefle
                    currentTarget = currentPath.waypoints[currentWaypointIndex];
                }
                else
                {
                    // Hayır, yol bitti. Artık asıl 'iş' hedefine git
                    isMovingOnPath = false;
                    currentTarget = workSpotTransform;
                }
            }
            else // (currentState == State.ReturningHome)
            {
                // Eve dönerken bir ara noktaya vardık
                currentWaypointIndex--; // Bir önceki indekse geç (tersine)
                
                // Takip edecek daha fazla nokta var mı?
                if (currentWaypointIndex >= 0)
                {
                    // Evet, bir önceki noktayı hedefle
                    currentTarget = currentPath.waypoints[currentWaypointIndex];
                }
                else
                {
                    // Hayır, yol bitti. Artık asıl 'ev' hedefine git
                    isMovingOnPath = false;
                    currentTarget = homeTransform;
                }
            }
        }
        // 2. Bir ara noktayı değil, asıl hedefi takip ediyorduk
        else
        {
            // Asıl hedefe vardık.
            State previousState = currentState;
            currentState = State.Idle; // Hareketi durdur
            
            if (previousState == State.GoingToWork)
            {
                OnArrivedAtWork?.Invoke(this);
            }
            else if (previousState == State.ReturningHome)
            {
                OnArrivedAtHome?.Invoke(this, currentPayloadAmount);
            }
        }
    }

    // --- Komut Metotları (v1.9 - Yol Mantığı) ---
    
    public void GoToWork()
    {
        currentPayloadAmount = 0; 
        currentState = State.GoingToWork;

        // Atanmış bir yol var mı?
        if (currentPath != null && currentPath.waypoints.Length > 0)
        {
            isMovingOnPath = true;
            currentWaypointIndex = 0; // Baştan başla
            currentTarget = currentPath.waypoints[currentWaypointIndex];
        }
        else
        {
            // Yol yok, direkt hedefe git
            isMovingOnPath = false;
            currentTarget = workSpotTransform;
        }
    }

    public void ReturnHome(int collectedAmount)
    {
        currentPayloadAmount = collectedAmount;
        currentState = State.ReturningHome;

        // Atanmış bir yol var mı?
        if (currentPath != null && currentPath.waypoints.Length > 0)
        {
            isMovingOnPath = true;
            currentWaypointIndex = currentPath.waypoints.Length - 1; // Sondan başla
            currentTarget = currentPath.waypoints[currentWaypointIndex];
        }
        else
        {
            // Yol yok, direkt hedefe git
            isMovingOnPath = false;
            currentTarget = homeTransform;
        }
    }
    // --- DEĞİŞİKLİK SONU ---
    
    public FriendlyNpcData GetNpcData()
    {
        return npcData;
    }
    
    private void FlipSprite(bool faceLeft)
    {
        // ... (Değişiklik yok)
        Vector3 baseScale = Vector3.one;
        if (npcData != null) { baseScale = npcData.scale; }
        if (faceLeft) { transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z); }
        else { transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z); }
    }
}