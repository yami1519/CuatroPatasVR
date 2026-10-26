using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CuatroPatasVR.Training
{
    [DefaultExecutionOrder(-50)]
    public sealed class TrainingSession : MonoBehaviour
    {
        [SerializeField] private Transform playerCamera;
        [SerializeField] private TrainingSpawner spawner;
        [SerializeField] private Transform statusPanel;
        [SerializeField] private Transform modeMenu;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField, Min(5f)] private float secondsPerLevel = 30f;
        [SerializeField, Min(0f)] private float countdownSeconds = 3f;
        [SerializeField, Min(1f)] private float freePreparationSeconds = 5f;
        [SerializeField, Range(2.6f, 4f)] private float freeMaximumSpeed = 3.6f;
        [SerializeField, Range(1.8f, 4f)] private float freeObstacleMaximumSpeed = 3.2f;
        [SerializeField, Range(0.35f, 1f)] private float freeMinimumInterval = 0.42f;
        [SerializeField, Range(0.4f, 1.3f)] private float freeMaximumInterval = 0.66f;
        [SerializeField, Range(0.7f, 1.3f)] private float freeMovementClearance = 0.8f;
        [SerializeField, Min(1f)] private float freeWarmupSeconds = 8f;
        [SerializeField, Range(0.15f, 0.6f)] private float duckDistance = 0.3f;
        [SerializeField, Range(0.15f, 0.6f)] private float sideDistance = 0.3f;
        [SerializeField, Min(0f)] private float minimumPunchSpeed = 0.25f;
#if UNITY_EDITOR
        [SerializeField] private bool desktopPreview = true;
        private Vector3 previewStandingLocal;
        private bool previousPreview;
        private bool headsetSessionStarted;
#endif
        private static readonly string[] Names = { "Golpes", "Golpes + agacharse", "Golpes + esquivas", "Combinaciones + velocidad" };
        private float countdown;
        private float elapsed;
        private bool calibrated;
        private bool tracked;
        private bool calibrateAfterCountdown;
        private string feedback = "";
        private float feedbackUntil;
        private InputAction stopFreePlay;
        private InputAction selectMode;
        private float stopHeld;
        private TrainingModeButton focusedButton;

        public int Level { get; private set; } = 1;
        public bool AwaitingModeChoice { get; private set; } = true;
        public bool Finished { get; private set; }
        public bool IsFreePlay { get; private set; }
        public bool CanChooseMode => AwaitingModeChoice || Finished;
        public bool CanMove => !CanChooseMode && calibrated && tracked && countdown <= 0f;
        public bool IsCalibrated => calibrated;
        public float FreePlaySeconds => IsFreePlay ? elapsed : 0f;
        public float ActiveSeconds { get; private set; }
        public float PreparationRemaining => countdown;
        public float MinimumPunchSpeed => minimumPunchSpeed;
        private float FreeWarmup => Mathf.Clamp01(elapsed / Mathf.Max(1f, freeWarmupSeconds));
        public float LevelProgress => IsFreePlay ? 1f : Mathf.Clamp01(elapsed / secondsPerLevel);
        public float SecondsRemaining => IsFreePlay ? float.PositiveInfinity : Mathf.Max(0f, secondsPerLevel - elapsed);
        public float TargetSpeed => IsFreePlay ? Mathf.Lerp(2.6f, Mathf.Clamp(freeMaximumSpeed, 2.6f, 4f), FreeWarmup) : Level == 4 ? Mathf.Lerp(1.2f, 1.6f, LevelProgress) : 0.7f + Level * 0.1f;
        public float FreeMinimumInterval => Mathf.Lerp(0.75f, Mathf.Clamp(freeMinimumInterval, 0.35f, 1f), FreeWarmup);
        public float FreeMaximumInterval => Mathf.Lerp(1f, Mathf.Max(FreeMinimumInterval, freeMaximumInterval), FreeWarmup);
        public float FreeSpawnInterval => (FreeMinimumInterval + FreeMaximumInterval) * 0.5f;
        public float FreeMovementClearance => Mathf.Clamp(freeMovementClearance, 0.7f, 1.3f);
        public float ObstacleSpeed => Mathf.Min(TargetSpeed, IsFreePlay ? Mathf.Clamp(freeObstacleMaximumSpeed, 1.8f, 4f) : 1.2f);
        public Vector3 StandingHead { get; private set; }
        public Vector3 Forward { get; private set; } = Vector3.forward;
        public Vector3 Right => Vector3.Cross(Vector3.up, Forward);
        public float HeadDrop => StandingHead.y - playerCamera.position.y;
        public float LateralOffset => Vector3.Dot(playerCamera.position - StandingHead, Right);
        public int Hits { get; private set; }
        public int Dodges { get; private set; }
        public int Missed { get; private set; }
        public int MissedTargets { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public bool HasResolvedTargets => Hits + MissedTargets > 0;
        public float Accuracy => HasResolvedTargets ? 100f * Hits / (Hits + MissedTargets) : 0f;
        public string CurrentInstruction { get; private set; } = "Elige como quieres entrenar";
        public bool UsingDesktopPreview
        {
            get
            {
#if UNITY_EDITOR
                return desktopPreview && !headsetSessionStarted && !XRSettings.isDeviceActive && !HeadTracked();
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            stopFreePlay = new InputAction("Terminar modo libre", InputActionType.Button);
            stopFreePlay.AddBinding("<XRController>{RightHand}/secondaryButton");
            stopFreePlay.AddBinding("<XRController>{LeftHand}/secondaryButton");
            selectMode = new InputAction("Elegir entrenamiento", InputActionType.Button);
            selectMode.AddBinding("<XRController>/triggerPressed");
            selectMode.AddBinding("<XRController>/trigger");
#if UNITY_EDITOR
            previewStandingLocal = playerCamera.localPosition;
#endif
        }
        private void OnEnable() { stopFreePlay.Enable(); selectMode.Enable(); }
        private void OnDisable() { stopFreePlay.Disable(); selectMode.Disable(); stopHeld = 0f; ClearFocus(); }
        private void OnDestroy() { stopFreePlay.Dispose(); selectMode.Dispose(); }
        private bool HeadTracked()
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool value) && value;
        }
        private void Update()
        {
#if UNITY_EDITOR
            if (HeadTracked()) headsetSessionStarted = true;
            bool preview = UsingDesktopPreview;
            var pose = playerCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            if (pose != null && pose.enabled == preview) pose.enabled = !preview;
            if (preview != previousPreview && calibrated)
            {
                calibrated = false;
                spawner.ClearActive();
            }
            previousPreview = preview;
            if (preview) UpdateDesktopPreview();
#endif
            if (IsFreePlay && !CanChooseMode)
            {
                stopHeld = stopFreePlay.IsPressed() ? stopHeld + Time.unscaledDeltaTime : 0f;
                if (stopHeld >= 1f) StopFreePlay();
            }
            bool available = UsingDesktopPreview || HeadTracked();
            if (!available)
            {
                if (tracked)
                {
                    countdown = Mathf.Max(countdown, countdownSeconds);
                    calibrateAfterCountdown = !IsFreePlay;
                    spawner.ClearActive();
                }
                tracked = false;
                RefreshDisplay();
                return;
            }
            tracked = true;
            if (!calibrated) { CalibrateStanding(); calibrated = true; }
            if (CanChooseMode) UpdateModeSelection();
            else if (countdown > 0f)
            {
                countdown = Mathf.Max(0f, countdown - Time.deltaTime);
                if (countdown <= 0f && calibrateAfterCountdown) { CalibrateStanding(); calibrateAfterCountdown = false; }
            }
            else
            {
                elapsed += Time.deltaTime;
                ActiveSeconds += Time.deltaTime;
                if (!IsFreePlay && elapsed >= secondsPerLevel)
                {
                    if (Level < 4) BeginLevel(Level + 1);
                    else { Finished = true; spawner.ClearActive(); }
                }
            }
            RefreshDisplay();
        }
        private void UpdateModeSelection()
        {
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            bool activate = selectMode.WasPressedThisFrame();
#if UNITY_EDITOR
            if (UsingDesktopPreview && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                ray = playerCamera.GetComponent<Camera>().ScreenPointToRay(Mouse.current.position.ReadValue());
                activate = true;
            }
#endif
            TrainingModeButton button = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Collide))
                button = hit.collider.GetComponentInParent<TrainingModeButton>();
            if (button != focusedButton)
            {
                ClearFocus();
                focusedButton = button;
                if (focusedButton != null) focusedButton.SetFocused(true);
            }
            if (activate && focusedButton != null) focusedButton.Select();
        }
        private void ClearFocus()
        {
            if (focusedButton != null) focusedButton.SetFocused(false);
            focusedButton = null;
        }
        private void ResetResults()
        {
            Hits = Dodges = Missed = MissedTargets = Streak = BestStreak = 0;
            ActiveSeconds = 0f;
        }
        public void ShowModeSelection()
        {
            AwaitingModeChoice = true;
            Finished = false;
            IsFreePlay = false;
            calibrated = false;
            elapsed = countdown = stopHeld = 0f;
            spawner.ClearActive();
            RefreshDisplay();
        }
        public void BeginPractice()
        {
            ResetResults();
            calibrated = false;
            BeginLevel(1);
        }
        public void Restart() => BeginPractice();
        public void BeginLevel(int level)
        {
            if (level < 1 || level > 4) return;
            if (!calibrated) { CalibrateStanding(); calibrated = true; }
            AwaitingModeChoice = false;
            Level = level;
            IsFreePlay = false;
            Finished = false;
            elapsed = stopHeld = 0f;
            countdown = countdownSeconds;
            calibrateAfterCountdown = true;
            feedback = "";
            CurrentInstruction = "Preparate";
            ClearFocus();
            spawner.ClearActive();
            RefreshDisplay();
        }
        public void BeginFreePlay()
        {
            if (!calibrated) { CalibrateStanding(); calibrated = true; }
            ResetResults();
            AwaitingModeChoice = false;
            IsFreePlay = true;
            Level = 4;
            Finished = false;
            elapsed = stopHeld = 0f;
            countdown = freePreparationSeconds;
            calibrateAfterCountdown = true;
            feedback = "";
            ClearFocus();
            spawner.ClearActive();
            RefreshDisplay();
        }
        public void StopFreePlay()
        {
            if (!IsFreePlay || CanChooseMode) return;
            Finished = true;
            stopHeld = 0f;
            spawner.ClearActive();
            RefreshDisplay();
        }
        private void CalibrateStanding()
        {
            StandingHead = playerCamera.position;
            Forward = Vector3.ProjectOnPlane(playerCamera.forward, Vector3.up).normalized;
            if (Forward.sqrMagnitude < 0.01f) Forward = Vector3.forward;
            if (statusPanel != null)
            {
                statusPanel.position = StandingHead + Forward * 4.8f + Vector3.up * 0.95f;
                statusPanel.rotation = Quaternion.LookRotation(Forward);
            }
            PositionMenu();
        }
        private void PositionMenu()
        {
            if (modeMenu == null) return;
            modeMenu.position = StandingHead + Forward * 3.2f - Vector3.up * (Finished ? 0.65f : 0f);
            modeMenu.rotation = Quaternion.LookRotation(Forward);
        }
        public bool HasDucked() => HeadDrop >= duckDistance;
        public bool HasDodged(int requiredSide) => LateralOffset * requiredSide >= sideDistance;
        public void ShowInstruction(string text) { CurrentInstruction = text; feedbackUntil = 0f; }
        public void Notify(string message) { feedback = message; feedbackUntil = Time.time + 1.2f; }
        private void ContinueStreak() { Streak++; BestStreak = Mathf.Max(BestStreak, Streak); }
        public void RecordHit() { Hits++; ContinueStreak(); Notify("Golpe correcto"); }
        public void RecordMiss() { Missed++; MissedTargets++; Streak = 0; Notify("Objetivo perdido"); }
        public void RecordDodge(bool success)
        {
            if (success) { Dodges++; ContinueStreak(); Notify("Bien esquivado"); }
            else { Missed++; Streak = 0; Notify("Intentalo en el siguiente"); }
        }
        private void RefreshDisplay()
        {
            if (modeMenu != null)
            {
                modeMenu.gameObject.SetActive(CanChooseMode);
                PositionMenu();
            }
            bool visible = !AwaitingModeChoice && (!IsFreePlay || Finished || countdown > 0f || !tracked);
            if (statusPanel != null) statusPanel.gameObject.SetActive(visible);
            if (!visible) return;
            string title = Finished ? (IsFreePlay ? "Modo libre finalizado" : "Practica completada") : countdown > 0f ? "PREPARATE" : $"NIVEL {Level} / 4 - {Names[Level - 1]}";
            string instruction = !tracked ? "Seguimiento pausado" : countdown > 0f ? "Ponte de pie en el centro y mira al frente" : Time.time < feedbackUntil ? feedback : CurrentInstruction;
            string progress = countdown > 0f ? $"Empieza en {Mathf.CeilToInt(countdown)}" : IsFreePlay ? "" : $"{Mathf.CeilToInt(SecondsRemaining)} s";
            if (Finished) { instruction = $"Golpes: {Hits} | Esquivas: {Dodges}"; progress = $"Tiempo activo: {FormatTime(ActiveSeconds)}"; }
            if (titleText != null) titleText.text = title;
            if (instructionText != null) instructionText.text = instruction;
            if (progressText != null) progressText.text = progress;
        }
        public static string FormatTime(float seconds) => $"{Mathf.FloorToInt(seconds / 60f):00}:{Mathf.FloorToInt(seconds % 60f):00}";
#if UNITY_EDITOR
        private void UpdateDesktopPreview()
        {
            var k = Keyboard.current;
            if (k == null) return;
            float side = (k.dKey.isPressed ? 0.42f : 0f) - (k.aKey.isPressed ? 0.42f : 0f);
            playerCamera.localPosition = previewStandingLocal + Vector3.right * side + Vector3.down * (k.spaceKey.isPressed ? 0.45f : 0f);
            if (k.pKey.wasPressedThisFrame || k.rKey.wasPressedThisFrame) { playerCamera.localPosition = previewStandingLocal; BeginPractice(); }
            if (k.fKey.wasPressedThisFrame) BeginFreePlay();
            if (k.escapeKey.wasPressedThisFrame) { if (IsFreePlay && !CanChooseMode) StopFreePlay(); else ShowModeSelection(); }
            if (!AwaitingModeChoice)
            {
                if (k.digit1Key.wasPressedThisFrame) BeginLevel(1);
                if (k.digit2Key.wasPressedThisFrame) BeginLevel(2);
                if (k.digit3Key.wasPressedThisFrame) BeginLevel(3);
                if (k.digit4Key.wasPressedThisFrame) BeginLevel(4);
            }
            if (k.jKey.wasPressedThisFrame) spawner.PreviewPunch(true);
            if (k.kKey.wasPressedThisFrame) spawner.PreviewPunch(false);
        }
        private void OnGUI()
        {
            if (!UsingDesktopPreview) return;
            if (CanChooseMode)
            {
                GUI.Box(new Rect(16, 16, 430, 125), "Elige tu entrenamiento");
                if (GUI.Button(new Rect(30, 48, 190, 40), "Practica (P)")) BeginPractice();
                if (GUI.Button(new Rect(234, 48, 190, 40), "Modo libre (F)")) BeginFreePlay();
                GUI.Label(new Rect(30, 98, 390, 24), "El modo libre empieza tras una cuenta regresiva.");
                return;
            }
            if (IsFreePlay) return;
            GUI.Box(new Rect(16, 16, 460, 130), "Prueba sin visor");
            GUI.Label(new Rect(30, 46, 430, 24), "J/K: golpes | Espacio: agacharse | A/D: esquivar");
            GUI.Label(new Rect(30, 70, 430, 24), "1-4: niveles | F: libre | Escape: elegir modo");
            for (int i = 1; i <= 4; i++)
                if (GUI.Button(new Rect(30 + (i - 1) * 108, 104, 98, 28), "Nivel " + i)) BeginLevel(i);
        }
#endif
    }
}
