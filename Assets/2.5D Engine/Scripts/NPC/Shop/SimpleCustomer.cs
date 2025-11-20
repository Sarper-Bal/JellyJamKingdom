using UnityEngine;
using DG.Tweening;

namespace IndianOceanAssets.Engine2_5D
{
    public class SimpleCustomer : MonoBehaviour
    {
        [Header("Görsel Ayarlar")]
        [SerializeField] private SpriteRenderer resourceIconRenderer;
        [SerializeField] private float appearDuration = 0.5f;

        // Müşterinin verisi
        public ResourceData RequestedResource { get; private set; }

        /// <summary>
        /// Havuzdan her çıktığında (Spawn) CustomerPooler tarafından çağrılır.
        /// </summary>
        public void OnSpawnFromPool()
        {
            // 1. Ölçeği sıfırla (Animasyon için hazırlık)
            transform.localScale = Vector3.zero;
            
            // 2. Olası eski animasyonları temizle (Güvenlik)
            transform.DOKill();
            if(resourceIconRenderer) resourceIconRenderer.transform.DOKill();
        }

        public void Initialize(ResourceData resourceRequest)
        {
            RequestedResource = resourceRequest;

            // İkon Görünümü
            if (resourceIconRenderer != null && resourceRequest.icon != null)
            {
                resourceIconRenderer.sprite = resourceRequest.icon;
                resourceIconRenderer.gameObject.SetActive(true);
                
                // İkon animasyonu
                resourceIconRenderer.transform.localScale = Vector3.zero;
                resourceIconRenderer.transform.DOScale(1f, 0.3f).SetDelay(appearDuration);
            }

            // Karakter Belirme Animasyonu (Elastic Pop-up)
            transform.DOScale(Vector3.one, appearDuration).SetEase(Ease.OutBack);
        }

        public void MoveToSpot(Vector3 targetPosition)
        {
            // Zıplayarak ilerle
            transform.DOJump(targetPosition, 0.5f, 1, 0.5f).SetEase(Ease.OutQuad);
        }

        public void LeaveHappy()
        {
            if(resourceIconRenderer) resourceIconRenderer.gameObject.SetActive(false);

            // Mutlu ayrılma efekti
            transform.DOJump(transform.position, 1f, 1, 0.5f).OnComplete(() =>
            {
                transform.DOScale(Vector3.zero, 0.2f).OnComplete(() =>
                {
                    // DEĞİŞİKLİK: Özel CustomerPooler'a dön
                    ReturnSelfToPool();
                });
            });
        }

        private void ReturnSelfToPool()
        {
            transform.DOKill();
            
            // Singleton kontrolü (Sahne kapatılırken hata vermemesi için)
            if (CustomerPooler.Instance != null)
            {
                CustomerPooler.Instance.ReturnCustomer(this);
            }
            else
            {
                Destroy(gameObject); // Pooler yoksa yok et
            }
        }
    }
}