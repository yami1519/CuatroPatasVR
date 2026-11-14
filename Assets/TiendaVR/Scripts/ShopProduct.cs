using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CuatroPatasVR.Shop
{
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    [AddComponentMenu("Cuatro Patas/Tienda/Producto entregable")]
    public sealed class ShopProduct : MonoBehaviour
    {
        [SerializeField] private ShopProductDefinition definition;
        [Tooltip("Si cae al suelo, vuelve al estante para que el turno no se bloquee.")]
        [SerializeField] private float recoveryHeight = 0.35f;
        private Rigidbody body;
        private XRGrabInteractable grab;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool desktopHeld;
        public ShopProductDefinition Definition => definition;
        public bool IsHeld => desktopHeld || (grab != null && grab.isSelected);
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
        private void FixedUpdate()
        {
            if (!IsHeld && transform.position.y < recoveryHeight) ReturnToShelf();
        }
        public void SetDesktopHeld(bool held)
        {
            desktopHeld = held;
            body.isKinematic = held;
            if (held) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        }
        public void ReturnToShelf()
        {
            if (IsHeld) return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = initialPosition;
            body.rotation = initialRotation;
            transform.SetPositionAndRotation(initialPosition, initialRotation);
        }
    }
}
