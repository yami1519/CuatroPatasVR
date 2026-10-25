using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Controla la partida del modo correr: velocidad, monedas, distancia y Game Over.
public class RunnerManager : MonoBehaviour
{
    public static RunnerManager Instance { get; private set; }

    [Header("Velocidad")]
    public float startSpeed = 10f;
    public float maxSpeed = 25f;
    [Tooltip("Cuánto sube la velocidad por segundo")]
    public float acceleration = 0.3f;

    [Header("UI (TextMeshPro)")]
    public TMP_Text coinsText;
    public TMP_Text distanceText;
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;

    [Header("Escenas")]
    public string houseSceneName = "Casa";

    public float CurrentSpeed { get; private set; }
    public bool IsRunning { get; private set; }

    int runCoins;
    float distance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var gm = GameManager.Get();
        if (!gm.data.TryBeginRun())
        {
            SceneManager.LoadScene(houseSceneName);
            return;
        }
        gm.cameFromRun = true;
        gm.Save();
        CurrentSpeed = startSpeed;
        IsRunning = true;
        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateUI();
    }

    void Update()
    {
        if (!IsRunning) return;
        CurrentSpeed = Mathf.Min(maxSpeed, CurrentSpeed + acceleration * Time.deltaTime);
        distance += CurrentSpeed * Time.deltaTime;
        UpdateUI();
    }

    public void CollectCoin(int value)
    {
        runCoins += value;
        UpdateUI();
    }

    public void GameOver()
    {
        if (!IsRunning) return;
        IsRunning = false;

        var gm = GameManager.Get();
        gm.cameFromRun = true;
        gm.AddCoins(runCoins);

        int d = Mathf.FloorToInt(distance);
        bool newRecord = d > gm.data.bestDistance;
        if (newRecord)
        {
            gm.data.bestDistance = d;
            gm.Save();
        }

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (gameOverText)
        {
            gameOverText.text = $"¡Chocaste!\nMonedas ganadas: {runCoins}\nDistancia: {d} m\n" +
                                (newRecord ? "¡Nuevo récord!" : $"Récord: {gm.data.bestDistance} m") +
                                $"\nTotal ahorrado: {gm.data.coins}" +
                                (gm.data.CanRun ? "\nCada intento consume 3 horas y necesidades del gato."
                                                : "\nYa es de noche. Vuelve a casa para cuidar al gato y dormir.");
        }
    }

    // Botones del panel de Game Over
    public void Retry()
    {
        if (IsRunning) return;
        SceneManager.LoadScene(GameManager.Get().data.CanRun
            ? SceneManager.GetActiveScene().name : houseSceneName);
    }
    public void GoHome() => SceneManager.LoadScene(houseSceneName);

    void UpdateUI()
    {
        if (coinsText) coinsText.text = $"Monedas: {runCoins}";
        if (distanceText) distanceText.text = $"{Mathf.FloorToInt(distance)} m";
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
