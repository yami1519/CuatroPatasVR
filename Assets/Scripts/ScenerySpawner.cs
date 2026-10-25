using UnityEngine;

// Genera la ciudad infinita del Modo Correr: calle, veredas, casas, árboles y nubes
// que vienen hacia el jugador a la misma velocidad que los obstáculos.
public class ScenerySpawner : MonoBehaviour
{
    [Header("Modelos (Kenney, CC0)")]
    public GameObject[] roadTiles;     // road-straight, road-straight-lightposts
    public GameObject sidewalk;        // pavement
    public GameObject[] buildings;     // casas y edificios
    public GameObject[] greenery;      // árboles, arbustos, jardines
    public GameObject[] clouds;

    [Header("Medidas")]
    public float segmentLength = 8f;
    public float spawnAhead = 110f;
    public float despawnZ = -25f;
    public float tileScale = 8f;        // la calle mide 8 m de ancho
    public float sidewalkWidth = 3f;
    public float buildingScale = 6f;
    public float buildingX = 10.5f;
    public float treeScale = 4f;
    [Range(0f, 1f)] public float greeneryChance = 0.35f;
    [Range(0f, 1f)] public float cloudChance = 0.25f;

    float nextZ;
    int count;

    void Start()
    {
        nextZ = despawnZ + segmentLength;
        while (nextZ < spawnAhead) SpawnSegment();
    }

    void Update()
    {
        var rm = RunnerManager.Instance;
        if (rm == null || !rm.IsRunning) return;
        nextZ -= rm.CurrentSpeed * Time.deltaTime;
        while (nextZ < spawnAhead) SpawnSegment();
    }

    void SpawnSegment()
    {
        var seg = new GameObject("Tramo");
        seg.transform.SetParent(transform, false);
        seg.transform.position = new Vector3(0f, 0f, nextZ);
        var mover = seg.AddComponent<MovingObject>();
        mover.destroyZ = despawnZ;

        // Calle (cada 3 tramos, con faroles)
        var road = Pick(roadTiles, count % 3 == 0 ? 1 : 0);
        Place(road, seg.transform, new Vector3(0f, -0.05f * tileScale, 0f), 0f, Vector3.one * tileScale);

        // Veredas
        float sx = tileScale * 0.5f + sidewalkWidth * 0.5f;
        var sw = new Vector3(sidewalkWidth, 3f, tileScale);
        Place(sidewalk, seg.transform, new Vector3(-sx, -0.08f, 0f), 0f, sw);
        Place(sidewalk, seg.transform, new Vector3(sx, -0.08f, 0f), 0f, sw);

        // Un lado a la izquierda (mira a +X) y otro a la derecha (mira a -X)
        SpawnSide(seg.transform, -1f);
        SpawnSide(seg.transform, 1f);

        // Nubes
        if (clouds != null && clouds.Length > 0 && Random.value < cloudChance)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            var pos = new Vector3(side * Random.Range(12f, 30f), Random.Range(12f, 20f), Random.Range(-3f, 3f));
            Place(Pick(clouds), seg.transform, pos, Random.Range(0f, 360f), Vector3.one * Random.Range(4f, 7f));
        }

        nextZ += segmentLength;
        count++;
    }

    void SpawnSide(Transform seg, float side)
    {
        float rot = side < 0 ? 90f : -90f; // la fachada (+Z del modelo) mira hacia la calle
        if (greenery != null && greenery.Length > 0 && Random.value < greeneryChance)
        {
            // Jardín con árboles
            float s = treeScale * Random.Range(0.85f, 1.2f);
            Place(Pick(greenery), seg, new Vector3(side * Random.Range(8.5f, 11f), 0f, Random.Range(-2f, 2f)), Random.Range(0f, 360f), Vector3.one * s);
        }
        else if (buildings != null && buildings.Length > 0)
        {
            Place(Pick(buildings), seg, new Vector3(side * buildingX, 0f, 0f), rot, Vector3.one * buildingScale);
        }
    }

    static GameObject Pick(GameObject[] arr, int preferred = -1)
    {
        if (arr == null || arr.Length == 0) return null;
        if (preferred >= 0 && preferred < arr.Length) return arr[preferred];
        return arr[Random.Range(0, arr.Length)];
    }

    static void Place(GameObject prefab, Transform parent, Vector3 localPos, float rotY, Vector3 scale)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        go.transform.localScale = scale;
        foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c); // decoración: sin colisiones
    }
}
