/*
 * DÜŞMAN VERİSİ (SCRIPTABLE OBJECT) - v1.1
 * * DEĞİŞİKLİKLER:
 * - 'Temel Statlar' bölümüne 'characterSprite' (Sprite) alanı eklendi.
 * - Bu, düşmanın görselini de data-driven hale getirir.
 */

using UnityEngine;

// Enum (MovementType) değişmedi, aynı kalıyor
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
    
    // --- DEĞİŞİKLİK BAŞLANGICI ---
    [Header("Görsel")]
    [Tooltip("Bu düşman tipinin kullanacağı ana Sprite. " +
             "EnemyAI'daki Sprite Renderer'a atanacak.")]
    public Sprite characterSprite; // <-- YENİ EKLENDİ
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