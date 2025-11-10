using UnityEngine;

// Stat değişikliklerinin türünü belirler (sabit ekleme veya yüzdesel çarpma).
public enum StatModType
{
    Flat,       // Düz ekleme (örn: +10 Can)
    PercentAdd, // Yüzdesel ekleme (örn: +%20 Can) -> Diğer yüzdesellerle toplanır
    PercentMult // Yüzdesel çarpma (örn: x1.5 Can) -> Diğer çarpanlarla çarpılır
}

[System.Serializable]
public class StatModifier
{
    public readonly float Value;
    public readonly StatModType Type;
    public readonly int Order; // İşlem sırası (Flat < PercentAdd < PercentMult)
    public readonly object Source; // Modifiye'yi kimin eklediği (örn: "SpeedPotion" string'i veya bir obje)

    // Constructor (Yapıcı Metot)
    public StatModifier(float value, StatModType type, int order, object source)
    {
        Value = value;
        Type = type;
        Order = order;
        Source = source;
    }

    // Kolaylık sağlamak için farklı parametreli yapıcı metotlar
    // Varsayılan işlem sırası ile
    public StatModifier(float value, StatModType type) : this(value, type, (int)type, null) { }

    // İşlem sırası ve kaynak ile
    public StatModifier(float value, StatModType type, int order) : this(value, type, order, null) { }
    
    // Kaynak ile
    public StatModifier(float value, StatModType type, object source) : this(value, type, (int)type, source) { }
}