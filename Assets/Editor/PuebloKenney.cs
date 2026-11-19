using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace CuatroPatasVR.EditorTools
{
    // Ciudad grande con modelos Kenney: rejilla de 9 calles, ~300 casas suburban
    // colocadas segun su huella real (sin traslaparse), negocios, autos y arboles.
    public static class PuebloKenney
    {
        private const string Escena = "Assets/Scenes/PuebloVR.unity";
        private const string KitSuburban = "Assets/Models/Kits/city-kit-suburban/Models/GLB format/";
        private const string KitModular = "Assets/Models/Kits/modular-buildings/Models/GLB format/";
        private const string KitCarros = "Assets/Models/Kits/car-kit/Models/GLB format/";
        private const string CarpetaMat = "Assets/Materials/Pueblo";

        private static readonly string[] Casas = {
            "building-type-a","building-type-b","building-type-c","building-type-d",
            "building-type-e","building-type-f","building-type-g","building-type-h",
            "building-type-i","building-type-j","building-type-k","building-type-l",
            "building-type-m","building-type-n","building-type-o","building-type-p",
            "building-type-q","building-type-r","building-type-s","building-type-t",
            "building-type-u"
        };

        private static readonly string[] Carros = {
            "sedan","taxi","hatchback-sports","suv","delivery","police","van","race"
        };

        // Calles horizontales (eje X) y verticales (eje Z)
        private static readonly float[] CallesH = { 16f, -16f, -48f, -80f, -112f, -144f };
        private static readonly float[] CallesV = { -58f, -26f, 40f, 72f };
        private const float XIni = -80f, XFin = 104f;
        private const float ZIni = 28f, ZFin = -156f;

        [MenuItem("Cuatro Patas/Tienda/11 - Pueblo con modelos Kenney")]
        public static void Build()
        {
            EditorSceneManager.OpenScene(Escena);

            foreach (var nombre in new[] { "Pueblo - calle y fachadas", "Pueblo Kenney" })
            {
                var viejo = GameObject.Find(nombre);
                if (viejo != null) Object.DestroyImmediate(viejo);
            }

            var root = new GameObject("Pueblo Kenney");
            var rng = new System.Random(11);

            var matPasto = MaterialSimple("Pasto", new Color(0.52f, 0.74f, 0.42f));
            var matAsfalto = MaterialSimple("Asfalto", new Color(0.24f, 0.24f, 0.27f));
            var matVereda = MaterialSimple("Vereda", new Color(0.78f, 0.76f, 0.72f));
            var matLinea = MaterialSimple("LineaCalle", new Color(0.95f, 0.95f, 0.9f));
            var matGrifo = MaterialSimple("Grifo", new Color(0.7f, 0.72f, 0.75f));
            var matAgua = MaterialSimple("AguaGrifo", new Color(0.4f, 0.65f, 0.85f));

            // ---------- suelo ----------
            var pasto = GameObject.CreatePrimitive(PrimitiveType.Plane);
            pasto.name = "Pasto del pueblo";
            pasto.transform.SetParent(root.transform);
            pasto.transform.position = new Vector3(12f, -0.02f, -64f);
            pasto.transform.localScale = new Vector3(21f, 1f, 21f); // 210 x 210
            pasto.GetComponent<Renderer>().sharedMaterial = matPasto;

            // ---------- calles ----------
            foreach (var z in CallesH) CalleHorizontal(root, z, matAsfalto, matLinea, matVereda);
            foreach (var x in CallesV) CalleVertical(root, x, matAsfalto, matLinea, matVereda);

            // Rangos prohibidos por calles (ancho 5 + margen)
            var saltosX = new List<Vector2>();
            foreach (var x in CallesV) saltosX.Add(new Vector2(x - 5.5f, x + 5.5f));
            var saltosZ = new List<Vector2>();
            foreach (var z in CallesH) saltosZ.Add(new Vector2(z - 5.5f, z + 5.5f));

            // ---------- tienda y negocios frente a la calle principal ----------
            var tienda = GameObject.Find("Tienda CuatroPatas");
            if (tienda != null) tienda.transform.position = new Vector3(16f, 0f, -22.5f);
            Negocio(root, KitModular + "building-sample-house-a.glb", "VETERINARIA", -14f, new Color(0.2f, 0.55f, 0.9f), 5.5f);
            Negocio(root, KitModular + "building-sample-house-b.glb", "BODEGA", -6f, new Color(0.9f, 0.55f, 0.15f), 5.5f);
            Negocio(root, KitModular + "building-sample-house-c.glb", "PASTELERIA", 30f, new Color(0.95f, 0.45f, 0.65f), 5.5f);
            Negocio(root, KitModular + "building-sample-tower-a.glb", "POLLERIA", 60f, new Color(0.9f, 0.25f, 0.2f), 7f);

            // ---------- CASAS (colocacion por huella real, sin traslapes) ----------
            int totalCasas = 0;
            foreach (var z in CallesH)
            {
                // fila norte de la calle (z + 7), mirando al sur (hacia la calle)
                var saltos = new List<Vector2>(saltosX);
                if (Mathf.Abs(z - 16f) < 1f) saltos.Add(new Vector2(-12f, 14f)); // zona de la casa del jugador
                totalCasas += FilaCasas(root, z + 7f, 180f, saltos, rng);
                // fila sur de la calle (z - 7.5), mirando al norte
                saltos = new List<Vector2>(saltosX);
                if (Mathf.Abs(z + 16f) < 1f)
                {
                    saltos.Add(new Vector2(-12f, 14f));                 // patio/casa
                    saltos.Add(new Vector2(-19f, -1f));                 // veterinaria + bodega
                    saltos.Add(new Vector2(11f, 21f));                  // tienda
                    saltos.Add(new Vector2(24f, 36f));                  // pasteleria
                    saltos.Add(new Vector2(54f, 66f));                  // polleria
                }
                totalCasas += FilaCasas(root, z - 7.5f, 0f, saltos, rng);
            }
            foreach (var x in CallesV)
            {
                totalCasas += ColumnaCasas(root, x - 7f, 90f, saltosZ, rng);
                totalCasas += ColumnaCasas(root, x + 7f, -90f, saltosZ, rng);
            }

            // ---------- autos estacionados en la calle principal ----------
            string[] modelos = { "sedan", "taxi", "hatchback-sports", "suv", "delivery", "police", "van", "race" };
            float[] autosX = { -46f, -34f, -14f, 2f, 24f, 48f, 62f, 80f };
            for (int i = 0; i < autosX.Length; i++)
            {
                float z = (i % 2 == 0) ? -14.2f : -17.8f;
                float rot = (i % 2 == 0) ? 90f : -90f;
                Modelo(root, KitCarros + modelos[i % modelos.Length] + ".glb", new Vector3(autosX[i], 0f, z), rot, 1.5f);
            }
            // autos en la segunda calle
            float[] autosX2 = { -40f, -8f, 20f, 56f };
            for (int i = 0; i < autosX2.Length; i++)
            {
                float z = (i % 2 == 0) ? -46.2f : -49.8f;
                float rot = (i % 2 == 0) ? 90f : -90f;
                Modelo(root, KitCarros + modelos[(i + 3) % modelos.Length] + ".glb", new Vector3(autosX2[i], 0f, z), rot, 1.5f);
            }

            // ---------- arboles en las veredas ----------
            int totalArboles = 0;
            foreach (var z in CallesH)
            {
                for (float x = XIni + 8f; x <= XFin - 8f; x += 18f)
                {
                    bool cercaCalleV = false;
                    foreach (var xv in CallesV) if (Mathf.Abs(x - xv) < 6f) { cercaCalleV = true; break; }
                    if (cercaCalleV) continue;
                    float lado = ((int)((x - XIni) / 18f) % 2 == 0) ? 4.6f : -4.6f;
                    string t = rng.Next(2) == 0 ? "tree-large" : "tree-small";
                    if (Modelo(root, KitSuburban + t + ".glb", new Vector3(x, 0f, z + lado), (float)rng.NextDouble() * 360f, t == "tree-large" ? 3.5f : 2.2f) != null)
                        totalArboles++;
                }
            }

            // ---------- cerca del patio de la casa ----------
            for (float x = -6f; x <= 10f; x += 2f)
                Modelo(root, KitSuburban + "fence-low.glb", new Vector3(x, 0f, -8.2f), 0f, 0.6f);

            // ---------- camino de piedra de la casa a la calle ----------
            Modelo(root, KitSuburban + "path-stones-long.glb", new Vector3(0.5f, 0.01f, -4.5f), 90f, 0.5f);
            Modelo(root, KitSuburban + "path-stones-long.glb", new Vector3(0.5f, 0.01f, -10.5f), 90f, 0.5f);

            // ---------- grifo junto a la vereda ----------
            CrearGrifo(root, new Vector3(11f, 0f, -12.6f), matGrifo, matAgua);

            // ---------- cielo y luz ----------
            CrearCielo();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("TIENDA_VR: ciudad Kenney lista (" + totalCasas + " casas, " + (CallesH.Length + CallesV.Length) + " calles, 4 negocios, 12 autos, " + totalArboles + " arboles).");
        }

        // Fila de casas sobre el eje X midiendo la huella de cada modelo.
        private static int FilaCasas(GameObject root, float z, float rotY, List<Vector2> saltos, System.Random rng)
        {
            int n = 0;
            float cursor = XIni;
            while (cursor < XFin)
            {
                var modelo = Casas[rng.Next(Casas.Length)];
                var go = ModeloSinMover(root, KitSuburban + modelo + ".glb", rotY, 4.5f);
                if (go == null) break;
                var b = CalcularBoundsLocal(go);
                float mitad = Mathf.Max(b.size.x, 4f) / 2f;
                float centro = cursor + mitad;
                if (centro + mitad > XFin || EnSalto(centro - mitad, saltos) || EnSalto(centro + mitad, saltos))
                {
                    Object.DestroyImmediate(go);
                    cursor += 2f;
                    continue;
                }
                go.transform.position = new Vector3(centro, 0f, z) - new Vector3(b.center.x, b.min.y, b.center.z);
                n++;
                cursor = centro + mitad + 1.5f;
            }
            return n;
        }

        // Columna de casas sobre el eje Z (para calles verticales).
        private static int ColumnaCasas(GameObject root, float x, float rotY, List<Vector2> saltos, System.Random rng)
        {
            int n = 0;
            float cursor = ZIni;
            while (cursor > ZFin)
            {
                var modelo = Casas[rng.Next(Casas.Length)];
                var go = ModeloSinMover(root, KitSuburban + modelo + ".glb", rotY, 4.5f);
                if (go == null) break;
                var b = CalcularBoundsLocal(go);
                float mitad = Mathf.Max(b.size.z, 4f) / 2f;
                float centro = cursor - mitad;
                if (centro - mitad < ZFin || EnSalto(centro + mitad, saltos) || EnSalto(centro - mitad, saltos))
                {
                    Object.DestroyImmediate(go);
                    cursor -= 2f;
                    continue;
                }
                go.transform.position = new Vector3(x, 0f, centro) - new Vector3(b.center.x, b.min.y, b.center.z);
                n++;
                cursor = centro - mitad - 1.5f;
            }
            return n;
        }

        private static bool EnSalto(float v, List<Vector2> saltos)
        {
            foreach (var s in saltos) if (v >= s.x && v <= s.y) return true;
            return false;
        }

        private static void CalleHorizontal(GameObject root, float z, Material asfalto, Material linea, Material vereda)
        {
            float largo = XFin - XIni;
            float cx = (XIni + XFin) / 2f;
            var calle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            calle.name = "Calle H z" + z;
            calle.transform.SetParent(root.transform);
            calle.transform.position = new Vector3(cx, 0.02f, z);
            calle.transform.localScale = new Vector3(largo, 0.04f, 5f);
            calle.GetComponent<Renderer>().sharedMaterial = asfalto;
            for (float x = XIni + 2f; x <= XFin - 2f; x += 3f)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = "Linea";
                g.transform.SetParent(calle.transform);
                g.transform.position = new Vector3(x, 0.045f, z);
                g.transform.localScale = new Vector3(0.8f / largo, 1.2f, 0.028f);
                g.GetComponent<Renderer>().sharedMaterial = linea;
            }
            CrearVereda(root, new Vector3(cx, 0.03f, z + 3.4f), largo, true, vereda);
            CrearVereda(root, new Vector3(cx, 0.03f, z - 3.4f), largo, true, vereda);
        }

        private static void CalleVertical(GameObject root, float x, Material asfalto, Material linea, Material vereda)
        {
            float largo = ZIni - ZFin;
            float cz = (ZIni + ZFin) / 2f;
            var calle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            calle.name = "Calle V x" + x;
            calle.transform.SetParent(root.transform);
            calle.transform.position = new Vector3(x, 0.02f, cz);
            calle.transform.localScale = new Vector3(5f, 0.04f, largo);
            calle.GetComponent<Renderer>().sharedMaterial = asfalto;
            for (float z = ZIni - 2f; z >= ZFin + 2f; z -= 3f)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = "Linea";
                g.transform.SetParent(calle.transform);
                g.transform.position = new Vector3(x, 0.045f, z);
                g.transform.localScale = new Vector3(0.028f, 1.2f, 0.8f / largo);
                g.GetComponent<Renderer>().sharedMaterial = linea;
            }
            CrearVereda(root, new Vector3(x + 3.4f, 0.03f, cz), largo, false, vereda);
            CrearVereda(root, new Vector3(x - 3.4f, 0.03f, cz), largo, false, vereda);
        }

        private static void CrearVereda(GameObject root, Vector3 pos, float largo, bool horizontal, Material mat)
        {
            var v = GameObject.CreatePrimitive(PrimitiveType.Cube);
            v.name = "Vereda";
            v.transform.SetParent(root.transform);
            v.transform.position = pos;
            v.transform.localScale = horizontal ? new Vector3(largo, 0.06f, 1.8f) : new Vector3(1.8f, 0.06f, largo);
            v.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void Negocio(GameObject root, string modelo, string nombre, float x, Color colorLetrero, float altura)
        {
            var go = Modelo(root, modelo, new Vector3(x, 0f, -22.5f), 0f, altura);
            if (go != null) go.name = "Edificio " + nombre;
            Letrero(root, nombre, new Vector3(x, 6f, -19.4f), colorLetrero);
        }

        // Instancia un GLB escalado a altura objetivo, sin colocarlo aun (posicion 0).
        private static GameObject ModeloSinMover(GameObject root, string ruta, float rotY, float alturaObjetivo)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                Debug.LogWarning("TIENDA_VR: no se encontro modelo " + ruta);
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(root.transform);
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.position = Vector3.zero;
            var b = CalcularBounds(go);
            if (b.size.y > 0.001f)
            {
                float k = alturaObjetivo / b.size.y;
                go.transform.localScale = go.transform.localScale * k;
            }
            return go;
        }

        private static GameObject Modelo(GameObject root, string ruta, Vector3 pos, float rotY, float alturaObjetivo)
        {
            var go = ModeloSinMover(root, ruta, rotY, alturaObjetivo);
            if (go == null) return null;
            var b = CalcularBounds(go);
            go.transform.position = pos - new Vector3(b.center.x, b.min.y, b.center.z);
            return go;
        }

        private static Bounds CalcularBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // Bounds con el objeto en el origen (para medir huella antes de colocar).
        private static Bounds CalcularBoundsLocal(GameObject go)
        {
            return CalcularBounds(go);
        }

        private static void Letrero(GameObject root, string texto, Vector3 pos, Color color)
        {
            var go = new GameObject("Letrero " + texto);
            go.transform.SetParent(root.transform);
            go.transform.position = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = texto;
            tmp.fontSize = 7f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(8f, 2f);
        }

        private static void CrearGrifo(GameObject root, Vector3 pos, Material matPoste, Material matAgua)
        {
            var grifo = new GameObject("Grifo");
            grifo.transform.SetParent(root.transform);
            grifo.transform.position = pos;

            var poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poste.transform.SetParent(grifo.transform);
            poste.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            poste.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            poste.GetComponent<Renderer>().sharedMaterial = matPoste;

            var pila = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pila.transform.SetParent(grifo.transform);
            pila.transform.localPosition = new Vector3(0f, 0.25f, 0.45f);
            pila.transform.localScale = new Vector3(0.7f, 0.5f, 0.6f);
            pila.GetComponent<Renderer>().sharedMaterial = matPoste;

            var agua = GameObject.CreatePrimitive(PrimitiveType.Cube);
            agua.transform.SetParent(grifo.transform);
            agua.transform.localPosition = new Vector3(0f, 0.48f, 0.45f);
            agua.transform.localScale = new Vector3(0.55f, 0.05f, 0.45f);
            agua.GetComponent<Renderer>().sharedMaterial = matAgua;
        }

        private static void CrearCielo()
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return;
            var ruta = CarpetaMat + "/CieloPueblo.mat";
            var cielo = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (cielo == null)
            {
                cielo = new Material(shader);
                AssetDatabase.CreateAsset(cielo, ruta);
            }
            cielo.SetFloat("_SunSize", 0.04f);
            cielo.SetFloat("_SunSizeConvergence", 5f);
            cielo.SetFloat("_AtmosphereThickness", 0.85f);
            cielo.SetColor("_SkyTint", new Color(0.55f, 0.7f, 0.95f));
            cielo.SetColor("_GroundColor", new Color(0.75f, 0.75f, 0.7f));
            cielo.SetFloat("_Exposure", 1.25f);
            RenderSettings.skybox = cielo;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.75f, 0.83f, 0.92f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 220f;

            var sol = GameObject.Find("Directional Light");
            if (sol != null)
            {
                var l = sol.GetComponent<Light>();
                if (l != null)
                {
                    l.color = new Color(1f, 0.96f, 0.86f);
                    l.intensity = 1.15f;
                }
                sol.transform.rotation = Quaternion.Euler(48f, -142f, 0f);
            }
        }

        private static Material MaterialSimple(string nombre, Color color)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(CarpetaMat))
                AssetDatabase.CreateFolder("Assets/Materials", "Pueblo");
            var ruta = CarpetaMat + "/" + nombre + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, ruta);
            }
            mat.color = color;
            return mat;
        }
    }
}
