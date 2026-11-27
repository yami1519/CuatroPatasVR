using UnityEngine;

public class CicloDiaNoche : MonoBehaviour
{
    [Header("Sol (Directional Light)")]
    public Light sol;
    [Tooltip("Segundos que dura un día completo (ej. 60 = 1 minuto)")]
    public float duracionDiaSegundos = 60f;

    [Header("Cielo y ambiente")]
    public Material cieloMaterial;
    public Color colorCieloDia = Color.white;
    public Color colorCieloNoche = new Color(0.05f, 0.08f, 0.2f);
    [Range(0f, 1.5f)] public float intensidadAmbienteDia = 1f;
    [Range(0f, 1.5f)] public float intensidadAmbienteNoche = 0.15f;

    [Header("Faroles")]
    public Color colorEmisionNoche = new Color(1f, 0.8f, 0.35f) * 2.5f;

    private Light[] lucesFaroles;
    private Material instanciaVidrioFarol;
    private bool esDeNocheActual;

    void Start()
    {
        if (sol == null)
            sol = GetComponent<Light>();

        if (cieloMaterial == null)
            cieloMaterial = RenderSettings.skybox;

        // Toma una copia segura del material Glass del primer farol que encuentre
        GameObject farol = GameObject.Find("quaternius_cc0-streetlight-1394");
        if (farol != null)
        {
            MeshRenderer mr = farol.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                foreach (Material mat in mr.sharedMaterials)
                {
                    if (mat != null && mat.name.ToLower().Contains("glass"))
                    {
                        instanciaVidrioFarol = mat;
                        break;
                    }
                }
            }
        }

        // Busca todas las luces etiquetadas como "LuzFarol"
        GameObject[] objetosLuz = GameObject.FindGameObjectsWithTag("LuzFarol");
        lucesFaroles = new Light[objetosLuz.Length];
        for (int i = 0; i < objetosLuz.Length; i++)
        {
            lucesFaroles[i] = objetosLuz[i].GetComponent<Light>();
        }

        ActualizarEstadoNoche(true);
    }

    void Update()
    {
        if (sol == null) return;

        float velocidad = 360f / duracionDiaSegundos;
        sol.transform.Rotate(Vector3.right * velocidad * Time.deltaTime);

        bool esDeNoche = sol.transform.forward.y > 0.05f;
        sol.intensity = esDeNoche ? 0.05f : 1.2f;

        if (esDeNoche != esDeNocheActual)
        {
            ActualizarEstadoNoche(false);
        }
    }

    void ActualizarEstadoNoche(bool forzar)
    {
        bool esDeNoche = sol.transform.forward.y > 0.05f;
        if (!forzar && esDeNoche == esDeNocheActual) return;

        esDeNocheActual = esDeNoche;

        // Oscurece o aclara la luz ambiental y el material del cielo
        RenderSettings.ambientIntensity = esDeNoche ? intensidadAmbienteNoche : intensidadAmbienteDia;

        if (cieloMaterial != null)
        {
            if (cieloMaterial.HasProperty("_Tint"))
                cieloMaterial.SetColor("_Tint", esDeNoche ? colorCieloNoche : colorCieloDia);
            else if (cieloMaterial.HasProperty("_SkyTint"))
                cieloMaterial.SetColor("_SkyTint", esDeNoche ? colorCieloNoche : colorCieloDia);
        }

        // Enciende o apaga las luces de los faroles
        foreach (Light luz in lucesFaroles)
        {
            if (luz != null)
                luz.enabled = esDeNoche;
        }

        // Brillo nocturno solo en el material Glass del farol (si el shader soporta emisión)
        if (instanciaVidrioFarol != null && instanciaVidrioFarol.HasProperty("_EmissionColor"))
        {
            if (esDeNoche)
            {
                instanciaVidrioFarol.EnableKeyword("_EMISSION");
                instanciaVidrioFarol.SetColor("_EmissionColor", colorEmisionNoche);
            }
            else
            {
                instanciaVidrioFarol.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}