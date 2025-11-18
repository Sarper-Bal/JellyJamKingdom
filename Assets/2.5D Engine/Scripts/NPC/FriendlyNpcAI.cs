/*
 * DOST NPC YAPAY ZEKASI (MOTOR) - v2.1 (Sağlam Pathing + Havuzlu)
 *
 * * DEĞİŞİKLİKLER (v2.1):
 * - ': IPooledNpc' arayüzü eklendi.
 * - 'OnNpcSpawned()' metodu eklendi.
 * - 'Initialize()' metodu 'Activate()' olarak yeniden adlandırıldı.
 * 'NpcHousing' tarafından havuzdan çekilince çağrılacak.
 * - 'OnNpcSpawned()' metodu şimdilik boş, çünkü 'Activate()' metodu
 * 'NpcHousing' (Beyin) tarafından çağrıldığı için daha fazla
 * veriye (hedeflere) ihtiyaç duyuyor.
 * - v2.0'daki "Sağlam Pathing" mantığı ('UpdatePathTarget' vb.)
 * korundu.
 */

using UnityEngine;

// --- DEĞİŞİKLİK BAŞLANGICI (v2.1 - Arayüz) ---
public class FriendlyNpcAI : MonoBehaviour, IPooledNpc
// --- DEĞİŞİKLİK SONU ---
{
    // Event'ler (Değişiklik yok)
    public event System.Action<FriendlyNpcAI> OnArrivedAtWork;
    public event System.Action<FriendlyNpcAI, int> OnArrivedAtHome;

    private enum State { Idle, GoingToWork, ReturningHome }
    
    [Header("Bileşen Referansları (Zorunlu)")]
    [SerializeField] private SpriteRenderer spriteRenderer; 
    
    // Anlık (Runtime) Veriler
    private FriendlyNpcData npcData;
    private Transform homeTransform;
    private Transform workSpotTransform;
    private State currentState = State.Idle;
    private Transform currentTarget;
    private int currentPayloadAmount = 0;

    // Opsiyonel Yol Takibi (Değişiklik yok)
    private NpcPath currentPath = null;    
    private int currentWaypointIndex = -1;   
    private bool isMovingOnPath = false;  
    
    // Optimize mesafe kontrolü (Değişiklik yok)
    private float sqrArrivalDistanceThreshold;
    private const float ARRIVAL_DISTANCE_THRESHOLD = 0.1f;

    // --- DEĞİŞİKLİK BAŞLANGICI (v2.1 - Arayüz Metodu) ---
    /// <summary>
    /// 'NpcPooler' tarafından 'SetActive(true)' yapıldıktan hemen sonra çağrılır.
    /// </summary>
    public void OnNpcSpawned()
    {
        // 'NpcHousing' (Beyin) 'Activate' metodunu çağıracağı için
        // buranın şimdilik boş kalması normaldir.
        currentState = State.Idle; // Harekete geçmeden önce bekle
    }
    // --- DEĞİŞİKLİK SONU ---

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("FriendlyNpcAI: 'Sprite Renderer' alanı Inspector'dan atanmamış!", this);
        }
        sqrArrivalDistanceThreshold = ARRIVAL_DISTANCE_THRESHOLD * ARRIVAL_DISTANCE_THRESHOLD;
    }

    /// <summary>
    /// 'NpcHousing' (Ev) tarafından NPC havuzdan çekildikten
    /// sonra çağrılır. (v2.1 - Adı 'Initialize'dan 'Activate'e değişti)
    /// </summary>
    public void Activate(FriendlyNpcData data, Transform home, Transform workSpot, NpcPath path)
    {
        this.npcData = data;
        if (this.npcData == null)
        {
            Debug.LogError("FriendlyNpcAI.Activate() 'FriendlyNpcData' null olarak çağrıldı.", this);
            gameObject.SetActive(false); 
            return;
        }
        
        this.homeTransform = home;
        this.workSpotTransform = workSpot;
        this.currentPath = path; 
        this.currentState = State.Idle;

        InitializeVisuals();
        GoToWork();
    }

    private void InitializeVisuals()
    {
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
    
    // --- (v2.0 - Sağlam Yol Mantığı) ---
    private void ArrivedAtTarget()
    {
        if (isMovingOnPath)
        {
            UpdatePathTarget();
        }
        else
        {
            State previousState = currentState;
            currentState = State.Idle; 
            if (previousState == State.GoingToWork) { OnArrivedAtWork?.Invoke(this); }
            else if (previousState == State.ReturningHome) { OnArrivedAtHome?.Invoke(this, currentPayloadAmount); }
        }
    }

    private void UpdatePathTarget()
    {
        if (currentState == State.GoingToWork)
        {
            currentWaypointIndex++; 
            if (currentWaypointIndex >= currentPath.waypoints.Length)
            {
                isMovingOnPath = false;
                currentTarget = workSpotTransform;
            }
            else
            {
                currentTarget = currentPath.waypoints[currentWaypointIndex];
            }
        }
        else // (currentState == State.ReturningHome)
        {
            currentWaypointIndex--; 
            if (currentWaypointIndex < 0)
            {
                isMovingOnPath = false;
                currentTarget = homeTransform;
            }
            else
            {
                currentTarget = currentPath.waypoints[currentWaypointIndex];
            }
        }
    }

    // --- Komut Metotları (v2.0 - Yol Mantığı) ---
    public void GoToWork()
    {
        currentPayloadAmount = 0; 
        currentState = State.GoingToWork;
        if (currentPath != null && currentPath.waypoints.Length > 0)
        {
            isMovingOnPath = true;
            currentWaypointIndex = 0; 
            currentTarget = currentPath.waypoints[currentWaypointIndex];
        }
        else
        {
            isMovingOnPath = false;
            currentTarget = workSpotTransform;
        }
    }

    public void ReturnHome(int collectedAmount)
    {
        currentPayloadAmount = collectedAmount;
        currentState = State.ReturningHome;
        if (currentPath != null && currentPath.waypoints.Length > 0)
        {
            isMovingOnPath = true;
            currentWaypointIndex = currentPath.waypoints.Length - 1; 
            currentTarget = currentPath.waypoints[currentWaypointIndex];
        }
        else
        {
            isMovingOnPath = false;
            currentTarget = homeTransform;
        }
    }
    
    public FriendlyNpcData GetNpcData()
    {
        return npcData;
    }
    
    private void FlipSprite(bool faceLeft)
    {
        Vector3 baseScale = Vector3.one;
        if (npcData != null) { baseScale = npcData.scale; }
        if (faceLeft) { transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z); }
        else { transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z); }
    }
}