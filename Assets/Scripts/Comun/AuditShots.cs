using System.Collections;
using System.IO;
using UnityEngine;

// Auditoria visual: en Play toma capturas desde la perspectiva del jugador
// empezando por la casa. Las guarda en <proyecto>/_audit/ y sale de Play solo.
public class AuditShots : MonoBehaviour
{
    struct V
    {
        public string n; public Vector3 p, l;
        public V(string n, float px, float py, float pz, float lx, float ly, float lz)
        { this.n = n; p = new Vector3(px, py, pz); l = new Vector3(lx, ly, lz); }
    }

    IEnumerator Start()
    {
        string dir = Path.Combine(Application.dataPath, "..", "_audit");
        Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);

        yield return new WaitForSecondsRealtime(4f); // navmesh + asentamiento (inmune a timeScale)

        var views = new[]
        {
            new V("01_frente_casa",      0f,   1.7f, -15f,  0f,   2.5f, -8f),
            new V("02_porton_patio",     0.5f, 1.6f, -12f,  0f,   1.5f, -8f),
            new V("03_sala_michi",       0f,   1.6f,  1.5f, 1.6f, 0.5f, 4.6f),
            new V("04_interior_casa",    5f,   1.6f,  4f,  -3f,   1f,  -2f),
            new V("05_casa_rescatada",   26f,  3f,  -14f,  26f,  3f,  2f),
            new V("06_calle_principal", -40f,  2.5f, -16f, 40f,  2f,  -16f),
            new V("07_edificios_nuevos", 20f,  3f,    2f,  20f,  5f,  20f),
            new V("08_aerea_pueblo",     12f, 70f,  -30f,  12f,  0f,  -70f),
            new V("09_tienda",           16f,  1.7f, -31f, 16f,  2.5f, -22.5f),
        };
        foreach (var v in views)
        {
            Capturar(dir, v.n, v.p, v.l);
            Debug.Log("TIENDA_VR: captura " + v.n);
            yield return null;
        }

        // segunda captura de Michi 5s despues: si se movio, CatWander funciona
        yield return new WaitForSecondsRealtime(5f);
        Capturar(dir, "10_sala_michi_2", new Vector3(0f, 1.6f, 1.5f), new Vector3(1.6f, 0.5f, 4.6f));

        Debug.Log("TIENDA_VR: auditoria lista, 10 capturas en " + dir);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#endif
        Destroy(gameObject);
    }

    void Capturar(string dir, string nombre, Vector3 pos, Vector3 mira)
    {
        var go = new GameObject("AuditCam");
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 600f;
        cam.transform.position = pos;
        cam.transform.LookAt(mira);
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(dir, nombre + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Destroy(tex);
        Destroy(go);
    }
}
