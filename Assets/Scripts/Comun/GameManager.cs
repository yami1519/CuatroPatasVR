using System;
using UnityEngine;

// Datos que se comparten entre escenas (Casa, Correr, Tienda) y se guardan.
[Serializable]
public class GameData
{
    public int coins = 0;

    // Barras del gato (0 a 100). 100 = perfecto.
    public float hunger = 100f;      // Hambre
    public float thirst = 100f;      // Sed
    public float cleanliness = 100f; // Limpieza
    public float mood = 100f;        // Ánimo

    public int day = 1;
    public float hour = 7f;          // hora del día en la casa (7:00 a 22:00)
    public int foodStock = 3;        // Porciones de comida
    public int sandStock = 3;        // Cambios de arena

    // Mejoras permanentes (tienda)
    public bool hasNet = false;      // Malla en terraza
    public bool hasDoorLock = false; // Seguro de puertas
    public bool hasBed = false;      // Cama

    public bool catIsSick = false;
    public int sickDays = 0;         // Días seguidos enfermo (3 = game over)
    public int bestDistance = 0;     // Récord del modo correr

    public bool IsGameOver => sickDays >= 3;
    public bool CanRun => !IsGameOver && hour < 22f;

    // Cada intento consume tiempo y recursos, incluso si se cierra el juego.
    public bool TryBeginRun()
    {
        if (!CanRun) return false;
        hunger = Mathf.Max(0f, hunger - 20f);
        thirst = Mathf.Max(0f, thirst - 20f);
        cleanliness = Mathf.Max(0f, cleanliness - 15f);
        mood = Mathf.Max(0f, mood - 15f);
        hour = Mathf.Min(22f, hour + 3f);
        return true;
    }
}

// Singleton que vive en todas las escenas. NO necesitas ponerlo en ninguna escena:
// se crea solo la primera vez que alguien llama a GameManager.Get().
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameData data = new GameData();

    // Indica el regreso a casa para mostrar el mensaje y resolver el riesgo de fuga.
    [NonSerialized] public bool cameFromRun;

    const string SaveKey = "CuatroPatas_Save";

    public static GameManager Get()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>(); // Awake se ejecuta aquí mismo
        }
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        AudioListener.volume = PlayerPrefs.GetFloat("CuatroPatas_Volumen", 0.8f);
    }

    public void AddCoins(int amount)
    {
        data.coins += amount;
        Save();
    }

    // Devuelve true si alcanzó el dinero y se cobró.
    public bool SpendCoins(int amount)
    {
        if (data.coins < amount) return false;
        data.coins -= amount;
        Save();
        return true;
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (PlayerPrefs.HasKey(SaveKey))
            data = JsonUtility.FromJson<GameData>(PlayerPrefs.GetString(SaveKey));
    }

    public void ResetGame()
    {
        data = new GameData();
        cameFromRun = false;
        Save();
    }

    void OnApplicationQuit() => Save();
}
