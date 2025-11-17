/*
 * DOST NPC VERİSİ (SCRIPTABLE OBJECT)
 * GÖREVİ:
 * Bu, bir component değildir, 'EnemyData' gibi bir veri konteyneridir.
 * NPC'lerin hız, görsel ve boyut gibi temel özelliklerini tanımlar.
 * NPC'ler saldırmadığı veya hasar almadığı için
 * 'damage', 'health' gibi statları içermez.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewNpcData", menuName = "Stats/Friendly NPC Data")]
public class FriendlyNpcData : ScriptableObject
{
    [Header("Temel Statlar")]
    [Tooltip("NPC'nin saniyedeki hareket hızı.")]
    [Min(0.1f)]
    public float speed = 2f;

    [Header("Görsel")]
    [Tooltip("Bu NPC tipinin kullanacağı ana Sprite.")]
    public Sprite characterSprite;
    
    [Tooltip("NPC prefab'ının ana transform'unun varsayılan boyutu (scale).")]
    public Vector3 scale = Vector3.one;
}