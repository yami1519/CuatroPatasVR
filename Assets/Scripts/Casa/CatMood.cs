using System.Collections;
using UnityEngine;

// Globo de pensamiento sobre el gato (muestra lo que necesita) y corazones cuando lo cuidas.
public class CatMood : MonoBehaviour
{
    public static CatMood Instance { get; private set; }

    [Header("Globo")]
    public Transform bubble;             // objeto con el SpriteRenderer del globo
    public SpriteRenderer bubbleIcon;    // ícono dentro del globo
    public float bubbleHeight = 1.1f;
    public float showBelow = 35f;        // muestra la necesidad cuando baja de este valor

    [Header("Íconos")]
    public Sprite hungerIcon, thirstIcon, cleanIcon, moodIcon, sickIcon;

    [Header("Corazones")]
    public Sprite heartSprite;

    float hideTimer;
    Vector3 bubbleBase;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        if (bubble) bubbleBase = bubble.localScale;
    }

    void LateUpdate()
    {
        hideTimer -= Time.deltaTime;
        if (!bubble) return;

        Sprite need = CurrentNeed();
        bool show = need != null && hideTimer <= 0f;
        bubble.gameObject.SetActive(show);
        if (!show) return;

        if (bubbleIcon) bubbleIcon.sprite = need;
        // Flota sobre la cabeza y siempre mira a la cámara
        bubble.position = transform.position + Vector3.up * (bubbleHeight + Mathf.Sin(Time.time * 2.5f) * 0.05f);
        var cam = Camera.main;
        if (cam) bubble.rotation = Quaternion.LookRotation(bubble.position - cam.transform.position);
        float p = 1f + Mathf.Sin(Time.time * 4f) * 0.04f;
        bubble.localScale = bubbleBase * p;
    }

    Sprite CurrentNeed()
    {
        var d = GameManager.Get().data;
        if (d.catIsSick) return sickIcon;
        float min = showBelow;
        Sprite s = null;
        if (d.hunger < min) { min = d.hunger; s = hungerIcon; }
        if (d.thirst < min) { min = d.thirst; s = thirstIcon; }
        if (d.cleanliness < min) { min = d.cleanliness; s = cleanIcon; }
        if (d.mood < min) { min = d.mood; s = moodIcon; }
        return s;
    }

    // Corazones que suben (acariciar, comer, jugar)
    public void Hearts(int count = 5)
    {
        hideTimer = 2.5f; // ocultar el globo mientras está feliz
        if (heartSprite == null) return;
        for (int i = 0; i < count; i++) StartCoroutine(Heart(i * 0.12f));
        StartCoroutine(Hop());
    }

    IEnumerator Heart(float delay)
    {
        yield return new WaitForSeconds(delay);
        var go = new GameObject("Corazon");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = heartSprite;
        Vector3 start = transform.position + new Vector3(Random.Range(-0.25f, 0.25f), 0.6f, Random.Range(-0.25f, 0.25f));
        float size = Random.Range(0.18f, 0.3f);
        float dur = Random.Range(1f, 1.4f);
        var cam = Camera.main;
        for (float t = 0; t < 1f; t += Time.deltaTime / dur)
        {
            go.transform.position = start + new Vector3(Mathf.Sin(t * 8f) * 0.08f, t * 0.9f, 0f);
            if (cam) go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.transform.position);
            float s = size * Mathf.Min(1f, t * 5f);
            go.transform.localScale = new Vector3(s, s, s);
            var c = sr.color; c.a = 1f - Mathf.Max(0f, (t - 0.6f) / 0.4f); sr.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // Saltito de alegría (mueve el modelo, no el objeto que controla el NavMesh)
    IEnumerator Hop()
    {
        var parts = new System.Collections.Generic.List<Transform>();
        foreach (Transform ch in transform) if (ch != bubble) parts.Add(ch);
        var basePos = new Vector3[parts.Count];
        for (int i = 0; i < parts.Count; i++) basePos[i] = parts[i].localPosition;
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f)
        {
            float h = Mathf.Sin(t * Mathf.PI) * 0.15f;
            for (int i = 0; i < parts.Count; i++) if (parts[i]) parts[i].localPosition = basePos[i] + Vector3.up * h;
            yield return null;
        }
        for (int i = 0; i < parts.Count; i++) if (parts[i]) parts[i].localPosition = basePos[i];
    }
}
