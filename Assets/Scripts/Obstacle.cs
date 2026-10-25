using UnityEngine;

// Poner en cada prefab de obstáculo (caja, basurero, planta, etc.).
public class Obstacle : MonoBehaviour
{
    void Awake()
    {
        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true;
    }
}
