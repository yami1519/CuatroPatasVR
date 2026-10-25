using UnityEngine;

// Cámara del Modo Correr: sigue suave a la corredora entre carriles,
// se abre (FOV) al subir la velocidad y tiembla al chocar.
[RequireComponent(typeof(Camera))]
public class RunnerCamera : MonoBehaviour
{
    public static RunnerCamera Instance { get; private set; }

    public Transform target;
    public float height = 3.2f;
    public float distance = 6.5f;
    public float pitch = 16f;
    public float followX = 5f;
    public float baseFov = 60f;
    public float maxFov = 74f;

    Camera cam;
    float shake;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 p = transform.position;
        float x = Mathf.Lerp(p.x, target.position.x * 0.7f, followX * Time.deltaTime);
        float y = height + (target.position.y - 1f) * 0.35f;
        Vector3 pos = new Vector3(x, y, target.position.z - distance);

        if (shake > 0f)
        {
            pos += Random.insideUnitSphere * shake * 0.35f;
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 2.5f);
        }
        transform.position = pos;
        transform.rotation = Quaternion.Euler(pitch, (target.position.x - x) * 1.5f, 0f);

        var rm = RunnerManager.Instance;
        if (rm != null && cam != null)
        {
            float t = Mathf.InverseLerp(rm.startSpeed, rm.maxSpeed, rm.CurrentSpeed);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Lerp(baseFov, maxFov, t), 3f * Time.deltaTime);
        }
    }

    public void Shake(float amount = 1f) => shake = Mathf.Max(shake, amount);
}
