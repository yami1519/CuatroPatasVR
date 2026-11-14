using System;
using System.IO;
using System.Linq;
using CuatroPatasVR.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using Unity.XR.CoreUtils;

namespace CuatroPatasVR.Shop.Editor
{
    public static class ShopSceneSetup
    {
        const string Root = "Assets/TiendaVR";
        const string ScenePath = Root + "/Scenes/TiendaVR.unity";
        static TMP_FontAsset font;

        [MenuItem("Cuatro Patas/Tienda/1 - Crear escena de trabajo")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Sal de Play antes de crear la escena.");
            if (File.Exists(ScenePath))
            { Debug.Log("TiendaVR ya existe. Abrela desde Assets/TiendaVR/Scenes; no se sobrescribio."); return; }
            foreach (var folder in new[] { "Scenes", "Productos", "Materiales", "Prefabs" })
                if (!AssetDatabase.IsValidFolder(Root + "/" + folder)) AssetDatabase.CreateFolder(Root, folder);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Fredoka SDF.asset");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var environment = new GameObject("01 - Puesto provisional (arte: integrante 2)");
                var gameplay = new GameObject("02 - Pedidos y entrega (integrante 1)");
                var rigGroup = new GameObject("03 - Jugador VR y vista previa");
                var cream = Material("Mostrador", new Color(0.7f, 0.59f, 0.42f));
                var green = Material("Comida", new Color(0.22f, 0.57f, 0.38f));
                var yellow = Material("Arena", new Color(0.87f, 0.61f, 0.2f));
                var trayMaterial = Material("Bandeja", new Color(0.2f, 0.48f, 0.61f));
                var dark = Material("Panel", new Color(0.08f, 0.16f, 0.14f));
                Cube("Suelo", environment.transform, new Vector3(0, -0.1f, 0), new Vector3(5, 0.2f, 5), cream);
                Cube("Mostrador", environment.transform, new Vector3(0, 0.82f, 0.55f), new Vector3(1.8f, 0.12f, 0.9f), cream);
                Cube("Soporte izquierdo", environment.transform, new Vector3(-0.7f, 0.4f, 0.55f), new Vector3(0.1f, 0.8f, 0.7f), cream);
                Cube("Soporte derecho", environment.transform, new Vector3(0.7f, 0.4f, 0.55f), new Vector3(0.1f, 0.8f, 0.7f), cream);
                Cube("Panel de pedidos", environment.transform, new Vector3(0, 1.65f, 1.15f), new Vector3(1.8f, 0.95f, 0.06f), dark);
                var sun = new GameObject("Luz provisional").AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.3f;
                sun.transform.rotation = Quaternion.Euler(45, -25, 0); sun.transform.SetParent(environment.transform);
                RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.65f);
                var session = gameplay.AddComponent<ShopSession>();
                var food = ProductDefinition("Comida", "Comida para gato");
                var litter = ProductDefinition("Arena", "Arena para gato");
                var config = new SerializedObject(session);
                var orders = config.FindProperty("orders"); orders.arraySize = 6;
                for (int i = 0; i < 6; i++) orders.GetArrayElementAtIndex(i).objectReferenceValue = i % 2 == 0 ? food : litter;
                config.FindProperty("orderText").objectReferenceValue = Label("Pedido actual", gameplay.transform, new Vector3(0, 1.85f, 1.105f), "TRABAJO EN LA TIENDA", 0.11f, 1.65f, 0.28f);
                config.FindProperty("statusText").objectReferenceValue = Label("Instrucciones", gameplay.transform, new Vector3(0, 1.55f, 1.105f), "Pulsa INICIAR TURNO. Entrega comida y arena.", 0.07f, 1.65f, 0.25f);
                config.FindProperty("resultText").objectReferenceValue = Label("Pago y resultados", gameplay.transform, new Vector3(0, 1.29f, 1.105f), "6 pedidos - 2 monedas por pedido", 0.065f, 1.65f, 0.18f);
                config.ApplyModifiedPropertiesWithoutUndo();
                CreateProduct(food, new Vector3(-0.5f, 1.01f, 0.48f), green, gameplay.transform);
                CreateProduct(litter, new Vector3(0.5f, 1.01f, 0.48f), yellow, gameplay.transform);
                Cube("Bandeja - base", gameplay.transform, new Vector3(0, 0.91f, 0.52f), new Vector3(0.42f, 0.05f, 0.4f), trayMaterial);
                var zone = new GameObject("Zona de entrega - soltar aqui"); zone.transform.SetParent(gameplay.transform);
                zone.transform.position = new Vector3(0, 1.12f, 0.52f);
                var trigger = zone.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(0.4f, 0.4f, 0.38f);
                SetReference(zone.AddComponent<ShopDeliveryZone>(), "session", session);
                Label("Etiqueta bandeja", gameplay.transform, new Vector3(0, 0.8f, 0.085f), "ENTREGAS", 0.065f, 0.5f, 0.14f);
                CreateButton("INICIAR TURNO", new Vector3(-0.43f, 0.98f, 0.13f), session.BeginShift, green, gameplay.transform);
                CreateButton("VOLVER A CASA", new Vector3(0.43f, 0.98f, 0.13f), session.ReturnHome, trayMaterial, gameplay.transform);
                new GameObject("XR Interaction Manager", typeof(XRInteractionManager)).transform.SetParent(rigGroup.transform);
                var rig = new GameObject("XR Origin - manos y visor"); rig.transform.SetParent(rigGroup.transform);
                var origin = rig.AddComponent<XROrigin>();
                var offset = new GameObject("Camera Offset"); offset.transform.SetParent(rig.transform, false);
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
                camera.transform.SetParent(offset.transform, false); camera.transform.localPosition = new Vector3(0, 1.65f, 0);
                camera.GetComponent<Camera>().nearClipPlane = 0.03f;
                origin.Camera = camera.GetComponent<Camera>(); origin.CameraFloorOffsetObject = offset;
                origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                ConfigurePose(camera, "<XRHMD>");
                Hand("LeftHand", offset.transform, green); Hand("RightHand", offset.transform, yellow);
                // Se reutiliza el inicio OpenXR ya existente del proyecto, fuera del rig de vista previa.
                rigGroup.AddComponent<CuatroPatasVR.Training.TrainingXRSession>();
                var preview = new GameObject("Camara de demostracion con raton", typeof(Camera), typeof(AudioListener));
                preview.transform.SetParent(rigGroup.transform); preview.transform.position = new Vector3(0, 2f, -1.7f);
                preview.transform.LookAt(new Vector3(0, 1.05f, 0.55f)); preview.GetComponent<Camera>().nearClipPlane = 0.03f;
                var desktop = rigGroup.AddComponent<ShopDesktopPreview>();
                SetReference(desktop, "previewCamera", preview.GetComponent<Camera>()); SetReference(desktop, "headsetRig", rig);
                rig.SetActive(false);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                    EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
                AssetDatabase.SaveAssets();
                Debug.Log("TIENDA_VR: escena creada. Abre Assets/TiendaVR/Scenes/TiendaVR.unity. Componentes, eventos y productos editables en Inspector.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        static Material Material(string name, Color color)
        {
            string path = Root + "/Materiales/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = color;
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static ShopProductDefinition ProductDefinition(string name, string display)
        {
            string path = Root + "/Productos/" + name + ".asset";
            var value = AssetDatabase.LoadAssetAtPath<ShopProductDefinition>(path);
            if (value != null) return value;
            value = ScriptableObject.CreateInstance<ShopProductDefinition>(); value.displayName = display;
            AssetDatabase.CreateAsset(value, path); return value;
        }
        static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
            go.transform.position = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        static TMP_Text Label(string name, Transform parent, Vector3 position, string text, float size, float width, float height)
        {
            var go = new GameObject(name, typeof(TextMeshPro)); go.transform.SetParent(parent);
            go.transform.position = position; go.transform.rotation = Quaternion.identity;
            var label = go.GetComponent<TextMeshPro>(); label.font = font; label.text = text;
            label.fontSize = size * 10; label.alignment = TextAlignmentOptions.Center; label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(width, height); return label;
        }
        static void CreateProduct(ShopProductDefinition definition, Vector3 position, Material material, Transform parent)
        {
            // El objeto físico conserva escala 1; la compañera puede sustituir solamente su hijo visual.
            var go = new GameObject(definition.name); go.transform.SetParent(parent); go.transform.position = position;
            go.AddComponent<BoxCollider>().size = new Vector3(0.21f, 0.24f, 0.18f);
            var body = go.AddComponent<Rigidbody>(); body.mass = 0.25f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var visual = Cube("Visual provisional - reemplazable", go.transform, position, new Vector3(0.21f, 0.24f, 0.18f), material);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            var grab = go.AddComponent<XRGrabInteractable>(); grab.movementType = XRBaseInteractable.MovementType.Kinematic; grab.throwOnDetach = false;
            SetReference(go.AddComponent<ShopProduct>(), "definition", definition);
            Label("Nombre del producto", go.transform, position + new Vector3(0, 0.02f, -0.095f), definition.name.ToUpperInvariant(), 0.045f, 0.2f, 0.12f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, Root + "/Prefabs/" + definition.name + ".prefab", InteractionMode.AutomatedAction);
        }
        static void CreateButton(string name, Vector3 position, UnityAction action, Material material, Transform parent)
        {
            var go = Cube(name, parent, position, new Vector3(0.42f, 0.13f, 0.1f), material);
            go.AddComponent<XRSimpleInteractable>(); var button = go.AddComponent<ShopStationButton>();
            var field = typeof(ShopStationButton).GetField("onPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            UnityEventTools.AddPersistentListener((UnityEvent)field.GetValue(button), action);
            EditorUtility.SetDirty(button);
            Label(name + " - texto", parent, position + new Vector3(0, 0, -0.055f), name, 0.045f, 0.4f, 0.12f);
        }
        static void ConfigurePose(GameObject go, string path)
        {
            var pose = go.AddComponent<TrackedPoseDriver>();
            pose.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, path + "/devicePosition", expectedControlType: "Vector3"));
            pose.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, path + "/deviceRotation", expectedControlType: "Quaternion"));
            pose.trackingStateInput = new InputActionProperty(new InputAction("Tracking", InputActionType.Value, path + "/trackingState", expectedControlType: "Integer"));
        }
        static void Hand(string side, Transform parent, Material material)
        {
            var go = new GameObject(side + " - agarrar con grip"); go.transform.SetParent(parent, false);
            ConfigurePose(go, "<XRController>{" + side + "}");
            var collider = go.AddComponent<SphereCollider>(); collider.radius = 0.07f; collider.isTrigger = true;
            var body = go.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var interactor = go.AddComponent<XRDirectInteractor>();
            interactor.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
            interactor.selectInput.inputActionPerformed = new InputAction("Agarrar", InputActionType.Button, "<XRController>{" + side + "}/gripPressed");
            interactor.selectInput.inputActionValue = new InputAction("Fuerza", InputActionType.Value, "<XRController>{" + side + "}/grip");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere); visual.name = "Mano provisional";
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>()); visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.1f; visual.GetComponent<Renderer>().sharedMaterial = material;
        }
        static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
