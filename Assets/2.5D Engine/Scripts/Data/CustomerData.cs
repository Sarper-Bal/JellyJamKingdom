/*
 * MÜŞTERİ VERİSİ
 * GÖREVİ: Müşterinin görselini, hızını ve alım gücünü tutar.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "Stats/Customer Data")]
public class CustomerData : ScriptableObject
{
    [Header("Görsel")]
    public Sprite characterSprite;
    public Vector3 scale = Vector3.one;

    [Header("Hareket")]
    public float speed = 2.5f;

    [Header("Ekonomi")]
    [Tooltip("Müşterinin tek seferde kaç adet ürün almak istediği.")]
    public int purchaseAmount = 1;
}