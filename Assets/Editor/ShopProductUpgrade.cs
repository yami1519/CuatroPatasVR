using System.Linq;
using CuatroPatasVR.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CuatroPatasVR.Shop.Editor
{
    // Reemplaza los visuales primitivos por modelos Kenney (Food Kit, CC0) y
    // amplia la tienda a 5 productos con un turno de 10 pedidos.
    public static class ShopProductUpgrade
    {
        const string ScenePath = "Assets/Scenes/TiendaVR.unity";
        const string Models = "Assets/Models/Tienda/";
        const string Productos = "Assets/Productos/";
        const string Prefabs = "Assets/Prefabs/Tienda/";

        [MenuItem("Cuatro Patas/Tienda/6 - Productos reales (5 productos)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Sal de Play antes de mejorar los productos."); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var gameplay = scene.GetRootGameObjects().FirstOrDefault(go => go.name.StartsWith("02 - Pedidos"));
            if (gameplay == null) { Debug.LogWarning("No se encontro el grupo 02 - Pedidos."); return; }

            // 1) Visuales reales para los productos existentes
            SwapVisual(Prefabs + "Comida.prefab", "bag");       // bolsa de croquetas
            SwapVisual(Prefabs + "Arena.prefab", "bag-flat");   // saco de arena

            // 2) Nuevas definiciones de producto
            var lata = Definition("Lata", "Lata de atun para gato");
            var shampoo = Definition("Shampoo", "Shampoo para gato");
            var premio = Definition("Premio", "Premio (hueso de pescado)");

            // 3) Fila de 5 productos sobre el mostrador
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Fredoka SDF.asset");
            MoveExisting(gameplay.transform, "Comida", -0.72f);
            MoveExisting(gameplay.transform, "Arena", -0.36f);
            EnsureProduct("Lata", lata, "can", new Vector3(0f, 1.01f, 0.48f), gameplay.transform, font);
            EnsureProduct("Shampoo", shampoo, "bottle-oil", new Vector3(0.36f, 1.01f, 0.48f), gameplay.transform, font);
            EnsureProduct("Premio", premio, "fish-bones", new Vector3(0.72f, 1.01f, 0.48f), gameplay.transform, font);

            // 4) Turno de 10 pedidos mezclando los 5 productos
            var comida = AssetDatabase.LoadAssetAtPath<ShopProductDefinition>(Productos + "Comida.asset");
            var arena = AssetDatabase.LoadAssetAtPath<ShopProductDefinition>(Productos + "Arena.asset");
            var session = gameplay.GetComponent<ShopSession>();
            if (session != null)
            {
                var seq = new[] { comida, arena, lata, comida, shampoo, arena, premio, lata, shampoo, premio };
                var so = new SerializedObject(session);
                var orders = so.FindProperty("orders");
                orders.arraySize = seq.Length;
                for (int i = 0; i < seq.Length; i++) orders.GetArrayElementAtIndex(i).objectReferenceValue = seq[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TIENDA_VR: 5 productos con modelos reales y turno de 10 pedidos.");
        }

        static void SwapVisual(string prefabPath, string glb)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            foreach (var child in root.transform.Cast<Transform>().Select(t => t.gameObject).ToArray())
                if (child.name.StartsWith("Visual")) Object.DestroyImmediate(child);
            var visual = MakeVisual(glb);
            visual.transform.SetParent(root.transform, false);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void MoveExisting(Transform parent, string name, float x)
        {
            var p = parent.Find(name);
            if (p != null) p.position = new Vector3(x, 1.01f, 0.48f);
        }

        static void EnsureProduct(string name, ShopProductDefinition def, string glb, Vector3 pos, Transform parent, TMP_FontAsset font)
        {
            var existing = parent.Find(name);
            if (existing != null) { existing.position = pos; return; }
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.AddComponent<BoxCollider>().size = new Vector3(0.21f, 0.24f, 0.18f);
            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.25f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var visual = MakeVisual(glb);
            visual.transform.SetParent(go.transform, false);
            var grab = go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Kinematic; grab.throwOnDetach = false;
            var so = new SerializedObject(go.AddComponent<ShopProduct>());
            so.FindProperty("definition").objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
            var label = new GameObject("Nombre del producto", typeof(TextMeshPro));
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0, 0.02f, -0.095f);
            var tmp = label.GetComponent<TextMeshPro>();
            if (font != null) tmp.font = font;
            tmp.text = name.ToUpperInvariant();
            tmp.fontSize = 0.45f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(0.2f, 0.12f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, Prefabs + name + ".prefab", InteractionMode.AutomatedAction);
        }

        static ShopProductDefinition Definition(string name, string display)
        {
            string path = Productos + name + ".asset";
            var v = AssetDatabase.LoadAssetAtPath<ShopProductDefinition>(path);
            if (v != null) { v.displayName = display; EditorUtility.SetDirty(v); return v; }
            v = ScriptableObject.CreateInstance<ShopProductDefinition>(); v.displayName = display;
            AssetDatabase.CreateAsset(v, path);
            return v;
        }

        // Instancia el GLB, lo escala a ~0.22 de alto y lo centra en el origen del producto.
        static GameObject MakeVisual(string glb)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + glb + ".glb");
            if (asset == null) { Debug.LogWarning("Falta el modelo: " + Models + glb + ".glb"); return new GameObject("Visual faltante"); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = "Visual " + glb + " - modelo Kenney";
            var b = CalcBounds(go);
            float scale = 0.22f / Mathf.Max(b.size.y, 0.0001f);
            go.transform.localScale = Vector3.one * scale;
            b = CalcBounds(go);
            go.transform.localPosition = -b.center;
            return go;
        }

        static Bounds CalcBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
