/*
 * DÜŞMAN VERİSİ (SCRIPTABLE OBJECT) - v1.3
 * * DEĞİŞİKLİKLER:
 * - 'Görsel' bölümüne 'deathEffectPrefab' eklendi.
 * - Bu, artık ölüm efektinin bile data-driven olmasını sağlar.
 * - WaveManager, havuz hesaplaması yaparken bu prefab'ı okuyacak.
 */

using UnityEngine;

// Enum (MovementType) değişmedi
public enum MovementType
{
    ChasePlayer,
    FollowPath,
    FixedDirection
}

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Stats/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Temel Statlar")]
    [Tooltip("Düşmanın maksimum canı.")]
    [Min(1)]
    public int maxHealth = 100;

    [Tooltip("Düşmanın saniyedeki hareket hızı.")]
    [Min(0)]
    public float speed = 3f;

    [Tooltip("Oyuncuya çarptığında vereceği hasar.")]
    [Min(0)]
    public int damageAmount = 10;
    
    [Header("Görsel")]
    [Tooltip("Bu düşman tipinin kullanacağı ana Sprite.")]
    public Sprite characterSprite;
    
    [Tooltip("Düşman prefab'ının ana transform'unun varsayılan boyutu (scale).")]
    public Vector3 scale = Vector3.one;
    
    // --- DEĞİŞİKLİK BAŞLANGICI ---
    [Tooltip("Bu düşman öldüğünde 'ObjectPooler'dan spawn edilecek efekt prefab'ı.")]
    public GameObject deathEffectPrefab; // <-- YENİ EKLENDİ
    // --- DEĞİŞİKLİK SONU ---
    
    [Header("Yapay Zeka Davranışı")]
    [Tooltip("Bu düşmanın kullanacağı hareket tipi.")]
    public MovementType movementType = MovementType.ChasePlayer;

    [Header("Mod Ayarları (Gerekliyse)")]
    [Tooltip("EĞER 'Movement Type = FollowPath' ise, yol bitince başa dönsün mü?")]
    public bool loopPath = true; 
    
    [Tooltip("EĞER 'Movement Type = FixedDirection' ise, ilerleyeceği yön.")]
    public Vector3 fixedDirection = new Vector3(0, 0, -1);
}