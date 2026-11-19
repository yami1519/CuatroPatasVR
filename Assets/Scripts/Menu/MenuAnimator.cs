using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Anima el menú principal: entrada de logo, gato y botones, fondo que respira,
// gato que se mueve suave y huellitas flotando.
public class MenuAnimator : MonoBehaviour
{
    public RectTransform background;
    public RectTransform logo;
    public RectTransform subtitle;
    public RectTransform cat;
    public RectTransform[] buttons;
    public RectTransform pawLayer;
    public Sprite pawSprite;
    public Color pawColor = new Color(0.93f, 0.55f, 0.28f, 0.55f);

    Vector2 logoPos, catPos, subPos;
    Vector2[] btnPos;
    bool introDone;

    void Start()
    {
        if (logo) logoPos = logo.anchoredPosition;
        if (cat) catPos = cat.anchoredPosition;
        if (subtitle) subPos = subtitle.anchoredPosition;
        btnPos = new Vector2[buttons.Length];
        for (int i = 0; i < buttons.Length; i++) btnPos[i] = buttons[i].anchoredPosition;
        StartCoroutine(Intro());
        if (pawLayer && pawSprite) StartCoroutine(PawLoop());
    }

    IEnumerator Intro()
    {
        // Posiciones iniciales fuera de pantalla / invisibles
        if (logo) { logo.anchoredPosition = logoPos + new Vector2(0, 500); }
        if (cat) { cat.anchoredPosition = catPos + new Vector2(900, 0); }
        if (subtitle) SetAlpha(subtitle, 0f);
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].anchoredPosition = btnPos[i] + new Vector2(-900, 0);
            SetAlpha(buttons[i], 0f);
        }

        StartCoroutine(Move(logo, logo ? logo.anchoredPosition : Vector2.zero, logoPos, 0.8f, true));
        StartCoroutine(Move(cat, cat ? cat.anchoredPosition : Vector2.zero, catPos, 0.9f, true));
        yield return new WaitForSeconds(0.45f);
        if (subtitle) StartCoroutine(Fade(subtitle, 0f, 1f, 0.5f));
        for (int i = 0; i < buttons.Length; i++)
        {
            StartCoroutine(Move(buttons[i], buttons[i].anchoredPosition, btnPos[i], 0.55f, true));
            StartCoroutine(Fade(buttons[i], 0f, 1f, 0.4f));
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.6f);
        introDone = true;
    }

    void Update()
    {
        float t = Time.time;

        // Fondo: zoom lento de ida y vuelta
        if (background)
        {
            float s = 1.04f + Mathf.Sin(t * 0.15f) * 0.015f;
            background.localScale = new Vector3(s, s, 1f);
            background.anchoredPosition = new Vector2(Mathf.Sin(t * 0.1f) * 12f, 0f);
        }

        if (!introDone) return;

        // Logo: flota y late un poquito
        if (logo)
        {
            logo.anchoredPosition = logoPos + new Vector2(0, Mathf.Sin(t * 1.6f) * 5f);
            float p = 1f + Mathf.Sin(t * 2.2f) * 0.008f;
            logo.localScale = new Vector3(p, p, 1f);
        }

        // Gato: respira e inclina la cabeza
        if (cat)
        {
            float breath = Mathf.Sin(t * 2f) * 0.012f;
            cat.localScale = new Vector3(1f - breath * 0.5f, 1f + breath, 1f);
            cat.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.8f) * 1.2f);
        }

        // Botón Jugar: late para llamar la atención
        var fx0 = buttons.Length > 0 && buttons[0] ? buttons[0].GetComponent<MenuButtonFX>() : null;
        if (fx0 != null && !fx0.IsHovered)
        {
            float p = 1f + Mathf.Sin(t * 2.5f) * 0.01f;
            buttons[0].localScale = new Vector3(p, p, 1f);
        }
    }

    IEnumerator PawLoop()
    {
        while (true)
        {
            SpawnPaw();
            yield return new WaitForSeconds(Random.Range(2f, 4f));
        }
    }

    void SpawnPaw()
    {
        var go = new GameObject("Huella", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(pawLayer, false);
        var img = go.GetComponent<Image>();
        img.sprite = pawSprite;
        img.color = pawColor;
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        float size = Random.Range(28f, 60f);
        rt.sizeDelta = new Vector2(size, size);
        Vector2 area = pawLayer.rect.size;
        rt.anchoredPosition = new Vector2(Random.Range(-area.x * 0.5f, area.x * 0.5f), -area.y * 0.5f - 40f);
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
        StartCoroutine(FloatUp(rt, img, Random.Range(6f, 10f)));
    }

    IEnumerator FloatUp(RectTransform rt, Image img, float duration)
    {
        Vector2 start = rt.anchoredPosition;
        float h = pawLayer.rect.height + 120f;
        float sway = Random.Range(20f, 60f);
        float phase = Random.Range(0f, 6f);
        Color c = img.color;
        for (float t = 0; t < 1f; t += Time.deltaTime / duration)
        {
            if (!rt) yield break;
            rt.anchoredPosition = start + new Vector2(Mathf.Sin(t * 6f + phase) * sway, t * h);
            float a = Mathf.Sin(t * Mathf.PI); // aparece y desaparece
            img.color = new Color(c.r, c.g, c.b, c.a * a);
            yield return null;
        }
        if (rt) Destroy(rt.gameObject);
    }

    // ---------- helpers ----------
    static IEnumerator Move(RectTransform rt, Vector2 from, Vector2 to, float dur, bool back)
    {
        if (!rt) yield break;
        for (float t = 0; t < 1f; t += Time.deltaTime / dur)
        {
            float e = back ? EaseOutBack(t) : Mathf.SmoothStep(0, 1, t);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    static IEnumerator Fade(RectTransform rt, float from, float to, float dur)
    {
        for (float t = 0; t < 1f; t += Time.deltaTime / dur)
        {
            SetAlpha(rt, Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetAlpha(rt, to);
    }

    static void SetAlpha(RectTransform rt, float a)
    {
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = a;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
    }
}
