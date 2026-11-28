using System;
using System.Collections;
using UnityEngine;

// Anima la mano del dueño (hija de la cámara) al estilo Nintendogs:
// se estira hacia el objeto, hace el gesto (servir, recoger, acariciar, tocar) y vuelve.
public class HandController : MonoBehaviour
{
    public Transform hand;                 // objeto "Mano" (hijo de la cámara)
    public Transform heldItem;             // objeto que aparece en la mano (bolsa de comida, etc.)
    public Renderer heldItemRenderer;

    public Vector3 restPos = new Vector3(0.08f, -0.2f, 0.45f);
    public Vector3 restEuler = new Vector3(-20f, -8f, -10f); // dedos apuntando al centro de la pantalla

    [Header("Colores del objeto en la mano")]
    public Color foodColor = new Color(0.79f, 0.41f, 0.18f);
    public Color waterColor = new Color(0.36f, 0.78f, 0.80f);
    public Color sandColor = new Color(0.91f, 0.77f, 0.42f);

    public bool IsBusy { get; private set; }

    bool stroking;
    float strokeT;
    float idleT;
    bool hovering;
    Vector3 hoverPoint;

    // La mano se acerca un poco a lo que estás mirando (como el puntero de Nintendogs)
    public void SetHover(bool active, Vector3 worldPoint)
    {
        hovering = active;
        hoverPoint = worldPoint;
    }

    void Start()
    {
        if (hand)
        {
            hand.localPosition = restPos;
            hand.localEulerAngles = restEuler;
        }
        ShowItem(false, Color.white);
    }

    void Update()
    {
        if (!hand || IsBusy || stroking) return;
        // pequeño balanceo en reposo
        idleT += Time.deltaTime;
        Vector3 sway = new Vector3(0f, Mathf.Sin(idleT * 2f) * 0.006f, 0f);
        Vector3 target = restPos;
        Quaternion rot = Quaternion.Euler(restEuler);
        if (hovering)
        {
            target = Vector3.Lerp(restPos, ReachPoint(hoverPoint), 0.25f);
            rot = Quaternion.Euler(-5f, 0f, 0f); // dedo apuntando
        }
        hand.localPosition = Vector3.Lerp(hand.localPosition, target + sway, 10f * Time.deltaTime);
        hand.localRotation = Quaternion.Slerp(hand.localRotation, rot, 10f * Time.deltaTime);
    }

    // Llamar cada frame mientras se mantiene el clic sobre el gato
    public void StrokeAt(Vector3 worldPoint)
    {
        if (!hand) return;
        stroking = true;
        strokeT += Time.deltaTime * 9f;
        Vector3 local = ReachPoint(worldPoint + Vector3.up * 0.08f);
        local += new Vector3(Mathf.Sin(strokeT) * 0.1f, Mathf.Abs(Mathf.Cos(strokeT)) * 0.02f, 0f);
        hand.localPosition = Vector3.Lerp(hand.localPosition, local, 12f * Time.deltaTime);
        hand.localRotation = Quaternion.Slerp(hand.localRotation, Quaternion.Euler(25f, Mathf.Sin(strokeT) * 15f, 0f), 10f * Time.deltaTime);
    }

    public void StopStroke() => stroking = false;

    public void PlayAction(InteractionType type, Vector3 worldTarget, Action onApply)
    {
        if (IsBusy || !hand) { onApply?.Invoke(); return; }
        StartCoroutine(ActionRoutine(type, worldTarget, onApply));
    }

    IEnumerator ActionRoutine(InteractionType type, Vector3 worldTarget, Action onApply)
    {
        IsBusy = true;
        stroking = false;

        if (type == InteractionType.Alimentar) ShowItem(true, foodColor);
        else if (type == InteractionType.DarAgua) ShowItem(true, waterColor);
        else if (type == InteractionType.LimpiarArena) ShowItem(true, sandColor);

        Vector3 reach = ReachPoint(worldTarget + Vector3.up * 0.15f);
        yield return MoveTo(reach, Quaternion.Euler(10f, 0f, 0f), 0.25f);

        switch (type)
        {
            case InteractionType.Alimentar:
            case InteractionType.DarAgua:
                // inclinar para servir
                yield return MoveTo(reach, Quaternion.Euler(10f, 0f, -95f), 0.2f);
                yield return new WaitForSeconds(0.35f);
                yield return MoveTo(reach, Quaternion.Euler(10f, 0f, 0f), 0.15f);
                break;

            case InteractionType.LimpiarArena:
                // recoger dos veces
                for (int i = 0; i < 2; i++)
                {
                    yield return MoveTo(reach + new Vector3(0f, -0.08f, 0.05f), Quaternion.Euler(45f, 0f, 0f), 0.15f);
                    yield return MoveTo(reach + new Vector3(0.05f, 0.05f, -0.03f), Quaternion.Euler(-20f, 0f, 0f), 0.15f);
                }
                break;

            case InteractionType.Acariciar:
                for (int i = 0; i < 3; i++)
                {
                    yield return MoveTo(reach + new Vector3(-0.08f, 0f, 0f), Quaternion.Euler(25f, -12f, 0f), 0.12f);
                    yield return MoveTo(reach + new Vector3(0.08f, 0f, 0f), Quaternion.Euler(25f, 12f, 0f), 0.12f);
                }
                break;

            default:
                // tocar (computadora, puerta, cama)
                yield return MoveTo(reach + new Vector3(0f, 0f, 0.06f), Quaternion.Euler(0f, 0f, 0f), 0.1f);
                yield return MoveTo(reach, Quaternion.Euler(10f, 0f, 0f), 0.1f);
                break;
        }

        ShowItem(false, Color.white);
        onApply?.Invoke();

        if (hand) yield return MoveTo(restPos, Quaternion.Euler(restEuler), 0.25f);
        IsBusy = false;
    }

    Vector3 ReachPoint(Vector3 worldTarget)
    {
        Vector3 local = hand.parent ? hand.parent.InverseTransformPoint(worldTarget) : worldTarget;
        local = Vector3.ClampMagnitude(local, 0.8f); // largo del brazo
        if (local.z < 0.35f) local.z = 0.35f;        // siempre delante de la cámara
        return local;
    }

    IEnumerator MoveTo(Vector3 localPos, Quaternion localRot, float duration)
    {
        if (!hand) yield break;
        Vector3 p0 = hand.localPosition;
        Quaternion r0 = hand.localRotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            if (!hand) yield break;
            float e = Mathf.SmoothStep(0f, 1f, t);
            hand.localPosition = Vector3.Lerp(p0, localPos, e);
            hand.localRotation = Quaternion.Slerp(r0, localRot, e);
            yield return null;
        }
        hand.localPosition = localPos;
        hand.localRotation = localRot;
    }

    void ShowItem(bool show, Color color)
    {
        if (!heldItem) return;
        heldItem.gameObject.SetActive(show);
        if (show && heldItemRenderer) heldItemRenderer.material.color = color;
    }
}
