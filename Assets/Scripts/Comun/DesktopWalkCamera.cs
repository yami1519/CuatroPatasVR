using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CuatroPatasVR.Comun
{
    // Camara de prueba en PC para probar sin visor VR.
    // Clic: captura el raton. WASD: caminar. Shift: correr. Escape: soltar el raton.
    // Clic sobre botones de la tienda: los activa como si los tocaras en VR.
    public class DesktopWalkCamera : MonoBehaviour
    {
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float runMultiplier = 2.5f;
        [SerializeField] private float lookSensitivity = 0.08f;

        private float yaw;
        private float pitch;

        private void Start()
        {
            yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                return;
            }
            if (kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * lookSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -80f, 80f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

                if (mouse.leftButton.wasPressedThisFrame) TryPressButton();
            }

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            var dir = new Vector3(x, 0f, z);
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            float v = speed * (kb.leftShiftKey.isPressed ? runMultiplier : 1f);
            var move = transform.TransformDirection(dir) * v * Time.deltaTime;
            move.y = 0f;
            transform.position += move;
        }

        private void TryPressButton()
        {
            var ray = new Ray(transform.position, transform.forward);
            if (!Physics.Raycast(ray, out var hit, 6f)) return;
            var interactable = hit.collider.GetComponentInParent<XRSimpleInteractable>();
            if (interactable != null)
            {
                interactable.selectEntered?.Invoke(new SelectEnterEventArgs());
                Debug.Log("PRUEBA_PC: boton presionado " + interactable.name);
            }
        }
    }
}
