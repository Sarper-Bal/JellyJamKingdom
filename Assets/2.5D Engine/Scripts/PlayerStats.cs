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

    // --- ANLIK (RUNTIME) HESAPLANMIŞ STAT'LAR ---
    // Diğer script'ler (PlayerController, HealthSystem vb.)
    // artık baseStatsData'ya değil, buradaki property'lere erişecek.
    // 'private set' sayesinde dışarıdan değiştirilemezler, sadece buradan hesaplanırlar.

    public int CurrentMaxHealth { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public float CurrentRollForce { get; private set; }
    public float CurrentRollCooldown { get; private set; }
    public float CurrentProjectileSpeed { get; private set; }
    public float CurrentProjectileRadius { get; private set; }
    public float CurrentAttackSpeed { get; private set; }

    // --- YENİ EKLENEN KISIM BAŞLANGICI (AttackRange) ---
    [Tooltip("Anlık saldırı menzili")]
    public float CurrentAttackRange { get; private set; }
    // --- YENİ EKLENEN KISIM SONU ---


    // --- MODIFIER (DEĞİŞTİRİCİ) LİSTELERİ ---
    private readonly List<StatModifier> maxHealthModifiers = new List<StatModifier>();
    private readonly List<StatModifier> moveSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> rollForceModifiers = new List<StatModifier>();
    private readonly List<StatModifier> rollCooldownModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileSpeedModifiers = new List<StatModifier>();
    private readonly List<StatModifier> projectileRadiusModifiers = new List<StatModifier>();
    private readonly List<StatModifier> attackSpeedModifiers = new List<StatModifier>();

    // --- YENİ EKLENEN KISIM BAŞLANGICI (AttackRange) ---
    private readonly List<StatModifier> attackRangeModifiers = new List<StatModifier>();
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
        
        // Başlangıçta tüm stat'ları temel değerlere göre hesapla.
        RecalculateAllStats();
    }

    /// <summary>
    /// Belirli bir temel değere, listedeki tüm modifiyerleri uygular ve son değeri döner.
    /// </summary>
    private float CalculateStat(float baseValue, List<StatModifier> modifiers)
    {
        float finalValue = baseValue;
        float sumPercentAdd = 0; // Toplanabilir yüzdesel artışlar (%10 + %20 = %30)

        // Modifiyerleri işlem sırasına göre sırala (Flat -> PercentAdd -> PercentMult)
        modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));

        foreach (var mod in modifiers)
        {
            switch (mod.Type)
            {
                case StatModType.Flat:
                    // Düz eklemeler
                    finalValue += mod.Value;
                    break;
                
                case StatModType.PercentAdd:
                    // Yüzdesel eklemeler toplanır
                    sumPercentAdd += mod.Value;
                    break;
                
                case StatModType.PercentMult:
                    // Yüzdesel çarpanlar sırayla çarpılır
                    finalValue *= (1 + mod.Value);
                    break;
            }
        }
        
        // Toplanan yüzdesel eklemeyi en son uygula (örn: 100 * (1 + 0.30))
        finalValue *= (1 + sumPercentAdd);
        
        return finalValue;
    }
    
    /// <summary>
    /// Tüm stat'ları temel değerlerden başlayarak yeniden hesaplar ve günceller.
    /// </summary>
    private void RecalculateAllStats()
    {
        // Her bir stat'ı kendi listesiyle hesapla
        CurrentMaxHealth = (int)CalculateStat(baseStatsData.maxHealth, maxHealthModifiers);
        CurrentMoveSpeed = CalculateStat(baseStatsData.moveSpeed, moveSpeedModifiers);
        CurrentRollForce = CalculateStat(baseStatsData.rollForce, rollForceModifiers);
        CurrentRollCooldown = CalculateStat(baseStatsData.rollCooldown, rollCooldownModifiers);
        CurrentProjectileSpeed = CalculateStat(baseStatsData.projectileSpeed, projectileSpeedModifiers);
        CurrentProjectileRadius = CalculateStat(baseStatsData.projectileRadius, projectileRadiusModifiers);
        CurrentAttackSpeed = CalculateStat(baseStatsData.attackSpeed, attackSpeedModifiers);
        
        // Güvenlik önlemi: Saldırı hızı 0 veya negatif olamaz (bölme hatası verir).
        if (CurrentAttackSpeed <= 0)
        {
            CurrentAttackSpeed = 0.1f; // Minimum bir değere çek.
        }
        
        // --- YENİ EKLENEN KISIM BAŞLANGICI (AttackRange) ---
        CurrentAttackRange = CalculateStat(baseStatsData.attackRange, attackRangeModifiers);
        // Güvenlik önlemi: Menzil negatif olamaz.
        if (CurrentAttackRange < 0)
        {
            CurrentAttackRange = 0;
        }
        // --- YENİ EKLENEN KISIM SONU ---

        
        // Değişiklikleri diğer script'lere haber ver.
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
    
    // --- Her stat için özel Add/Remove metotları (Kullanım kolaylığı için) ---
    
    // Hareket Hızı
    public void AddMoveSpeedModifier(StatModifier mod) => AddModifier(mod, moveSpeedModifiers);
    public bool RemoveMoveSpeedModifiersFromSource(object source) => RemoveModifiersFromSource(source, moveSpeedModifiers);
    
    // Maksimum Can
    public void AddMaxHealthModifier(StatModifier mod) => AddModifier(mod, maxHealthModifiers);
    public bool RemoveMaxHealthModifiersFromSource(object source) => RemoveModifiersFromSource(source, maxHealthModifiers);
    
    // Saldırı Hızı
    public void AddAttackSpeedModifier(StatModifier mod) => AddModifier(mod, attackSpeedModifiers);
    public bool RemoveAttackSpeedModifiersFromSource(object source) => RemoveModifiersFromSource(source, attackSpeedModifiers);

    // --- YENİ EKLENEN KISIM BAŞLANGICI (AttackRange) ---
    // Saldırı Menzili (Attack Range)
    public void AddAttackRangeModifier(StatModifier mod) => AddModifier(mod, attackRangeModifiers);
    public bool RemoveAttackRangeModifiersFromSource(object source) => RemoveModifiersFromSource(source, attackRangeModifiers);
    // --- YENİ EKLENEN KISIM SONU ---
}