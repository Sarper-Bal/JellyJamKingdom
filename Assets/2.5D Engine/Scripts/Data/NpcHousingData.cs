using UnityEngine;

[CreateAssetMenu(fileName = "NewHousingData", menuName = "Stats/NPC Housing Data")]
public class NpcHousingData : ScriptableObject
{
    [Header("Spawn Ayarları")]
    public GameObject genericNpcPrefab;
    public FriendlyNpcData npcDataToSpawn;
    public int populationCount = 3;
    public float spawnInterval = 1.5f;

    [Header("Davranış Ayarları")]
    public float restDuration = 3.0f;
    
    // --- DEĞİŞİKLİK (v4.0) ---
    [Header("Ekonomi")]
    [Tooltip("Bu evin ürettiği veya depoladığı kaynak tipi.")]
    public ResourceType producedResourceType = ResourceType.None;
    // -------------------------
}