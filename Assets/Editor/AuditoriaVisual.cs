using UnityEngine;
using UnityEditor;

// Entra a Play con el AuditShots temporal y lo limpia al salir.
public static class AuditoriaVisual
{
    [MenuItem("Cuatro Patas/Tienda/17 - Auditoria visual (play)")]
    public static void Auditar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("TIENDA_VR: ya estas en Play, sal primero.");
            return;
        }
        EditorApplication.isPaused = false;
        SessionState.SetBool("cp_audit", true); // sobrevive a la recarga de dominio
        if (GameObject.Find("AuditShots") == null)
            new GameObject("AuditShots").AddComponent<AuditShots>();
        EditorApplication.EnterPlaymode();
        Debug.Log("TIENDA_VR: entrando a Play para la auditoria visual...");
    }
}

// Limpieza y anti-pausa que sobreviven a la recarga de dominio al entrar a Play.
[InitializeOnLoad]
public static class AuditCleanup
{
    static AuditCleanup()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("cp_audit", false);
                var go = GameObject.Find("AuditShots");
                if (go) Object.DestroyImmediate(go);
            }
        };
        EditorApplication.update += () =>
        {
            // Si algo pausa el editor durante la auditoria (p.ej. Error Pause), despausar.
            if (SessionState.GetBool("cp_audit", false) && EditorApplication.isPlaying && EditorApplication.isPaused)
            {
                Debug.Log("TIENDA_VR: auditoria: el editor estaba pausado, despausando.");
                EditorApplication.isPaused = false;
            }
        };
    }
}
