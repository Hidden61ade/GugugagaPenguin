using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the title / pause / victory game-flow for the penguin run.
/// All UI elements are wired via the Unity Inspector — no UI is built at runtime.
///
/// Setup:
///   1. Add this component to a GameObject in your scene.
///   2. Wire every [SerializeField] field in the Inspector (canvas groups, texts, buttons, slider).
///   3. Assign each Button.onClick to the matching public method on this component:
///        Start     → StartRun
///        Pause     → EnterPauseState
///        Resume    → ResumeRun
///        Settings  → OpenSettings / CloseSettings
///        Credits   → OpenCredits  / CloseCredits
///        Title     → ReturnToTitle
///   4. Assign Slider.onValueChanged → ApplyVolume (float Dynamic).
/// </summary>
[DefaultExecutionOrder(100)]
public class PenguinGameFlowController : MonoBehaviour
{
    private enum FlowState
    {
        Title,
        Transition,
        Playing,
        Paused,
        Victory
    }

    private const string VolumePrefKey = "Penguin.MasterVolume";
    private const float TitleTransitionDelay = 0.9f;

    // ── Run settings ─────────────────────────────────────────────────────────
    [Header("Run")]
    [SerializeField] private float runDistanceToWin = 70f;
    [SerializeField] private float failYDistance = 16f;
    [SerializeField] private float tutorialVisibleSeconds = 9f;

    [Header("Finish Gate")]
    [SerializeField] private float finishGateHeight = 5.5f;
    [SerializeField] private float finishGateWidth = 9f;

    // ── UI — Panels ──────────────────────────────────────────────────────────
    [Header("UI – Panels")]
    [SerializeField] private GameObject titleGroup;
    [SerializeField] private GameObject hudGroup;
    [SerializeField] private GameObject pauseGroup;
    [SerializeField] private GameObject settingsGroup;
    [SerializeField] private GameObject creditsGroup;
    [SerializeField] private GameObject victoryGroup;

    // ── UI — HUD ─────────────────────────────────────────────────────────────
    [Header("UI – HUD")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Image leftIndicatorImage;
    [SerializeField] private Image rightIndicatorImage;
    [SerializeField] private RectTransform leftIndicatorRect;
    [SerializeField] private RectTransform rightIndicatorRect;

    // ── UI — Title ───────────────────────────────────────────────────────────
    [Header("UI – Title")]
    [SerializeField] private TextMeshProUGUI leapStatusText;

    // ── UI — Victory ─────────────────────────────────────────────────────────
    [Header("UI – Victory")]
    [SerializeField] private TextMeshProUGUI victorySummaryText;

    // ── UI — Settings ────────────────────────────────────────────────────────
    [Header("UI – Settings")]
    [SerializeField] private Slider volumeSlider;

    // ── Audio ────────────────────────────────────────────────────────────────
    [Header("Audio – Sources (auto-created if unassigned)")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Audio – Clips (leave empty for procedural fallback)")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip startClip;
    [SerializeField] private AudioClip pauseClip;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip flapClip;

    // ── Runtime state ────────────────────────────────────────────────────────
    private FlowState currentState = FlowState.Title;

    private FlyingPenguinController penguinController;
    private LeapWingInputController leapInput;
    private KeyboardWingInputController keyboardInput;
    private ThirdPersonCamera cameraRig;
    private Rigidbody penguinBody;
    private Transform penguinTransform;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float spawnY;
    private float runStartUnscaledTime;
    private bool finishTriggered;
    private bool leftSeen;
    private bool rightSeen;
    private float leftFlashUntil;
    private float rightFlashUntil;
    private bool settingsVisible;
    private bool creditsVisible;

    private Coroutine runRoutine;

    // Colors used only for the runtime indicator visual (not UI building)
    private static readonly Color AccentCyan   = new Color(0.15f, 0.84f, 0.98f, 1f);
    private static readonly Color AccentOrange = new Color(1f,    0.56f, 0.16f, 1f);
    private static readonly Color IndicatorIdle     = new Color(0.14f, 0.22f, 0.38f, 0.86f);
    private static readonly Color IndicatorHotLeft  = new Color(0.16f, 0.77f, 1f,    1f);
    private static readonly Color IndicatorHotRight = new Color(1f,    0.58f, 0.19f, 1f);

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        PenguinGameFlowController[] controllers = FindObjectsOfType<PenguinGameFlowController>(true);
        if (controllers.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        if (!ResolveSceneReferences())
        {
            Debug.LogWarning("PenguinGameFlowController could not find the active penguin scene. Disabling.");
            enabled = false;
            return;
        }

        BuildAudio();
        BindEvents();

        ApplyVolume(PlayerPrefs.GetFloat(VolumePrefKey, 0.85f));
        EnterTitleState(true);
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void Update()
    {
        if (penguinController == null)
        {
            return;
        }

        UpdateHandIndicators();
        HandleMenuHotkeys();

        if (currentState == FlowState.Playing)
        {
            UpdatePlayingState();
        }
    }

    // ── Public API — wire to Button.onClick / Slider.onValueChanged ──────────
    public void NotifyFinishReached()
    {
        if (currentState != FlowState.Playing || finishTriggered)
        {
            return;
        }

        EnterVictoryState();
    }

    public void StartRun()
    {
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
        }

        PlayUi(clickClip);
        PlayUi(startClip);

        SetGroupVisible(settingsGroup, false);
        SetGroupVisible(creditsGroup, false);
        SetGroupVisible(victoryGroup, false);
        SetGroupVisible(hudGroup, true);
        SetGroupVisible(titleGroup, false);

        penguinController.ResetRun(spawnPosition, spawnRotation, false);
        penguinController.SetInputEnabled(false);
        cameraRig.SetMode(ThirdPersonCamera.CameraMode.Gameplay);

        currentState = FlowState.Transition;
        finishTriggered = false;
        leftSeen = false;
        rightSeen = false;

        SetPauseButtonActive(false);
        SetHintText("Slide in... then flap left and right to steer");
        SetProgressText("Gliding onto the ice run");

        runRoutine = StartCoroutine(BeginRunRoutine());
    }

    public void EnterPauseState()
    {
        if (currentState != FlowState.Playing)
        {
            return;
        }

        PlayUi(pauseClip);
        currentState = FlowState.Paused;
        Time.timeScale = 0f;
        penguinController.SetInputEnabled(false);
        SetGroupVisible(pauseGroup, true);
    }

    public void ResumeRun()
    {
        if (currentState != FlowState.Paused)
        {
            return;
        }

        PlayUi(clickClip);
        SetGroupVisible(settingsGroup, false);
        SetGroupVisible(pauseGroup, false);
        settingsVisible = false;
        currentState = FlowState.Playing;
        Time.timeScale = 1f;
        penguinController.SetInputEnabled(true);
    }

    public void OpenSettings()
    {
        PlayUi(clickClip);
        settingsVisible = true;
        SetGroupVisible(settingsGroup, true);
    }

    public void CloseSettings()
    {
        PlayUi(clickClip);
        settingsVisible = false;
        SetGroupVisible(settingsGroup, false);
    }

    public void OpenCredits()
    {
        PlayUi(clickClip);
        creditsVisible = true;
        SetGroupVisible(creditsGroup, true);
    }

    public void CloseCredits()
    {
        PlayUi(clickClip);
        creditsVisible = false;
        SetGroupVisible(creditsGroup, false);
    }

    public void ReturnToTitle()
    {
        PlayUi(clickClip);
        settingsVisible = false;
        creditsVisible = false;
        EnterTitleState(false);
    }

    public void ApplyVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        AudioListener.volume = clamped;
        PlayerPrefs.SetFloat(VolumePrefKey, clamped);
        PlayerPrefs.Save();

        if (volumeSlider != null && !Mathf.Approximately(volumeSlider.value, clamped))
        {
            volumeSlider.SetValueWithoutNotify(clamped);
        }
    }

    // ── Private flow ─────────────────────────────────────────────────────────
    private bool ResolveSceneReferences()
    {
        penguinController = FindObjectOfType<FlyingPenguinController>(true);
        cameraRig = FindObjectOfType<ThirdPersonCamera>(true);

        if (penguinController == null || cameraRig == null)
        {
            return false;
        }

        penguinTransform = penguinController.transform;
        penguinBody      = penguinController.GetComponent<Rigidbody>();
        leapInput        = penguinController.GetComponent<LeapWingInputController>();
        keyboardInput    = penguinController.GetComponent<KeyboardWingInputController>();

        spawnPosition = penguinTransform.position;
        spawnRotation = penguinTransform.rotation;
        spawnY        = spawnPosition.y;

        cameraRig.target = penguinTransform;
        cameraRig.CaptureGameplayPoseFromCurrent();
        return true;
    }

    private void BuildAudio()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.loop = false;
            uiAudioSource.ignoreListenerPause = true;
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.ignoreListenerPause = true;
        }

        if (clickClip == null)
        {
            clickClip = Resources.Load<AudioClip>("Audio/ui_click") ?? ProceduralAudioUtility.CreateToneSequence(
                "ui_click", new[] { 740f, 980f }, 0.045f, 0.18f, 44100);
        }

        if (startClip == null)
        {
            startClip = Resources.Load<AudioClip>("Audio/ui_start") ?? ProceduralAudioUtility.CreateToneSequence(
                "ui_start", new[] { 440f, 620f, 860f }, 0.065f, 0.22f, 44100);
        }

        if (pauseClip == null)
        {
            pauseClip = Resources.Load<AudioClip>("Audio/ui_pause") ?? ProceduralAudioUtility.CreateToneSequence(
                "ui_pause", new[] { 520f, 390f }, 0.06f, 0.18f, 44100);
        }

        if (victoryClip == null)
        {
            victoryClip = Resources.Load<AudioClip>("Audio/ui_victory") ?? ProceduralAudioUtility.CreateToneSequence(
                "ui_victory", new[] { 660f, 880f, 1100f, 1320f }, 0.085f, 0.22f, 44100);
        }

        if (flapClip == null)
        {
            flapClip = Resources.Load<AudioClip>("Audio/flap") ?? ProceduralAudioUtility.CreateSweep(
                "flap", 230f, 90f, 0.16f, 0.2f, 44100);
        }
    }

    private void BindEvents()
    {
        penguinController.Flapped += OnPenguinFlapped;
    }

    private void UnbindEvents()
    {
        if (penguinController != null)
        {
            penguinController.Flapped -= OnPenguinFlapped;
        }
    }

    private void OnPenguinFlapped(FlyingPenguinController.FlapSide side, float strength)
    {
        if (side == FlyingPenguinController.FlapSide.Left)
        {
            leftFlashUntil = Time.unscaledTime + 0.22f;
            leftSeen = true;
        }
        else
        {
            rightFlashUntil = Time.unscaledTime + 0.22f;
            rightSeen = true;
        }

        if (currentState == FlowState.Playing)
        {
            // PlaySfx(flapClip, 0.85f); 太恐怖了这动静
        }
    }

    private void EnterTitleState(bool snapCamera)
    {
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }

        currentState = FlowState.Title;
        finishTriggered = false;
        leftSeen = false;
        rightSeen = false;
        leftFlashUntil  = 0f;
        rightFlashUntil = 0f;

        Time.timeScale = 1f;
        penguinController.ResetRun(spawnPosition, spawnRotation, true);
        cameraRig.SetMode(ThirdPersonCamera.CameraMode.Title, snapCamera);

        SetGroupVisible(titleGroup,    true);
        SetGroupVisible(hudGroup,      false);
        SetGroupVisible(pauseGroup,    false);
        SetGroupVisible(settingsGroup, false);
        SetGroupVisible(creditsGroup,  false);
        SetGroupVisible(victoryGroup,  false);

        SetPauseButtonActive(false);
        SetProgressText("Ski safari mode: ready at the ridge");
        SetHintText(GetTitleHint());

        if (leapStatusText != null)
        {
            leapStatusText.text = GetLeapStatusText();
        }
    }

    private IEnumerator BeginRunRoutine()
    {
        yield return new WaitForSecondsRealtime(TitleTransitionDelay);

        currentState = FlowState.Playing;
        runStartUnscaledTime = Time.unscaledTime;
        penguinController.SetInputEnabled(true);
        SetPauseButtonActive(true);
        SetHintText(GetPlayingHint());
    }

    private void EnterVictoryState()
    {
        finishTriggered = true;
        currentState = FlowState.Victory;
        penguinController.SetInputEnabled(false);
        penguinBody.velocity = Vector3.zero;
        penguinBody.angularVelocity = Vector3.zero;
        penguinBody.isKinematic = true;
        cameraRig.SetMode(ThirdPersonCamera.CameraMode.Victory);
        SetPauseButtonActive(false);
        PlaySfx(victoryClip, 1f);

        Time.timeScale = 0f;
        float distance = GetRunDistance();

        if (victorySummaryText != null)
        {
            victorySummaryText.text = $"You made it across the ice run.\nDistance traveled: {distance:0}m";
        }

        SetGroupVisible(hudGroup,      false);
        SetGroupVisible(victoryGroup,  true);
        SetGroupVisible(pauseGroup,    false);
        SetGroupVisible(settingsGroup, false);
    }

    private void RestartFromFall()
    {
        if (currentState != FlowState.Playing)
        {
            return;
        }

        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
        }

        penguinController.ResetRun(spawnPosition, spawnRotation, false);
        penguinController.SetInputEnabled(false);
        currentState = FlowState.Transition;
        SetHintText("Try again and keep your flaps balanced");
        SetProgressText("Resetting to the ridge");

        runRoutine = StartCoroutine(BeginRunRoutine());
    }

    private void UpdatePlayingState()
    {
        float distance = GetRunDistance();
        SetProgressText($"Finish line {Mathf.Clamp(distance, 0f, runDistanceToWin):0}/{runDistanceToWin:0}m");

        // 注释掉距离终止判定，暂时只保留通过碰撞箱结束游戏的功能
        // if (!finishTriggered && distance >= runDistanceToWin)
        // {
        //     EnterVictoryState();
        //     return;
        // }

        if (penguinTransform.position.y < spawnY - failYDistance)
        {
            RestartFromFall();
            return;
        }

        SetHintText(GetPlayingHint());
    }

    private float GetRunDistance()
    {
        return Mathf.Max(0f, Vector3.Dot(penguinTransform.position - spawnPosition, Vector3.forward));
    }

    private void HandleMenuHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Return) && currentState == FlowState.Title && !settingsVisible && !creditsVisible)
        {
            StartRun();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsVisible)
            {
                CloseSettings();
                return;
            }

            if (creditsVisible)
            {
                CloseCredits();
                return;
            }

            if (currentState == FlowState.Playing)
            {
                EnterPauseState();
                return;
            }

            if (currentState == FlowState.Paused)
            {
                ResumeRun();
                return;
            }

            if (currentState == FlowState.Victory)
            {
                ReturnToTitle();
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentState == FlowState.Playing)
            {
                EnterPauseState();
            }
            else if (currentState == FlowState.Paused && !settingsVisible)
            {
                ResumeRun();
            }
        }
    }

    private void UpdateHandIndicators()
    {
        bool leftHot  = Time.unscaledTime < leftFlashUntil;
        bool rightHot = Time.unscaledTime < rightFlashUntil;

        UpdateIndicatorVisual(leftIndicatorImage,  leftIndicatorRect,  leftHot,  IndicatorHotLeft,  leftSeen);
        UpdateIndicatorVisual(rightIndicatorImage, rightIndicatorRect, rightHot, IndicatorHotRight, rightSeen);
    }

    private static void UpdateIndicatorVisual(Image image, RectTransform rect, bool isHot, Color hotColor, bool seen)
    {
        if (image == null || rect == null)
        {
            return;
        }

        image.color = isHot
            ? hotColor
            : Color.Lerp(IndicatorIdle, hotColor * 0.55f, seen ? 0.3f : 0f);

        float targetScale = isHot ? 1.08f : 1f;
        rect.localScale = Vector3.Lerp(rect.localScale, Vector3.one * targetScale, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
    }

    // ── Text helpers (null-safe) ──────────────────────────────────────────────
    private void SetProgressText(string text)
    {
        if (progressText != null)
        {
            progressText.text = text;
        }
    }

    private void SetHintText(string text)
    {
        if (hintText != null)
        {
            hintText.text = text;
        }
    }

    private void SetPauseButtonActive(bool active)
    {
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(active);
        }
    }

    // ── Hint strings ─────────────────────────────────────────────────────────
    private string GetTitleHint()
    {
        return leapInput != null && leapInput.leapProvider != null
            ? "Press START, then flap each hand to weave through the course."
            : "Press START. Leap provider is missing, so A / D also flap left and right.";
    }

    private string GetPlayingHint()
    {
        if (Time.unscaledTime - runStartUnscaledTime > tutorialVisibleSeconds || (leftSeen && rightSeen))
        {
            return "Hold your line and ride the ice to the finish gate.";
        }

        if (!leftSeen && !rightSeen)
        {
            return "Flap either hand to light up its indicator and steer.";
        }

        if (!leftSeen)
        {
            return "Try a LEFT flap to bump across the lane.";
        }

        if (!rightSeen)
        {
            return "Now try a RIGHT flap to balance the glide.";
        }

        return "Great. Keep alternating your hands to stay on course.";
    }

    private string GetLeapStatusText()
    {
        return leapInput != null && leapInput.leapProvider != null
            ? "Leap Motion ready: left and right hand flaps are live."
            : "Leap Motion not detected. Keyboard fallback: A / D or arrow keys.";
    }

    // ── Audio helpers ────────────────────────────────────────────────────────
    private void PlayUi(AudioClip clip)
    {
        if (clip == null || uiAudioSource == null)
        {
            return;
        }

        uiAudioSource.PlayOneShot(clip, 1f);
    }

    private void PlaySfx(AudioClip clip, float volumeScale)
    {
        if (clip == null || sfxAudioSource == null)
        {
            return;
        }

        sfxAudioSource.PlayOneShot(clip, volumeScale);
    }

    // ── Panel visibility helper ───────────────────────────────────────────────
    private static void SetGroupVisible(GameObject panel, bool visible)
    {
        if (panel != null)
        {
            panel.SetActive(visible);
        }
    }

    // ── Optional runtime finish gate (3-D geometry) ───────────────────────────
    private void BuildFinishGate()
    {
        Vector3 finishCenter = spawnPosition + Vector3.forward * runDistanceToWin + Vector3.up * 2.1f;

        GameObject finishRoot = new GameObject("RuntimeFinishGate");
        finishRoot.transform.SetParent(transform, false);
        finishRoot.transform.position = finishCenter;

        Material poleMaterial   = CreateFlatMaterial(AccentCyan);
        Material bannerMaterial = CreateFlatMaterial(AccentOrange);

        CreateGatePart("LeftPole",  finishRoot.transform, new Vector3(-finishGateWidth * 0.5f, 0f, 0f), new Vector3(0.35f, finishGateHeight, 0.35f), poleMaterial);
        CreateGatePart("RightPole", finishRoot.transform, new Vector3( finishGateWidth * 0.5f, 0f, 0f), new Vector3(0.35f, finishGateHeight, 0.35f), poleMaterial);
        CreateGatePart("Banner",    finishRoot.transform, new Vector3(0f, finishGateHeight * 0.35f, 0f), new Vector3(finishGateWidth + 1.2f, 0.45f, 0.45f), bannerMaterial);
    }

    private static void CreateGatePart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale    = localScale;

        part.GetComponent<Renderer>().material = material;

        Collider col = part.GetComponent<Collider>();
        if (col != null)
        {
            Object.Destroy(col);
        }
    }

    private static Material CreateFlatMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}


// ---------------------------------------------------------------------------
// Procedural audio — unchanged utility kept here for the fallback clips.
// ---------------------------------------------------------------------------
internal static class ProceduralAudioUtility
{
    public static AudioClip CreateSweep(string name, float startFrequency, float endFrequency, float durationSeconds, float volume, int sampleRate)
    {
        int samples = Mathf.CeilToInt(durationSeconds * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t         = i / (float)(samples - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            float envelope  = Mathf.Sin(Mathf.PI * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip CreateToneSequence(string name, float[] frequencies, float noteLengthSeconds, float volume, int sampleRate)
    {
        int noteSamples  = Mathf.CeilToInt(noteLengthSeconds * sampleRate);
        int totalSamples = noteSamples * frequencies.Length;
        float[] data     = new float[totalSamples];

        for (int noteIndex = 0; noteIndex < frequencies.Length; noteIndex++)
        {
            float frequency = frequencies[noteIndex];
            for (int i = 0; i < noteSamples; i++)
            {
                int   sampleIndex = noteIndex * noteSamples + i;
                float t           = i / (float)Mathf.Max(1, noteSamples - 1);
                float envelope    = Mathf.Sin(Mathf.PI * t);
                data[sampleIndex] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
            }
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
