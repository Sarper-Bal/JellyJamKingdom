/*
 * MÜŞTERİ MOTORU - v1.1 (Market Bağlantısı)
 * DEĞİŞİKLİKLER:
 * - 'Initialize' metodu 'shopTransform' yerine 'MarketController' alıyor.
 * - 'OnArrivedAtShop' event'ini tetiklemeden önce, 'MarketController'a kendini kaydettirmesi gerekiyor.
 * - Müşterinin duracağı nokta artık 'shopTransform' değil, 'targetMarket.GetInteractionPoint()' olmalı.
 */

using UnityEngine;

public class CustomerAI : MonoBehaviour
{
    public event System.Action<CustomerAI> OnArrivedAtShop;
    public event System.Action<CustomerAI> OnArrivedAtExit;

    private enum State { Idle, GoingToShop, Leaving }
    
    [SerializeField] private SpriteRenderer spriteRenderer;

    public CustomerData data;
    private MarketController targetMarket; // <-- Market'in kendisi
    private Transform shopInteractionPoint; // <-- Markette duracağı nokta
    private Transform exitTransform;
    private State currentState = State.Idle;
    
    private float sqrArrivalDistanceThreshold = 0.01f; 

    private void Awake() { /* ... */ }

    /// <summary>
    /// Manager tarafından çağrılır (Activate).
    /// </summary>
    public void Initialize(CustomerData customerData, MarketController market, Transform exit)
    {
        data = customerData;
        targetMarket = market;
        exitTransform = exit;
        
        // Müşterinin duracağı yeri Market'ten iste
        shopInteractionPoint = targetMarket.GetInteractionPoint();
        
        if(spriteRenderer != null && data.characterSprite != null)
            spriteRenderer.sprite = data.characterSprite;
        
        transform.localScale = data.scale;

        GoToShop();
    }

    private void Update()
    {
        if (currentState == State.Idle || data == null) return;

        Transform currentTarget = (currentState == State.GoingToShop) ? shopInteractionPoint : exitTransform;
        
        // ... (Hareket mantığı) ...
        Vector3 targetPos = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
        
        if (targetPos.x > transform.position.x) FlipSprite(false);
        else if (targetPos.x < transform.position.x) FlipSprite(true);

        transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * data.speed);

        if ((transform.position - targetPos).sqrMagnitude < sqrArrivalDistanceThreshold)
        {
            Arrived();
        }
    }

    private void Arrived()
    {
        if (currentState == State.GoingToShop)
        {
            currentState = State.Idle;
            // Markete vardığını bildir ve hizmeti başlat
            targetMarket.AttendToCustomer(this); // <-- Kendini Markete kaydet
            // Artık event'i kullanmıyoruz, çünkü Market'i direkt tetikliyoruz
        }
        else if (currentState == State.Leaving)
        {
            currentState = State.Idle;
            OnArrivedAtExit?.Invoke(this); // Çıkış Manager'a bildirilir
        }
    }

    public void GoToShop()
    {
        currentState = State.GoingToShop;
    }

    public void LeaveShop()
    {
        currentState = State.Leaving;
    }

    private void FlipSprite(bool faceLeft)
    {
        Vector3 s = data.scale;
        transform.localScale = new Vector3(faceLeft ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
    }
}