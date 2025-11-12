using UnityEngine;

// Bu [CreateAssetMenu] özelliği, Unity'nin Assets > Create menüsüne yeni bir seçenek ekler.
// Artık "Stats/Player Stats Data" seçeneği ile bu veri konteynerinden yeni asset'ler oluşturabilirsin.
[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "Stats/Player Stats Data")]
public class PlayerStatsData : ScriptableObject
{
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
    [Tooltip("Merminin (Projectile) düşmanlara vereceği temel hasar miktarı.")]
    public int projectileDamage = 10;
    [Tooltip("Fırlatılan merminin saniyedeki hızı.")]
    public float projectileSpeed = 10f;
    [Tooltip("Merminin patlama yarıçapı (radius).")]
    public float projectileRadius = 1f;
    [Tooltip("Saniyede kaç atış yapılacağı (Atış Aralığı = 1 / Saldırı Hızı).")]
    public float attackSpeed = 2f; 
    [Tooltip("Otomatik saldırının düşmanları hedef alacağı maksimum menzil.")]
    public float attackRange = 10f;

    [Header("Combat Settings")]
    [Tooltip("Eğer bu 'True' ise, karakter hareket ederken de otomatik saldırı yapmaya devam eder.")]
    public bool canFireWhileMoving = false; 

    // --- YENİ EKLENEN KISIM BAŞLANGICI ---
    [Header("Burst Fire Settings")]
    [Tooltip("Tek bir atış komutunda (AutoAttack tetiklendiğinde) kaç mermi atılacağı. '1' = Normal tekli atış.")]
    [Range(1, 10)] // (Inspector'da 1 ile 10 arasında bir slider olarak görünür)
    public int projectilesPerShot = 1;
    
    [Tooltip("EĞER 'Projectiles Per Shot' > 1 ise, mermiler arasında beklenecek saniye cinsinden gecikme. (örn: 0.1)")]
    public float burstFireDelay = 0.1f;
    // --- YENİ EKLENEN KISIM SONU ---
}