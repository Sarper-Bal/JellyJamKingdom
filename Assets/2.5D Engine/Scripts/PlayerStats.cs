/*
 * PLAYER STATS (YÖNETİCİ COMPONENT)
 * * DEĞİŞİKLİKLER (v4 - Hasar):
 * - 'CurrentProjectileDamage' (int) anlık özelliği eklendi.
 * - 'projectileDamageModifiers' (List<StatModifier>) eklendi.
 * - 'RecalculateAllStats' metodu, 'CurrentProjectileDamage'i hesaplayacak şekilde güncellendi.
 * - 'AddProjectileDamageModifier' ve 'RemoveProjectileDamageModifiersFromSource'
 * yardımcı metotları eklendi.
 */

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

    [Header("Component References")]
    [Tooltip("Karakterin görünümünün (Sprite) atanacağı SpriteRenderer. " +
             "Genellikle 'GFX' isimli child objenin üzerindedir.")]
    [SerializeField] private SpriteRenderer characterGfxRenderer;

    // --- ANLIK (RUNTIME) HESAPLANMIŞ STAT'LAR ---
    // (Diğer script'ler bu property'leri okur)
    public int CurrentMaxHealth { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public float CurrentRollForce { get; private set; }
    public float CurrentRollCooldown { get; private set; }
    public float CurrentProjectileSpeed { get; private set; }
    public float CurrentProjectileRadius { get; private set; }
    public float CurrentAttackSpeed { get; private set; }
    public float CurrentAttackRange { get; private set; }
    
    // --- YENİ EKLENEN KISIM BAŞLANGICI (Projectile Damage) ---
    [Tooltip("Anlık mermi hasarı")]
    public int CurrentProjectileDamage { get; private set; }
    // --- YENİ EKLENEN KISIM SONU ---


    // --- MODIFIER (DEĞİŞTİRİCİ) LİSTELERİ ---
    private readonly List<StatModifier> maxHealthModifiers = new List<StatModifier>();
    private readonly List<StatModifier> moveSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> rollForceModifiers = new List<StatModifier>();
    private readonly List<StatModifier> rollCooldownModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileRadiusModifiers = new List<StatModifier>();
    private readonly List<StatModifier> attackSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> attackRangeModifiers = new List<StatModifier>();
    
    // --- YENİ EKLENEN KISIM BAŞLANGICI (Projectile Damage) ---
    private readonly List<StatModifier> projectileDamageModifiers = new List<StatModifier>();
    // --- YENİ EKLENEN KISIM SONU ---

    // Stat'lar değiştiğinde tetiklenecek olay (event).
    public event System.Action OnStatsChanged;

    private void Awake()
    {
        if (baseStatsData == null)
        {
            Debug.LogError("PlayerStats component'ine 'baseStatsData' atanmamış!");
            return;
        }
        
        // Görünümü ayarla
        InitializeVisuals();
        
        // Tüm stat'ları hesapla
        RecalculateAllStats();
    }
    
    /// <summary>
    /// Karakterin görselini baseStatsData'dan (SO) okuyarak ayarlar.
    /// </summary>
    private void InitializeVisuals()
    {
        if (characterGfxRenderer == null)
        {
            Debug.LogError("PlayerStats üzerinde 'Character Gfx Renderer' referansı atanmamış! " +
                           "Lütfen Player prefab'ındaki 'GFX' objesini bu alana sürükleyin.");
            return;
        }

        if (baseStatsData.characterSprite != null)
        {
            characterGfxRenderer.sprite = baseStatsData.characterSprite;
        }
        else
        {
            Debug.LogWarning($"PlayerStatsData ({baseStatsData.name}) üzerinde 'Character Sprite' atanmamış.");
        }
    }

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
        // Float Değerler
        CurrentMoveSpeed = CalculateStat(baseStatsData.moveSpeed, moveSpeedModifiers);
        CurrentRollForce = CalculateStat(baseStatsData.rollForce, rollForceModifiers);
        CurrentRollCooldown = CalculateStat(baseStatsData.rollCooldown, rollCooldownModifiers);
        CurrentProjectileSpeed = CalculateStat(baseStatsData.projectileSpeed, projectileSpeedModifiers);
        CurrentProjectileRadius = CalculateStat(baseStatsData.projectileRadius, projectileRadiusModifiers);
        CurrentAttackSpeed = CalculateStat(baseStatsData.attackSpeed, attackSpeedModifiers);
        CurrentAttackRange = CalculateStat(baseStatsData.attackRange, attackRangeModifiers);

        // Int Değerler (Hesaplama float yapılır, sonra int'e çevrilir)
        CurrentMaxHealth = (int)CalculateStat(baseStatsData.maxHealth, maxHealthModifiers);
        
        // --- YENİ EKLENEN KISIM BAŞLANGICI (Projectile Damage) ---
        CurrentProjectileDamage = (int)CalculateStat(baseStatsData.projectileDamage, projectileDamageModifiers);
        // --- YENİ EKLENEN KISIM SONU ---

        
        // Güvenlik kontrolleri (Negatif değerleri veya sıfırı engellemek için)
        if (CurrentAttackSpeed <= 0) CurrentAttackSpeed = 0.1f;
        if (CurrentAttackRange < 0) CurrentAttackRange = 0;
        if (CurrentProjectileDamage < 0) CurrentProjectileDamage = 0;
        
        // Değişiklikleri diğer script'lere (örn: HealthSystem) haber ver.
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
    
    // --- Stat'a özel Add/Remove metotları (Kullanım kolaylığı için) ---
    
    public void AddMoveSpeedModifier(StatModifier mod) => AddModifier(mod, moveSpeedModifiers);
    public bool RemoveMoveSpeedModifiersFromSource(object source) => RemoveModifiersFromSource(source, moveSpeedModifiers);
    
    public void AddMaxHealthModifier(StatModifier mod) => AddModifier(mod, maxHealthModifiers);
    public bool RemoveMaxHealthModifiersFromSource(object source) => RemoveModifiersFromSource(source, maxHealthModifiers);
    
    public void AddAttackSpeedModifier(StatModifier mod) => AddModifier(mod, attackSpeedModifiers);
    public bool RemoveAttackSpeedModifiersFromSource(object source) => RemoveModifiersFromSource(source, attackSpeedModifiers);

    public void AddAttackRangeModifier(StatModifier mod) => AddModifier(mod, attackRangeModifiers);
    public bool RemoveAttackRangeModifiersFromSource(object source) => RemoveModifiersFromSource(source, attackRangeModifiers);
    
    // --- YENİ EKLENEN KISIM BAŞLANGICI (Projectile Damage) ---
    public void AddProjectileDamageModifier(StatModifier mod) => AddModifier(mod, projectileDamageModifiers);
    public bool RemoveProjectileDamageModifiersFromSource(object source) => RemoveModifiersFromSource(source, projectileDamageModifiers);
    // --- YENİ EKLENEN KISIM SONU ---
}