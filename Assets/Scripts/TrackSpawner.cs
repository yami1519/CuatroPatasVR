using UnityEngine;

// Genera filas de obstáculos y monedas delante del jugador.
// El jugador se queda en Z = 0 y el mundo viene hacia él.
public class TrackSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Obstáculos: bajo (saltar), alto (deslizar), bloque (cambiar carril). Cada uno con el script Obstacle.")]
    public GameObject[] obstaclePrefabs;
    [Tooltip("Moneda con el script Coin")]
    public GameObject coinPrefab;

    [Header("Posición")]
    public float spawnZ = 60f;
    public float laneDistance = 2f;

    [Header("Frecuencia")]
    public float minGap = 9f;   // distancia mínima entre filas
    public float maxGap = 15f;
    [Range(0f, 1f)] public float doubleObstacleChance = 0.3f;
    [Range(0f, 1f)] public float coinLineChance = 0.5f;
    public int coinsPerLine = 5;
    public float coinSpacing = 1.5f;

    float distanceSinceLast;
    float nextGap;

    void Start()
    {
        nextGap = maxGap;
    }

    void Update()
    {
        var rm = RunnerManager.Instance;
        if (rm == null || !rm.IsRunning) return;

        distanceSinceLast += rm.CurrentSpeed * Time.deltaTime;
        if (distanceSinceLast >= nextGap)
        {
            SpawnRow();
            distanceSinceLast = 0f;
            nextGap = Random.Range(minGap, maxGap);
        }
    }

    void SpawnRow()
    {
        // Siempre queda al menos 1 carril libre
        int blocked1 = Random.Range(0, 3);
        int blocked2 = Random.value < doubleObstacleChance ? (blocked1 + Random.Range(1, 3)) % 3 : -1;

        for (int lane = 0; lane < 3; lane++)
        {
            Vector3 lanePos = transform.position + new Vector3((lane - 1) * laneDistance, 0f, spawnZ);

            if (lane == blocked1 || lane == blocked2)
            {
                if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) continue;
                var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                Spawn(prefab, lanePos);
            }
            else if (coinPrefab != null && Random.value < coinLineChance)
            {
                for (int i = 0; i < coinsPerLine; i++)
                    Spawn(coinPrefab, lanePos + Vector3.forward * (i * coinSpacing));
            }
        }
    }

    void Spawn(GameObject prefab, Vector3 lanePos)
    {
        // Se respeta la altura (Y) y rotación que tenga el prefab
        var go = Instantiate(prefab, lanePos + Vector3.up * prefab.transform.position.y, prefab.transform.rotation);
        if (!go.GetComponent<MovingObject>()) go.AddComponent<MovingObject>();
    }
}
