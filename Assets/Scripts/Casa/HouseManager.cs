using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

// Controla el Modo Casa: barras del gato, interacciones, tienda, dormir (fin del día),
// accidentes por falta de prevención y game over.
public class HouseManager : MonoBehaviour
{
    public static HouseManager Instance { get; private set; }

    [Header("Desgaste por segundo (0-100)")]
    public float hungerRate = 0.16f;
    public float thirstRate = 0.20f;
    public float cleanRate = 0.10f;
    public float moodRate = 0.12f;

    [Header("Precios de la tienda")]
    public int priceFood = 15;
    public int priceSand = 10;
    public int priceToy = 20;
    public int priceVet = 40;
    public int priceNet = 120;
    public int priceLock = 50;
    public int priceBed = 200;

    [Header("Barras (relleno)")]
    public RectTransform hungerFill, thirstFill, cleanFill, moodFill;

    [Header("Textos")]
    public TMP_Text infoText;
    public TMP_Text promptText;
    public GameObject messagePanel;
    public TMP_Text messageText;

    [Header("Paneles")]
    public GameObject shopPanel;
    public TMP_Text shopInfoText;
    public GameObject summaryPanel;
    public TMP_Text summaryText;
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;

    [Header("Objetos que aparecen al comprar")]
    public GameObject netVisual;
    public GameObject catBedVisual;
    public GameObject doorLockVisual;

    [Header("Escenas")]
    public string runSceneName = "Correr";
    public string menuSceneName = "Menu";

    public bool IsUIOpen => D.IsGameOver ||
        (shopPanel && shopPanel.activeSelf) ||
        (summaryPanel && summaryPanel.activeSelf) ||
        (gameOverPanel && gameOverPanel.activeSelf);

    GameData D => GameManager.Get().data;

    readonly Queue<string> messages = new Queue<string>();
    float messageTimer;
    float petCooldown;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        var gm = GameManager.Get();
        foreach (var p in new[] { shopPanel, summaryPanel, gameOverPanel, messagePanel })
            if (p) p.SetActive(false);

        if (D.IsGameOver)
        {
            gm.cameFromRun = false;
            RefreshVisuals();
            UpdateUI();
            GameOver();
            return;
        }

        if (gm.cameFromRun)
        {
            gm.cameFromRun = false;
            ReturnFromRun();
        }

        RefreshVisuals();
        UpdateUI();
    }

    void Update()
    {
        if (!IsUIOpen) Decay(Time.deltaTime);
        petCooldown -= Time.deltaTime;

        // Mensajes en cola
        if (messagePanel)
        {
            if (messagePanel.activeSelf)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f) messagePanel.SetActive(false);
            }
            if (!messagePanel.activeSelf && messages.Count > 0)
            {
                if (messageText) messageText.text = messages.Dequeue();
                messagePanel.SetActive(true);
                messageTimer = 4f;
            }
        }

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && shopPanel && shopPanel.activeSelf) CloseShop();

        UpdateUI();
    }

    // ---------------- Barras ----------------
    void Decay(float dt)
    {
        float sick = D.catIsSick ? 1.5f : 1f;
        D.hunger = Mathf.Max(0f, D.hunger - hungerRate * dt * sick);
        D.thirst = Mathf.Max(0f, D.thirst - thirstRate * dt * sick);
        D.cleanliness = Mathf.Max(0f, D.cleanliness - cleanRate * dt);

        float moodMul = (D.hasBed ? 0.7f : 1f) * sick;
        if (D.hunger < 20f || D.thirst < 20f || D.cleanliness < 20f) moodMul *= 2f; // necesidades bajas lo ponen triste
        D.mood = Mathf.Max(0f, D.mood - moodRate * dt * moodMul);
    }

    // ---------------- Interacciones ----------------
    public void Interact(Interactable it)
    {
        if (IsUIOpen) return;

        switch (it.type)
        {
            case InteractionType.Alimentar:
                if (D.hunger >= 95f) { Say("No tiene hambre ahora."); break; }
                if (D.foodStock <= 0) { Say("¡No queda comida! Cómprala en la tienda (computadora)."); break; }
                D.foodStock--;
                D.hunger = Mathf.Min(100f, D.hunger + 45f);
                Say("Le serviste comida. ¡Ñam!");
                FX("comer");
                break;

            case InteractionType.DarAgua:
                D.thirst = 100f;
                Say("Agua fresca servida.");
                FX("agua");
                break;

            case InteractionType.LimpiarArena:
                if (D.cleanliness >= 95f) { Say("El arenero está limpio."); break; }
                if (D.sandStock <= 0) { Say("¡No queda arena! Cómprala en la tienda."); break; }
                D.sandStock--;
                D.cleanliness = 100f;
                Say("Arenero limpio y con arena nueva.");
                FX("arena");
                break;

            case InteractionType.Acariciar:
                if (petCooldown > 0f) { Say("Tu gato quiere un respiro."); break; }
                petCooldown = 5f;
                D.mood = Mathf.Min(100f, D.mood + 20f);
                Say(D.catIsSick ? "Tu gato ronronea débil... necesita veterinario." : "Purrr... ¡le encanta!");
                FX("mimo");
                break;

            case InteractionType.Tienda:
                OpenShop();
                break;

            case InteractionType.SalirACorrer:
                GoRun();
                break;

            case InteractionType.Dormir:
                if (HouseFX.Instance) HouseFX.Instance.SleepWithFade(Sleep);
                else Sleep();
                break;
        }

        GameManager.Get().Save();
        UpdateUI();
    }

    // Acariciar manteniendo el clic (primera persona)
    float purrTimer;
    public void PetTick(float dt)
    {
        if (IsUIOpen) return;
        D.mood = Mathf.Min(100f, D.mood + 12f * dt);
        purrTimer -= dt;
        if (purrTimer <= 0f)
        {
            purrTimer = 3f;
            FX("mimo");
            Say(D.catIsSick ? "Ronronea débil... necesita veterinario." : "Purrr... ¡le encanta!");
        }
    }

    void GoRun()
    {
        if (!D.CanRun)
        {
            Say("Ya es de noche. Cuida a tu gato y ve a dormir antes de salir otra vez.");
            return;
        }
        GameManager.Get().Save();
        SceneManager.LoadScene(runSceneName);
    }

    void ReturnFromRun()
    {
        // El desgaste y el reloj ya se aplicaron al iniciar cada intento.
        AddMessage("Volviste a casa. Cada carrera consumió tiempo y necesidades de tu gato.");

        // Riesgo de fuga si no hay seguro de puertas
        if (!D.hasDoorLock)
        {
            float chance = 0.15f + (D.mood < 40f ? 0.2f : 0f);
            if (Random.value < chance)
            {
                int cost = Mathf.Min(D.coins, 25);
                D.coins -= cost;
                D.mood = Mathf.Max(0f, D.mood - 25f);
                AddMessage($"¡Tu gato se escapó por la puerta! Lo encontraste, pero gastaste {cost} monedas buscándolo. (El seguro de puertas lo evita)");
            }
        }
        GameManager.Get().Save();
    }

    // ---------------- Fin del día ----------------
    void Sleep()
    {
        var events = new List<string>();
        int finishedDay = D.day;

        // La noche
        D.hunger = Mathf.Max(0f, D.hunger - 25f);
        D.thirst = Mathf.Max(0f, D.thirst - 25f);
        D.cleanliness = Mathf.Max(0f, D.cleanliness - 15f);
        D.mood = Mathf.Clamp(D.mood + (D.hasBed ? 10f : -10f), 0f, 100f);
        if (D.hasBed) events.Add("Tu gato durmió cómodo en su cama (+ánimo).");

        // Enfermedad por descuido
        if (!D.catIsSick && (D.hunger <= 0f || D.thirst <= 0f || D.cleanliness <= 0f || D.mood <= 10f))
        {
            D.catIsSick = true;
            events.Add("Tu gato se enfermó por descuido.");
        }

        // Caída desde la terraza si no hay malla
        if (!D.hasNet)
        {
            float chance = 0.12f + (D.mood < 40f ? 0.2f : 0f);
            if (Random.value < chance)
            {
                D.catIsSick = true;
                D.mood = Mathf.Max(0f, D.mood - 30f);
                events.Add("¡Accidente! Tu gato se cayó de la terraza porque no había malla. Está herido.");
            }
        }

        if (D.catIsSick)
        {
            D.sickDays++;
            events.Add($"Tu gato está enfermo ({D.sickDays}/3 días). Llévalo al veterinario desde la tienda.");
        }

        if (D.sickDays >= 3)
        {
            GameManager.Get().Save();
            GameOver();
            return;
        }

        D.day++;
        GameManager.Get().Save();

        if (summaryText)
        {
            string ev = events.Count > 0 ? "- " + string.Join("\n- ", events) : "Todo tranquilo. ¡Buen trabajo cuidando a tu gato!";
            summaryText.text =
                $"<b>Fin del día {finishedDay}</b>\n\n" +
                $"Hambre {Mathf.RoundToInt(D.hunger)}%   Sed {Mathf.RoundToInt(D.thirst)}%   " +
                $"Limpieza {Mathf.RoundToInt(D.cleanliness)}%   Ánimo {Mathf.RoundToInt(D.mood)}%\n" +
                $"Monedas: {D.coins}   Récord corriendo: {D.bestDistance} m\n\n{ev}";
        }
        if (summaryPanel) summaryPanel.SetActive(true);
    }

    public void CloseSummary()
    {
        if (summaryPanel) summaryPanel.SetActive(false);
        Say($"Día {D.day}. ¡A cuidar a tu gato!");
    }

    void GameOver()
    {
        if (gameOverText)
            gameOverText.text =
                "<b>Tu gato enfermó gravemente</b>\n\n" +
                $"Lo cuidaste {D.day} días.\n" +
                "Prevenir cuesta menos que reparar:\nmalla, seguro de puertas y sus necesidades básicas.";
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    public void Restart()
    {
        GameManager.Get().ResetGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoMenu() => SceneManager.LoadScene(menuSceneName);

    // ---------------- Tienda ----------------
    public void OpenShop()
    {
        if (D.IsGameOver) return;
        if (shopPanel) shopPanel.SetActive(true);
        if (shopInfoText) shopInfoText.text = $"Tienes {D.coins} monedas.";
    }

    public void CloseShop()
    {
        if (shopPanel) shopPanel.SetActive(false);
    }

    public void Buy(string id)
    {
        if (D.IsGameOver) return;
        var gm = GameManager.Get();
        int coinsBefore = D.coins;
        string msg;
        const string noMoney = "No te alcanzan las monedas. ¡Sal a correr para ganar más!";

        switch (id)
        {
            case "comida":
                if (gm.SpendCoins(priceFood)) { D.foodStock += 3; msg = "+3 porciones de comida."; } else msg = noMoney;
                break;
            case "arena":
                if (gm.SpendCoins(priceSand)) { D.sandStock += 3; msg = "+3 cambios de arena."; } else msg = noMoney;
                break;
            case "juguete":
                if (gm.SpendCoins(priceToy)) { D.mood = Mathf.Min(100f, D.mood + 40f); msg = "¡Juguete nuevo! Tu gato está feliz."; } else msg = noMoney;
                break;
            case "veterinario":
                if (!D.catIsSick) { msg = "Tu gato está sano, no necesita veterinario."; break; }
                if (gm.SpendCoins(priceVet)) { D.catIsSick = false; D.sickDays = 0; D.mood = Mathf.Min(100f, D.mood + 20f); msg = "El veterinario curó a tu gato."; } else msg = noMoney;
                break;
            case "malla":
                if (D.hasNet) { msg = "Ya instalaste la malla."; break; }
                if (gm.SpendCoins(priceNet)) { D.hasNet = true; msg = "Malla instalada en la terraza: no habrá caídas."; } else msg = noMoney;
                break;
            case "seguro":
                if (D.hasDoorLock) { msg = "Ya tienes seguro de puertas."; break; }
                if (gm.SpendCoins(priceLock)) { D.hasDoorLock = true; msg = "Seguro instalado: tu gato no podrá escaparse."; } else msg = noMoney;
                break;
            case "cama":
                if (D.hasBed) { msg = "Tu gato ya tiene cama."; break; }
                if (gm.SpendCoins(priceBed)) { D.hasBed = true; msg = "Cama comprada: su ánimo bajará más lento."; } else msg = noMoney;
                break;
            default:
                msg = "Producto desconocido.";
                break;
        }

        gm.Save();
        if (D.coins < coinsBefore) FX("compra");
        if (shopInfoText) shopInfoText.text = $"{msg}\nTienes {D.coins} monedas.";
        RefreshVisuals();
        UpdateUI();
    }

    // ---------------- UI ----------------
    public void SetPrompt(string text)
    {
        if (promptText) promptText.text = text;
    }

    void Say(string m)
    {
        messages.Clear();
        if (messagePanel && messageText)
        {
            messageText.text = m;
            messagePanel.SetActive(true);
            messageTimer = 2.5f;
        }
    }

    void AddMessage(string m) => messages.Enqueue(m);

    // Mensaje desde otros scripts (por ejemplo, "ya es tarde")
    public void Notify(string m) => Say(m);

    static void FX(string kind)
    {
        if (HouseFX.Instance) HouseFX.Instance.Feedback(kind);
    }

    void RefreshVisuals()
    {
        if (netVisual) netVisual.SetActive(D.hasNet);
        if (catBedVisual) catBedVisual.SetActive(D.hasBed);
        if (doorLockVisual) doorLockVisual.SetActive(D.hasDoorLock);
    }

    void UpdateUI()
    {
        SetBar(hungerFill, D.hunger);
        SetBar(thirstFill, D.thirst);
        SetBar(cleanFill, D.cleanliness);
        SetBar(moodFill, D.mood);

        if (infoText)
            infoText.text =
                $"Día {D.day}    Monedas: {D.coins}\n" +
                $"Comida: {D.foodStock}    Arena: {D.sandStock}" +
                (D.catIsSick ? "\n<color=#F4A5A5>¡Gato enfermo!</color>" : "");
    }

    static void SetBar(RectTransform fill, float value)
    {
        if (!fill) return;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
    }
}
