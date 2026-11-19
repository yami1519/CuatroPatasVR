using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace CuatroPatasVR.EditorTools
{
    // Nivel 6: unir CasaVR + TiendaVR en una sola escena "PuebloVR":
    // la casa, la calle y la tienda de mascotas con fachadas de
    // veterinaria, bodega, pasteleria y polleria. El jugador sale de
    // su casa y camina a la tienda, sin cargas de escena.
    public static class ShopPuebloBuilder
    {
        const string PuebloPath = "Assets/Scenes/PuebloVR.unity";

        [MenuItem("Cuatro Patas/Tienda/9 - Crear PuebloVR (pueblito unificado)")]
        public static void Build()
        {
            // 1) Copiar casaVR como base (mantiene intacta toda la logica de cuidado)
            var casa = EditorSceneManager.OpenScene("Assets/Scenes/casaVR.unity");
            EditorSceneManager.SaveScene(casa, PuebloPath, true);
            var pueblo = EditorSceneManager.OpenScene(PuebloPath);

            // 2) Traer la tienda (arte + logica de pedidos) desde TiendaVR
            var tiendaScene = EditorSceneManager.OpenScene("Assets/Scenes/TiendaVR.unity", OpenSceneMode.Additive);
            var raizTienda = new GameObject("Tienda CuatroPatas");
            SceneManager.MoveGameObjectToScene(raizTienda, pueblo);
            raizTienda.transform.position = new Vector3(16f, 0f, -12f);
            raizTienda.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var raices = tiendaScene.GetRootGameObjects()
                .Where(go => go.name.StartsWith("01 - ") || go.name.StartsWith("02 - ")).ToArray();
            foreach (var r in raices)
            {
                SceneManager.MoveGameObjectToScene(r, pueblo);
                r.transform.SetParent(raizTienda.transform, false);
            }
            EditorSceneManager.CloseScene(tiendaScene, true);

            // 3) En el pueblo se vuelve a casa caminando: apagar el boton VOLVER A CASA
            foreach (var t in raizTienda.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("VOLVER A CASA")))
                t.gameObject.SetActive(false);

            // 4) Calle y pueblo
            var urbano = new GameObject("Pueblo - calle y fachadas");
            SceneManager.MoveGameObjectToScene(urbano, pueblo);
            var mats = CrearMateriales();

            Suelo(urbano.transform, "Pasto del pueblo", new Vector3(10f, -0.1f, -4f), new Vector3(70f, 0.2f, 30f), mats["pasto"]);
            Suelo(urbano.transform, "Calle", new Vector3(10f, 0.005f, -6f), new Vector3(70f, 0.02f, 5f), mats["asfalto"]);
            Suelo(urbano.transform, "Vereda casa", new Vector3(10f, 0.02f, -2.75f), new Vector3(70f, 0.05f, 1.6f), mats["vereda"]);
            Suelo(urbano.transform, "Vereda tiendas", new Vector3(10f, 0.02f, -9.25f), new Vector3(70f, 0.05f, 1.6f), mats["vereda"]);

            Grifo(urbano.transform, new Vector3(14f, 0f, -3f), mats["grifo"]);

            Fachada(urbano.transform, "VETERINARIA", new Vector3(-14f, 0f, -12f), mats["vet"], mats);
            Fachada(urbano.transform, "BODEGA", new Vector3(-6f, 0f, -12f), mats["bodega"], mats);
            Fachada(urbano.transform, "PASTELERIA", new Vector3(26f, 0f, -12f), mats["pastel"], mats);
            Fachada(urbano.transform, "POLLERIA", new Vector3(34f, 0f, -12f), mats["polleria"], mats);

            // 5) Build Settings: Menu + PuebloVR
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
                new EditorBuildSettingsScene(PuebloPath, true),
            };

            EditorSceneManager.MarkSceneDirty(pueblo);
            EditorSceneManager.SaveScene(pueblo);
            AssetDatabase.SaveAssets();
            Debug.Log("TIENDA_VR: PuebloVR creado (casa + calle + tienda + 4 fachadas). Revisa Menu.unity: debe cargar PuebloVR.");
        }

        static Dictionary<string, Material> CrearMateriales()
        {
            var defs = new Dictionary<string, Color>
            {
                ["pasto"] = new Color(0.36f, 0.56f, 0.30f),
                ["asfalto"] = new Color(0.20f, 0.20f, 0.22f),
                ["vereda"] = new Color(0.62f, 0.62f, 0.60f),
                ["grifo"] = new Color(0.78f, 0.15f, 0.10f),
                ["puerta"] = new Color(0.28f, 0.20f, 0.14f),
                ["ventana"] = new Color(0.62f, 0.80f, 0.86f),
                ["tejado"] = new Color(0.50f, 0.22f, 0.16f),
                ["vet"] = new Color(0.52f, 0.75f, 0.68f),
                ["bodega"] = new Color(0.62f, 0.48f, 0.32f),
                ["pastel"] = new Color(0.92f, 0.62f, 0.66f),
                ["polleria"] = new Color(0.92f, 0.76f, 0.34f),
            };
            var dict = new Dictionary<string, Material>();
            const string dir = "Assets/Materials/Tienda";
            foreach (var kv in defs)
            {
                string path = $"{dir}/Pueblo {kv.Key}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Pueblo " + kv.Key };
                    AssetDatabase.CreateAsset(m, path);
                }
                m.color = kv.Value;
                dict[kv.Key] = m;
            }
            return dict;
        }

        static void Suelo(Transform padre, string nombre, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.position = pos;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void Grifo(Transform padre, Vector3 pos, Material mat)
        {
            var grifo = new GameObject("Grifo");
            grifo.transform.SetParent(padre, false);
            grifo.transform.position = pos;
            var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cuerpo.name = "Cuerpo";
            cuerpo.transform.SetParent(grifo.transform, false);
            cuerpo.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            cuerpo.transform.localScale = new Vector3(0.22f, 0.28f, 0.22f);
            var tapa = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tapa.name = "Tapa";
            tapa.transform.SetParent(grifo.transform, false);
            tapa.transform.localPosition = new Vector3(0f, 0.60f, 0f);
            tapa.transform.localScale = Vector3.one * 0.20f;
            foreach (var lado in new[] { -1, 1 })
            {
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "Tapon";
                cap.transform.SetParent(grifo.transform, false);
                cap.transform.localPosition = new Vector3(0.14f * lado, 0.38f, 0f);
                cap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                cap.transform.localScale = new Vector3(0.10f, 0.07f, 0.10f);
            }
            foreach (var r in grifo.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = mat;
            foreach (var c in grifo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            grifo.AddComponent<BoxCollider>().size = new Vector3(0.45f, 0.75f, 0.3f);
        }

        static void Fachada(Transform padre, string nombre, Vector3 pos, Material matFrente, Dictionary<string, Material> mats)
        {
            var edificio = new GameObject("Edificio " + nombre);
            edificio.transform.SetParent(padre, false);
            edificio.transform.position = pos;

            var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cuerpo.name = "Cuerpo";
            cuerpo.transform.SetParent(edificio.transform, false);
            cuerpo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            cuerpo.transform.localScale = new Vector3(7f, 5f, 6f);
            cuerpo.GetComponent<MeshRenderer>().sharedMaterial = matFrente;

            var tejado = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tejado.name = "Tejado";
            tejado.transform.SetParent(edificio.transform, false);
            tejado.transform.localPosition = new Vector3(0f, 5.15f, 0f);
            tejado.transform.localScale = new Vector3(7.4f, 0.3f, 6.4f);
            Object.DestroyImmediate(tejado.GetComponent<Collider>());
            tejado.GetComponent<MeshRenderer>().sharedMaterial = mats["tejado"];

            // frente hacia la calle (+z): la calle esta en z=-6, el edificio en z=-12
            var puerta = GameObject.CreatePrimitive(PrimitiveType.Cube);
            puerta.name = "Puerta";
            puerta.transform.SetParent(edificio.transform, false);
            puerta.transform.localPosition = new Vector3(0f, 1.15f, 3.02f);
            puerta.transform.localScale = new Vector3(1.3f, 2.3f, 0.10f);
            Object.DestroyImmediate(puerta.GetComponent<Collider>());
            puerta.GetComponent<MeshRenderer>().sharedMaterial = mats["puerta"];

            foreach (var x in new[] { -2.1f, 2.1f })
            {
                var ventana = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ventana.name = "Ventana";
                ventana.transform.SetParent(edificio.transform, false);
                ventana.transform.localPosition = new Vector3(x, 2.7f, 3.02f);
                ventana.transform.localScale = new Vector3(1.5f, 1.4f, 0.08f);
                Object.DestroyImmediate(ventana.GetComponent<Collider>());
                ventana.GetComponent<MeshRenderer>().sharedMaterial = mats["ventana"];
            }

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel letrero";
            panel.transform.SetParent(edificio.transform, false);
            panel.transform.localPosition = new Vector3(0f, 4.1f, 3.05f);
            panel.transform.localScale = new Vector3(5.2f, 1.0f, 0.08f);
            Object.DestroyImmediate(panel.GetComponent<Collider>());
            var blanco = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tienda/Panel.mat");
            if (blanco != null) panel.GetComponent<MeshRenderer>().sharedMaterial = blanco;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Fredoka SDF.asset");
            var texto = new GameObject("Letrero", typeof(TextMeshPro));
            texto.transform.SetParent(panel.transform, false);
            texto.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            texto.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tmp = texto.GetComponent<TextMeshPro>();
            if (font != null) tmp.font = font;
            tmp.text = nombre;
            tmp.fontSize = 0.55f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.15f, 0.15f, 0.18f);
            tmp.rectTransform.sizeDelta = new Vector2(4.8f, 0.9f);
        }
    }
}
