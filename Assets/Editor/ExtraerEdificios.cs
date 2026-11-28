using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Extrae los edificios individuales del pack CityGameReady (Sketchfab)
// y los guarda como prefabs reutilizables para el generador de ciudad.
public static class ExtraerEdificios
{
    private const string Pack = "Assets/Models/Ciudad/CityGameReady/scene.gltf";
    private const string Destino = "Assets/Models/Ciudad/CityGameReady/Edificios";

    [MenuItem("Cuatro Patas/Tienda/24 - Extraer edificios de CityGameReady")]
    public static void Extraer()
    {
        var pack = AssetDatabase.LoadAssetAtPath<GameObject>(Pack);
        if (pack == null) { Debug.LogError("TIENDA_VR: no se encontro " + Pack); return; }

        if (!AssetDatabase.IsValidFolder(Destino))
            AssetDatabase.CreateFolder("Assets/Models/Ciudad/CityGameReady", "Edificios");

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(pack);
        // El .gltf entra como prefab de modelo: hay que desempaquetar por completo
        // o sus hijos no se pueden guardar como prefabs nuevos.
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        int guardados = 0, saltados = 0;
        var candidatos = new List<Transform>();
        foreach (Transform t in inst.transform)
            BuscarEdificios(t, candidatos);

        foreach (var t in candidatos)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { saltados++; continue; }
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            // edificio usable: ni muy chico ni un bloque entero
            if (b.size.y < 2f || b.size.x > 60f || b.size.z > 60f) { saltados++; continue; }

            string ruta = Destino + "/" + t.name.Replace(".", "_") + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(t.gameObject, ruta);
            guardados++;
            Debug.Log("TIENDA_VR: edificio " + t.name + " tamaño " +
                      b.size.x.ToString("F1") + "x" + b.size.y.ToString("F1") + "x" + b.size.z.ToString("F1"));
        }
        Object.DestroyImmediate(inst);
        AssetDatabase.SaveAssets();
        Debug.Log("TIENDA_VR: extraccion lista. " + guardados + " edificios guardados en " + Destino + ", " + saltados + " saltados.");
    }

    // Los edificios son los nodos 'Building.XXX' que cuelgan del RootNode del pack.
    private static void BuscarEdificios(Transform t, List<Transform> lista)
    {
        if (t.name.StartsWith("Building"))
        {
            lista.Add(t);
            return;
        }
        foreach (Transform h in t) BuscarEdificios(h, lista);
    }
}
