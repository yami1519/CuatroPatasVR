using UnityEngine;
using UnityEngine.EventSystems;

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
    bool pressed;
    float target = 1f;

    void Update()
    {
        if (idleDrivenExternally && !IsHovered && !pressed) return;
        float s = Mathf.Lerp(transform.localScale.x, target, 14f * Time.unscaledDeltaTime);
        transform.localScale = new Vector3(s, s, 1f);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        IsHovered = true;
        target = hoverScale;
        Play(hoverClip, 0.4f);
    }

    public void OnPointerExit(PointerEventData e)
    {
        IsHovered = false;
        target = 1f;
    }

    public void OnPointerDown(PointerEventData e)
    {
        pressed = true;
        target = pressScale;
        Play(clickClip, 1f);
    }

    public void OnPointerUp(PointerEventData e)
    {
        pressed = false;
        target = IsHovered ? hoverScale : 1f;
    }

    void Play(AudioClip clip, float mul)
    {
        if (clip == null) return;
        var cam = Camera.main;
        AudioSource.PlayClipAtPoint(clip, cam ? cam.transform.position : Vector3.zero, volume * mul);
    }
}
