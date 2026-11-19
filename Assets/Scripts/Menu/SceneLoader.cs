using UnityEngine;
using UnityEngine.SceneManagement;

// Para botones de menú: en el OnClick() del botón, elige SceneLoader > LoadScene y escribe el nombre.
public class SceneLoader : MonoBehaviour
{
    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // En el navegador no se puede cerrar el juego: ocultamos el botón Salir.
        var salir = GameObject.Find("BotonSalir");
        if (salir) salir.SetActive(false);
#endif
    }

    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);

    public void NewGame(string firstScene)
    {
        GameManager.Get().ResetGame();
        SceneManager.LoadScene(firstScene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
