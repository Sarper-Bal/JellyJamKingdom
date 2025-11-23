namespace IndianOceanAssets.Engine2_5D
{
    /// <summary>
    /// Oyundaki geliştirilebilir tüm istatistik türlerinin listesi.
    /// Yeni bir stat eklediğinde buraya da eklemelisin.
    /// </summary>
    public enum StatType
    {
        MoveSpeed,          // Hareket Hızı
        RollForce,          // Takla Mesafesi
        RollCooldown,       // Takla Bekleme Süresi
        MaxHealth,          // Maksimum Can
        ProjectileDamage,   // Mermi Hasarı
        ProjectileSpeed,    // Mermi Hızı
        ProjectileRadius,   // Patlama Alanı
        AttackSpeed,        // Saldırı Hızı
        AttackRange,        // Saldırı Menzili
        ProjectilesPerShot, // Çoklu Atış Sayısı
        BurstFireDelay      // Seri Atış Gecikmesi
    }
}