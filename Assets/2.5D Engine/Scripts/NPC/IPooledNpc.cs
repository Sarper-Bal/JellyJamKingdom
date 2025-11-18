/*
 * HAVUZLANABİLİR NPC ARAYÜZÜ
 * GÖREVİ:
 * 'NpcPooler' tarafından yönetilecek tüm NPC'lerin
 * uyması gereken standartları belirler.
 */

using UnityEngine;

public interface IPooledNpc
{
    /// <summary>
    /// NPC havuzdan çıktığında ('SetActive(true)' olduğunda)
    /// 'NpcPooler' tarafından çağrılır.
    /// </summary>
    void OnNpcSpawned();
    
    // (İsteğiniz üzerine, NPC'ler artık havuza dönmediği için
    // 'OnNpcReturned' metoduna şimdilik ihtiyacımız yok)
}