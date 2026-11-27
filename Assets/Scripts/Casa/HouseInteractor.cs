using UnityEngine;
using UnityEngine.InputSystem;

// Va en el jugador de la casa. Detecta el objeto interactuable más cercano y lo usa con la tecla E.
public class HouseInteractor : MonoBehaviour
{
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

        hm.SetPrompt(current != null && !hm.IsUIOpen ? $"[E] {current.label}" : "");

        var kb = Keyboard.current;
        if (current != null && kb != null && kb.eKey.wasPressedThisFrame)
            hm.Interact(current);
    }
}
