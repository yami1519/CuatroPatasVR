using UnityEngine;

// Sonidos, música, destellos al agarrar monedas y "salto" del contador.
public class RunnerFX : MonoBehaviour
{
    public static RunnerFX Instance { get; private set; }

    [Header("Sonidos")]
    public AudioClip coinClip;
    public AudioClip jumpClip;
    public AudioClip slideClip;
    public AudioClip crashClip;
    public AudioClip loseClip;
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.35f;

    [Header("Efectos")]
    public Material sparkMaterial;
    public RectTransform coinsCounter;

    AudioSource sfx, musicSrc;
    ParticleSystem sparks;
    float punch;

    void Awake()
    {
        Instance = this;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        if (music != null)
        {
            musicSrc = gameObject.AddComponent<AudioSource>();
            musicSrc.clip = music;
            musicSrc.loop = true;
            musicSrc.volume = musicVolume;
            musicSrc.Play();
        }
        BuildSparks();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void BuildSparks()
    {
        var go = new GameObject("Destellos");
        go.transform.SetParent(transform, false);
        sparks = go.AddComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = sparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.55f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.35f), new Color(1f, 0.7f, 0.2f));
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        var em = sparks.emission;
        em.rateOverTime = 0f;

        var shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var col = sparks.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        if (sparkMaterial) r.sharedMaterial = sparkMaterial;
    }

    void Update()
    {
        if (coinsCounter)
        {
            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 5f);
            float s = 1f + punch * 0.3f;
            coinsCounter.localScale = new Vector3(s, s, 1f);
        }
    }

    public void Coin(Vector3 pos)
    {
        Play(coinClip, 0.55f);
        if (sparks)
        {
            sparks.transform.position = pos;
            sparks.Emit(14);
        }
        punch = 1f;
    }

    public void Jump() => Play(jumpClip, 0.45f);
    public void Slide() => Play(slideClip, 0.6f);

    public void Crash()
    {
        Play(crashClip, 0.9f);
        Play(loseClip, 0.7f);
        if (RunnerCamera.Instance != null) RunnerCamera.Instance.Shake(1f);
        if (musicSrc) musicSrc.volume = musicVolume * 0.3f;
    }

    void Play(AudioClip c, float v)
    {
        if (c != null) sfx.PlayOneShot(c, v);
    }
}
