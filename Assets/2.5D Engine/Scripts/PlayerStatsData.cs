using UnityEngine;

// Bu [CreateAssetMenu] özelliği, Unity'nin Assets > Create menüsüne yeni bir seçenek ekler.
// Artık "Stats/Player Stats Data" seçeneği ile bu veri konteynerinden yeni asset'ler oluşturabilirsin.
[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "Stats/Player Stats Data")]
public class PlayerStatsData : ScriptableObject
{
    // ScriptableObject'lar, MonoBehaviour gibi çalışmayan, sadece veri tutan
    // özel class'lardır. Oyunun dengesini (balancing) buradan yapacağız.

    [Header("Visuals")]
    [Tooltip("Karakterin 'GFX' objesinde kullanılacak ana görünümü (Sprite).")]
    public Sprite characterSprite;

    [Header("Movement Stats")]
    [Tooltip("Karakterin saniyedeki hareket hızı.")]
    public float moveSpeed = 5f;
    [Tooltip("Takla (Roll) mekaniğinin ne kadar ileri atılacağı.")]
    public float rollForce = 8f;
    [Tooltip("İki takla arasında beklenmesi gereken saniye.")]
    public float rollCooldown = 1f;

    [Header("Health Stats")]
    [Tooltip("Karakterin maksimum can değeri.")]
    public int maxHealth = 100;

    [Header("Projectile Attack Stats")]
    [Tooltip("Fırlatılan merminin saniyedeki hızı.")]
    public float projectileSpeed = 10f;
    [Tooltip("Merminin patlama yarıçapı (radius).")]
    public float projectileRadius = 1f;
    [Tooltip("Saniyede kaç atış yapılacağı (Atış Aralığı = 1 / Saldırı Hızı).")]
    public float attackSpeed = 2f; 
    [Tooltip("Otomatik saldırının düşmanları hedef alacağı maksimum menzil.")]
    public float attackRange = 10f;
    
    // --- YENİ EKLENEN KISIM BAŞLANGICI ---
    // Hasar miktarını HealthSystem.Damage() metodu 'int' aldığı için 'int' olarak tutmak daha sağlıklıdır.
    [Tooltip("Merminin (Projectile) düşmanlara vereceği temel hasar miktarı.")]
    public int projectileDamage = 10;
    // --- YENİ EKLENEN KISIM SONU ---
}