using UnityEngine;
using UnityEngine.InputSystem;

namespace CuatroPatasVR.Training
{
    // Only tracked controllers can hit targets; a disconnected hand cannot score a contact.
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class TrainingHand : MonoBehaviour
    {
        [SerializeField] private bool leftHand;
        private InputAction tracked;
        private Collider hitCollider;
        private Renderer handRenderer;

        private Vector3 previousPosition;
        public bool IsLeftHand => leftHand;
        public bool IsTracked { get; private set; }
        public float Speed { get; private set; }

        public void SetHand(bool isLeft) => leftHand = isLeft;

        private void Awake()
        {
            hitCollider = GetComponent<Collider>();
            handRenderer = GetComponent<Renderer>();
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            tracked = new InputAction("Hand tracked", InputActionType.Value,
                leftHand ? "<XRController>{LeftHand}/isTracked" : "<XRController>{RightHand}/isTracked");
        }

        private void OnEnable()
        {
            tracked.Enable();
            previousPosition = transform.position;
            Speed = 0f;
        }
        private void OnDisable()
        {
            tracked.Disable();
            hitCollider.enabled = false;
            IsTracked = false;
            Speed = 0f;
        }
        private void OnDestroy() => tracked.Dispose();

        private void LateUpdate()
        {
            Speed = IsTracked && Time.unscaledDeltaTime > 0f
                ? Vector3.Distance(transform.position, previousPosition) / Time.unscaledDeltaTime : 0f;
            previousPosition = transform.position;
        }

        private void Update()
        {
            bool isTracked = tracked.ReadValue<float>() > 0.5f;
            if (!IsTracked && isTracked) previousPosition = transform.position;
            IsTracked = isTracked;
            hitCollider.enabled = isTracked;
            if (handRenderer != null) handRenderer.enabled = isTracked;
        }
    }
}
