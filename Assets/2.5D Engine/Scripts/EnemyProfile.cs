using UnityEngine;

// CreateAssetMenu, Unity'nin Assets > Create menüsüne yeni bir seçenek ekler.
[CreateAssetMenu(fileName = "New Enemy Profile", menuName = "Enemy System/Enemy Profile")]
public class EnemyProfile : ScriptableObject
{
    [Header("Stats")]
    [Tooltip("Düşmanın maksimum can değeri.")]
    public int maxHealth = 100;

    [Tooltip("Düşmanın hareket hızı.")]
    public float speed = 2f;

    [Tooltip("Düşmanın oyuncuya temas ettiğinde vereceği hasar.")]
    public int damageAmount = 10;

    [Header("Visuals")]
    [Tooltip("Düşmanın görseli. Bu alana bir Sprite dosyası sürüklenmelidir.")]
    public Sprite sprite;
}