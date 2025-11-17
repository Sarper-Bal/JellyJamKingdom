/*
 * ÇALIŞMA ALANI ETKİLEŞİMİ
 * GÖREVİ:
 * Bu component, NPC'lerin 'workSpot' olarak atanan objelerin (ağaç, taş vb.)
 * üzerine eklenir. 'NpcHousing' tarafından tetiklenir.
 * DOTween kullanarak basit bir "sallanma" animasyonu oynatır.
 */

using UnityEngine;
using DG.Tweening; // DOTween kütüphanesini kullanıyoruz

public class WorkSpotInteractable : MonoBehaviour
{
    [Header("DOTween Ayarları")]
    [Tooltip("Animasyonun süresi (saniye).")]
    [SerializeField] private float duration = 0.5f;
    
    [Tooltip("Sallanma gücü.")]
    [SerializeField] private float strength = 0.2f;
    
    [Tooltip("Sallanma (titreşim) sayısı.")]
    [SerializeField] private int vibrato = 10;
    
    // DOTween'in aynı anda birden fazla çalışmasını engellemek için
    private bool isInteracting = false;

    /// <summary>
    /// 'NpcHousing' tarafından çağrılır.
    /// DOTween sallanma animasyonunu başlatır.
    /// </summary>
    public void TriggerInteraction()
    {
        // Zaten sallanıyorsa, tekrar başlatma
        if (isInteracting) return;
        
        // DOTween'in optimize DOShakePosition metodunu kullanıyoruz
        isInteracting = true;
        transform.DOShakePosition(duration, strength, vibrato, 90, false, true)
            .OnComplete(() => {
                isInteracting = false; // Animasyon bitince kilidi aç
            });
    }
}