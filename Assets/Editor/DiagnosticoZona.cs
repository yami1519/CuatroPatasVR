using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Text;

// Diagnostico: que objetos tocan la zona de la casa y el patio.
public static class DiagnosticoZona
{
    [MenuItem("Cuatro Patas/Tienda/22 - Diagnosticar zona de la casa")]
    public static void Diagnosticar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PuebloVR.unity");
        var sb = new StringBuilder();
        sb.AppendLine("TIENDA_VR: ==== DIAGNOSTICO ZONA CASA ====");

        var casa = GameObject.Find("Casa");
        if (casa == null) { Debug.LogError("TIENDA_VR: no hay objeto Casa"); return; }
        Bounds bCasa = BoundsDe(casa);
        sb.AppendLine("Casa pos=" + casa.transform.position + " bounds min=" + bCasa.min + " max=" + bCasa.max);

        // Hijos directos de la casa (para ver si la cerca esta DENTRO del prefab)
        sb.AppendLine("-- Hijos de Casa: " + casa.transform.childCount);
        foreach (Transform h in casa.transform)
            sb.AppendLine("   hijo: " + h.name + " pos=" + h.position);

        // Zona amplia casa + patio
        Vector3 cMin = bCasa.min - new Vector3(6f, 2f, 12f);
        Vector3 cMax = bCasa.max + new Vector3(6f, 2f, 12f);
        sb.AppendLine("-- Zona vigilada min=" + cMin + " max=" + cMax);

        // Todos los objetos raiz de la escena
        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in escena.GetRootGameObjects())
        {
            sb.AppendLine("RAIZ: " + root.name + " pos=" + root.transform.position + " hijos=" + root.transform.childCount);
        }

        // Objetos de primer nivel del pueblo que intersectan la zona (sin ser la casa)
        var pueblo = GameObject.Find("Pueblo Kenney");
        if (pueblo != null)
        {
            foreach (Transform h in pueblo.transform)
            {
                var b = BoundsDe(h.gameObject);
                if (Intersecta(b, cMin, cMax))
                    sb.AppendLine("TOCA ZONA: " + h.name + " pos=" + h.position + " min=" + b.min + " max=" + b.max);
            }
        }
        // Y cualquier otro objeto raiz que toque la zona
        foreach (var root in escena.GetRootGameObjects())
        {
            if (root.name == "Casa" || root.name == "Pueblo Kenney") continue;
            var b = BoundsDe(root);
            if (Intersecta(b, cMin, cMax))
                sb.AppendLine("RAIZ TOCA ZONA: " + root.name + " pos=" + root.transform.position + " min=" + b.min + " max=" + b.max);
        }
        sb.AppendLine("==== FIN DIAGNOSTICO ====");
        Debug.Log(sb.ToString());
    }

    private static bool Intersecta(Bounds b, Vector3 min, Vector3 max)
    {
        return b.max.x >= min.x && b.min.x <= max.x &&
               b.max.z >= min.z && b.min.z <= max.z &&
               b.max.y >= min.y && b.min.y <= max.y;
    }

    private static Bounds BoundsDe(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
