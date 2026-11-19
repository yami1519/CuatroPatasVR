using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace CuatroPatasVR.Shop.Editor
{
    // Mejora visual del puesto provisional de TiendaVR (sin tocar logica ni interacciones).
    public static class ShopBeautify
    {
        const string Root = "Assets/Materials/Tienda";
        const string ScenePath = "Assets/Scenes/TiendaVR.unity";

        [MenuItem("Cuatro Patas/Tienda/3 - Mejorar puesto provisional")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Sal de Play antes de mejorar la escena."); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var env = scene.GetRootGameObjects().FirstOrDefault(go => go.name.StartsWith("01 - Puesto"));
            var gameplay = scene.GetRootGameObjects().FirstOrDefault(go => go.name.StartsWith("02 - Pedidos"));
            if (env == null || gameplay == null) { Debug.LogWarning("No se encontraron los grupos 01/02 de la escena."); return; }

            // ---- Materiales ----
            var madera = Mat("Madera", new Color(0.45f, 0.30f, 0.18f));
            var maderaClara = Mat("Madera clara", new Color(0.66f, 0.47f, 0.30f));
            var crema = Mat("Crema pared", new Color(0.93f, 0.88f, 0.78f));
            var terracota = Mat("Terracota toldo", new Color(0.78f, 0.42f, 0.30f));
            var verdeLata = Mat("Verde lata", new Color(0.25f, 0.60f, 0.40f));
            var amarilloBolsa = Mat("Amarillo bolsa", new Color(0.90f, 0.70f, 0.25f));
            var azulSuave = Mat("Azul bandeja", new Color(0.35f, 0.58f, 0.75f));

            // ---- Cielo y ambiente (adios vacio gris) ----
            var sky = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Cielo.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, Root + "/Cielo.mat");
            }
            sky.SetFloat("_SunSize", 0.05f);
            sky.SetColor("_SkyTint", new Color(0.62f, 0.72f, 0.85f));
            sky.SetFloat("_AtmosphereThickness", 0.9f);
            sky.SetColor("_GroundColor", new Color(0.80f, 0.75f, 0.68f));
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.05f;
            RenderSettings.fog = false;

            // ---- Luz calida con sombras suaves ----
            var luz = env.transform.Find("Luz provisional");
            if (luz != null)
            {
                var light = luz.GetComponent<Light>();
                light.color = new Color(1f, 0.95f, 0.84f);
                light.intensity = 1.35f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.75f;
                luz.name = "Luz principal";
                luz.rotation = Quaternion.Euler(50, -30, 0);
            }
            // Luz de relleno suave desde el frente
            if (env.transform.Find("Luz de relleno") == null)
            {
                var fill = new GameObject("Luz de relleno").AddComponent<Light>();
                fill.type = LightType.Directional; fill.intensity = 0.35f;
                fill.color = new Color(0.85f, 0.90f, 1f); fill.shadows = LightShadows.None;
                fill.transform.rotation = Quaternion.Euler(30, 155, 0);
                fill.transform.SetParent(env.transform);
            }

            // ---- Suelo amplio de madera ----
            var suelo = env.transform.Find("Suelo");
            if (suelo != null)
            {
                suelo.localScale = new Vector3(14, 0.2f, 14);
                suelo.GetComponent<Renderer>().sharedMaterial = maderaClara;
            }

            // ---- Paredes: cerrar el local ----
            Pared(env.transform, "Pared fondo", new Vector3(0, 1.75f, 3.6f), new Vector3(14f, 3.5f, 0.2f), crema);
            Pared(env.transform, "Pared izquierda", new Vector3(-5f, 1.75f, -0.5f), new Vector3(0.2f, 3.5f, 8.4f), crema);
            Pared(env.transform, "Pared derecha", new Vector3(5f, 1.75f, -0.5f), new Vector3(0.2f, 3.5f, 8.4f), crema);
            // Zocalo de madera en la pared del fondo
            Pared(env.transform, "Zocalo fondo", new Vector3(0, 0.25f, 3.48f), new Vector3(14f, 0.5f, 0.06f), madera);

            // ---- Toldo sobre el mostrador ----
            if (env.transform.Find("Toldo") == null)
            {
                var toldo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                toldo.name = "Toldo"; toldo.transform.SetParent(env.transform);
                toldo.transform.position = new Vector3(0, 2.45f, 0.9f);
                toldo.transform.rotation = Quaternion.Euler(12, 0, 0);
                toldo.transform.localScale = new Vector3(2.6f, 0.04f, 1.1f);
                toldo.GetComponent<Renderer>().sharedMaterial = terracota;
                Object.DestroyImmediate(toldo.GetComponent<Collider>());
                for (int i = -1; i <= 1; i += 2)
                {
                    var poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    poste.name = "Poste toldo"; poste.transform.SetParent(env.transform);
                    poste.transform.position = new Vector3(1.15f * i, 1.25f, 1.35f);
                    poste.transform.localScale = new Vector3(0.05f, 1.25f, 0.05f);
                    poste.GetComponent<Renderer>().sharedMaterial = madera;
                }
            }

            // ---- Marco del panel de pedidos ----
            var panel = env.transform.Find("Panel de pedidos");
            if (panel != null && env.transform.Find("Marco panel") == null)
            {
                var marco = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marco.name = "Marco panel"; marco.transform.SetParent(env.transform);
                marco.transform.position = panel.position + new Vector3(0, 0, 0.05f);
                marco.transform.localScale = panel.localScale + new Vector3(0.12f, 0.12f, 0.02f);
                marco.GetComponent<Renderer>().sharedMaterial = madera;
            }

            // ---- Mostrador con frente de madera ----
            foreach (var nombre in new[] { "Soporte izquierdo", "Soporte derecho" })
            {
                var sop = env.transform.Find(nombre);
                if (sop != null) sop.GetComponent<Renderer>().sharedMaterial = madera;
            }

            // ---- Bandeja con color mas amable ----
            var bandeja = gameplay.transform.Find("Bandeja - base");
            if (bandeja != null) bandeja.GetComponent<Renderer>().sharedMaterial = azulSuave;

            // ---- Productos: lata de comida (cilindro) y bolsa de arena ----
            RestyleProduct(gameplay.transform, "Comida", verdeLata, true);
            RestyleProduct(gameplay.transform, "Arena", amarilloBolsa, false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TIENDA_VR: puesto provisional mejorado (cielo, paredes, toldo, madera, productos).");
        }

        static void Pared(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            if (parent.Find(name) != null) return;
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = name; p.transform.SetParent(parent);
            p.transform.position = pos; p.transform.localScale = size;
            p.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void RestyleProduct(Transform parent, string name, Material mat, bool lata)
        {
            var prod = parent.Find(name);
            if (prod == null) return;
            var visual = prod.Find("Visual provisional - reemplazable");
            if (visual == null) return;
            // En lata: reemplazar el cubo por un cilindro
            if (lata && visual.GetComponent<MeshFilter>().sharedMesh != null && !visual.name.Contains("lata"))
            {
                var pos = visual.position;
                Object.DestroyImmediate(visual.gameObject);
                var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = "Visual lata - reemplazable";
                cyl.transform.SetParent(prod, false);
                cyl.transform.position = pos;
                cyl.transform.localScale = new Vector3(0.16f, 0.12f, 0.16f);
                Object.DestroyImmediate(cyl.GetComponent<Collider>());
                cyl.GetComponent<Renderer>().sharedMaterial = mat;
            }
            else
            {
                visual.GetComponent<Renderer>().sharedMaterial = mat;
                if (!lata) visual.rotation = Quaternion.Euler(0, 12, 0);
            }
        }

        [MenuItem("Cuatro Patas/Tienda/4 - Decorar tienda (nivel 2)")]
        public static void Decorate()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Sal de Play antes de decorar la escena."); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var env = scene.GetRootGameObjects().FirstOrDefault(go => go.name.StartsWith("01 - Puesto"));
            if (env == null) { Debug.LogWarning("No se encontro el grupo 01 - Puesto."); return; }
            var t = env.transform;

            var madera = Mat("Madera", new Color(0.45f, 0.30f, 0.18f));
            var maderaOscura = Mat("Madera oscura", new Color(0.32f, 0.20f, 0.12f));
            var terracota = Mat("Terracota toldo", new Color(0.78f, 0.42f, 0.30f));
            var crema = Mat("Crema pared", new Color(0.93f, 0.88f, 0.78f));
            var verde = Mat("Verde lata", new Color(0.25f, 0.60f, 0.40f));
            var amarillo = Mat("Amarillo bolsa", new Color(0.90f, 0.70f, 0.25f));
            var azul = Mat("Azul bandeja", new Color(0.35f, 0.58f, 0.75f));
            var oscuro = Mat("Panel", new Color(0.08f, 0.16f, 0.14f));
            var salvia = Mat("Salvia planta", new Color(0.45f, 0.60f, 0.40f));
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/Fonts/Fredoka SDF.asset");

            // ---- Tablones del suelo (lineas oscuras) ----
            if (t.Find("Tablones") == null)
            {
                var grupo = new GameObject("Tablones"); grupo.transform.SetParent(t);
                for (int i = 0; i < 9; i++)
                {
                    var linea = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    linea.name = "Junta"; linea.transform.SetParent(grupo.transform);
                    linea.transform.position = new Vector3(0, 0.002f, -6f + i * 1.5f);
                    linea.transform.localScale = new Vector3(14f, 0.004f, 0.025f);
                    linea.GetComponent<Renderer>().sharedMaterial = maderaOscura;
                    Object.DestroyImmediate(linea.GetComponent<Collider>());
                }
            }

            // ---- Vigas de madera en esquinas del fondo ----
            foreach (var sx in new[] { -1, 1 })
            {
                var nombre = sx < 0 ? "Viga izquierda" : "Viga derecha";
                if (t.Find(nombre) != null) continue;
                var viga = GameObject.CreatePrimitive(PrimitiveType.Cube);
                viga.name = nombre; viga.transform.SetParent(t);
                viga.transform.position = new Vector3(4.85f * sx, 1.75f, 3.4f);
                viga.transform.localScale = new Vector3(0.25f, 3.5f, 0.25f);
                viga.GetComponent<Renderer>().sharedMaterial = madera;
            }

            // ---- Estanterias con productos de decoracion ----
            if (t.Find("Estanterias") == null)
            {
                var est = new GameObject("Estanterias"); est.transform.SetParent(t);
                var colores = new[] { verde, amarillo, azul, terracota, crema };
                foreach (var sx in new[] { -2.9f, 2.9f })
                {
                    foreach (var nivel in new[] { 0.95f, 1.55f })
                    {
                        var tabla = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        tabla.name = "Tabla"; tabla.transform.SetParent(est.transform);
                        tabla.transform.position = new Vector3(sx, nivel, 3.15f);
                        tabla.transform.localScale = new Vector3(2.3f, 0.06f, 0.4f);
                        tabla.GetComponent<Renderer>().sharedMaterial = madera;
                        // productos encima
                        int n = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            var prod = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                            prod.name = "Producto deco"; prod.transform.SetParent(est.transform);
                            float px = sx - 0.85f + i * 0.42f;
                            prod.transform.position = new Vector3(px, nivel + 0.17f, 3.15f);
                            prod.transform.localScale = i % 2 == 0 ? new Vector3(0.16f, 0.14f, 0.16f) : new Vector3(0.2f, 0.26f, 0.14f);
                            prod.transform.rotation = Quaternion.Euler(0, (i * 17) % 30 - 15, 0);
                            prod.GetComponent<Renderer>().sharedMaterial = colores[(n + (sx > 0 ? 2 : 0)) % colores.Length];
                            Object.DestroyImmediate(prod.GetComponent<Collider>());
                            n++;
                        }
                    }
                }
            }

            // ---- Letrero de la tienda ----
            if (t.Find("Letrero") == null)
            {
                var letrero = GameObject.CreatePrimitive(PrimitiveType.Cube);
                letrero.name = "Letrero"; letrero.transform.SetParent(t);
                letrero.transform.position = new Vector3(0, 2.85f, 3.45f);
                letrero.transform.localScale = new Vector3(2.8f, 0.65f, 0.08f);
                letrero.GetComponent<Renderer>().sharedMaterial = madera;
                var texto = new GameObject("Letrero - texto", typeof(TMPro.TextMeshPro));
                texto.transform.SetParent(t);
                texto.transform.position = new Vector3(0, 2.85f, 3.40f);
                texto.transform.rotation = Quaternion.Euler(0, 180, 0);
                var tmp = texto.GetComponent<TMPro.TextMeshPro>();
                tmp.text = "TIENDA CUATRO PATAS"; if (font != null) tmp.font = font;
                tmp.fontSize = 4.2f; tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = new Color(0.93f, 0.88f, 0.78f); tmp.rectTransform.sizeDelta = new Vector2(2.7f, 0.55f);
            }

            // ---- Alfombra frente al mostrador ----
            if (t.Find("Alfombra") == null)
            {
                var alf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                alf.name = "Alfombra"; alf.transform.SetParent(t);
                alf.transform.position = new Vector3(0, 0.004f, -1.1f);
                alf.transform.localScale = new Vector3(2.4f, 0.003f, 1.6f);
                alf.GetComponent<Renderer>().sharedMaterial = terracota;
                Object.DestroyImmediate(alf.GetComponent<Collider>());
            }

            // ---- Caja registradora sobre el mostrador ----
            if (t.Find("Caja registradora") == null)
            {
                var caja = new GameObject("Caja registradora"); caja.transform.SetParent(t);
                var baseCaja = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseCaja.name = "Base"; baseCaja.transform.SetParent(caja.transform);
                baseCaja.transform.position = new Vector3(0.72f, 0.94f, 0.42f);
                baseCaja.transform.localScale = new Vector3(0.28f, 0.12f, 0.22f);
                baseCaja.GetComponent<Renderer>().sharedMaterial = oscuro;
                var display = GameObject.CreatePrimitive(PrimitiveType.Cube);
                display.name = "Display"; display.transform.SetParent(caja.transform);
                display.transform.position = new Vector3(0.72f, 1.06f, 0.47f);
                display.transform.rotation = Quaternion.Euler(-18, 180, 0);
                display.transform.localScale = new Vector3(0.2f, 0.1f, 0.02f);
                display.GetComponent<Renderer>().sharedMaterial = verde;
            }

            // ---- Planta en la esquina ----
            if (t.Find("Planta") == null)
            {
                var planta = new GameObject("Planta"); planta.transform.SetParent(t);
                var maceta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                maceta.name = "Maceta"; maceta.transform.SetParent(planta.transform);
                maceta.transform.position = new Vector3(-4.2f, 0.22f, 2.6f);
                maceta.transform.localScale = new Vector3(0.36f, 0.22f, 0.36f);
                maceta.GetComponent<Renderer>().sharedMaterial = terracota;
                var tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tronco.name = "Tronco"; tronco.transform.SetParent(planta.transform);
                tronco.transform.position = new Vector3(-4.2f, 0.6f, 2.6f);
                tronco.transform.localScale = new Vector3(0.05f, 0.2f, 0.05f);
                tronco.GetComponent<Renderer>().sharedMaterial = madera;
                Object.DestroyImmediate(tronco.GetComponent<Collider>());
                foreach (var (dy, s) in new[] { (0.95f, 0.5f), (1.25f, 0.38f), (1.5f, 0.26f) })
                {
                    var hoja = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    hoja.name = "Copa"; hoja.transform.SetParent(planta.transform);
                    hoja.transform.position = new Vector3(-4.2f, dy, 2.6f);
                    hoja.transform.localScale = Vector3.one * s;
                    hoja.GetComponent<Renderer>().sharedMaterial = salvia;
                    Object.DestroyImmediate(hoja.GetComponent<Collider>());
                }
            }

            // ---- Luces puntuales calidas sobre el mostrador ----
            if (t.Find("Luces colgantes") == null)
            {
                var grupo = new GameObject("Luces colgantes"); grupo.transform.SetParent(t);
                foreach (var sx in new[] { -0.8f, 0.8f })
                {
                    var cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cable.name = "Cable"; cable.transform.SetParent(grupo.transform);
                    cable.transform.position = new Vector3(sx, 2.7f, 0.6f);
                    cable.transform.localScale = new Vector3(0.012f, 0.4f, 0.012f);
                    cable.GetComponent<Renderer>().sharedMaterial = oscuro;
                    Object.DestroyImmediate(cable.GetComponent<Collider>());
                    var foco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    foco.name = "Foco"; foco.transform.SetParent(grupo.transform);
                    foco.transform.position = new Vector3(sx, 2.28f, 0.6f);
                    foco.transform.localScale = Vector3.one * 0.12f;
                    var focoMat = Mat("Ambar foco", new Color(1f, 0.85f, 0.55f));
                    focoMat.EnableKeyword("_EMISSION");
                    focoMat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.45f) * 1.6f);
                    foco.GetComponent<Renderer>().sharedMaterial = focoMat;
                    Object.DestroyImmediate(foco.GetComponent<Collider>());
                    var pl = new GameObject("Luz puntual").AddComponent<Light>();
                    pl.type = LightType.Point; pl.range = 4f; pl.intensity = 1.1f;
                    pl.color = new Color(1f, 0.88f, 0.66f); pl.shadows = LightShadows.None;
                    pl.transform.SetParent(grupo.transform);
                    pl.transform.position = new Vector3(sx, 2.2f, 0.6f);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TIENDA_VR: decoracion nivel 2 aplicada (estanterias, letrero, alfombra, caja, planta, luces).");
        }

        [MenuItem("Cuatro Patas/Tienda/5 - Decorar tienda (nivel 3 - Kenney)")]
        public static void Decorate3()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Sal de Play antes de decorar la escena."); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var env = scene.GetRootGameObjects().FirstOrDefault(go => go.name.StartsWith("01 - Puesto"));
            if (env == null) { Debug.LogWarning("No se encontro el grupo 01 - Puesto."); return; }
            if (env.transform.Find("Decoracion Kenney") != null) { Debug.Log("La decoracion Kenney ya existe; no se duplica."); return; }

            var grupo = new GameObject("Decoracion Kenney");
            grupo.transform.SetParent(env.transform);

            // Estanterias grandes contra la pared del fondo
            Place(grupo.transform, "bookcaseOpen", new Vector3(-4.1f, 0f, 3.05f), 180f);
            Place(grupo.transform, "bookcaseOpen", new Vector3(4.1f, 0f, 3.05f), 180f);
            // Osito de peluche sobre el mostrador
            Place(grupo.transform, "bear", new Vector3(0.55f, 0.88f, 0.45f), -25f, 0.8f);
            // Cajas de carton apiladas junto a la pared izquierda
            Place(grupo.transform, "cardboardBoxClosed", new Vector3(-3.9f, 0f, 1.6f), 12f);
            Place(grupo.transform, "cardboardBoxOpen", new Vector3(-3.3f, 0f, 1.9f), -18f);
            Place(grupo.transform, "cardboardBoxClosed", new Vector3(-3.75f, 0.52f, 1.7f), 40f, 0.85f);
            // Mesa auxiliar con radio junto a la pared izquierda
            Place(grupo.transform, "sideTable", new Vector3(-4.4f, 0f, -0.6f), 90f);
            Place(grupo.transform, "radio", new Vector3(-4.4f, 0.62f, -0.6f), 115f, 0.9f);
            // Lampara de pie junto a la pared derecha
            Place(grupo.transform, "lampSquareFloor", new Vector3(4.4f, 0f, -0.6f), -90f);
            // Planta en maceta (esquina derecha del fondo)
            Place(grupo.transform, "pottedPlant", new Vector3(3.4f, 0f, 2.9f), 0f);
            // Taburete detras del mostrador
            Place(grupo.transform, "stoolBarSquare", new Vector3(0f, 0f, 1.5f), 10f);
            // Papelera junto al mostrador
            Place(grupo.transform, "trashcan", new Vector3(1.35f, 0f, 1.25f), 0f);
            // Perchero en la esquina izquierda del fondo
            Place(grupo.transform, "coatRackStanding", new Vector3(-4.5f, 0f, 2.9f), 30f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TIENDA_VR: decoracion nivel 3 aplicada (modelos Kenney).");
        }

        static void Place(Transform parent, string model, Vector3 pos, float rotY, float scale = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Kenney/" + model + ".glb");
            if (asset == null) { Debug.LogWarning("No se encontro el modelo Kenney: " + model); return; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = model;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, rotY, 0);
            go.transform.localScale = Vector3.one * scale;
        }

        static Material Mat(string name, Color color)
        {
            string path = Root + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) { m.color = color; return m; }
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
