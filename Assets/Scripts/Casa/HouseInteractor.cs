using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Va en el jugador de la casa. Detecta el objeto interactuable más cercano y lo usa con la tecla E.
public class HouseInteractor : MonoBehaviour
{
    [Header("Punto de mira")]
    public Image crosshair;
    public Color crosshairIdleColor = new Color(1f, 1f, 1f, 0.6f);
    public Color crosshairTargetColor = new Color(0.25f, 1f, 0.35f, 0.95f);

    Interactable current;

    void Update()
    {
        var hm = HouseManager.Instance;
        if (hm == null) return;

        current = null;
        float best = float.MaxValue;
        foreach (var it in Interactable.All)
        {
            Vector3 d = it.transform.position - transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < it.range && dist < best)
            {
                best = dist;
                current = it;
            }
        }

        bool canInteract = current != null && !hm.IsUIOpen;
        hm.SetPrompt(canInteract ? $"[E] {current.label}" : "");
        if (crosshair != null)
            crosshair.color = canInteract ? crosshairTargetColor : crosshairIdleColor;

        var kb = Keyboard.current;
        if (current != null && kb != null && kb.eKey.wasPressedThisFrame)
            hm.Interact(current);
    }
}
