using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CuatroPatasVR.EditorTools
{
    // Coloca la casa rescatada del template de Godot (convertida a OBJ) en la ciudad.
    public static class CasaRescatada
    {
        private const string Escena = "Assets/Scenes/PuebloVR.unity";
        private const string Modelo = "Assets/Models/CasaTemplate/house_template.obj";

        [MenuItem("Cuatro Patas/Tienda/13 - Colocar casa rescatada (template)")]
        public static void Add()
        {
            EditorSceneManager.OpenScene(Escena);

            var viejo = GameObject.Find("Casa Rescatada (template Godot)");
            if (viejo != null) Object.DestroyImmediate(viejo);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Modelo);
            if (prefab == null)
            {
                Debug.LogWarning("TIENDA_VR: no se encontro " + Modelo);
                return;
            }

            var casa = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            casa.name = "Casa Rescatada (template Godot)";
            casa.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // frente a la calle principal

            // escalar a altura de casa de dos pisos y apoyar en el suelo
            var b = CalcularBounds(casa);
            if (b.size.y > 0.001f)
            {
                float k = 5.5f / b.size.y;
                casa.transform.localScale = casa.transform.localScale * k;
            }
            casa.transform.position = new Vector3(18f, 0f, 2f);
            b = CalcularBounds(casa);
            casa.transform.position += new Vector3(18f - b.center.x, -b.min.y, 2f - b.center.z);

            // color calido para que no quede gris
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = new Color(0.92f, 0.85f, 0.72f) };
            foreach (var r in casa.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("TIENDA_VR: casa rescatada del template Godot colocada junto a la casa del jugador.");
        }

        private static Bounds CalcularBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
