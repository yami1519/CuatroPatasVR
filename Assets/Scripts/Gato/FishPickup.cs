using System.Collections;
using CuatroPatasVR.Comun;
using UnityEngine;

public class FishPickup : MonoBehaviour
{
    public float hungerAmount = 18f;
    public float pickupDistance = 1.5f;
    public float floatAmplitude = 0.15f;
    public float floatFrequency = 1.5f;
    public float rotationSpeed = 25f;
    public float consumeDuration = 0.18f;

    SphereCollider pickupTrigger;
    Vector3 startPosition;
    float floatPhase;
    bool consumed;

    void Awake()
    {
        pickupTrigger = GetComponent<SphereCollider>();
        if (pickupTrigger == null)
            pickupTrigger = gameObject.AddComponent<SphereCollider>();
        pickupTrigger.isTrigger = true;
    }

    void Start()
    {
        startPosition = transform.position;
        floatPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (consumed) return;

        transform.position = startPosition +
            Vector3.up * (Mathf.Sin(Time.time * floatFrequency + floatPhase) * floatAmplitude);
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other) => TryPickup(other);

    void OnTriggerStay(Collider other) => TryPickup(other);

    void TryPickup(Collider other)
    {
        if (consumed) return;

        var cat = other.GetComponentInParent<CatModeController>();
        if (cat == null || !cat.IsPlayerControlled ||
            Vector3.Distance(cat.transform.position, transform.position) > pickupDistance)
            return;

        consumed = true;
        if (HouseManager.Instance != null)
        {
            HouseManager.Instance.FeedCat(hungerAmount);
        }
        else
        {
            var gm = GameManager.Get();
            gm.data.hunger = Mathf.Min(100f, gm.data.hunger + hungerAmount);
            gm.Save();
        }

        if (pickupTrigger != null) pickupTrigger.enabled = false;
        StartCoroutine(Consume());
    }

    IEnumerator Consume()
    {
        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < consumeDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / consumeDuration);
            yield return null;
        }
        Destroy(gameObject);
    }
}
