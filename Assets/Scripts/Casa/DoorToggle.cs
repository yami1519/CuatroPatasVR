using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class DoorToggle : MonoBehaviour
{
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AudioClip openClip;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.18f;

    private Interactable interactable;
    private AudioSource audioSource;
    private Quaternion closedRotation;
    private Coroutine rotationRoutine;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        audioSource = GetComponent<AudioSource>();
        closedRotation = transform.localRotation;

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        if (interactable == null)
            Debug.LogError($"DoorToggle on '{name}' requires an Interactable component.", this);
        else
            UpdateLabel();
    }

    public void Toggle()
    {
        if (interactable == null)
        {
            Debug.LogError($"DoorToggle on '{name}' cannot update its Interactable label.", this);
            return;
        }

        isOpen = !isOpen;
        Quaternion targetRotation = isOpen
            ? closedRotation * Quaternion.Euler(0f, openAngle, 0f)
            : closedRotation;

        if (rotationRoutine != null)
            StopCoroutine(rotationRoutine);
        rotationRoutine = StartCoroutine(RotateTo(targetRotation));

        if (isOpen && openClip != null)
            audioSource.PlayOneShot(openClip, audioVolume);

        UpdateLabel();
    }

    private IEnumerator RotateTo(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / transitionDuration);
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.localRotation = targetRotation;
        rotationRoutine = null;
    }

    private void UpdateLabel()
    {
        interactable.label = isOpen ? "Cerrar puerta" : "Abrir puerta";
    }
}
