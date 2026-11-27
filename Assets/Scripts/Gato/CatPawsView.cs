using CuatroPatasVR.Comun;
using UnityEngine;
using UnityEngine.InputSystem;

public class CatPawsView : MonoBehaviour
{
    [SerializeField] Transform pawsRoot;
    [SerializeField] float walkBobAmount = 0.012f;
    [SerializeField] float gestureLift = 0.055f;
    [SerializeField] float gestureDuration = 0.3f;

    CatModeController owner;
    CharacterController characterController;
    Renderer[] pawRenderers;
    Vector3 basePosition;
    Quaternion baseRotation;
    float stepPhase;
    float gestureTimer = -1f;
    bool pawsVisible = true;

    void Awake()
    {
        owner = GetComponentInParent<CatModeController>();
        characterController = owner != null ? owner.GetComponent<CharacterController>() : null;
        if (pawsRoot == null)
            pawsRoot = transform.Find("CatPawModel");
        if (pawsRoot == null)
            pawsRoot = transform;

        basePosition = pawsRoot.localPosition;
        baseRotation = pawsRoot.localRotation;
        pawRenderers = pawsRoot.GetComponentsInChildren<Renderer>(true);
        SetPawsVisible(false);
    }

    void LateUpdate()
    {
        bool controlled = owner != null && owner.IsPlayerControlled;
        SetPawsVisible(controlled);
        if (!controlled)
            return;

        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            gestureTimer = gestureDuration;

        float speed = 0f;
        if (characterController != null)
        {
            Vector3 velocity = characterController.velocity;
            speed = new Vector2(velocity.x, velocity.z).magnitude;
        }

        float movement = Mathf.Clamp01(speed / 0.8f);
        stepPhase += Time.deltaTime * (2f + speed * 7f);
        float bob = Mathf.Sin(stepPhase) * walkBobAmount * movement;
        float roll = Mathf.Sin(stepPhase) * 4f * movement;
        float gesture = 0f;

        if (gestureTimer >= 0f)
        {
            float progress = 1f - gestureTimer / gestureDuration;
            gesture = Mathf.Sin(progress * Mathf.PI) * gestureLift;
            gestureTimer -= Time.deltaTime;
            if (gestureTimer < 0f)
                gestureTimer = -1f;
        }

        pawsRoot.localPosition = basePosition + Vector3.up * (bob + gesture);
        pawsRoot.localRotation = baseRotation * Quaternion.Euler(-gesture * 100f, 0f, roll);
    }

    void OnDisable()
    {
        SetPawsVisible(false);
    }

    void SetPawsVisible(bool visible)
    {
        if (pawsVisible == visible)
            return;

        pawsVisible = visible;
        if (pawRenderers == null)
            return;

        foreach (var pawRenderer in pawRenderers)
        {
            if (pawRenderer != null)
                pawRenderer.enabled = visible;
        }
    }
}
