using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel; // ReadOnlyCollection için

// Bu component, Player'ın tüm anlık stat'larını yönetir.
// ScriptableObject'tan (PlayerStatsData) temel verileri alır,
// üzerine anlık modifiyerleri (buff/debuff) ekler ve son değeri hesaplar.
public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [Tooltip("Tüm temel stat'ların çekildiği ScriptableObject")]
    [SerializeField] private PlayerStatsData baseStatsData;

    // --- YENİ EKLENEN KISIM BAŞLANGICI (Referanslar) ---
    [Header("Component References")]
    [Tooltip("Karakterin görünümünün (Sprite) atanacağı SpriteRenderer. " +
             "Genellikle 'GFX' isimli child objenin üzerindedir.")]
    [SerializeField] private SpriteRenderer characterGfxRenderer;
    // --- YENİ EKLENEN KISIM SONU ---


    // --- ANLIK (RUNTIME) HESAPLANMIŞ STAT'LAR ---
    public int CurrentMaxHealth { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public float CurrentRollForce { get; private set; }
    public float CurrentRollCooldown { get; private set; }
    public float CurrentProjectileSpeed { get; private set; }
    public float CurrentProjectileRadius { get; private set; }
    public float CurrentAttackSpeed { get; private set; }
    public float CurrentAttackRange { get; private set; }


    // --- MODIFIER (DEĞİŞTİRİCİ) LİSTELERİ ---
    private readonly List<StatModifier> maxHealthModifiers = new List<StatModifier>();
    private readonly List<StatModifier> moveSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> rollForceModifiers = new List<StatModifier>();
    private readonly List<StatModifier> rollCooldownModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileRadiusModifiers = new List<StatModifier>();
    private readonly List<StatModifier> attackSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> attackRangeModifiers = new List<StatModifier>();

    // Stat'lar değiştiğinde tetiklenecek olay (event).
    public event System.Action OnStatsChanged;

    private void Awake()
    {
        if (baseStatsData == null)
        {
            Debug.LogError("PlayerStats component'ine 'baseStatsData' atanmamış!");
            return;
        }
        
        // --- YENİ EKLENEN KISIM BAŞLANGICI (Görünüm Ayarlama) ---
        // Stat hesaplamalarından önce görünümü ayarla.
        InitializeVisuals();
        // --- YENİ EKLENEN KISIM SONU ---
        
        // Başlangıçta tüm stat'ları temel değerlere göre hesapla.
        RecalculateAllStats();
    }

    // --- YENİ EKLENEN FONKSİYON BAŞLANGICI ---
    /// <summary>
    /// Karakterin görselini baseStatsData'dan (SO) okuyarak ayarlar.
    /// </summary>
    private void InitializeVisuals()
    {
        // Inspector'dan GFX objesinin SpriteRenderer'ı atanmış mı kontrol et.
        if (characterGfxRenderer == null)
        {
            Debug.LogError("PlayerStats üzerinde 'Character Gfx Renderer' referansı atanmamış! " +
                           "Lütfen Player prefab'ındaki 'GFX' objesini bu alana sürükleyin.");
            return;
        }

        // ScriptableObject'ta bir sprite tanımlanmış mı kontrol et.
        if (baseStatsData.characterSprite != null)
        {
            // Sprite'ı ata.
            characterGfxRenderer.sprite = baseStatsData.characterSprite;
        }
        else
        {
            Debug.LogWarning($"PlayerStatsData ({baseStatsData.name}) üzerinde 'Character Sprite' atanmamış. " +
                             "Mevcut sprite korunacak.");
        }
    }
    // --- YENİ EKLENEN FONKSİYON SONU ---

    /// <summary>
    /// Belirli bir temel değere, listedeki tüm modifiyerleri uygular ve son değeri döner.
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
                case StatModType.Flat:
                    finalValue += mod.Value;
                    break;
                case StatModType.PercentAdd:
                    sumPercentAdd += mod.Value;
                    break;
                case StatModType.PercentMult:
                    finalValue *= (1 + mod.Value);
                    break;
            }
        }
        
        finalValue *= (1 + sumPercentAdd);
        return finalValue;
    }
    
    /// <summary>
    /// Tüm stat'ları temel değerlerden başlayarak yeniden hesaplar ve günceller.
    /// </summary>
    private void RecalculateAllStats()
    {
        CurrentMaxHealth = (int)CalculateStat(baseStatsData.maxHealth, maxHealthModifiers);
        CurrentMoveSpeed = CalculateStat(baseStatsData.moveSpeed, moveSpeedModifiers);
        CurrentRollForce = CalculateStat(baseStatsData.rollForce, rollForceModifiers);
        CurrentRollCooldown = CalculateStat(baseStatsData.rollCooldown, rollCooldownModifiers);
        CurrentProjectileSpeed = CalculateStat(baseStatsData.projectileSpeed, projectileSpeedModifiers);
        CurrentProjectileRadius = CalculateStat(baseStatsData.projectileRadius, projectileRadiusModifiers);
        CurrentAttackSpeed = CalculateStat(baseStatsData.attackSpeed, attackSpeedModifiers);
        
        if (CurrentAttackSpeed <= 0)
        {
            CurrentAttackSpeed = 0.1f;
        }
        
        CurrentAttackRange = CalculateStat(baseStatsData.attackRange, attackRangeModifiers);
        if (CurrentAttackRange < 0)
        {
            CurrentAttackRange = 0;
        }
        
        OnStatsChanged?.Invoke();
    }
    

    // --- DIŞARIDAN ERİŞİM (PUBLIC) METOTLARI ---

    /// <summary>
    /// Bir stat'a yeni bir modifiyer ekler.
    /// </summary>
    public void AddModifier(StatModifier mod, List<StatModifier> list)
    {
        list.Add(mod);
        RecalculateAllStats(); // Stat'ları yeniden hesapla
    }

    /// <summary>
    /// Belirli bir kaynaktan (Source) gelen tüm modifiyerleri kaldırır.
    /// </summary>
    public bool RemoveModifiersFromSource(object source, List<StatModifier> list)
    {
        int numRemoved = list.RemoveAll(mod => mod.Source == source);

        if (numRemoved > 0)
        {
            RecalculateAllStats(); // Stat'ları yeniden hesapla
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
}