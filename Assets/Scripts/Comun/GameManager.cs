using System;
using UnityEngine;

public enum PlayerRole
{
    Caregiver,
    CatPlayer
}

// Datos que se comparten entre escenas y se guardan.
[Serializable]
public class GameData
{
    public int coins = 0;

    // Rol actual y gato seleccionado (0 = amarillo, 1 = negro, 2 = ojitos)
    public PlayerRole role = PlayerRole.Caregiver;
    public int selectedCatIndex = 0;

    // Barras del gato (0 a 100). 100 = perfecto.
    public float hunger = 100f;
    public float thirst = 100f;
    public float cleanliness = 100f;
    public float mood = 100f;

    public int day = 1;
    public float hour = 7f;
    public int foodStock = 3;
    public int sandStock = 3;

    // Mejoras permanentes (tienda)
    public bool hasNet = false;
    public bool hasDoorLock = false;
    public bool hasBed = false;

    public bool catIsSick = false;
    public int sickDays = 0;
    public int bestDistance = 0;

    public bool IsGameOver => sickDays >= 3;
    public bool CanRun => !IsGameOver && hour < 22f;

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

// Singleton que vive en todas las escenas.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameData data = new GameData();

    [NonSerialized] public bool cameFromRun;

    const string SaveKey = "CuatroPatas_Save";

    public static GameManager Get()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
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

    public void SetRoleAndCat(PlayerRole newRole, int catIndex)
    {
        data.role = newRole;
        data.selectedCatIndex = Mathf.Clamp(catIndex, 0, 2);
        Save();
    }

    public void AddCoins(int amount)
    {
        data.coins += amount;
        Save();
    }

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