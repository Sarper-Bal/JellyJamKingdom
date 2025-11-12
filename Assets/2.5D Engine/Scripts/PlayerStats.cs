/*
 * PLAYER STATS (YÖNETİCİ COMPONENT)
 * * DEĞİŞİKLİKLER (v6 - Burst Fire):
 * - 'CurrentProjectilesPerShot' (int) anlık özelliği eklendi.
 * - 'CurrentBurstFireDelay' (float) anlık özelliği eklendi.
 * - Bu iki stat için modifier (değiştirici) listeleri eklendi.
 * - 'RecalculateAllStats' metodu, bu iki yeni stat'ı hesaplayacak şekilde güncellendi.
 * - Yeni stat'lar için 'Add/Remove' yardımcı metotları eklendi.
 */

using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel; // ReadOnlyCollection için

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [Tooltip("Tüm temel stat'ların çekildiği ScriptableObject")]
    [SerializeField] private PlayerStatsData baseStatsData;

    [Header("Component References")]
    [Tooltip("Karakterin görünümünün (Sprite) atanacağı SpriteRenderer. ")]
    [SerializeField] private SpriteRenderer characterGfxRenderer;

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
    
    // --- YENİ EKLENEN KISIM BAŞLANGICI (Burst Fire) ---
    [Tooltip("Anlık olarak bir atışta kaç mermi atılacağı.")]
    public int CurrentProjectilesPerShot { get; private set; }
    [Tooltip("Anlık olarak burst mermileri arasındaki gecikme.")]
    public float CurrentBurstFireDelay { get; private set; }
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
    private readonly List<StatModifier> projectileDamageModifiers = new List<StatModifier>();
    
    // --- YENİ EKLENEN KISIM BAŞLANGICI (Burst Fire) ---
    private readonly List<StatModifier> projectilesPerShotModifiers = new List<StatModifier>();
    private readonly List<StatModifier> burstFireDelayModifiers = new List<StatModifier>();
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
        
        InitializeVisuals();
        RecalculateAllStats();
    }
    
    private void InitializeVisuals()
    {
        if (characterGfxRenderer == null)
        {
            Debug.LogError("PlayerStats üzerinde 'Character Gfx Renderer' referansı atanmamış!");
            return;
        }
        if (baseStatsData.characterSprite != null)
        {
            characterGfxRenderer.sprite = baseStatsData.characterSprite;
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

        // Int Değerler
        CurrentMaxHealth = (int)CalculateStat(baseStatsData.maxHealth, maxHealthModifiers);
        CurrentProjectileDamage = (int)CalculateStat(baseStatsData.projectileDamage, projectileDamageModifiers);
        
        // --- YENİ EKLENEN KISIM BAŞLANGICI (Burst Fire) ---
        CurrentProjectilesPerShot = (int)CalculateStat(baseStatsData.projectilesPerShot, projectilesPerShotModifiers);
        CurrentBurstFireDelay = CalculateStat(baseStatsData.burstFireDelay, burstFireDelayModifiers);
        // --- YENİ EKLENEN KISIM SONU ---

        // Bool (Seçenek) Değerler
        CurrentCanFireWhileMoving = baseStatsData.canFireWhileMoving;
        
        // Güvenlik kontrolleri
        if (CurrentAttackSpeed <= 0) CurrentAttackSpeed = 0.1f;
        if (CurrentAttackRange < 0) CurrentAttackRange = 0;
        if (CurrentProjectileDamage < 0) CurrentProjectileDamage = 0;
        if (CurrentProjectilesPerShot < 1) CurrentProjectilesPerShot = 1; // En az 1 mermi atılmalı
        if (CurrentBurstFireDelay < 0.01f) CurrentBurstFireDelay = 0.01f; // Gecikme çok düşük olmamalı
        // --- YENİ KONTROLLER EKLENDİ ---
        
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
    
    // --- YENİ EKLENEN KISIM BAŞLANGICI (Burst Fire) ---
    public void AddProjectilesPerShotModifier(StatModifier mod) => AddModifier(mod, projectilesPerShotModifiers);
    public bool RemoveProjectilesPerShotModifiersFromSource(object source) => RemoveModifiersFromSource(source, projectilesPerShotModifiers);
    
    public void AddBurstFireDelayModifier(StatModifier mod) => AddModifier(mod, burstFireDelayModifiers);
    public bool RemoveBurstFireDelayModifiersFromSource(object source) => RemoveModifiersFromSource(source, burstFireDelayModifiers);
    // --- YENİ EKLENEN KISIM SONU ---
}