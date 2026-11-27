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

    [MenuItem("Cuatro Patas/Tienda/18 - Fix arbol de la puerta y cerca caida")]
    public static void FixArbolYCerca()
    {
        var pueblo = GameObject.Find("Pueblo Kenney");
        if (pueblo == null) { Debug.LogError("TIENDA_VR: no esta 'Pueblo Kenney'."); return; }

        // ---- 1. Arbol que tapa la puerta de la casa (cerca de x=0, z=-11.4) ----
        int arboles = 0;
        var borrar = new List<GameObject>();
        foreach (Transform h in pueblo.transform)
            if (h.name.StartsWith("tree") && Mathf.Abs(h.position.x) < 3f && h.position.z > -14f && h.position.z < -9f)
                borrar.Add(h.gameObject);
        foreach (var b in borrar) { Object.DestroyImmediate(b); arboles++; }

        // ---- 2. Cercas acostadas: medir bounds y pararlas (el eje alto debe ser Y)
        int cercas = 0;
        foreach (Transform h in pueblo.transform)
        {
            if (!h.name.StartsWith("fence-low")) continue;
            var b = CalcularBounds(h.gameObject);
            int intentos = 0;
            while (b.size.y < b.size.z * 0.8f && intentos < 3)
            {
                h.localRotation = h.localRotation * Quaternion.Euler(90f, 0f, 0f);
                b = CalcularBounds(h.gameObject);
                intentos++;
            }
            // apoyar en el suelo
            var p = h.position;
            b = CalcularBounds(h.gameObject);
            h.position = new Vector3(p.x, p.y - b.min.y + 0.01f, p.z);
            cercas++;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: fix listo. Arboles quitados de la puerta: " + arboles + ", cercas enderezadas: " + cercas + ".");
    }

    [MenuItem("Cuatro Patas/Tienda/21 - Regenerar pueblo sin traslapar casa")]
    public static void RegenerarPueblo()
    {
        // Regenera la ciudad con los saltos corregidos (borra y reconstruye 'Pueblo Kenney',
        // incluidos los edificios nuevos que cuelgan de el; por eso se recolocan despues).
        CuatroPatasVR.EditorTools.PuebloKenney.Build();
        EdificiosNuevos();
        ReemplazarRescatada();
        BarridoAntiTraslape();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: pueblo regenerado sin casas traslapando la casa principal.");
    }

    [MenuItem("Cuatro Patas/Tienda/23 - Reemplazar casa rescatada por casa Kenney")]
    public static void ReemplazarRescatada()
    {
        // La 'Casa Rescatada (template Godot)' tiene la malla deformada (conversion DAE->OBJ):
        // se borra y en su terreno va una casa Kenney igual a las demas.
        Vector3 pos = new Vector3(23f, 0f, 2f);
        var rescatada = GameObject.Find("Casa Rescatada (template Godot)");
        if (rescatada != null)
        {
            pos = rescatada.transform.position;
            pos.y = 0f;
            Object.DestroyImmediate(rescatada);
        }
        var pueblo = GameObject.Find("Pueblo Kenney");
        if (pueblo == null) { Debug.LogError("TIENDA_VR: no esta 'Pueblo Kenney'."); return; }

        string ruta = "Assets/Models/Kits/city-kit-suburban/Models/GLB format/building-type-b.glb";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) { Debug.LogError("TIENDA_VR: no se encontro " + ruta); return; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "Casa Vecina";
        go.transform.SetParent(pueblo.transform);
        go.transform.rotation = Quaternion.identity;
        var b = CalcularBounds(go);
        float k = 4.5f / Mathf.Max(0.01f, b.size.y);
        go.transform.localScale = go.transform.localScale * k;
        b = CalcularBounds(go);
        go.transform.position = pos - new Vector3(b.center.x, b.min.y, b.center.z);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: casa rescatada reemplazada por 'Casa Vecina' (Kenney) en " + go.transform.position + ".");
    }

    // Red de seguridad: borra cualquier casa Kenney que toque la casa del jugador,
    // la casa rescatada o los edificios grandes del usuario.
    private static void BarridoAntiTraslape()
    {
        var pueblo = GameObject.Find("Pueblo Kenney");
        if (pueblo == null) return;

        var protegidas = new List<Bounds>();
        var casa = GameObject.Find("Casa");
        if (casa != null) protegidas.Add(ExpandirXZ(CalcularBounds(casa), 1.2f));
        var rescatada = GameObject.Find("Casa Rescatada (template Godot)");
        if (rescatada != null) protegidas.Add(ExpandirXZ(CalcularBounds(rescatada), 1f));
        foreach (Transform h in pueblo.transform)
        {
            var n = h.name;
            bool esEdificioExtra = (n.Contains("Building") || n.Contains("building")) && !n.StartsWith("building-type");
            if (esEdificioExtra) protegidas.Add(ExpandirXZ(CalcularBounds(h.gameObject), 0.5f));
        }

        var borrar = new List<GameObject>();
        foreach (Transform h in pueblo.transform)
        {
            if (!h.name.StartsWith("building-type")) continue;
            var b = CalcularBounds(h.gameObject);
            foreach (var p in protegidas)
                if (b.Intersects(p)) { borrar.Add(h.gameObject); break; }
        }
        foreach (var g in borrar) Object.DestroyImmediate(g);
        Debug.Log("TIENDA_VR: barrido anti-traslape: " + borrar.Count + " casas eliminadas sobre " +
                  protegidas.Count + " zonas protegidas.");
    }

    private static Bounds ExpandirXZ(Bounds b, float m)
    {
        return new Bounds(b.center, b.size + new Vector3(m * 2f, 0f, m * 2f));
    }

    [MenuItem("Cuatro Patas/Tienda/19 - Quitar cerca del patio (se veia mal)")]
    public static void QuitarCerca()
    {
        var pueblo = GameObject.Find("Pueblo Kenney");
        if (pueblo == null) { Debug.LogError("TIENDA_VR: no esta 'Pueblo Kenney'."); return; }
        var borrar = new List<GameObject>();
        foreach (Transform h in pueblo.transform)
            if (h.name.StartsWith("fence-low")) borrar.Add(h.gameObject);
        foreach (var b in borrar) Object.DestroyImmediate(b);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: cerca del patio eliminada (" + borrar.Count + " piezas). El patio queda abierto con su camino de piedra.");
    }

    [MenuItem("Cuatro Patas/Tienda/20 - Auditoria rapida (sin play)")]
    public static void AuditoriaSinPlay()
    {
        string dir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "_audit");
        System.IO.Directory.CreateDirectory(dir);
        foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) System.IO.File.Delete(f);

        string[][] views = {
            new[]{"01_frente_casa",      "0","1.7","-15",  "0","2.5","-8"},
            new[]{"02_patio_aereo",      "0.5","9","-17",  "0.5","0","-7"},
            new[]{"03_sala_michi",       "0","1.6","1.5",  "1.6","0.5","4.6"},
            new[]{"05_casa_vecina",      "26","3","-14",   "26","3","2"},
            new[]{"06_calle_principal",  "-40","2.5","-16","40","2","-16"},
            new[]{"07_aerea_casa",       "0.5","32","-4",  "0.5","0","-4.01"},
            new[]{"08_aerea_pueblo",     "12","70","-30",  "12","0","-70"},
        };
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var v in views)
        {
            var pos = new Vector3(float.Parse(v[1], ci), float.Parse(v[2], ci), float.Parse(v[3], ci));
            var mir = new Vector3(float.Parse(v[4], ci), float.Parse(v[5], ci), float.Parse(v[6], ci));

            var go = new GameObject("AuditCam");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.transform.position = pos;
            cam.transform.LookAt(mir);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, v[0] + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }
        Debug.Log("TIENDA_VR: auditoria sin play lista, " + views.Length + " capturas en " + dir);
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
        // Al norte de la calle principal, pegados a la vereda pero sin invadirla (z min 19.6).
        int n = 0;
        n += Colocar(pueblo, "Assets/Models/EdificiosExtra/RedBrick/Simple Building.obj", new Vector3(-38f, 0f, 22f), 180f, 8f, 19.6f) ? 1 : 0;
        n += Colocar(pueblo, "Assets/Models/EdificiosExtra/Building2/building.obj",        new Vector3(20f, 0f, 22f),  180f, 9f, 19.6f) ? 1 : 0;
        n += Colocar(pueblo, "Assets/Models/EdificiosExtra/RvBuilding/Rv_Building_3.obj",  new Vector3(56f, 0f, 22f),  180f, 9f, 19.6f) ? 1 : 0;
        n += Colocar(pueblo, "Assets/Models/EdificiosExtra/Edificio55/Building55.fbx",     new Vector3(86f, 0f, 22f),  180f, 8f, 19.6f) ? 1 : 0;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: " + n + " edificios nuevos colocados al norte de la calle principal.");
    }

    // Instancia un modelo, lo escala a la altura deseada midiendo su tamano real
    // y lo apoya en el suelo centrado en la posicion indicada.
    private static bool Colocar(GameObject padre, string ruta, Vector3 pos, float rotY, float altura, float zMin = float.NegativeInfinity)
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
        // no invadir la calle/vereda: empujar al norte si la cara sur pasa del limite
        b = CalcularBounds(go);
        if (b.min.z < zMin)
            go.transform.position += new Vector3(0f, 0f, zMin - b.min.z);
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
