using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

// Fixes de la auditoria + prefab de la casa + edificios nuevos del usuario.
public static class CasaFixes
{
    [MenuItem("Cuatro Patas/Tienda/14 - Arreglar cerca, casa rescatada y michi caminando")]
    public static void ArreglarAuditoria()
    {
        // ---- 1. Cerca del patio: estaba pegada a la baranda (z=-8.2) -> va a z=-10.5
        //         con porton donde esta el camino de piedra (x entre -0.5 y 2.5).
        int movidas = 0, quitadas = 0;
        var pueblo = GameObject.Find("Pueblo Kenney");
        if (pueblo != null)
        {
            var cercas = new List<Transform>();
            foreach (Transform h in pueblo.transform)
                if (h.name.StartsWith("fence-low")) cercas.Add(h);
            foreach (var c in cercas)
            {
                if (Mathf.Abs(c.position.z - (-8.2f)) < 1f)
                {
                    if (c.position.x >= -0.5f && c.position.x <= 2.5f)
                    {
                        Object.DestroyImmediate(c.gameObject);
                        quitadas++;
                    }
                    else
                    {
                        c.position = new Vector3(c.position.x, c.position.y, -10.5f);
                        movidas++;
                    }
                }
            }
        }

        // ---- 2. Casa rescatada: estaba a <1m de la pared derecha de la casa -> se va al este.
        var rescatada = GameObject.Find("Casa Rescatada (template Godot)");
        if (rescatada != null)
            rescatada.transform.position = new Vector3(26f, 0f, 2f);

        // ---- 3. Michi camina solo: NavMesh en la casa + NavMeshAgent + CatWander + CatMood.
        var casa = GameObject.Find("Casa");
        var michi = GameObject.Find("Michi");
        string michiMsg = "no encontrado";
        if (casa != null && michi != null)
        {
            if (michi.transform.parent != casa.transform)
                michi.transform.SetParent(casa.transform, true);

            var surface = casa.GetComponent<NavMeshSurface>();
            if (surface == null) surface = casa.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;

            var agent = michi.GetComponent<NavMeshAgent>();
            if (agent == null) agent = michi.AddComponent<NavMeshAgent>();
            agent.radius = 0.15f;
            agent.height = 0.3f;
            agent.baseOffset = 0f;

            var wander = michi.GetComponent<CatWander>();
            if (wander == null) wander = michi.AddComponent<CatWander>();
            wander.surface = surface;
            wander.speed = 0.7f;
            wander.waitMin = 3f;
            wander.waitMax = 7f;

            if (michi.GetComponent<CatMood>() == null) michi.AddComponent<CatMood>();
            michiMsg = "con NavMeshAgent + CatWander + CatMood";
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: fixes listos. Cerca: " + movidas + " movidas a z=-10.5, " + quitadas +
                  " quitadas para el porton. Casa rescatada -> (26,0,2). Michi " + michiMsg + ".");
    }

    [MenuItem("Cuatro Patas/Tienda/15 - Convertir casa en prefab (para moverla facil)")]
    public static void CasaAPrefab()
    {
        var casa = GameObject.Find("Casa");
        if (casa == null)
        {
            Debug.LogError("TIENDA_VR: no se encontro el objeto 'Casa' en la escena.");
            return;
        }
        // Michi debe quedar dentro del prefab para moverse junto con la casa.
        var michi = GameObject.Find("Michi");
        if (michi != null && michi.transform.parent != casa.transform)
            michi.transform.SetParent(casa.transform, true);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(casa, "Assets/Prefabs/Casa.prefab");
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: casa guardada como prefab en Assets/Prefabs/Casa.prefab. " +
                  "La instancia de la escena ahora esta enlazada: muevela arrastrando el objeto 'Casa'.");
    }

    [MenuItem("Cuatro Patas/Tienda/16 - Colocar edificios nuevos en el pueblo")]
    public static void EdificiosNuevos()
    {
        var pueblo = GameObject.Find("Pueblo Kenney");
        if (pueblo == null)
        {
            Debug.LogError("TIENDA_VR: no se encontro 'Pueblo Kenney'.");
            return;
        }
        // Al norte de la calle principal (z=16), mirando hacia la calle.
        int n = 0;
        n += Colocar(pueblo, "Assets/Models/EdificiosExtra/RedBrick/Simple Building.obj", new Vector3(-38f, 0f, 20f), 180f, 8f) ? 1 : 0;
        n += Colocar(pueblo, "Assets/Models/EdificiosExtra/Building2/building.obj",        new Vector3(20f, 0f, 20f),  180f, 9f) ? 1 : 0;
        n += Colocar(pueblo, "Assets/Models/EdificiosExtra/RvBuilding/Rv_Building_3.obj",  new Vector3(56f, 0f, 20f),  180f, 9f) ? 1 : 0;
        n += Colocar(pueblo, "Assets/Models/EdificiosExtra/Edificio55/Building55.fbx",     new Vector3(86f, 0f, 20f),  180f, 8f) ? 1 : 0;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: " + n + " edificios nuevos colocados al norte de la calle principal.");
    }

    // Instancia un modelo, lo escala a la altura deseada midiendo su tamano real
    // y lo apoya en el suelo centrado en la posicion indicada.
    private static bool Colocar(GameObject padre, string ruta, Vector3 pos, float rotY, float altura)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (asset == null)
        {
            Debug.LogWarning("TIENDA_VR: no se pudo cargar " + ruta);
            return false;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.transform.SetParent(padre.transform, false);

        Bounds b = CalcularBounds(go);
        float k = altura / Mathf.Max(0.01f, b.size.y);
        go.transform.localScale = go.transform.localScale * k;
        go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        b = CalcularBounds(go);
        Vector3 p = go.transform.position;
        go.transform.position = new Vector3(
            p.x + (pos.x - b.center.x),
            p.y - b.min.y,
            p.z + (pos.z - b.center.z));
        return true;
    }

    private static Bounds CalcularBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
