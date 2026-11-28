using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Prueba visual: pone los 3 edificios CityGameReady y 2 casas Kenney en fila
// al norte del mapa y les toma una foto para ver orientacion real.
public static class TestCityB
{
    [MenuItem("Cuatro Patas/Tienda/25 - Test orientacion modelos")]
    public static void Test()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PuebloVR.unity");
        var viejo = GameObject.Find("TestModelos");
        if (viejo != null) Object.DestroyImmediate(viejo);
        var root = new GameObject("TestModelos");

        string[] rutas = {
            "Assets/Models/Ciudad/CityGameReady/Edificios/Building_001.prefab",
            "Assets/Models/Ciudad/CityGameReady/Edificios/Building_002.prefab",
            "Assets/Models/Ciudad/CityGameReady/Edificios/Building_013.prefab",
            "Assets/Models/Kits/city-kit-suburban/Models/GLB format/building-type-a.glb",
            "Assets/Models/Kits/city-kit-suburban/Models/GLB format/building-type-k.glb"
        };
        float x = -30f;
        foreach (var r in rutas)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(r);
            if (p == null) { Debug.Log("TIENDA_VR: test, no se encontro " + r); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
            go.transform.SetParent(root.transform);
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // como FilaCasas
            go.transform.position = Vector3.zero;
            var b = BoundsDe(go);
            // escala como el generador: city a ~8m, kenney a 4.5m
            float altura = r.Contains("Building_") ? 8f : 4.5f;
            float k = altura / b.size.y;
            go.transform.localScale = go.transform.localScale * k;
            b = BoundsDe(go);
            go.transform.position = new Vector3(x, 0f, 40f) - new Vector3(b.center.x, b.min.y, b.center.z);
            x += Mathf.Max(b.size.x, 6f) + 6f;
            Debug.Log("TIENDA_VR: test " + p.name + " bounds=" + b.size.ToString("F1"));
        }

        // camara y foto
        var camGo = new GameObject("TestCam");
        var cam = camGo.AddComponent<Camera>();
        camGo.transform.position = new Vector3(0f, 6f, 18f);
        camGo.transform.LookAt(new Vector3(0f, 3f, 40f));
        cam.fieldOfView = 60f;

        // tambien: instanciar el pack completo tal cual y fotografiarlo
        var pack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ciudad/CityGameReady/scene.gltf");
        if (pack != null)
        {
            var full = (GameObject)PrefabUtility.InstantiatePrefab(pack);
            full.name = "TestPackCompleto";
            full.transform.position = Vector3.zero;
            // fotografiar la zona donde esta Building.001 dentro del pack
            var b1 = full.transform.Find("RootNode/Building.001");
            if (b1 == null) b1 = BuscarPorNombre(full.transform, "Building.001");
            if (b1 != null)
            {
                var rb = BoundsDe(b1.gameObject);
                var cam2Go = new GameObject("TestCam2");
                var cam2 = cam2Go.AddComponent<Camera>();
                var centro = rb.center;
                cam2Go.transform.position = centro + new Vector3(0f, 8f, -14f);
                cam2Go.transform.LookAt(centro);
                cam2.fieldOfView = 60f;
                var rt2 = new RenderTexture(1280, 720, 24);
                cam2.targetTexture = rt2;
                cam2.Render();
                RenderTexture.active = rt2;
                var tex2 = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex2.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex2.Apply();
                RenderTexture.active = null;
                cam2.targetTexture = null;
                System.IO.File.WriteAllBytes("/Users/yamiurbano/Desktop/PRACTICAS_ING.MIGUEL/CuatroPatasVR/_audit/test_pack_original.png", tex2.EncodeToPNG());
                Object.DestroyImmediate(cam2Go);
                Debug.Log("TIENDA_VR: foto del pack original en _audit/test_pack_original.png");
            }
            Object.DestroyImmediate(full);
        }
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        System.IO.File.WriteAllBytes("/Users/yamiurbano/Desktop/PRACTICAS_ING.MIGUEL/CuatroPatasVR/_audit/test_modelos.png", tex.EncodeToPNG());
        Object.DestroyImmediate(camGo);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("TIENDA_VR: test listo, foto en _audit/test_modelos.png");
    }

    private static Transform BuscarPorNombre(Transform t, string nombre)
    {
        if (t.name == nombre) return t;
        foreach (Transform h in t)
        {
            var r = BuscarPorNombre(h, nombre);
            if (r != null) return r;
        }
        return null;
    }

    private static Bounds BoundsDe(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
