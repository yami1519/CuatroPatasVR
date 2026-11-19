using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
using UnityEngine.XR;
#endif

namespace CuatroPatasVR.Shop
{
    [AddComponentMenu("Cuatro Patas/Tienda/Vista previa con raton")]
    public sealed class ShopDesktopPreview : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private GameObject headsetRig;
#if UNITY_EDITOR
        private ShopProduct held;
        private void Update()
        {
            bool desktop = !XRSettings.isDeviceActive;
            previewCamera.gameObject.SetActive(desktop);
            headsetRig.SetActive(!desktop);
            if (!desktop) { Release(); return; }
            var mouse = Mouse.current;
            if (mouse == null) return;
            Ray ray = previewCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out var hit, 10f))
            {
                var button = hit.collider.GetComponentInParent<ShopStationButton>();
                if (button != null) button.Press();
                else
                {
                    held = hit.collider.GetComponentInParent<ShopProduct>();
                    if (held != null) held.SetDesktopHeld(true);
                }
            }
            if (held != null && new Plane(Vector3.up, new Vector3(0f, 1.12f, 0f)).Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                point.x = Mathf.Clamp(point.x, -0.75f, 0.75f);
                point.z = Mathf.Clamp(point.z, 0.15f, 0.85f);
                held.GetComponent<Rigidbody>().MovePosition(point);
            }
            if (mouse.leftButton.wasReleasedThisFrame) Release();
        }
        private void Release() { if (held != null) { held.SetDesktopHeld(false); held = null; } }
        private void OnDisable() => Release();
#else
        private void Awake() { previewCamera.gameObject.SetActive(false); headsetRig.SetActive(true); }
#endif
    }
}
