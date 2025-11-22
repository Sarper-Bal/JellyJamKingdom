/*
 * PLAYER STATS (YÖNETİCİ COMPONENT)
 * * DEĞİŞİKLİKLER (v7 - Opsiyonel GFX):
 * - 'InitializeVisuals()' metodu güncellendi.
 * - 'characterGfxRenderer' referansı atanmamışsa (null ise) artık
 * 'Debug.LogError' VERMEZ.
 * - Eğer bu referans 'null' ise, fonksiyon sessizce 'return' olur.
 * - Bu sayede bu component'i Kule (Tower) gibi GFX'i olmayan objelerde
 * hata almadan kullanabiliriz.
 */

using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel; // ReadOnlyCollection için

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [Tooltip("Tüm temel stat'ların çekildiği ScriptableObject")]
    [SerializeField] private PlayerStatsData baseStatsData;

    // --- DEĞİŞİKLİK: Bu referans artık OPSİYONEL ---
    [Header("Component References (Optional)")]
    [Tooltip("(Opsiyonel) Karakterin görünümünün (Sprite) atanacağı SpriteRenderer. " +
             "Eğer boş bırakılırsa, sprite atama işlemi yapılmaz.")]
    [SerializeField] private SpriteRenderer characterGfxRenderer;
    // --- DEĞİŞİKLİK SONU ---


    // --- ANLIK (RUNTIME) HESAPLANMIŞ STAT'LAR ---
    public int CurrentMaxHealth { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public float CurrentRollForce { get; private set; }
    public float CurrentRollCooldown { get; private set; }
    public float CurrentProjectileSpeed { get; private set; }
    public float CurrentProjectileRadius { get; private set; }
    public float CurrentAttackSpeed { get; private set; }
    public float CurrentAttackRange { get; private set; }
    public int CurrentProjectileDamage { get; private set; }
    public bool CurrentCanFireWhileMoving { get; private set; }
    public int CurrentProjectilesPerShot { get; private set; }
    public float CurrentBurstFireDelay { get; private set; }

    // --- MODIFIER (DEĞİŞTİRİCİ) LİSTELERİ ---
    private readonly List<StatModifier> maxHealthModifiers = new List<StatModifier>();
    private readonly List<StatModifier> moveSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> rollForceModifiers = new List<StatModifier>();
    private readonly List<StatModifier> rollCooldownModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileRadiusModifiers = new List<StatModifier>();
    private readonly List<StatModifier> attackSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> attackRangeModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileDamageModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectilesPerShotModifiers = new List<StatModifier>();
    private readonly List<StatModifier> burstFireDelayModifiers = new List<StatModifier>();

    // Stat'lar değiştiğinde tetiklenecek olay (event).
    public event System.Action OnStatsChanged;

    private void Awake()
    {
        if (baseStatsData == null)
        {
            Debug.LogError("PlayerStats component'ine 'baseStatsData' atanmamış!");
            return;
        }
        
        // Görünümü ayarla (eğer atanmışsa)
        InitializeVisuals();
        
        // Tüm stat'ları hesapla
        RecalculateAllStats();
    }
    
    // --- DEĞİŞİKLİK BAŞLANGICI: 'InitializeVisuals' Artık Opsiyonel ---
    /// <summary>
    /// (Opsiyonel) Karakterin görselini baseStatsData'dan (SO) okuyarak ayarlar.
    /// Eğer 'characterGfxRenderer' atanmamışsa hiçbir şey yapmaz.
    /// </summary>
    private void InitializeVisuals()
    {
        // 1. Opsiyonel kontrol:
        // Eğer Inspector'da bu alana bir SpriteRenderer atanmamışsa,
        // bu component'in (örn: bir Kule) görselini stat'lardan yönetmek
        // istemiyoruz demektir. Hata vermeden sessizce çık.
        if (characterGfxRenderer == null)
        {
            return; // Bu artık bir hata değil, beklenen bir durum.
        }

        // 2. Renderer Atanmışsa (örn: Player):
        // ScriptableObject'ta (baseStatsData) bir sprite tanımlanmış mı kontrol et.
        if (baseStatsData.characterSprite != null)
        {
            // Sprite'ı ata.
            characterGfxRenderer.sprite = baseStatsData.characterSprite;
        }
        else
        {
            // Renderer atanmış AMA data asset'inde sprite yok.
            // Bu bir uyarı olmalı (hata değil).
            Debug.LogWarning($"PlayerStatsData ({baseStatsData.name}) üzerinde 'Character Sprite' atanmamış, " +
                             $"ancak '{gameObject.name}' objesi bir GFX Renderer bekliyor. Mevcut sprite korunacak.");
        }
    }
    // --- DEĞİŞİKLİK SONU ---

    /// <summary>
    /// Belirli bir temel değere (float), listedeki tüm modifiyerleri uygular ve son değeri döner.
    /// </summary>
    private float CalculateStat(float baseValue, List<StatModifier> modifiers)
    {
        float finalValue = baseValue;
        float sumPercentAdd = 0; 
        modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
        foreach (var mod in modifiers)
        {
            switch (mod.Type)
            {
                case StatModType.Flat: finalValue += mod.Value; break;
                case StatModType.PercentAdd: sumPercentAdd += mod.Value; break;
                case StatModType.PercentMult: finalValue *= (1 + mod.Value); break;
            }
        }
        finalValue *= (1 + sumPercentAdd);
        return finalValue;
    }
    
    /// <summary>
    /// Tüm stat'ları ve davranış seçeneklerini temel değerlerden başlayarak yeniden hesaplar.
    /// </summary>
    private void RecalculateAllStats()
    {
        // Float Değerler
        CurrentMoveSpeed = CalculateStat(baseStatsData.moveSpeed, moveSpeedModifiers);
        CurrentRollForce = CalculateStat(baseStatsData.rollForce, rollForceModifiers);
        CurrentRollCooldown = CalculateStat(baseStatsData.rollCooldown, rollCooldownModifiers);
        CurrentProjectileSpeed = CalculateStat(baseStatsData.projectileSpeed, projectileSpeedModifiers);
        CurrentProjectileRadius = CalculateStat(baseStatsData.projectileRadius, projectileRadiusModifiers);
        CurrentAttackSpeed = CalculateStat(baseStatsData.attackSpeed, attackSpeedModifiers);
        CurrentAttackRange = CalculateStat(baseStatsData.attackRange, attackRangeModifiers);
        CurrentBurstFireDelay = CalculateStat(baseStatsData.burstFireDelay, burstFireDelayModifiers);

        // Int Değerler
        CurrentMaxHealth = (int)CalculateStat(baseStatsData.maxHealth, maxHealthModifiers);
        CurrentProjectileDamage = (int)CalculateStat(baseStatsData.projectileDamage, projectileDamageModifiers);
        CurrentProjectilesPerShot = (int)CalculateStat(baseStatsData.projectilesPerShot, projectilesPerShotModifiers);

        // Bool (Seçenek) Değerler
        CurrentCanFireWhileMoving = baseStatsData.canFireWhileMoving;
        
        // Güvenlik kontrolleri
        if (CurrentAttackSpeed <= 0) CurrentAttackSpeed = 0.1f;
        if (CurrentAttackRange < 0) CurrentAttackRange = 0;
        if (CurrentProjectileDamage < 0) CurrentProjectileDamage = 0;
        if (CurrentProjectilesPerShot < 1) CurrentProjectilesPerShot = 1;
        if (CurrentBurstFireDelay < 0.01f) CurrentBurstFireDelay = 0.01f;
        
        // Değişiklikleri diğer script'lere haber ver
        OnStatsChanged?.Invoke();
    }
    
    // --- DIŞARIDAN ERİŞİM (PUBLIC) METOTLARI ---

    public void AddModifier(StatModifier mod, List<StatModifier> list)
    {
        list.Add(mod);
        RecalculateAllStats();
    }

    public bool RemoveModifiersFromSource(object source, List<StatModifier> list)
    {
        int numRemoved = list.RemoveAll(mod => mod.Source == source);
        if (numRemoved > 0)
        {
            RecalculateAllStats();
            return true;
        }
        return false;
    }
    
    // --- Stat'a özel Add/Remove metotları ---
    
    public void AddMoveSpeedModifier(StatModifier mod) => AddModifier(mod, moveSpeedModifiers);
    public bool RemoveMoveSpeedModifiersFromSource(object source) => RemoveModifiersFromSource(source, moveSpeedModifiers);
    
    public void AddMaxHealthModifier(StatModifier mod) => AddModifier(mod, maxHealthModifiers);
    public bool RemoveMaxHealthModifiersFromSource(object source) => RemoveModifiersFromSource(source, maxHealthModifiers);
    
    public void AddAttackSpeedModifier(StatModifier mod) => AddModifier(mod, attackSpeedModifiers);
    public bool RemoveAttackSpeedModifiersFromSource(object source) => RemoveModifiersFromSource(source, attackSpeedModifiers);

    public void AddAttackRangeModifier(StatModifier mod) => AddModifier(mod, attackRangeModifiers);
    public bool RemoveAttackRangeModifiersFromSource(object source) => RemoveModifiersFromSource(source, attackRangeModifiers);
    
    public void AddProjectileDamageModifier(StatModifier mod) => AddModifier(mod, projectileDamageModifiers);
    public bool RemoveProjectileDamageModifiersFromSource(object source) => RemoveModifiersFromSource(source, projectileDamageModifiers);
    
    public void AddProjectilesPerShotModifier(StatModifier mod) => AddModifier(mod, projectilesPerShotModifiers);
    public bool RemoveProjectilesPerShotModifiersFromSource(object source) => RemoveModifiersFromSource(source, projectilesPerShotModifiers);
    
    public void AddBurstFireDelayModifier(StatModifier mod) => AddModifier(mod, burstFireDelayModifiers);
    public bool RemoveBurstFireDelayModifiersFromSource(object source) => RemoveModifiersFromSource(source, burstFireDelayModifiers);

    public void Initialize(PlayerStatsData newData)
    {
        if (newData == null)
        {
            Debug.LogWarning("PlayerStats: Initialize için boş veri gönderildi!");
            return;
        }

        // 1. Veriyi değiştir
        this.baseStatsData = newData;

        // 2. Görseli yenile (Eğer yeni datada farklı bir sprite varsa)
        InitializeVisuals();

        // 3. Statları yeni veriye göre tekrar hesapla
        RecalculateAllStats();
        
        Debug.Log($"PlayerStats: Karakter verisi '{newData.name}' olarak güncellendi.");
    }
}