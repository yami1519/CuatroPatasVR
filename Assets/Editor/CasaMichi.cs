using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CuatroPatasVR.EditorTools
{
    // Coloca al michi (munchkin_cat) dentro de la casa en PuebloVR.
    public static class CasaMichi
    {
        private const string Escena = "Assets/Scenes/PuebloVR.unity";
        private const string ModeloMichi = "Assets/Models/Mascotas/munchkin/munchkin_cat.glb";

        [MenuItem("Cuatro Patas/Tienda/12 - Agregar michi a la casa")]
        public static void Add()
        {
            EditorSceneManager.OpenScene(Escena);

            var viejo = GameObject.Find("Michi");
            if (viejo != null) Object.DestroyImmediate(viejo);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModeloMichi);
            if (prefab == null)
            {
                Debug.LogWarning("TIENDA_VR: no se encontro " + ModeloInformativo());
                return;
            }

            var michi = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            michi.name = "Michi";
            michi.transform.position = new Vector3(1.6f, 0f, 4.6f);
            michi.transform.rotation = Quaternion.Euler(0f, -135f, 0f); // mirando hacia el jugador

            // escalar a tamano real de gato (~0.35 m de alto)
            var b = CalcularBounds(michi);
            if (b.size.y > 0.001f)
            {
                float k = 0.35f / b.size.y;
                michi.transform.localScale = michi.transform.localScale * k;
                b = CalcularBounds(michi);
                michi.transform.position += new Vector3(0f, -b.min.y, 0f);
            }

            int clips = 0;
            var anim = michi.GetComponentInChildren<Animation>();
            if (anim != null) clips = anim.GetClipCount();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("TIENDA_VR: Michi agregado a la casa (altura 0.35m, animaciones detectadas: " + clips + ").");
        }

        private static string ModeloInformativo() => ModeloMichi;

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
