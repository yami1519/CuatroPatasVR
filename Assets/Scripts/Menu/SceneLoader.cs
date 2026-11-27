using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Nombres de Escenas")]
    [SerializeField] private string casaSceneName = "PuebloVR";
    [SerializeField] private string puebloSceneName = "PuebloVR";
    [SerializeField] private string tiendaSceneName = "TiendaVR";
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Paneles del Menú (Opcional)")]
    [SerializeField] private GameObject panelOpciones;
    [SerializeField] private GameObject panelCreditos;

    // Botón: Jugar como Cuidador
    public void PlayAsCaregiver()
    {
        var gm = GameManager.Get();
        gm.SetRoleAndCat(PlayerRole.Caregiver, 0);
        gm.Save();
        SceneManager.LoadScene(casaSceneName);
    }

    // Botón: Jugar como Gato (por defecto Gato 0)
    public void PlayAsCat()
    {
        var gm = GameManager.Get();
        gm.SetRoleAndCat(PlayerRole.CatPlayer, 0);
        gm.Save();
        SceneManager.LoadScene(puebloSceneName);
    }

    // Por si luego quieres elegir gato específico (0, 1 o 2)
    public void PlayAsCatIndex(int catIndex)
    {
        var gm = GameManager.Get();
        gm.SetRoleAndCat(PlayerRole.CatPlayer, catIndex);
        gm.Save();
        SceneManager.LoadScene(puebloSceneName);
    }

    public void LoadMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
    }

    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    public void OpenOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    public void CloseOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    public void OpenCreditos()
    {
        if (panelCreditos != null) panelCreditos.SetActive(true);
    }

    public void CloseCreditos()
    {
        if (panelCreditos != null) panelCreditos.SetActive(false);
    }

    public void QuitGame()
    {
        GameManager.Get().Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}