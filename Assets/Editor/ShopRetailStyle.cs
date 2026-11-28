using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace CuatroPatasVR.EditorTools
{
    // Nivel 5: convertir el puesto de mercado en una tienda retail normal.
    // - Apaga toldo, postes y letrero tipo pizarra.
    // - Reemplaza los cubos "Producto deco" por modelos reales (latas, bolsas, botellas).
    // - Letrero limpio, techo, lamparas de techo y piso claro.
    public static class ShopRetailStyle
    {
        [MenuItem("Cuatro Patas/Tienda/8 - Estilo tienda retail (normal)")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TiendaVR.unity");
            var art = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name.Contains("Puesto provisional"));
            if (art == null) { Debug.LogWarning("TIENDA_VR: no se encontro el objeto de arte"); return; }

            // 1) Apagar elementos de puesto de mercado / pizarra
            foreach (var name in new[] { "Toldo", "Poste toldo", "Letrero", "Letrero - texto", "Alfombra" })
                foreach (var t in art.Cast<Transform>().Where(t => t.name == name).ToArray())
                    t.gameObject.SetActive(false);

            // 2) Reponer estanterias con productos reales
            var estanterias = art.Cast<Transform>().FirstOrDefault(t => t.name == "Estanterias");
            var modelPaths = new[]
            {
                "Assets/Models/Tienda/can.glb",
                "Assets/Models/Tienda/bag.glb",
                "Assets/Models/Tienda/bottle-oil.glb",
                "Assets/Models/Tienda/bag-flat.glb",
                "Assets/Models/Tienda/can.glb",
                "Assets/Models/Tienda/bag.glb",
            };
            if (estanterias != null)
            {
                var viejos = estanterias.Cast<Transform>().Where(t => t.name.StartsWith("Producto deco")).ToArray();
                int i = 0;
                foreach (var viejo in viejos)
                {
                    var pos = viejo.position;
                    Object.DestroyImmediate(viejo.gameObject);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPaths[i % modelPaths.Length]);
                    i++;
                    if (asset == null) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                    go.name = "Stock " + asset.name;
                    go.transform.SetParent(estanterias, true);
                    go.transform.position = pos;
                    // altura coherente de empaque y base apoyada en la tabla
                    var b = BoundsOf(go.transform);
                    if (b.size.y > 0.0001f) go.transform.localScale *= 0.26f / b.size.y;
                    b = BoundsOf(go.transform);
                    float shelfTop = pos.y < 1.4f ? 0.99f : 1.59f;
                    go.transform.position += new Vector3(0f, shelfTop - b.min.y, 0f);
                }
            }

            // 3) Letrero limpio (panel claro + texto de marca)
            var panelMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tienda/Panel.mat");
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Letrero tienda";
            Object.DestroyImmediate(sign.GetComponent<Collider>());
            sign.transform.SetParent(art, false);
            sign.transform.localPosition = new Vector3(0f, 2.75f, 3.45f);
            sign.transform.localScale = new Vector3(3.2f, 0.8f, 0.08f);
            if (panelMat != null) sign.GetComponent<MeshRenderer>().sharedMaterial = panelMat;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Fredoka SDF.asset");
            var texto = new GameObject("Letrero tienda - texto", typeof(TextMeshPro));
            texto.transform.SetParent(sign.transform, false);
            texto.transform.localPosition = new Vector3(0f, 0f, -0.51f);
            texto.transform.localScale = Vector3.one;
            var tmp = texto.GetComponent<TextMeshPro>();
            if (font != null) tmp.font = font;
            tmp.text = "CuatroPatas";
            tmp.fontSize = 0.9f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.16f, 0.45f, 0.38f); // verde petshop
            tmp.rectTransform.sizeDelta = new Vector2(3f, 0.7f);

            // 4) Techo a la altura real de las paredes
            var paredFondo = art.Cast<Transform>().FirstOrDefault(t => t.name == "Pared fondo");
            float techoY = 3.5f;
            if (paredFondo != null)
            {
                var r = paredFondo.GetComponentInChildren<Renderer>();
                if (r != null) techoY = r.bounds.max.y;
            }
            var crema = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tienda/Crema pared.mat");
            var techo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            techo.name = "Techo";
            Object.DestroyImmediate(techo.GetComponent<Collider>());
            techo.transform.SetParent(art, false);
            techo.transform.localPosition = new Vector3(0f, techoY + 0.05f, -0.5f);
            techo.transform.localScale = new Vector3(10.2f, 0.1f, 8.4f);
            if (crema != null) techo.GetComponent<MeshRenderer>().sharedMaterial = crema;

            // 5) Lamparas de techo (modelo + luz puntual calida)
            var lampAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Kenney/lampSquareCeiling.glb");
            foreach (var z in new[] { -2.2f, -0.2f, 1.8f })
            {
                if (lampAsset != null)
                {
                    var lamp = (GameObject)PrefabUtility.InstantiatePrefab(lampAsset);
                    lamp.name = "Lampara techo retail";
                    lamp.transform.SetParent(art, true);
                    lamp.transform.position = new Vector3(0f, techoY - 0.02f, z);
                }
                var luz = new GameObject("Luz techo retail");
                luz.transform.SetParent(art, false);
                luz.transform.localPosition = new Vector3(0f, techoY - 0.25f, z);
                var l = luz.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.96f, 0.88f);
                l.intensity = 1.1f;
                l.range = 7f;
            }

            // 6) Piso claro
            var suelo = art.Cast<Transform>().FirstOrDefault(t => t.name == "Suelo")
                ?? Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.name == "Suelo");
            if (suelo != null)
            {
                var piso = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                piso.name = "Piso claro";
                piso.color = new Color(0.84f, 0.81f, 0.76f);
                AssetDatabase.CreateAsset(piso, "Assets/Materials/Tienda/Piso claro.mat");
                var mr = suelo.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = piso;
            }

            // 7) Cajas de stock junto a las estanterias
            var cajaAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Kenney/cardboardBoxClosed.glb");
            if (cajaAsset != null)
            {
                foreach (var x in new[] { -3.9f, 4.1f })
                {
                    var caja = (GameObject)PrefabUtility.InstantiatePrefab(cajaAsset);
                    caja.name = "Caja de stock";
                    caja.transform.SetParent(art, true);
                    caja.transform.position = new Vector3(x, 0f, 2.7f);
                    var b = BoundsOf(caja.transform);
                    if (b.size.y > 0.0001f) caja.transform.localScale *= 0.45f / b.size.y;
                    b = BoundsOf(caja.transform);
                    caja.transform.position += new Vector3(0f, -b.min.y, 0f);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TIENDA_VR: estilo tienda retail aplicado (sin toldo ni pizarra, estanterias surtidas, letrero limpio, techo y luces).");
        }

        static Bounds BoundsOf(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 0.1f);
            var b = rs[0].bounds;
            foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
