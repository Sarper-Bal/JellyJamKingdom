/*
 * SIMPLE CUSTOMER - FIXED (Rejection Visuals)
 * EKLENEN:
 * - 'PlayRejectionAnim': Müşteri reddedildiğinde balonunu sallar ve kırmızı yapar.
 */

using UnityEngine;
using DG.Tweening; 

public class SimpleCustomer : MonoBehaviour, IPooledObject
{
    [Header("Görsel Referanslar")]
    [Tooltip("Müşterinin istediği ürünü gösterecek Sprite Renderer.")]
    [SerializeField] private SpriteRenderer requestIconRenderer;

    // Runtime
    public ResourceData RequestedResource { get; private set; }
    public string PoolTag { get; set; }
    
    private Vector3 originalIconScale;

    private void Awake()
    {
        if (requestIconRenderer != null) originalIconScale = requestIconRenderer.transform.localScale;
        else originalIconScale = Vector3.one;
    }

    public void OnObjectSpawn()
    {
        HideRequestBubble();
        // Rengi sıfırla (Eğer önceden kızardıysa)
        if(requestIconRenderer != null) requestIconRenderer.color = Color.white;
    }

    public void Initialize(ResourceData resource)
    {
        RequestedResource = resource;
        HideRequestBubble();
    }

    public void ShowRequestBubble()
    {
        if (requestIconRenderer == null || RequestedResource == null || RequestedResource.icon == null) return;
        if (requestIconRenderer.gameObject.activeSelf) return;

        requestIconRenderer.sprite = RequestedResource.icon;
        requestIconRenderer.color = Color.white; // Rengi beyaz yap
        requestIconRenderer.gameObject.SetActive(true);

        requestIconRenderer.transform.localScale = Vector3.zero; 
        requestIconRenderer.transform.DOScale(originalIconScale, 0.3f).SetEase(Ease.OutBack);
    }

    // --- YENİ: REDDEDİLME ANİMASYONU ---
    public void PlayRejectionAnim()
    {
        if (requestIconRenderer != null)
        {
            // İkonu kırmızı yap ve sallar
            requestIconRenderer.color = Color.red;
            requestIconRenderer.transform.DOShakePosition(0.5f, 0.1f, 10, 90, false, true);
        }
    }
    // ------------------------------------

    public void HideRequestBubble()
    {
        if (requestIconRenderer != null) requestIconRenderer.gameObject.SetActive(false);
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