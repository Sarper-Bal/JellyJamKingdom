/*
 * SIMPLE CUSTOMER - FIXED (Scale Correction)
 * DÜZELTME:
 * - İstek balonunun boyutu artık sabit (1,1,1) olmaya zorlanmıyor.
 * - 'Awake' metodunda senin Inspector'da ayarladığın boyut (originalIconScale) kaydediliyor.
 * - Animasyon sırasında bu kaydedilen boyut kullanılıyor.
 */

using UnityEngine;
using DG.Tweening; 

public class SimpleCustomer : MonoBehaviour, IPooledObject
{
    [Header("Görsel Referanslar")]
    [Tooltip("Müşterinin istediği ürünü gösterecek Sprite Renderer (Başının üstündeki).")]
    [SerializeField] private SpriteRenderer requestIconRenderer;

    // Runtime
    public ResourceData RequestedResource { get; private set; }
    public string PoolTag { get; set; }
    
    // --- YENİ: Orijinal Boyut Hafızası ---
    private Vector3 originalIconScale;

    private void Awake()
    {
        // Oyun başında, senin ayarladığın boyutu kaydet
        if (requestIconRenderer != null)
        {
            originalIconScale = requestIconRenderer.transform.localScale;
        }
        else
        {
            originalIconScale = Vector3.one; // Güvenlik
        }
    }

    public void OnObjectSpawn()
    {
        // Havuzdan çıkarken balonu gizle
        HideRequestBubble();
    }

    public void Initialize(ResourceData resource)
    {
        RequestedResource = resource;
        HideRequestBubble();
    }

    // --- İSTEK BALONU KONTROLÜ ---
    public void ShowRequestBubble()
    {
        if (requestIconRenderer == null || RequestedResource == null || RequestedResource.icon == null) return;

        // Eğer zaten açıksa tekrar açma
        if (requestIconRenderer.gameObject.activeSelf) return;

        requestIconRenderer.sprite = RequestedResource.icon;
        requestIconRenderer.gameObject.SetActive(true);

        // "Pop" Animasyonu
        requestIconRenderer.transform.localScale = Vector3.zero; // 0'dan başla
        
        // --- DÜZELTME BURADA: Vector3.one yerine originalIconScale kullanıyoruz ---
        requestIconRenderer.transform.DOScale(originalIconScale, 0.3f).SetEase(Ease.OutBack);
        // --------------------------------------------------------------------------
    }

    public void HideRequestBubble()
    {
        if (requestIconRenderer != null)
        {
            requestIconRenderer.gameObject.SetActive(false);
        }
    }

    public void MoveToSpot(Vector3 targetPos)
    {
        transform.DOMove(targetPos, 1.0f).SetEase(Ease.Linear);
    }

    public void LeaveHappy()
    {
        HideRequestBubble();
        gameObject.SetActive(false);
    }
}