/*
 * PLAYER STATS (YÖNETİCİ COMPONENT)
 * * GÜNCELLEME (v8 - Upgrade System):
 * - 'ApplyUpgrade(UpgradeData)' metodu eklendi.
 * - 'StatType' enum yapısını kullanarak modüler geliştirme sistemi kuruldu.
 * - Namespace 'IndianOceanAssets.Engine2_5D' olarak güncellendi.
 */

using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    public class PlayerStats : MonoBehaviour
    {
        #region Base Configuration
        [Header("Base Stats")]
        [Tooltip("Tüm temel stat'ların çekildiği ScriptableObject")]
        [SerializeField] private PlayerStatsData baseStatsData;

        [Header("Component References (Optional)")]
        [Tooltip("(Opsiyonel) Karakterin görünümünün (Sprite) atanacağı SpriteRenderer.")]
        [SerializeField] private SpriteRenderer characterGfxRenderer;
        #endregion

        #region Runtime Stats (Properties)
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
        #endregion

        #region Modifier Lists
        // --- MODIFIER (DEĞİŞTİRİCİ) LİSTELERİ ---
        // Her stat için ayrı bir liste tutuyoruz.
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
        #endregion

        // Stat'lar değiştiğinde tetiklenecek olay.
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
            if (characterGfxRenderer == null) return;

            if (baseStatsData.characterSprite != null)
            {
                characterGfxRenderer.sprite = baseStatsData.characterSprite;
            }
            else
            {
                Debug.LogWarning($"PlayerStatsData ({baseStatsData.name}) üzerinde 'Character Sprite' atanmamış.");
            }
        }

        #region Calculation Logic
        
        private float CalculateStat(float baseValue, List<StatModifier> modifiers)
        {
            float finalValue = baseValue;
            float sumPercentAdd = 0; 
            
            // Sıralama önemli: Flat -> PercentAdd -> PercentMult
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

            // Int Değerler (Mathf.RoundToInt veya cast kullanılabilir)
            CurrentMaxHealth = (int)CalculateStat(baseStatsData.maxHealth, maxHealthModifiers);
            CurrentProjectileDamage = (int)CalculateStat(baseStatsData.projectileDamage, projectileDamageModifiers);
            CurrentProjectilesPerShot = (int)CalculateStat(baseStatsData.projectilesPerShot, projectilesPerShotModifiers);

            // Bool Değerler
            CurrentCanFireWhileMoving = baseStatsData.canFireWhileMoving;
            
            // Güvenlik kontrolleri (Limitler)
            ValidateStats();
            
            OnStatsChanged?.Invoke();
        }

        private void ValidateStats()
        {
            if (CurrentAttackSpeed <= 0) CurrentAttackSpeed = 0.1f;
            if (CurrentAttackRange < 0) CurrentAttackRange = 0;
            if (CurrentProjectileDamage < 0) CurrentProjectileDamage = 0;
            if (CurrentProjectilesPerShot < 1) CurrentProjectilesPerShot = 1;
            if (CurrentBurstFireDelay < 0.01f) CurrentBurstFireDelay = 0.01f;
        }

        #endregion

        #region Public API & Modifiers

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
        
        // --- YENİ UPGRADE SİSTEMİ ENTEGRASYONU ---

        /// <summary>
        /// Bir UpgradeData (Kart) alır ve içindeki tüm bonusları ilgili statlara işler.
        /// </summary>
        /// <param name="upgrade">Uygulanacak upgrade kartı</param>
        public void ApplyUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogWarning("PlayerStats: Boş (null) bir upgrade uygulanmaya çalışıldı.");
                return;
            }

            Debug.Log($"PlayerStats: '{upgrade.upgradeName}' geliştirmesi uygulanıyor...");

            foreach (StatBonus bonus in upgrade.bonuses)
            {
                // Modifier oluştur: Değer, Tip, Kaynak (UpgradeData'nın kendisi)
                StatModifier newMod = new StatModifier(bonus.value, bonus.modType, upgrade);

                // Hangi listeye ekleneceğini belirle (Switch-Case optimize bir yöntemdir)
                switch (bonus.statType)
                {
                    case StatType.MoveSpeed: AddModifier(newMod, moveSpeedModifiers); break;
                    case StatType.RollForce: AddModifier(newMod, rollForceModifiers); break;
                    case StatType.RollCooldown: AddModifier(newMod, rollCooldownModifiers); break;
                    case StatType.MaxHealth: AddModifier(newMod, maxHealthModifiers); break;
                    case StatType.ProjectileDamage: AddModifier(newMod, projectileDamageModifiers); break;
                    case StatType.ProjectileSpeed: AddModifier(newMod, projectileSpeedModifiers); break;
                    case StatType.ProjectileRadius: AddModifier(newMod, projectileRadiusModifiers); break;
                    case StatType.AttackSpeed: AddModifier(newMod, attackSpeedModifiers); break;
                    case StatType.AttackRange: AddModifier(newMod, attackRangeModifiers); break;
                    case StatType.ProjectilesPerShot: AddModifier(newMod, projectilesPerShotModifiers); break;
                    case StatType.BurstFireDelay: AddModifier(newMod, burstFireDelayModifiers); break;
                    
                    default:
                        Debug.LogWarning($"PlayerStats: '{bonus.statType}' tipi için switch case tanımlanmamış!");
                        break;
                }
            }
        }

        #endregion

        #region Helper Methods (Specific Add/Remove)
        // Dışarıdan manuel eklemeler için helperlar (Eski uyumluluk için korundu)
        public void AddMoveSpeedModifier(StatModifier mod) => AddModifier(mod, moveSpeedModifiers);
        public void AddMaxHealthModifier(StatModifier mod) => AddModifier(mod, maxHealthModifiers);
        // ... Diğerleri gerekirse buraya eklenebilir.
        #endregion
        
        public void Initialize(PlayerStatsData newData)
        {
            if (newData == null) return;
            baseStatsData = newData;
            InitializeVisuals();
            RecalculateAllStats();
        }
    }
}