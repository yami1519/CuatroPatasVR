using UnityEngine;

// Mueve el objeto hacia el jugador según la velocidad de la partida y lo borra al pasar.
// (TrackSpawner lo agrega automáticamente; no hace falta ponerlo a mano.)
public class MovingObject : MonoBehaviour
{
    public float destroyZ = -10f;

    void Update()
    {
        var rm = RunnerManager.Instance;
        if (rm == null || !rm.IsRunning) return;

        transform.position += Vector3.back * rm.CurrentSpeed * Time.deltaTime;
        if (transform.position.z < destroyZ) Destroy(gameObject);
    }
}
