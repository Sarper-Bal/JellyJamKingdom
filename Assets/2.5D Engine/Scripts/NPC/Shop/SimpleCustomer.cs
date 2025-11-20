using UnityEngine;
using DG.Tweening;

public class SimpleCustomer : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    [SerializeField] private SpriteRenderer resourceIconRenderer;
    [SerializeField] private float appearDuration = 0.5f;

    public ResourceData RequestedResource { get; private set; }

    public void OnSpawnFromPool()
    {
        transform.localScale = Vector3.zero;
        transform.DOKill();
        if(resourceIconRenderer) resourceIconRenderer.transform.DOKill();
    }

    public void Initialize(ResourceData resourceRequest)
    {
        RequestedResource = resourceRequest;
        if (resourceIconRenderer != null && resourceRequest.icon != null)
        {
            resourceIconRenderer.sprite = resourceRequest.icon;
            resourceIconRenderer.gameObject.SetActive(true);
            resourceIconRenderer.transform.localScale = Vector3.zero;
            resourceIconRenderer.transform.DOScale(1f, 0.3f).SetDelay(appearDuration);
        }
        transform.DOScale(Vector3.one, appearDuration).SetEase(Ease.OutBack);
    }

    public void MoveToSpot(Vector3 targetPosition)
    {
        transform.DOJump(targetPosition, 0.5f, 1, 0.5f).SetEase(Ease.OutQuad);
    }

    public void LeaveHappy()
    {
        if(resourceIconRenderer) resourceIconRenderer.gameObject.SetActive(false);
        transform.DOJump(transform.position, 1f, 1, 0.5f).OnComplete(() =>
        {
            transform.DOScale(Vector3.zero, 0.2f).OnComplete(() =>
            {
                ReturnSelfToPool();
            });
        });
    }

    private void ReturnSelfToPool()
    {
        transform.DOKill();
        if (CustomerPooler.Instance != null) CustomerPooler.Instance.ReturnCustomer(this);
        else Destroy(gameObject);
    }
}