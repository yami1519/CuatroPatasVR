using UnityEngine;

// Cámara tipo "casa de muñecas" que sigue al jugador desde arriba.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 11f, -9f);
    public float smooth = 5f;

    void LateUpdate()
    {
        if (!target) return;
        transform.position = Vector3.Lerp(transform.position, target.position + offset, smooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up);
    }
}
