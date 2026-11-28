using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CuatroPatasVR.Comun;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class MotoController : MonoBehaviour
{
    [Header("Jugador persona")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private PCWalkController pcWalkController;
    [SerializeField] private DesktopWalkCamera desktopWalkCamera;
    [SerializeField] private HouseInteractor houseInteractor;
    [SerializeField] private CharacterController playerCharacterController;
    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private AudioListener firstPersonAudioListener;

    [Header("Montaje")]
    [SerializeField] private Transform seatAnchor;
    [SerializeField] private GameObject riderVisual;
    [SerializeField] private Vector3 dismountOffset = new Vector3(1.35f, 1.6f, 0f);

    [Header("Cámara de conducción")]
    [SerializeField] private Camera thirdPersonCamera;
    [SerializeField] private AudioListener thirdPersonAudioListener;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2.2f, -4.5f);
    [SerializeField] private Vector3 cameraLookOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private float cameraLerpSpeed = 5f;
    [SerializeField] private float cameraCollisionRadius = 0.2f;

    [Header("HUD")]
    [SerializeField] private GameObject drivingHud;
    [SerializeField] private TextMeshProUGUI drivingHudText;

    [Header("Conducción")]
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float acceleration = 7f;
    [SerializeField] private float brakeAcceleration = 14f;
    [SerializeField] private float lateralGrip = 4f;
    [SerializeField] private float maxTurnRate = 90f;
    [SerializeField] private float minTurnRate = 35f;

    private Rigidbody body;
    private Image crosshair;
    private bool riding;
    private bool pcWalkWasEnabled;
    private bool desktopCameraWasEnabled;
    private bool interactorWasEnabled;
    private bool characterControllerWasEnabled;
    private bool firstPersonCameraWasEnabled;
    private bool firstPersonListenerWasEnabled;
    private bool crosshairWasActive;
    private float throttleInput;
    private float brakeInput;
    private float steeringInput;

    public bool IsMounted => riding;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.linearDamping = 0.2f;
        body.angularDamping = 4f;

        if (drivingHudText != null)
            drivingHudText.text = "W/S acelerar · A/D girar · F bajar";

        if (riderVisual != null)
            riderVisual.SetActive(false);
        if (drivingHud != null)
            drivingHud.SetActive(false);
        if (thirdPersonCamera != null)
            thirdPersonCamera.gameObject.SetActive(false);

        if (!ReferencesAreValid())
            enabled = false;
    }

    private void Start()
    {
        if (!riding)
            enabled = false;
    }

    public void ToggleMount()
    {
        if (riding)
        {
            Dismount();
            return;
        }

        if (!ReferencesAreValid())
            return;

        Mount();
    }

    private void Update()
    {
        if (!riding)
            return;

        playerRoot.SetPositionAndRotation(seatAnchor.position, transform.rotation);

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.fKey.wasPressedThisFrame)
        {
            Dismount();
            return;
        }

        throttleInput = keyboard.wKey.isPressed ? 1f : 0f;
        brakeInput = keyboard.sKey.isPressed ? 1f : 0f;
        steeringInput = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
    }

    private void FixedUpdate()
    {
        if (!riding || body == null)
            return;

        Vector3 forward = transform.forward;
        Vector3 velocity = body.linearVelocity;
        float forwardSpeed = Vector3.Dot(velocity, forward);

        if (throttleInput > 0f && forwardSpeed < maxSpeed)
            body.AddForce(forward * acceleration * throttleInput, ForceMode.Acceleration);

        if (brakeInput > 0f && Mathf.Abs(forwardSpeed) > 0.01f)
            body.AddForce(-Mathf.Sign(forwardSpeed) * forward * brakeAcceleration * brakeInput, ForceMode.Acceleration);

        Vector3 lateralVelocity = velocity - forward * forwardSpeed;
        body.AddForce(-lateralVelocity * lateralGrip, ForceMode.Acceleration);

        float speedRatio = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
        float turnRate = Mathf.Lerp(maxTurnRate, minTurnRate, speedRatio);
        float travelDirection = forwardSpeed < -0.1f ? -1f : 1f;
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, steeringInput * turnRate * travelDirection * Time.fixedDeltaTime, 0f));

        velocity = body.linearVelocity;
        forwardSpeed = Vector3.Dot(velocity, transform.forward);
        if (Mathf.Abs(forwardSpeed) > maxSpeed)
            body.linearVelocity = velocity + transform.forward * (Mathf.Sign(forwardSpeed) * maxSpeed - forwardSpeed);
    }

    private void LateUpdate()
    {
        if (!riding || thirdPersonCamera == null)
            return;

        Transform cameraTransform = thirdPersonCamera.transform;
        Vector3 lookTarget = transform.TransformPoint(cameraLookOffset);
        Vector3 desiredPosition = FindCameraPosition(lookTarget, transform.TransformPoint(cameraOffset));
        float blend = 1f - Mathf.Exp(-cameraLerpSpeed * Time.deltaTime);

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, blend);
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - cameraTransform.position, Vector3.up);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, blend);
    }

    private void Mount()
    {
        Vector3 playerPositionBeforeMount = playerRoot.position;
        Vector3 seatWorldPosition = seatAnchor.position;
        pcWalkWasEnabled = pcWalkController.enabled;
        desktopCameraWasEnabled = desktopWalkCamera.enabled;
        interactorWasEnabled = houseInteractor.enabled;
        characterControllerWasEnabled = playerCharacterController.enabled;
        firstPersonCameraWasEnabled = firstPersonCamera.enabled;
        firstPersonListenerWasEnabled = firstPersonAudioListener.enabled;

        crosshair = houseInteractor.crosshair;
        crosshairWasActive = crosshair != null && crosshair.gameObject.activeSelf;

        pcWalkController.enabled = false;
        desktopWalkCamera.enabled = false;
        houseInteractor.enabled = false;
        playerCharacterController.enabled = false;
        firstPersonCamera.enabled = false;
        firstPersonAudioListener.enabled = false;

        if (crosshair != null)
            crosshair.gameObject.SetActive(false);

        playerRoot.SetParent(null);
        riding = true;
        playerRoot.SetPositionAndRotation(seatWorldPosition, transform.rotation);
        Debug.Log(
            $"Moto mount: jugador antes={playerPositionBeforeMount:F3}, asiento={seatWorldPosition:F3}, " +
            $"jugador después={playerRoot.position:F3}, parent={(playerRoot.parent != null ? playerRoot.parent.name : "null")}.",
            this);

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        enabled = true;

        riderVisual.SetActive(true);
        drivingHud.SetActive(true);
        thirdPersonCamera.gameObject.SetActive(true);
        if (thirdPersonAudioListener != null)
            thirdPersonAudioListener.enabled = true;

        Vector3 lookTarget = transform.TransformPoint(cameraLookOffset);
        thirdPersonCamera.transform.position = FindCameraPosition(lookTarget, transform.TransformPoint(cameraOffset));
        thirdPersonCamera.transform.rotation = Quaternion.LookRotation(lookTarget - thirdPersonCamera.transform.position, Vector3.up);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (HouseManager.Instance != null)
            HouseManager.Instance.SetPrompt("");
    }

    private void Dismount()
    {
        riding = false;
        throttleInput = 0f;
        brakeInput = 0f;
        steeringInput = 0f;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        drivingHud.SetActive(false);
        riderVisual.SetActive(false);
        thirdPersonCamera.gameObject.SetActive(false);

        Vector3 dismountPosition = transform.position + transform.right * dismountOffset.x;
        dismountPosition.y = playerRoot.position.y;
        playerRoot.SetPositionAndRotation(dismountPosition, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

        firstPersonAudioListener.enabled = firstPersonListenerWasEnabled;
        firstPersonCamera.enabled = firstPersonCameraWasEnabled;
        houseInteractor.enabled = interactorWasEnabled;
        desktopWalkCamera.enabled = desktopCameraWasEnabled;
        pcWalkController.enabled = pcWalkWasEnabled;

        if (crosshair != null)
            crosshair.gameObject.SetActive(crosshairWasActive);
        if (thirdPersonAudioListener != null)
            thirdPersonAudioListener.enabled = false;

        playerCharacterController.enabled = characterControllerWasEnabled;
        enabled = false;
    }

    private Vector3 FindCameraPosition(Vector3 lookTarget, Vector3 desiredPosition)
    {
        Vector3 offset = desiredPosition - lookTarget;
        float distance = offset.magnitude;
        if (distance <= 0.01f)
            return desiredPosition;

        Vector3 direction = offset / distance;
        float nearestDistance = distance;
        foreach (RaycastHit hit in Physics.SphereCastAll(
                     lookTarget, cameraCollisionRadius, direction, distance, Physics.DefaultRaycastLayers,
                     QueryTriggerInteraction.Ignore))
        {
            Transform hitTransform = hit.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;
            nearestDistance = Mathf.Min(nearestDistance, Mathf.Max(0.5f, hit.distance - cameraCollisionRadius));
        }

        return lookTarget + direction * nearestDistance;
    }

    private bool ReferencesAreValid()
    {
        if (body != null && playerRoot != null && pcWalkController != null && desktopWalkCamera != null &&
            houseInteractor != null && playerCharacterController != null && firstPersonCamera != null &&
            firstPersonAudioListener != null && seatAnchor != null && riderVisual != null &&
            thirdPersonCamera != null && drivingHud != null && drivingHudText != null)
            return true;

        Debug.LogError($"MotoController on '{name}' is missing one or more required scene references.", this);
        return false;
    }
}
