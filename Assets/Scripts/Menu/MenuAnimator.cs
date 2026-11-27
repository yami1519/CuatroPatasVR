using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Anima el menú principal de forma segura sin desalinear botones sobre una imagen de fondo.
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

    [Header("Ajustes con imagen única")]
    [Tooltip("Déjalo desactivado si los botones ya están alineados sobre la imagen de fondo")]
    public bool slideButtonsOnIntro = false;
    [Tooltip("Déjalo desactivado para que el fondo no se mueva respecto a los botones")]
    public bool animateBackground = false;

    Vector2 logoPos, catPos, subPos, bgPos;
    Vector2[] btnPos;
    bool introDone;

    void Start()
    {
        if (background) bgPos = background.anchoredPosition;
        if (logo) logoPos = logo.anchoredPosition;
        if (cat) catPos = cat.anchoredPosition;
        if (subtitle) subPos = subtitle.anchoredPosition;

        if (buttons != null && buttons.Length > 0)
        {
            btnPos = new Vector2[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    btnPos[i] = buttons[i].anchoredPosition;
            }
        }
        else
        {
            btnPos = new Vector2[0];
        }

        StartCoroutine(Intro());
        if (pawLayer && pawSprite) StartCoroutine(PawLoop());
    }

    IEnumerator Intro()
    {
        if (logo)
        {
            logo.anchoredPosition = logoPos + new Vector2(0, 250f);
            SetAlpha(logo, 0f);
        }
        if (cat)
        {
            cat.anchoredPosition = catPos + new Vector2(400f, 0f);
            SetAlpha(cat, 0f);
        }
        if (subtitle) SetAlpha(subtitle, 0f);

        for (int i = 0; i < btnPos.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;

            if (slideButtonsOnIntro)
                btn.anchoredPosition = btnPos[i] + new Vector2(-300f, 0f);

            SetAlpha(btn, 0f);
        }

        if (logo)
        {
            StartCoroutine(Move(logo, logo.anchoredPosition, logoPos, 0.75f, true));
            StartCoroutine(Fade(logo, 0f, 1f, 0.5f));
        }
        if (cat)
        {
            StartCoroutine(Move(cat, cat.anchoredPosition, catPos, 0.85f, true));
            StartCoroutine(Fade(cat, 0f, 1f, 0.5f));
        }

        yield return new WaitForSeconds(0.35f);

        if (subtitle) StartCoroutine(Fade(subtitle, 0f, 1f, 0.45f));

        for (int i = 0; i < btnPos.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;

            if (slideButtonsOnIntro)
                StartCoroutine(Move(btn, btn.anchoredPosition, btnPos[i], 0.45f, true));

            StartCoroutine(Fade(btn, 0f, 1f, 0.35f));
            yield return new WaitForSeconds(0.08f);
        }

        yield return new WaitForSeconds(0.5f);
        introDone = true;
    }

    void Update()
    {
        float t = Time.unscaledTime;

        if (background && animateBackground)
        {
            float s = 1.02f + Mathf.Sin(t * 0.25f) * 0.008f;
            background.localScale = new Vector3(s, s, 1f);
            background.anchoredPosition = bgPos + new Vector2(Mathf.Sin(t * 0.2f) * 6f, 0f);
        }

        if (!introDone) return;

        if (logo)
        {
            logo.anchoredPosition = logoPos + new Vector2(0, Mathf.Sin(t * 1.6f) * 4f);
            float p = 1f + Mathf.Sin(t * 2.2f) * 0.006f;
            logo.localScale = new Vector3(p, p, 1f);
        }

        if (cat)
        {
            float breath = Mathf.Sin(t * 2f) * 0.01f;
            cat.localScale = new Vector3(1f - breath * 0.5f, 1f + breath, 1f);
            cat.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.8f) * 1f);
        }

        if (buttons != null && buttons.Length > 0 && buttons[0] != null)
        {
            var fx0 = buttons[0].GetComponent<MenuButtonFX>();
            if (fx0 != null && !fx0.IsHovered)
            {
                float p = 1f + Mathf.Sin(t * 2.5f) * 0.01f;
                buttons[0].localScale = new Vector3(p, p, 1f);
            }
        }
    }

    IEnumerator PawLoop()
    {
        while (true)
        {
            SpawnPaw();
            yield return new WaitForSecondsRealtime(Random.Range(2f, 4f));
        }
    }

    void SpawnPaw()
    {
        if (!pawLayer || !pawSprite) return;

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

        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            if (!rt) yield break;
            rt.anchoredPosition = start + new Vector2(Mathf.Sin(t * 6f + phase) * sway, t * h);
            float a = Mathf.Sin(t * Mathf.PI);
            img.color = new Color(c.r, c.g, c.b, c.a * a);
            yield return null;
        }
        if (rt) Destroy(rt.gameObject);
    }

    static IEnumerator Move(RectTransform rt, Vector2 from, Vector2 to, float dur, bool back)
    {
        if (!rt) yield break;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / dur)
        {
            float e = back ? EaseOutBack(t) : Mathf.SmoothStep(0, 1, t);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    static IEnumerator Fade(RectTransform rt, float from, float to, float dur)
    {
        if (!rt) yield break;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / dur)
        {
            SetAlpha(rt, Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetAlpha(rt, to);
    }

    static void SetAlpha(RectTransform rt, float a)
    {
        if (!rt) return;
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