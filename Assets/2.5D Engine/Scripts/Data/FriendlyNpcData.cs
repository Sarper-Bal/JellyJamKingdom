/*
 * DOST NPC VERİSİ (SCRIPTABLE OBJECT) - v1.1
 * * DEĞİŞİKLİKLER:
 * - 'Temel Statlar' bölümüne, 'speed' yanına
 * 'maxCarryCapacity' (int) alanı eklendi.
 * - Bu, NPC'nin tek seferde kaç kaynak taşıyabileceğini
 * data asset'i üzerinden belirlememizi sağlar.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewNpcData", menuName = "Stats/Friendly NPC Data")]
public class FriendlyNpcData : ScriptableObject
{
    [Header("Temel Statlar")]
    [Tooltip("NPC'nin saniyedeki hareket hızı.")]
    [Min(0.1f)]
    public float speed = 2f;

    // --- DEĞİŞİKLİK BAŞLANGICI ---
    [Tooltip("NPC'nin tek seferde taşıyabileceği maksimum kaynak miktarı.")]
    [Min(1)]
    public int maxCarryCapacity = 1; // Varsayılan olarak 1
    // --- DEĞİŞİKLİK SONU ---

    [Header("Görsel")]
    [Tooltip("Bu NPC tipinin kullanacağı ana Sprite.")]
    public Sprite characterSprite;
    
    [Tooltip("NPC prefab'ının ana transform'unun varsayılan boyutu (scale).")]
    public Vector3 scale = Vector3.one;
}