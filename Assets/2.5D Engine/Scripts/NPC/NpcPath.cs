/*
 * NPC YOLU (NPC PATH)
 * GÖREVİ:
 * Bu component, sahneye yerleştirilen bir 'GameObject' üzerine eklenir.
 * 'FriendlyNpcAI' tarafından takip edilecek ara noktaların
 * (waypoints) bir listesini tutar.
 *
 * 'NpcHousing' script'indeki 'optionalNpcPath' alanına
 * sürüklenerek atanır.
 */

using UnityEngine;

public class NpcPath : MonoBehaviour
{
    [Tooltip("NPC'nin işe giderken (0'dan -> sona) ve eve dönerken " +
             "(sondan -> 0'a) takip edeceği ara noktalar.")]
    public Transform[] waypoints;
}