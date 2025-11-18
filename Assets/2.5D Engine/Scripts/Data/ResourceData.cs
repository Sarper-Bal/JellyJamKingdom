/*
 * KAYNAK VERİSİ (RESOURCE DATA)
 * GÖREVİ:
 * Eski 'ResourceType' enum'unun yerini alır.
 * Artık her kaynak (Odun, Taş, Altın) birer 'Asset' dosyasıdır.
 * Bu sayede kod yazmadan yeni kaynak ekleyip çıkarabilirsiniz.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "Economy/Resource")]
public class ResourceData : ScriptableObject
{
    [Tooltip("Kaynağın oyun içi adı.")]
    public string resourceName;

    [Tooltip("UI'da gösterilecek ikon (Opsiyonel).")]
    public Sprite icon;

    // İleride buraya 'fiyat', 'ağırlık' gibi özellikler ekleyebilirsiniz.
}