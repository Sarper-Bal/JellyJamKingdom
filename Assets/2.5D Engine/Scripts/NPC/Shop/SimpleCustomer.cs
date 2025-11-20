using UnityEngine;
using DG.Tweening; // Animasyonlar için DOTween kullanıyoruz

namespace IndianOceanAssets.Engine2_5D
{
    public class SimpleCustomer : MonoBehaviour
    {
        [Header("Görsel Ayarlar")]
        [Tooltip("Müşterinin istediği ürünü göstereceği Sprite Renderer (Başının üzerindeki balon ikonu).")]
        [SerializeField] private SpriteRenderer resourceIconRenderer;

        [Tooltip("Müşteri spawn olduğunda yapılacak scale animasyonu süresi.")]
        [SerializeField] private float appearDuration = 0.5f;

        // Müşterinin şu an ne istediği verisi (Market bunu okur)
        public ResourceData RequestedResource { get; private set; }

        private void Awake()
        {
            // Başlangıçta görünmez olsun (Scale 0) - Efektle büyüyecek
            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Müşteriyi başlatır, isteğini görselleştirir ve animasyonla ekrana getirir.
        /// </summary>
        public void Initialize(ResourceData resourceRequest)
        {
            RequestedResource = resourceRequest;

            // 1. İkonu ayarla (Eğer atandıysa)
            if (resourceIconRenderer != null && resourceRequest.icon != null)
            {
                resourceIconRenderer.sprite = resourceRequest.icon;
                
                // İkon da ufak bir gecikmeli animasyonla gelsin
                resourceIconRenderer.transform.localScale = Vector3.zero;
                resourceIconRenderer.transform.DOScale(1f, 0.3f).SetDelay(appearDuration);
            }
            else if (resourceRequest != null)
            {
                // İkon atamadıysan bile konsola bilgi verelim
                // Debug.Log($"Müşteri İsteği: {resourceRequest.resourceName}");
            }

            // 2. Puf diye belirme efekti (Elastic Scale Up)
            transform.DOScale(Vector3.one, appearDuration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Müşteri kuyrukta öne kayarken çağrılır.
        /// </summary>
        public void MoveToSpot(Vector3 targetPosition)
        {
            // Yürüme yerine tatlı bir "zıplama" (Jump) ile kayma efekti
            transform.DOJump(targetPosition, 0.5f, 1, 0.5f).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// Sipariş tamamlandığında çağrılır. Mutlu bir şekilde yok olur.
        /// </summary>
        public void LeaveHappy()
        {
            // İkonu hemen kapat
            if(resourceIconRenderer) resourceIconRenderer.gameObject.SetActive(false);

            // Yukarı zıplayıp küçülerek kaybolma
            transform.DOJump(transform.position, 1f, 1, 0.5f).OnComplete(() =>
            {
                transform.DOScale(Vector3.zero, 0.2f).OnComplete(() =>
                {
                    Destroy(gameObject); // Müşterileri havuzlamadığımız için yok ediyoruz.
                });
            });
        }
    }
}