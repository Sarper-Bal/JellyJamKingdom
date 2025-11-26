using UnityEngine;
using TMPro; // TextMeshPro kullanacağız
using DG.Tweening; 

public class FloatingText : MonoBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private TextMeshPro textMesh; // World Space TMP
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private float moveDistance = 1.5f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    private void Awake()
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
    }

    public void Init(string content, Color color)
    {
        if (textMesh != null)
        {
            textMesh.text = content;
            textMesh.color = color;
            
            // Başlangıçta görünür yap
            textMesh.alpha = 1f; 

            // --- ANİMASYONLAR ---
            
            // 1. Yukarı Hareket
            transform.DOMoveY(transform.position.y + moveDistance, moveDuration)
                .SetEase(easeType);

            // 2. Şeffaflaşma (Fade Out) ve Yok Olma
            textMesh.DOFade(0, moveDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => Destroy(gameObject)); // İş bitince kendini yok et
        }
        else
        {
            // TMP yoksa direkt yok et
            Destroy(gameObject, 1f);
        }
    }
}