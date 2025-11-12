using UnityEngine;

// Bu script, bir component DEĞİLDİR. Sadece verileri tutan bir veri yapısıdır (data structure).
// PlayerStats.cs tarafından kullanılır.

/// <summary>
/// Stat değişikliklerinin türünü belirler (sabit ekleme veya yüzdesel çarpma).
/// </summary>
public enum StatModType
{
    // Yorum satırları, 100 temel değerine sahip bir stat (örn: 100 Can) üzerinden örneklendirilmiştir.

    /// <summary>
    /// Düz ekleme. Örn: +10 Can. (Sonuç: 110)
    /// </summary>
    Flat,
    
    /// <summary>
    /// Yüzdesel ekleme. Diğer 'PercentAdd' modifiyerleri ile toplanır.
    /// Örn: +%10 bonus (1. item) ve +%20 bonus (2. item) = +%30. (Sonuç: 100 * 1.30 = 130)
    /// </summary>
    PercentAdd,
    
    /// <summary>
    /// Yüzdesel çarpma. Diğer 'PercentMult' modifiyerleri ile çarpılır.
    /// Örn: x1.5 bonus (1. item) ve x2.0 bonus (2. item) = x3.0. (Sonuç: 100 * 1.5 * 2.0 = 300)
    /// </summary>
    PercentMult 
}

[System.Serializable]
public class StatModifier
{
    // Değişkenler 'readonly' (sadece okunabilir) olarak ayarlandı,
    // bu sayede oluşturulduktan sonra değiştirilemezler.
    
    /// <summary>
    /// Değişikliğin sayısal değeri (örn: 10 veya 0.2).
    /// </summary>
    public readonly float Value;
    
    /// <summary>
    /// Değişikliğin türü (Flat, PercentAdd, PercentMult).
    /// </summary>
    public readonly StatModType Type;
    
    /// <summary>
    /// Hesaplama sırası. Düşük sayılar önce hesaplanır.
    /// (Varsayılan olarak Flat=0, PercentAdd=1, PercentMult=2)
    /// </summary>
    public readonly int Order;
    
    /// <summary>
    /// Bu değişikliği hangi objenin veya sistemin eklediği (örn: "SpeedPotion").
    /// Bu, değişikliği geri alırken "kaynağa" göre filtreleme yapmak için kullanılır.
    /// </summary>
    public readonly object Source;

    /// <summary>
    /// Stat Değiştirici (Modifier) için ana yapıcı metot (constructor).
    /// </summary>
    /// <param name="value">Değer</param>
    /// <param name="type">Tür</param>
    /// <param name="order">İşlem sırası</param>
    /// <param name="source">Ekleyen kaynak</param>
    public StatModifier(float value, StatModType type, int order, object source)
    {
        Value = value;
        Type = type;
        Order = order;
        Source = source;
    }

    // --- Kullanım Kolaylığı Sağlayan Ek Yapıcı Metotlar (Overloads) ---

    /// <summary>
    /// Varsayılan işlem sırasını (Order) ve (null) kaynağı kullanır.
    /// </summary>
    public StatModifier(float value, StatModType type) 
        : this(value, type, (int)type, null) { }

    /// <summary>
    /// (null) kaynağı kullanır.
    /// </summary>
    public StatModifier(float value, StatModType type, int order) 
        : this(value, type, order, null) { }
    
    /// <summary>
    /// Varsayılan işlem sırasını (Order) kullanır.
    /// </summary>
    public StatModifier(float value, StatModType type, object source) 
        : this(value, type, (int)type, source) { }
}