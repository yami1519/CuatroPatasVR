using UnityEngine;

public class CarTrafficMover : MonoBehaviour
{
    [Header("Ruta en la pista")]
    [SerializeField] private RoadWaypoint currentWaypoint;
    [SerializeField] private float waypointReachDistance = 1.2f;

    [Header("Velocidad y Giro")]
    [SerializeField] private float minSpeed = 4.5f;
    [SerializeField] private float maxSpeed = 7.5f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float acceleration = 4f;

    [Header("Frenado ante obstáculos (otros carros / jugador)")]
    [SerializeField] private float sensorDistance = 4.5f;
    [SerializeField] private float sensorHeight = 0.6f;
    [SerializeField] private LayerMask obstacleLayers = ~0;

    [Header("Animación de Ruedas (Opcional)")]
    [SerializeField] private Transform[] wheels;
    [SerializeField] private float wheelSpinMultiplier = 180f;

    private float targetSpeed;
    private float currentSpeed;

    private void Start()
    {
        // Cada carro arranca con una velocidad ligeramente distinta para que no vayan en fila robótica
        targetSpeed = Random.Range(minSpeed, maxSpeed);
        currentSpeed = targetSpeed;

        if (currentWaypoint != null)
        {
            Vector3 lookPos = new Vector3(currentWaypoint.transform.position.x, transform.position.y, currentWaypoint.transform.position.z);
            transform.LookAt(lookPos);
        }
    }

    private void Update()
    {
        if (currentWaypoint == null) return;

        // 1. Detectar si hay otro carro o el jugador delante
        bool blocked = CheckFrontObstacle();
        float desiredSpeed = blocked ? 0f : targetSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

        // 2. Dirección hacia el siguiente waypoint de la pista (manteniendo la altura Y del carro)
        Vector3 targetPos = new Vector3(currentWaypoint.transform.position.x, transform.position.y, currentWaypoint.transform.position.z);
        Vector3 dir = targetPos - transform.position;
        float dist = dir.magnitude;

        if (dist <= waypointReachDistance)
        {
            RoadWaypoint next = currentWaypoint.GetNextWaypoint();
            if (next != null)
            {
                currentWaypoint = next;
                // Cambia sutilmente la velocidad al tomar un nuevo tramo
                targetSpeed = Random.Range(minSpeed, maxSpeed);
            }
            return;
        }

        // 3. Rotar suavemente hacia el punto de la pista y avanzar
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * (currentSpeed * Time.deltaTime);

        // 4. Girar las ruedas si fueron asignadas
        if (wheels != null && currentSpeed > 0.05f)
        {
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                    wheel.Rotate(Vector3.right, currentSpeed * wheelSpinMultiplier * Time.deltaTime, Space.Self);
            }
        }
    }

    private bool CheckFrontObstacle()
    {
        Vector3 origin = transform.position + Vector3.up * sensorHeight + transform.forward * 1.2f;
        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, sensorDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            // Ignora colisiones con el propio carro
            if (hit.transform.root != transform.root)
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = transform.position + Vector3.up * sensorHeight + transform.forward * 1.2f;
        Gizmos.DrawLine(origin, origin + transform.forward * sensorDistance);
    }
}