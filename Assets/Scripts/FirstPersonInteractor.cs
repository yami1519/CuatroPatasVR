using UnityEngine;
using UnityEngine.InputSystem;

// Interacción en primera persona: mira un objeto (punto del centro) y usa clic o E.
// Con el gato: mantén el clic para acariciarlo (estilo Nintendogs).
public class FirstPersonInteractor : MonoBehaviour
{
    public Camera cam;
    public HandController hand;
    public CatWander cat;
    public float range = 3f;

    void Update()
    {
        var hm = HouseManager.Instance;
        if (hm == null || cam == null || hand == null) return;

        if (hm.IsUIOpen)
        {
            hm.SetPrompt("");
            hand.StopStroke();
            hand.SetHover(false, Vector3.zero);
            return;
        }

        Interactable it = null;
        Vector3 hitPoint = Vector3.zero;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide))
        {
            it = hit.collider.GetComponentInParent<Interactable>();
            hitPoint = hit.point;
        }

        hand.SetHover(it != null, hitPoint);

        var mouse = Mouse.current;
        var kb = Keyboard.current;

        // Acariciar manteniendo el clic
        bool petting = it != null && it.type == InteractionType.Acariciar &&
                       mouse != null && mouse.leftButton.isPressed && !hand.IsBusy;
        if (petting)
        {
            hand.StrokeAt(hitPoint);
            hm.PetTick(Time.deltaTime);
            if (cat) cat.Pause(1f, transform.position);
            hm.SetPrompt("Acariciando... <3");
            return;
        }
        hand.StopStroke();

        if (it == null)
        {
            hm.SetPrompt("");
            return;
        }

        hm.SetPrompt(it.type == InteractionType.Acariciar
            ? "Acariciar al gato"
            : it.label);

        bool pressed = (kb != null && kb.eKey.wasPressedThisFrame) ||
                       (mouse != null && mouse.leftButton.wasPressedThisFrame && it.type != InteractionType.Acariciar);

        if (pressed && !hand.IsBusy)
        {
            var target = it;
            if (it.type == InteractionType.Acariciar && cat) cat.Pause(2.5f, transform.position);
            hand.PlayAction(it.type, hitPoint, () => hm.Interact(target));
        }
    }
}
