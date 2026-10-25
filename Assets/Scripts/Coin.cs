using UnityEngine;

// Poner en el prefab de la moneda.
public class Coin : MonoBehaviour
{
    public int value = 1;
    public float rotateSpeed = 180f;

    void Awake()
    {
        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true;
    }

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }
}
