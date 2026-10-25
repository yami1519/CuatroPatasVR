using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Vida de la casa: reloj del día con luz que cambia (mañana → atardecer → noche),
// fundido a negro al dormir, música, sonidos de acciones, pasos y barras que parpadean.
public class HouseFX : MonoBehaviour
{
    public static HouseFX Instance { get; private set; }

    [Header("Día")]
    public Light sun;
    public TMP_Text clockText;
    [Tooltip("Segundos reales que dura un día (de 7:00 a 22:00)")]
    public float dayDuration = 300f;
    public float startHour = 7f, endHour = 22f;
    public float hoursAwayWhenRunning = 3f;

    [Header("Fundido")]
    public CanvasGroup fade;

    [Header("Audio")]
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.25f;
    public AudioClip eatClip, drinkClip, litterClip, petClip, buyClip, sleepClip, warningClip;
    public AudioClip[] footsteps;

    [Header("Jugador (para los pasos)")]
    public CharacterController player;

    AudioSource sfx, musicSrc, stepSrc;
    float stepTimer;
    bool warnedLate;
    Image[] fills;
    Color[] fillBase;

    GameData D => GameManager.Get().data;

    void Awake()
    {
        Instance = this;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        stepSrc = gameObject.AddComponent<AudioSource>();
        stepSrc.playOnAwake = false;
        stepSrc.volume = 0.35f;
        if (music)
        {
            musicSrc = gameObject.AddComponent<AudioSource>();
            musicSrc.clip = music; musicSrc.loop = true; musicSrc.volume = musicVolume;
            musicSrc.Play();
        }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        var gm = GameManager.Get();
        if (D.hour < startHour || D.hour > 24f) D.hour = startHour;

        var hm = HouseManager.Instance;
        if (hm)
        {
            var rts = new[] { hm.hungerFill, hm.thirstFill, hm.cleanFill, hm.moodFill };
            fills = new Image[rts.Length];
            fillBase = new Color[rts.Length];
            for (int i = 0; i < rts.Length; i++)
            {
                fills[i] = rts[i] ? rts[i].GetComponent<Image>() : null;
                fillBase[i] = fills[i] ? fills[i].color : Color.white;
            }
        }

        if (fade) { fade.alpha = 1f; StartCoroutine(FadeTo(0f, 0.8f)); }
        ApplyLight();
    }

    // Llamado por HouseManager al volver del Modo Correr
    public void AddHoursAway()
    {
        D.hour = Mathf.Min(endHour, D.hour + hoursAwayWhenRunning);
    }

    void Update()
    {
        var hm = HouseManager.Instance;
        bool ui = hm != null && hm.IsUIOpen;

        // Reloj
        if (!ui && D.hour < endHour)
        {
            D.hour += (endHour - startHour) / dayDuration * Time.deltaTime;
            if (D.hour >= 20.5f && !warnedLate)
            {
                warnedLate = true;
                Play(warningClip, 0.6f);
                hm?.Notify("Ya es tarde... cuando termines, ve a dormir en la cama.");
            }
        }
        if (clockText)
        {
            int h = Mathf.FloorToInt(D.hour);
            int m = Mathf.FloorToInt((D.hour - h) * 60f / 10f) * 10;
            clockText.text = $"{h:00}:{m:00}";
        }
        ApplyLight();

        // Barras que parpadean en rojo cuando están bajas
        if (fills != null)
        {
            float[] v = { D.hunger, D.thirst, D.cleanliness, D.mood };
            for (int i = 0; i < fills.Length; i++)
            {
                if (!fills[i]) continue;
                if (v[i] < 20f)
                {
                    float k = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                    fills[i].color = Color.Lerp(fillBase[i], new Color(0.9f, 0.15f, 0.15f), k);
                }
                else fills[i].color = fillBase[i];
            }
        }

        // Pasos
        if (player && !ui && player.isGrounded)
        {
            Vector3 vel = player.velocity; vel.y = 0f;
            if (vel.magnitude > 0.8f)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f && footsteps != null && footsteps.Length > 0)
                {
                    stepSrc.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                    stepSrc.PlayOneShot(footsteps[UnityEngine.Random.Range(0, footsteps.Length)]);
                    stepTimer = 0.45f;
                }
            }
        }
    }

    void ApplyLight()
    {
        if (!sun) return;
        float t = Mathf.InverseLerp(startHour, endHour, D.hour); // 0 = mañana, 1 = noche
        // Color: mañana cálida → mediodía blanco → atardecer naranja → noche azul
        Color c;
        if (t < 0.35f) c = Color.Lerp(new Color(1f, 0.88f, 0.72f), new Color(1f, 0.98f, 0.94f), t / 0.35f);
        else if (t < 0.75f) c = Color.Lerp(new Color(1f, 0.98f, 0.94f), new Color(1f, 0.62f, 0.35f), (t - 0.35f) / 0.4f);
        else c = Color.Lerp(new Color(1f, 0.62f, 0.35f), new Color(0.45f, 0.5f, 0.8f), (t - 0.75f) / 0.25f);
        sun.color = c;
        sun.intensity = t < 0.8f ? 1.25f : Mathf.Lerp(1.25f, 0.45f, (t - 0.8f) / 0.2f);
        float elev = Mathf.Sin(t * Mathf.PI) * 55f + 10f;
        sun.transform.rotation = Quaternion.Euler(elev, Mathf.Lerp(-80f, 80f, t), 0f);
        RenderSettings.ambientLight = c * (t < 0.8f ? 0.55f : 0.35f);
    }

    // ---------- Fundido al dormir ----------
    public void SleepWithFade(Action sleep)
    {
        StartCoroutine(SleepRoutine(sleep));
    }

    IEnumerator SleepRoutine(Action sleep)
    {
        Play(sleepClip, 0.6f);
        yield return FadeTo(1f, 0.9f);
        D.hour = startHour;
        sleep?.Invoke(); // Guarda el nuevo día con la hora de la mañana.
        warnedLate = false;
        ApplyLight();
        yield return new WaitForSeconds(0.4f);
        yield return FadeTo(0f, 0.9f);
    }

    IEnumerator FadeTo(float target, float dur)
    {
        if (!fade) yield break;
        fade.blocksRaycasts = true;
        float start = fade.alpha;
        for (float t = 0; t < 1f; t += Time.deltaTime / dur)
        {
            fade.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }
        fade.alpha = target;
        fade.blocksRaycasts = target > 0.5f;
    }

    // ---------- Sonidos de acciones ----------
    public void Feedback(string kind)
    {
        switch (kind)
        {
            case "comer": Play(eatClip, 0.7f); Hearts(3); break;
            case "agua": Play(drinkClip, 0.6f); Hearts(2); break;
            case "arena": Play(litterClip, 0.6f); break;
            case "mimo": Play(petClip, 0.6f); Hearts(5); break;
            case "compra": Play(buyClip, 0.7f); break;
        }
    }

    void Hearts(int n)
    {
        if (CatMood.Instance) CatMood.Instance.Hearts(n);
    }

    void Play(AudioClip c, float v)
    {
        if (c) sfx.PlayOneShot(c, v);
    }
}
