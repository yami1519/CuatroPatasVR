using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CuatroPatasVR.EditorTools
{
    // Nivel 4: coherencia visual de productos.
    // - Borra el cubo placeholder "Visual lata - reemplazable" de la escena.
    // - Escala cada modelo Kenney a un tamaño real coherente.
    // - Ajusta collider, etiqueta y apoyo sobre el mostrador (top = 0.88).
    public static class ShopCoherenceFix
    {
        const float CounterTop = 0.88f;

        class Spec
        {
            public string Name;
            public string Dim;      // "x", "y" o "z": dimensión que manda
            public float Target;    // tamaño real deseado en esa dimensión (m)
            public float MinColH;   // alto mínimo de collider para poder agarrarlo en VR
        }

        [MenuItem("Cuatro Patas/Tienda/7 - Tamanos coherentes de productos")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TiendaVR.unity");

            // 1) Eliminar placeholder viejo que quedó colgado en la instancia de Comida
            var stray = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "Visual lata - reemplazable");
            if (stray != null) Object.DestroyImmediate(stray.gameObject);

            var specs = new[]
            {
                new Spec { Name = "Comida",  Dim = "y", Target = 0.28f, MinColH = 0.10f }, // bolsa de croquetas
                new Spec { Name = "Arena",   Dim = "x", Target = 0.30f, MinColH = 0.06f }, // saco ancho y plano
                new Spec { Name = "Lata",    Dim = "y", Target = 0.12f, MinColH = 0.06f }, // lata de atún
                new Spec { Name = "Shampoo", Dim = "y", Target = 0.24f, MinColH = 0.10f }, // botella
                new Spec { Name = "Premio",  Dim = "z", Target = 0.18f, MinColH = 0.04f }, // huesito plano
            };

            foreach (var spec in specs)
            {
                var root = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.name == spec.Name && t.GetComponent<Shop.ShopProduct>() != null);
                if (root == null) { Debug.LogWarning("TIENDA_VR: no se encontro " + spec.Name); continue; }

                var visual = root.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Visual"));
                if (visual == null) { Debug.LogWarning("TIENDA_VR: " + spec.Name + " sin visual"); continue; }

                // 2) Escala coherente según la dimensión que manda
                var wb = WorldBounds(visual);
                float current = spec.Dim == "x" ? wb.size.x : spec.Dim == "y" ? wb.size.y : wb.size.z;
                if (current > 0.0001f)
                    visual.localScale *= spec.Target / current;

                // 3) Collider ajustado al tamaño real (roots tienen escala 1 => mundo = local)
                wb = WorldBounds(visual);
                var size = new Vector3(
                    Mathf.Max(wb.size.x + 0.02f, 0.05f),
                    Mathf.Max(wb.size.y + 0.01f, spec.MinColH),
                    Mathf.Max(wb.size.z + 0.02f, 0.05f));
                var col = root.GetComponent<BoxCollider>();
                if (col != null) { col.size = size; col.center = Vector3.zero; }

                // 4) Root a la altura coherente sobre el mostrador
                root.position = new Vector3(root.position.x, CounterTop + size.y / 2f, 0.48f);

                // 5) Modelo apoyado en el mostrador y centrado en el root
                wb = WorldBounds(visual);
                visual.position += new Vector3(
                    root.position.x - wb.center.x,
                    CounterTop - wb.min.y,
                    root.position.z - wb.center.z);

                // 6) Etiqueta flotando sobre el modelo
                var label = root.Cast<Transform>().FirstOrDefault(t => t.name == "Nombre del producto");
                if (label != null) label.localPosition = new Vector3(0f, size.y / 2f + 0.03f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TIENDA_VR: tamanos coherentes aplicados (5 productos, etiquetas arriba, placeholder eliminado).");
        }

        static Bounds WorldBounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 0.1f);
            var b = rs[0].bounds;
            foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
