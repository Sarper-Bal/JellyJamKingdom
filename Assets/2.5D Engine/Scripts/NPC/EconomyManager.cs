using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    #region Singleton
    public static EconomyManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    #endregion

    [Header("Yönetim Ayarları")]
    [Tooltip("Oyun açılır açılmaz ekonomi başlasın mı?")]
    public bool autoStartOnPlay = true;

    // Sistemin durumunu tutan değişken
    public bool IsSystemActive { get; private set; } = false;

    // Diğer scriptlerin dinleyeceği Olaylar (Events)
    public event Action OnEconomyStart;
    public event Action OnEconomyStop;

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartEconomy();
        }
    }

    // --- KONTROL METOTLARI (UI Butonlarına Bağlayabilirsin) ---

    [ContextMenu("Sistemi Başlat")] // Editörde sağ tıklayıp test edebilirsin
    public void StartEconomy()
    {
        if (IsSystemActive) return;

        IsSystemActive = true;
        Debug.Log("<color=green>EconomyManager: SİSTEM BAŞLATILIYOR...</color>");
        
        // Tüm abonelere "Başla" haberi yolla
        OnEconomyStart?.Invoke();
    }

    [ContextMenu("Sistemi Durdur")]
    public void StopEconomy()
    {
        if (!IsSystemActive) return;

        IsSystemActive = false;
        Debug.Log("<color=red>EconomyManager: SİSTEM DURDURULDU.</color>");
        
        // Tüm abonelere "Dur" haberi yolla
        OnEconomyStop?.Invoke();
    }
}