using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Efecto de los botones del menú: crece al pasar el mouse, se aplasta al hacer clic y suena.
public class MenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.07f;
    public float pressScale = 0.94f;
    public AudioClip hoverClip;
    public AudioClip clickClip;
    [Range(0f, 1f)] public float volume = 0.6f;

    [Tooltip("Si otro script anima este botón en reposo (por ejemplo, el latido de Jugar)")]
    public bool idleDrivenExternally = false;

    public bool IsHovered { get; private set; }
    private bool pressed;
    private float target = 1f;
    private Button button;
    private AudioSource uiAudio;

    private void Awake()
    {
        button = GetComponent<Button>();

        uiAudio = gameObject.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.spatialBlend = 0f; // Sonido 2D directo en UI
    }

    private void OnEnable()
    {
        IsHovered = false;
        pressed = false;
        target = 1f;
        transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        IsHovered = false;
        pressed = false;
        target = 1f;
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (idleDrivenExternally && !IsHovered && !pressed) return;

        float s = Mathf.Lerp(transform.localScale.x, target, 14f * Time.unscaledDeltaTime);
        transform.localScale = new Vector3(s, s, 1f);
    }

    private bool CanInteract() => button == null || button.interactable;

    public void OnPointerEnter(PointerEventData e)
    {
        if (!CanInteract()) return;
        IsHovered = true;
        target = hoverScale;
        Play(hoverClip, 0.4f);
    }

    public void OnPointerExit(PointerEventData e)
    {
        IsHovered = false;
        target = pressed ? pressScale : 1f;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!CanInteract()) return;
        pressed = true;
        target = pressScale;
        Play(clickClip, 1f);
    }

    public void OnPointerUp(PointerEventData e)
    {
        pressed = false;
        target = IsHovered && CanInteract() ? hoverScale : 1f;
    }

    private void Play(AudioClip clip, float mul)
    {
        if (clip == null || uiAudio == null) return;
        uiAudio.PlayOneShot(clip, Mathf.Clamp01(volume * mul));
    }
}