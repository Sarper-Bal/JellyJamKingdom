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
    
    // --- DEĞİŞİKLİK: Enum yerine ScriptableObject ---
    [Header("Ekonomi")]
    [Tooltip("Bu evin ürettiği kaynak tipi (Asset).")]
    public ResourceData producedResource; // <-- ResourceType yerine ResourceData
    // -----------------------------------------------
}