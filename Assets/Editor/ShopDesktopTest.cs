using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CuatroPatasVR.Comun;

namespace CuatroPatasVR.EditorTools
{
    // Agrega una camara de prueba para PC (WASD + raton) a PuebloVR,
    // para probar el juego en el editor sin visor VR.
    public static class ShopDesktopTest
    {
        [MenuItem("Cuatro Patas/Tienda/10 - Agregar camara de prueba PC")]
        public static void Add()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/PuebloVR.unity");
            var existente = Object.FindObjectsByType<DesktopWalkCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (existente != null) { Debug.Log("TIENDA_VR: la camara de prueba ya existe."); return; }

            var go = new GameObject("MODO PRUEBA PC (WASD + raton)");
            go.transform.position = new Vector3(0f, 1.6f, 2f);
            var cam = go.AddComponent<Camera>();
            cam.depth = 10f;
            cam.clearFlags = CameraClearFlags.Skybox;
            var listener = go.GetComponent<AudioListener>();
            if (listener != null) Object.DestroyImmediate(listener);
            go.AddComponent<DesktopWalkCamera>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TIENDA_VR: camara de prueba PC agregada (WASD + raton, clic en botones de la tienda).");
        }
    }
}
