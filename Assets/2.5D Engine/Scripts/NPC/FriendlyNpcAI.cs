/*
 * DOST NPC MOTORU - v3.1 (ResourceData Destekli)
 * DEĞİŞİKLİKLER:
 * - 'ResourceType' (Enum) kullanımları 'ResourceData' (ScriptableObject) ile değiştirildi.
 * - 'ReturnHome' ve 'OnArrivedAtHome' imzaları güncellendi.
 */

using UnityEngine;

public class FriendlyNpcAI : MonoBehaviour, IPooledNpc
{
    public event System.Action<FriendlyNpcAI> OnArrivedAtWork;
    
    // --- DEĞİŞİKLİK: Event artık ResourceData taşıyor ---
    public event System.Action<FriendlyNpcAI, int, ResourceData> OnArrivedAtHome;
    // ---------------------------------------------------

    private enum State { Idle, GoingToWork, ReturningHome }
    
    [Header("Bileşenler")]
    [SerializeField] private SpriteRenderer spriteRenderer; 
    
    private FriendlyNpcData npcData;
    private Transform homeTransform;
    private Transform workSpotTransform;
    private State currentState = State.Idle;
    private Transform currentTarget;
    
    // --- DEĞİŞİKLİK: Payload Verisi ---
    private int currentPayloadAmount = 0;
    private ResourceData currentPayloadResource = null; // <-- Enum yerine Class referansı
    // ----------------------------------

    private NpcPath currentPath = null;    
    private int currentWaypointIndex = -1;   
    private bool isMovingOnPath = false;  
    
    private float sqrArrivalDistanceThreshold;
    private const float ARRIVAL_DISTANCE_THRESHOLD = 0.1f;

    public void OnNpcSpawned() { currentState = State.Idle; }

    private void Awake()
    {
        if (spriteRenderer == null) Debug.LogError("FriendlyNpcAI: SpriteRenderer eksik!", this);
        sqrArrivalDistanceThreshold = ARRIVAL_DISTANCE_THRESHOLD * ARRIVAL_DISTANCE_THRESHOLD;
    }

    public void Activate(FriendlyNpcData data, Transform home, Transform workSpot, NpcPath path)
    {
        this.npcData = data;
        if (this.npcData == null) { gameObject.SetActive(false); return; }
        
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
        if (npcData.characterSprite != null) spriteRenderer.sprite = npcData.characterSprite;
        transform.localScale = (npcData.scale != Vector3.zero) ? npcData.scale : Vector3.one;
    }
    
    private void Update()
    {
        if (currentState == State.Idle || currentTarget == null || npcData == null) return;
        
        Vector3 targetPos = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
        
        if (targetPos.x > transform.position.x) FlipSprite(false); 
        else if (targetPos.x < transform.position.x) FlipSprite(true); 
            
        transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * npcData.speed);

        if ((transform.position - targetPos).sqrMagnitude < sqrArrivalDistanceThreshold)
        {
            ArrivedAtTarget();
        }
    }
    
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
            
            if (previousState == State.GoingToWork)
            {
                OnArrivedAtWork?.Invoke(this);
            }
            else if (previousState == State.ReturningHome)
            {
                // --- DEĞİŞİKLİK ---
                OnArrivedAtHome?.Invoke(this, currentPayloadAmount, currentPayloadResource);
                // ------------------
            }
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
            else { currentTarget = currentPath.waypoints[currentWaypointIndex]; }
        }
        else 
        {
            currentWaypointIndex--; 
            if (currentWaypointIndex < 0)
            {
                isMovingOnPath = false;
                currentTarget = homeTransform;
            }
            else { currentTarget = currentPath.waypoints[currentWaypointIndex]; }
        }
    }

    public void GoToWork()
    {
        currentPayloadAmount = 0;
        currentPayloadResource = null; // Sıfırla
        currentState = State.GoingToWork;
        SetupPath(true);
    }

    // --- DEĞİŞİKLİK: ResourceData alıyor ---
    public void ReturnHome(int collectedAmount, ResourceData resource)
    {
        currentPayloadAmount = collectedAmount;
        currentPayloadResource = resource;
        currentState = State.ReturningHome;
        SetupPath(false);
    }
    // ---------------------------------------

    private void SetupPath(bool toWork)
    {
        if (currentPath != null && currentPath.waypoints.Length > 0)
        {
            isMovingOnPath = true;
            currentWaypointIndex = toWork ? 0 : currentPath.waypoints.Length - 1;
            currentTarget = currentPath.waypoints[currentWaypointIndex];
        }
        else
        {
            isMovingOnPath = false;
            currentTarget = toWork ? workSpotTransform : homeTransform;
        }
    }
    
    public FriendlyNpcData GetNpcData() { return npcData; }
    
    private void FlipSprite(bool faceLeft)
    {
        Vector3 s = (npcData != null) ? npcData.scale : Vector3.one;
        transform.localScale = new Vector3(faceLeft ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
    }
}