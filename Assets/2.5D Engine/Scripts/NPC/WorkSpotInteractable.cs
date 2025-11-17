/*
 * ÇALIŞMA ALANI ETKİLEŞİMİ - v1.1
 * GÖREVİ:
 * NPC'lerin 'workSpot' olarak atanan objelerin (ağaç, taş vb.)
 * üzerine eklenir.
 *
 * * DEĞİŞİKLİKLER (v1.1):
 * - YENİ ALAN: 'interactionPoint' (Transform) eklendi. NPC'ler artık
 * bu objenin merkezine değil, bu noktaya gelecekler.
 * - YENİ ALAN: 'workDuration' (float) eklendi. Bekleme süresi
 * artık 'NpcHousing'de değil, doğrudan bu objede tanımlanıyor.
 * Bu, her 'workSpot'un farklı bekleme süresine sahip olabilmesini sağlar.
 */

using UnityEngine;
using DG.Tweening; // DOTween kütüphanesi

public class WorkSpotInteractable : MonoBehaviour
{
    // --- DEĞİŞİKLİK BAŞLANGICI (v1.1) ---
    [Header("Davranış Ayarları")]
    [Tooltip("NPC'nin etkileşime girmek için duracağı tam nokta. " +
             "Boş bırakılırsa, bu objenin merkezi kullanılır.")]
    public Transform interactionPoint; // <-- YENİ EKLENDİ (Hedef Nokta)

    [Tooltip("NPC'nin bu noktaya vardığında kaç saniye bekleyeceği.")]
    public float workDuration = 5.0f; // <-- YENİ EKLENDİ (Bekleme Süresi)
    // --- DEĞİŞİKLİK SONU ---
    
    [Header("DOTween Ayarları")]
    [Tooltip("Animasyonun süresi (saniye).")]
    [SerializeField] private float duration = 0.5f;
    [Tooltip("Sallanma gücü.")]
    [SerializeField] private float strength = 0.2f;
    [Tooltip("Sallanma (titreşim) sayısı.")]
    [SerializeField] private int vibrato = 10;
    
    private bool isInteracting = false;

    /// <summary>
    /// 'NpcHousing' tarafından çağrılır.
    /// DOTween sallanma animasyonunu başlatır.
    /// </summary>
    public void TriggerInteraction()
    {
        if (isInteracting) return;
        
        isInteracting = true;
        // Animasyonu objenin kendisine uygula
        transform.DOShakePosition(duration, strength, vibrato, 90, false, true)
            .OnComplete(() => {
                isInteracting = false; 
            });
    }
}