/*
 * DÜŞMAN VERİSİ (SCRIPTABLE OBJECT)
 * GÖREVİ:
 * Bu script, bir component DEĞİLDİR. Bu, bir veri konteyneridir.
 * Tıpkı 'PlayerStatsData' gibi, farklı düşman tipleri (Goblin, Skeleton vb.)
 * oluşturmak için 'Asset'ler yaratmamızı sağlar.
 *
 * 'EnemyAI' script'i, tüm temel özelliklerini (can, hız, hasar, hareket tipi)
 * doğrudan bu asset'ten okuyacaktır.
 */

using UnityEngine;

// Bu 'enum'u 'EnemyAI' script'inden buraya taşıdık, çünkü veri,
// hareket tipini de içermelidir.
public enum MovementType
{
    /// <summary>
    /// Oyuncuyu aktif olarak arar ve takip eder.
    /// </summary>
    ChasePlayer,

    /// <summary>
    /// Spawn olduğu noktanın 'EnemyPath' component'indeki yolu takip eder.
    /// </summary>
    FollowPath,

    /// <summary>
    /// 'fixedDirection' yönünde sabit olarak ilerler.
    /// </summary>
    FixedDirection
}

// Unity'nin "Assets/Create" menüsüne yeni bir seçenek ekler.
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
    
    [Header("Yapay Zeka Davranışı")]
    [Tooltip("Bu düşmanın kullanacağı hareket tipi.")]
    public MovementType movementType = MovementType.ChasePlayer;

    [Header("Mod Ayarları (Gerekliyse)")]
    [Tooltip("EĞER 'Movement Type = FollowPath' ise, yol bitince başa dönsün mü?")]
    public bool loopPath = true; // Bu veriyi de data'da tutmak en modüler olanıdır.
    
    [Tooltip("EĞER 'Movement Type = FixedDirection' ise, ilerleyeceği yön.")]
    public Vector3 fixedDirection = new Vector3(0, 0, -1);
}