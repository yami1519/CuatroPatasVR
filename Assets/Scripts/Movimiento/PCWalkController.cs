using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CuatroPatasVR.Comun
{
    [RequireComponent(typeof(CharacterController))]
    public class PCWalkController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float speed = 3.0f;
        [SerializeField] private float runMultiplier = 2.0f;
        [SerializeField] private float gravity = -20f;

        [Header("Vista (Ratón)")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float lookSensitivity = 0.15f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Interacción con clic")]
        [SerializeField] private float interactDistance = 6f;

        private CharacterController cc;
        private float yaw;
        private float pitch;
        private float verticalVelocity;

        private bool IsCatPlayerActive
        {
            get
            {
                var gm = GameManager.Get();
                return gm != null && gm.data != null && gm.data.role == PlayerRole.CatPlayer;
            }
        }

        private void Awake()
        {
            cc = GetComponent<CharacterController>();

            if (cameraTransform == null)
            {
                foreach (var cam in GetComponentsInChildren<Camera>(true))
                {
                    if (cam.transform != transform)
                    {
                        cameraTransform = cam.transform;
                        break;
                    }
                }
            }

            if (cameraTransform == null && Camera.main != null && Camera.main.transform != transform)
                cameraTransform = Camera.main.transform;
        }

        private void Start()
        {
            yaw = transform.eulerAngles.y;
            if (cameraTransform != null && cameraTransform != transform)
            {
                float rawPitch = cameraTransform.localEulerAngles.x;
                if (rawPitch > 180f) rawPitch -= 360f;
                pitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            // Si estás jugando como gato, no muevas el cuerpo humano de prueba
            if (IsCatPlayerActive)
            {
                ApplyGravityOnly();
                return;
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                ApplyGravityOnly();
                return;
            }

            var delta = mouse.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (cameraTransform != null && cameraTransform != transform)
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            else
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (mouse.leftButton.wasPressedThisFrame)
                TryPressButton();

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

            Vector3 wishDir = transform.right * x + transform.forward * z;
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            float currentSpeed = speed * (kb.leftShiftKey.isPressed ? runMultiplier : 1f);
            Vector3 horizontalVelocity = wishDir * currentSpeed;

            if (cc.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            cc.Move(new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z) * Time.deltaTime);
        }

        private void ApplyGravityOnly()
        {
            if (cc == null || !cc.enabled) return;

            if (cc.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            cc.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
        }

        private void TryPressButton()
        {
            Transform origin = (cameraTransform != null && cameraTransform != transform) ? cameraTransform : transform;
            var ray = new Ray(origin.position, origin.forward);
            if (!Physics.Raycast(ray, out var hit, interactDistance)) return;

            var interactable = hit.collider.GetComponentInParent<XRSimpleInteractable>();
            if (interactable != null)
            {
                interactable.selectEntered?.Invoke(new SelectEnterEventArgs());
                Debug.Log("PRUEBA_PC: boton presionado " + interactable.name);
            }
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}