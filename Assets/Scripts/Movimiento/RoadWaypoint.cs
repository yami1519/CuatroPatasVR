using System.Collections.Generic;
using UnityEngine;

public class RoadWaypoint : MonoBehaviour
{
    [Tooltip("Siguiente(s) punto(s) de la pista. Si pones más de uno, el carro elegirá un camino al azar en el cruce.")]
    public List<RoadWaypoint> nextWaypoints = new List<RoadWaypoint>();

    public RoadWaypoint GetNextWaypoint()
    {
        if (nextWaypoints == null || nextWaypoints.Count == 0)
            return null;

        // Filtra nulos por si borraste algún punto
        var valid = nextWaypoints.FindAll(w => w != null);
        if (valid.Count == 0) return null;

        return valid[Random.Range(0, valid.Count)];
    }

    // Dibuja líneas amarillas en la escena para ver el circuito de pistas
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.35f);

        if (nextWaypoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var next in nextWaypoints)
        {
            if (next != null)
            {
                Gizmos.DrawLine(transform.position, next.transform.position);
            }
        }
    }
}