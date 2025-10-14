using UnityEngine;
namespace IndianOceanAssets.Engine2_5D
{
    // Spawns enemies at random positions and intervals
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject enemyEntity; // Enemy prefab

        [SerializeField]
        private Transform[] spawnPosition; // Possible spawn points

        [SerializeField]
        private float spawnTime = 4f; // Time between spawns

        // Starts repeated spawning
        private void Start()
        {
            InvokeRepeating("SpawnEnemies", 0f, spawnTime);
        }

        // Spawns a random number of enemies at random positions
        public void SpawnEnemies()
        {
            int enemyNo = Random.Range(1, 4);

            for (int i = 0; i < enemyNo; i++)
            {
                int randomSpawnPosition = Random.Range(0, spawnPosition.Length);

                if (spawnPosition[randomSpawnPosition])
                {
                    // Randomize spawn position slightly
                    Vector3 randomUnitCircle = new Vector3(Random.Range(-1f, 1f), .5f, Random.Range(-1f, 1f));
                    Instantiate(enemyEntity, spawnPosition[randomSpawnPosition].position + randomUnitCircle, Quaternion.identity);
                }
            }

            // Decrease spawn time to increase difficulty, but not below 2 seconds
            spawnTime -= .5f;

            if (spawnTime <= 2f)
                spawnTime = 2f;
        }
    }
}