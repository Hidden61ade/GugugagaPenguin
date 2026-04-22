using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds and controls the complete single-scene title/pause/victory flow for the penguin run.
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

    [Header("Run")]
    [SerializeField] private float runDistanceToWin = 70f;
    [SerializeField] private float failYDistance = 16f;
    [SerializeField] private float tutorialVisibleSeconds = 9f;

    [Header("Finish Marker")]
    [SerializeField] private float finishGateHeight = 5.5f;
    [SerializeField] private float finishGateWidth = 9f;

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

    private Canvas rootCanvas;
    private CanvasGroup titleGroup;
    private CanvasGroup hudGroup;
    private CanvasGroup pauseGroup;
    private CanvasGroup settingsGroup;
    private CanvasGroup creditsGroup;
    private CanvasGroup victoryGroup;

    private Button pauseButton;
    private Slider volumeSlider;
    private TextMeshProUGUI leapStatusText;
    private TextMeshProUGUI progressText;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI victorySummaryText;
    private Image leftIndicatorImage;
    private Image rightIndicatorImage;
    private RectTransform leftIndicatorRect;
    private RectTransform rightIndicatorRect;

    private Texture2D titleTexture;
    private Sprite slicedSprite;
    private Sprite backgroundSprite;
    private Sprite knobSprite;
    private TMP_FontAsset bodyFont;
    private TMP_FontAsset titleFont;

    private AudioSource uiAudioSource;
    private AudioSource sfxAudioSource;
    private AudioClip clickClip;
    private AudioClip startClip;
    private AudioClip pauseClip;
    private AudioClip victoryClip;
    private AudioClip flapClip;

    private Coroutine runRoutine;

    private static readonly Color PanelColor = new Color(0.95f, 0.98f, 1f, 0.96f);
    private static readonly Color PanelShadowColor = new Color(0.34f, 0.77f, 0.9f, 0.18f);
    private static readonly Color AccentBlue = new Color(0.23f, 0.56f, 1f, 1f);
    private static readonly Color AccentCyan = new Color(0.15f, 0.84f, 0.98f, 1f);
    private static readonly Color AccentOrange = new Color(1f, 0.56f, 0.16f, 1f);
    private static readonly Color AccentWarm = new Color(1f, 0.75f, 0.28f, 1f);
    private static readonly Color DarkText = new Color(0.1f, 0.18f, 0.32f, 1f);
    private static readonly Color SoftBackdrop = new Color(0.05f, 0.14f, 0.26f, 0.76f);
    private static readonly Color IndicatorIdle = new Color(0.14f, 0.22f, 0.38f, 0.86f);
    private static readonly Color IndicatorHotLeft = new Color(0.16f, 0.77f, 1f, 1f);
    private static readonly Color IndicatorHotRight = new Color(1f, 0.58f, 0.19f, 1f);

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

        LoadPresentationAssets();
        BuildUi();
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

    public void NotifyFinishReached()
    {
        if (currentState != FlowState.Playing || finishTriggered)
        {
            return;
        }

        EnterVictoryState();
    }

    private bool ResolveSceneReferences()
    {
        penguinController = FindObjectOfType<FlyingPenguinController>(true);
        cameraRig = FindObjectOfType<ThirdPersonCamera>(true);

        if (penguinController == null || cameraRig == null)
        {
            return false;
        }

        penguinTransform = penguinController.transform;
        penguinBody = penguinController.GetComponent<Rigidbody>();
        leapInput = penguinController.GetComponent<LeapWingInputController>();
        keyboardInput = penguinController.GetComponent<KeyboardWingInputController>();

        spawnPosition = penguinTransform.position;
        spawnRotation = penguinTransform.rotation;
        spawnY = spawnPosition.y;

        cameraRig.target = penguinTransform;
        cameraRig.CaptureGameplayPoseFromCurrent();
        return true;
    }

    private void LoadPresentationAssets()
    {
        titleTexture = Resources.Load<Texture2D>("start_image");
        slicedSprite = CreateRoundedRectSprite(128, 28f, 3.5f);
        backgroundSprite = CreateSolidSprite();
        knobSprite = CreateRoundedRectSprite(128, 50f, 3.5f);
        bodyFont = TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        titleFont = bodyFont;
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("PenguinUiCanvas");
        canvasObject.transform.SetParent(transform, false);
        rootCanvas = canvasObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.55f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform root = rootCanvas.GetComponent<RectTransform>();

        titleGroup = CreateScreenGroup("TitleGroup", root, true);
        hudGroup = CreateScreenGroup("HudGroup", root, false);
        pauseGroup = CreateScreenGroup("PauseGroup", root, false);
        settingsGroup = CreateScreenGroup("SettingsGroup", root, false);
        creditsGroup = CreateScreenGroup("CreditsGroup", root, false);
        victoryGroup = CreateScreenGroup("VictoryGroup", root, false);

        BuildTitleUi(titleGroup.transform as RectTransform);
        BuildHud(hudGroup.transform as RectTransform);
        BuildPauseUi(pauseGroup.transform as RectTransform);
        BuildSettingsUi(settingsGroup.transform as RectTransform);
        BuildCreditsUi(creditsGroup.transform as RectTransform);
        BuildVictoryUi(victoryGroup.transform as RectTransform);
    }

    private void BuildAudio()
    {
        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.loop = false;
        uiAudioSource.ignoreListenerPause = true;

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
        sfxAudioSource.ignoreListenerPause = true;

        clickClip = Resources.Load<AudioClip>("Audio/ui_click") ?? ProceduralAudioUtility.CreateToneSequence(
            "ui_click",
            new[] { 740f, 980f },
            0.045f,
            0.18f,
            44100);

        startClip = Resources.Load<AudioClip>("Audio/ui_start") ?? ProceduralAudioUtility.CreateToneSequence(
            "ui_start",
            new[] { 440f, 620f, 860f },
            0.065f,
            0.22f,
            44100);

        pauseClip = Resources.Load<AudioClip>("Audio/ui_pause") ?? ProceduralAudioUtility.CreateToneSequence(
            "ui_pause",
            new[] { 520f, 390f },
            0.06f,
            0.18f,
            44100);

        victoryClip = Resources.Load<AudioClip>("Audio/ui_victory") ?? ProceduralAudioUtility.CreateToneSequence(
            "ui_victory",
            new[] { 660f, 880f, 1100f, 1320f },
            0.085f,
            0.22f,
            44100);

        flapClip = Resources.Load<AudioClip>("Audio/flap") ?? ProceduralAudioUtility.CreateSweep(
            "flap",
            230f,
            90f,
            0.16f,
            0.2f,
            44100);
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
            PlaySfx(flapClip, 0.85f);
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
        leftFlashUntil = 0f;
        rightFlashUntil = 0f;

        Time.timeScale = 1f;
        penguinController.ResetRun(spawnPosition, spawnRotation, true);
        cameraRig.SetMode(ThirdPersonCamera.CameraMode.Title, snapCamera);

        SetGroupVisible(titleGroup, true);
        SetGroupVisible(hudGroup, false);
        SetGroupVisible(pauseGroup, false);
        SetGroupVisible(settingsGroup, false);
        SetGroupVisible(creditsGroup, false);
        SetGroupVisible(victoryGroup, false);

        pauseButton.gameObject.SetActive(false);
        progressText.text = "Ski safari mode: ready at the ridge";
        hintText.text = GetTitleHint();
        leapStatusText.text = GetLeapStatusText();
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
        pauseButton.gameObject.SetActive(false);
        hintText.text = "Slide in... then flap left and right to steer";
        progressText.text = "Gliding onto the ice run";

        runRoutine = StartCoroutine(BeginRunRoutine());
    }

    private IEnumerator BeginRunRoutine()
    {
        yield return new WaitForSecondsRealtime(TitleTransitionDelay);

        currentState = FlowState.Playing;
        runStartUnscaledTime = Time.unscaledTime;
        penguinController.SetInputEnabled(true);
        pauseButton.gameObject.SetActive(true);
        hintText.text = GetPlayingHint();
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

    private void EnterVictoryState()
    {
        finishTriggered = true;
        currentState = FlowState.Victory;
        penguinController.SetInputEnabled(false);
        penguinBody.velocity = Vector3.zero;
        penguinBody.angularVelocity = Vector3.zero;
        penguinBody.isKinematic = true;
        cameraRig.SetMode(ThirdPersonCamera.CameraMode.Victory);
        pauseButton.gameObject.SetActive(false);
        PlaySfx(victoryClip, 1f);

        Time.timeScale = 0f;
        float distance = GetRunDistance();
        victorySummaryText.text = $"You made it across the ice run.\nDistance traveled: {distance:0}m";
        SetGroupVisible(hudGroup, false);
        SetGroupVisible(victoryGroup, true);
        SetGroupVisible(pauseGroup, false);
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
        hintText.text = "Try again and keep your flaps balanced";
        progressText.text = "Resetting to the ridge";

        runRoutine = StartCoroutine(BeginRunRoutine());
    }

    private void UpdatePlayingState()
    {
        float distance = GetRunDistance();
        progressText.text = $"Finish line {Mathf.Clamp(distance, 0f, runDistanceToWin):0}/{runDistanceToWin:0}m";

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

        hintText.text = GetPlayingHint();
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
        bool leftHot = Time.unscaledTime < leftFlashUntil;
        bool rightHot = Time.unscaledTime < rightFlashUntil;

        UpdateIndicatorVisual(leftIndicatorImage, leftIndicatorRect, leftHot, IndicatorHotLeft, leftSeen);
        UpdateIndicatorVisual(rightIndicatorImage, rightIndicatorRect, rightHot, IndicatorHotRight, rightSeen);
    }

    private void UpdateIndicatorVisual(Image image, RectTransform rect, bool isHot, Color hotColor, bool seen)
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

    private void ApplyVolume(float value)
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

    private void BuildFinishGate()
    {
        Vector3 finishCenter = spawnPosition + Vector3.forward * runDistanceToWin + Vector3.up * 2.1f;

        GameObject finishRoot = new GameObject("RuntimeFinishGate");
        finishRoot.transform.SetParent(transform, false);
        finishRoot.transform.position = finishCenter;

        Material poleMaterial = CreateFlatMaterial(AccentCyan);
        Material bannerMaterial = CreateFlatMaterial(AccentOrange);

        CreateGatePart("LeftPole", finishRoot.transform, new Vector3(-finishGateWidth * 0.5f, 0f, 0f), new Vector3(0.35f, finishGateHeight, 0.35f), poleMaterial);
        CreateGatePart("RightPole", finishRoot.transform, new Vector3(finishGateWidth * 0.5f, 0f, 0f), new Vector3(0.35f, finishGateHeight, 0.35f), poleMaterial);
        CreateGatePart("Banner", finishRoot.transform, new Vector3(0f, finishGateHeight * 0.35f, 0f), new Vector3(finishGateWidth + 1.2f, 0.45f, 0.45f), bannerMaterial);

    }

    private static void CreateGatePart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        renderer.material = material;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
    }

    private static Material CreateFlatMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private static Sprite LoadBuiltinSprite(string primaryName, string fallbackName)
    {
        Sprite sprite = Resources.GetBuiltinResource<Sprite>(primaryName);
        if (sprite == null)
        {
            sprite = Resources.GetBuiltinResource<Sprite>(fallbackName);
        }

        return sprite ?? CreateFallbackSprite();
    }

    private static Sprite CreateSolidSprite()
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.SetPixels(new[]
        {
            Color.white, Color.white, Color.white, Color.white,
            Color.white, Color.white, Color.white, Color.white,
            Color.white, Color.white, Color.white, Color.white,
            Color.white, Color.white, Color.white, Color.white
        });
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f, 0u, SpriteMeshType.FullRect, new Vector4(1f, 1f, 1f, 1f));
        sprite.name = "RuntimeFallbackSprite";
        return sprite;
    }

    private static Sprite CreateRoundedRectSprite(int size, float radius, float feather)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float halfSize = size * 0.5f;
        Vector2 halfExtents = new Vector2(halfSize - feather - 1f, halfSize - feather - 1f);
        Color[] colors = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f - halfSize, y + 0.5f - halfSize);
                Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - (halfExtents - Vector2.one * radius);
                Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                float signedDistance = outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                float alpha = Mathf.InverseLerp(feather, -feather, signedDistance);
                colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;

        float border = Mathf.Clamp(radius + feather, 1f, size * 0.48f);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0u,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
        sprite.name = "RuntimeRoundedSprite";
        return sprite;
    }

    private static Sprite CreateFallbackSprite()
    {
        return CreateSolidSprite();
    }

    private void CreateBackdrop(RectTransform root)
    {
        Image baseBackdrop = CreateImage("Backdrop", root, root.sizeDelta, new Color(0.05f, 0.14f, 0.26f, 0.82f), backgroundSprite);
        Stretch(baseBackdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private void BuildTitleUi(RectTransform parent)
    {
        CreateBackdrop(parent);

        RectTransform card = CreatePanel(parent, "TitleCard", new Vector2(940f, 780f), new Vector2(0.5f, 0.55f), PanelColor, 0f);
        AddShadow(card.gameObject, PanelShadowColor, new Vector2(5f, -6f));

        if (titleTexture != null)
        {
            GameObject rawImageObject = new GameObject("StartImage", typeof(RectTransform), typeof(RawImage));
            rawImageObject.transform.SetParent(card, false);
            RawImage rawImage = rawImageObject.GetComponent<RawImage>();
            rawImage.texture = titleTexture;
            rawImage.color = Color.white;
            RectTransform rt = rawImage.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            float imageWidth = 760f;
            float imageHeight = imageWidth / Mathf.Max(0.01f, titleTexture.width / (float)titleTexture.height);
            rt.sizeDelta = new Vector2(imageWidth, imageHeight);
            rt.anchoredPosition = new Vector2(0f, -26f);
            rt.localRotation = Quaternion.identity;
            AddShadow(rawImageObject, new Color(0.13f, 0.53f, 0.72f, 0.16f), new Vector2(6f, -6f));
        }
        else
        {
            TextMeshProUGUI fallbackTitle = CreateText(card, "Gugugaga Penguin", 64, titleFont, FontStyles.Bold, DarkText);
            fallbackTitle.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            fallbackTitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            fallbackTitle.rectTransform.anchoredPosition = new Vector2(0f, -110f);
            fallbackTitle.alignment = TextAlignmentOptions.Center;
        }

        leapStatusText = CreateBadge(card, new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(620f, 50f), new Color(0.08f, 0.18f, 0.3f, 0.9f), AccentWarm);
        leapStatusText.text = GetLeapStatusText();
        leapStatusText.fontSize = 20f;

        TextMeshProUGUI subtitle = CreateText(card, "Ski Safari inspired flow: start on the ridge, flap to thread the course, reach the gate.", 28, bodyFont, FontStyles.Bold, DarkText);
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        subtitle.rectTransform.sizeDelta = new Vector2(650f, 74f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -112f);

        Button startButton = CreateButton(card, "START", new Vector2(0.5f, 0.5f), new Vector2(0f, -208f), new Vector2(320f, 88f), AccentOrange, 0f);
        startButton.onClick.AddListener(StartRun);

        Button settingsButton = CreateButton(card, "SETTINGS", new Vector2(0.5f, 0.5f), new Vector2(-154f, -296f), new Vector2(248f, 76f), AccentBlue, 0f);
        settingsButton.onClick.AddListener(OpenSettings);

        Button creditsButton = CreateButton(card, "CREDITS", new Vector2(0.5f, 0.5f), new Vector2(154f, -296f), new Vector2(248f, 76f), AccentCyan, 0f);
        creditsButton.onClick.AddListener(OpenCredits);
    }

    private void BuildHud(RectTransform parent)
    {
        pauseButton = CreateButton(parent, "II", new Vector2(1f, 1f), new Vector2(-92f, -88f), new Vector2(82f, 82f), AccentBlue, 0f);
        pauseButton.onClick.AddListener(EnterPauseState);

        RectTransform header = CreatePanel(parent, "ProgressPanel", new Vector2(540f, 106f), new Vector2(0.5f, 1f), new Color(0.09f, 0.18f, 0.3f, 0.82f), 0f);
        header.anchoredPosition = new Vector2(0f, -86f);
        progressText = CreateText(header, "Ready at the ridge", 32, bodyFont, FontStyles.Bold, Color.white);
        progressText.alignment = TextAlignmentOptions.Center;
        Stretch(progressText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 16f), new Vector2(-28f, -16f));

        hintText = CreateText(parent, string.Empty, 26, bodyFont, FontStyles.Bold, Color.white);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        hintText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        hintText.rectTransform.sizeDelta = new Vector2(860f, 64f);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, -158f);

        leftIndicatorRect = CreateIndicator(parent, "LEFT HAND", "Flap left / A", new Vector2(0f, 0f), new Vector2(120f, 116f), out leftIndicatorImage, 0f);
        rightIndicatorRect = CreateIndicator(parent, "RIGHT HAND", "Flap right / D", new Vector2(1f, 0f), new Vector2(-120f, 116f), out rightIndicatorImage, 0f);
    }

    private void BuildPauseUi(RectTransform parent)
    {
        CreateDimmer(parent, new Color(0f, 0.05f, 0.12f, 0.66f));
        RectTransform panel = CreatePanel(parent, "PausePanel", new Vector2(640f, 580f), new Vector2(0.5f, 0.5f), PanelColor, 0f);
        CreateHeader(panel, "PAUSED", "Take a breath, then drop back into the run.");

        Button resumeButton = CreateButton(panel, "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(300f, 76f), AccentOrange, 0f);
        resumeButton.onClick.AddListener(ResumeRun);

        Button settingsButton = CreateButton(panel, "SETTINGS", new Vector2(0.5f, 0.5f), new Vector2(0f, -168f), new Vector2(300f, 76f), AccentBlue, 0f);
        settingsButton.onClick.AddListener(OpenSettings);

        Button titleButton = CreateButton(panel, "BACK TO TITLE", new Vector2(0.5f, 0.5f), new Vector2(0f, -246f), new Vector2(340f, 76f), AccentCyan, 0f);
        titleButton.onClick.AddListener(ReturnToTitle);
    }

    private void BuildSettingsUi(RectTransform parent)
    {
        CreateDimmer(parent, new Color(0f, 0.05f, 0.12f, 0.55f));
        RectTransform panel = CreatePanel(parent, "SettingsPanel", new Vector2(700f, 320f), new Vector2(0.5f, 0.5f), PanelColor, 0f);
        CreateHeader(panel, "SETTINGS", string.Empty);

        TextMeshProUGUI volumeLabel = CreateText(panel, "VOLUME", 28, bodyFont, FontStyles.Bold, DarkText);
        volumeLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        volumeLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        volumeLabel.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        volumeLabel.alignment = TextAlignmentOptions.Center;

        volumeSlider = CreateSlider(panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(470f, 50f));
        volumeSlider.onValueChanged.AddListener(ApplyVolume);

        Button closeButton = CreateButton(panel, "CLOSE", new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(260f, 74f), AccentOrange, 0f);
        closeButton.onClick.AddListener(CloseSettings);
    }

    private void BuildCreditsUi(RectTransform parent)
    {
        CreateDimmer(parent, new Color(0f, 0.05f, 0.12f, 0.58f));
        RectTransform panel = CreatePanel(parent, "CreditsPanel", new Vector2(860f, 500f), new Vector2(0.5f, 0.5f), PanelColor, 0f);
        CreateHeader(panel, "CREDITS", "Single-scene game wrapper for the current penguin build.");

        TextMeshProUGUI body = CreateText(
            panel,
            "Visual anchor: start_image.png\n" +
            "Input: Ultraleap left / right palm flaps with keyboard fallback\n" +
            "Flow: title -> ridge drop -> play -> pause -> victory\n" +
            "Audio: resource-loaded clips when present, otherwise procedural placeholders\n" +
            "UI styling: rounded stickers, icy blues and warm orange accents",
            28,
            bodyFont,
            FontStyles.Normal,
            DarkText);
        body.alignment = TextAlignmentOptions.Center;
        body.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        body.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        body.rectTransform.sizeDelta = new Vector2(700f, 230f);
        body.rectTransform.anchoredPosition = new Vector2(0f, -26f);

        Button closeButton = CreateButton(panel, "BACK", new Vector2(0.5f, 0.5f), new Vector2(0f, -188f), new Vector2(250f, 74f), AccentBlue, 0f);
        closeButton.onClick.AddListener(CloseCredits);
    }

    private void BuildVictoryUi(RectTransform parent)
    {
        CreateDimmer(parent, new Color(0f, 0.06f, 0.14f, 0.62f));
        RectTransform panel = CreatePanel(parent, "VictoryPanel", new Vector2(760f, 450f), new Vector2(0.5f, 0.5f), PanelColor, 0f);
        CreateHeader(panel, "YOU MADE IT", "The penguin cleared the ice run.");

        victorySummaryText = CreateText(panel, "Distance traveled: 0m", 30, bodyFont, FontStyles.Bold, DarkText);
        victorySummaryText.alignment = TextAlignmentOptions.Center;
        victorySummaryText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        victorySummaryText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        victorySummaryText.rectTransform.sizeDelta = new Vector2(600f, 96f);
        victorySummaryText.rectTransform.anchoredPosition = new Vector2(0f, -42f);

        Button replayButton = CreateButton(panel, "PLAY AGAIN", new Vector2(0.5f, 0.5f), new Vector2(-156f, -176f), new Vector2(270f, 76f), AccentOrange, 0f);
        replayButton.onClick.AddListener(StartRun);

        Button titleButton = CreateButton(panel, "TITLE", new Vector2(0.5f, 0.5f), new Vector2(156f, -176f), new Vector2(220f, 76f), AccentBlue, 0f);
        titleButton.onClick.AddListener(ReturnToTitle);
    }

    private static CanvasGroup CreateScreenGroup(string name, RectTransform parent, bool visible)
    {
        GameObject groupObject = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        groupObject.transform.SetParent(parent, false);
        RectTransform rt = groupObject.GetComponent<RectTransform>();
        Stretch(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CanvasGroup group = groupObject.GetComponent<CanvasGroup>();
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        return group;
    }

    private static void SetGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private RectTransform CreatePanel(RectTransform parent, string name, Vector2 size, Vector2 anchor, Color color, float rotationZ)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rt = panelObject.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        Image image = panelObject.GetComponent<Image>();
        image.sprite = slicedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return rt;
    }

    private static Image CreateImage(string name, RectTransform parent, Vector2 size, Color color, Sprite sprite)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;

        RectTransform rt = image.rectTransform;
        rt.sizeDelta = size;
        return image;
    }

    private void CreateDimmer(RectTransform parent, Color color)
    {
        Image dimmer = CreateImage("Dimmer", parent, parent.sizeDelta, color, backgroundSprite);
        Stretch(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private void CreateHeader(RectTransform panel, string title, string subtitle)
    {
        TextMeshProUGUI titleText = CreateText(panel, title, 44, titleFont, FontStyles.Bold, DarkText);
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -52f);
        titleText.rectTransform.sizeDelta = new Vector2(Mathf.Max(420f, panel.sizeDelta.x - 96f), 72f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.enableWordWrapping = false;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 28f;
        titleText.fontSizeMax = 44f;

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            TextMeshProUGUI subtitleText = CreateText(panel, subtitle, 24, bodyFont, FontStyles.Bold, new Color(0.22f, 0.32f, 0.47f, 1f));
            subtitleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            subtitleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            subtitleText.rectTransform.anchoredPosition = new Vector2(0f, -98f);
            subtitleText.rectTransform.sizeDelta = new Vector2(620f, 56f);
            subtitleText.alignment = TextAlignmentOptions.Center;
        }
    }

    private TextMeshProUGUI CreateBadge(RectTransform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color fillColor, Color textColor)
    {
        RectTransform badge = CreatePanel(parent, "Badge", size, anchor, fillColor, 0f);
        badge.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = CreateText(badge, string.Empty, 23, bodyFont, FontStyles.Bold, textColor);
        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 10f), new Vector2(-24f, -10f));
        return text;
    }

    private Button CreateButton(RectTransform parent, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color fillColor, float rotationZ)
    {
        RectTransform buttonRoot = CreatePanel(parent, label + "Button", size, anchor, fillColor, rotationZ);
        buttonRoot.anchoredPosition = anchoredPosition;
        AddShadow(buttonRoot.gameObject, new Color(0f, 0.15f, 0.25f, 0.12f), new Vector2(3f, -3f));

        Button button = buttonRoot.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.97f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.9f, 0.98f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        button.colors = colors;
        button.targetGraphic = buttonRoot.GetComponent<Image>();

        TextMeshProUGUI text = CreateText(buttonRoot, label, 34, titleFont, FontStyles.Bold, Color.white);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 34f;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        AddShadow(text.gameObject, new Color(0f, 0f, 0f, 0.18f), new Vector2(1f, -1f));
        return button;
    }

    private TextMeshProUGUI CreateText(Transform parent, string content, float fontSize, TMP_FontAsset font, FontStyles fontStyle, Color color)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.enableWordWrapping = true;
        return text;
    }

    private RectTransform CreateIndicator(RectTransform parent, string title, string subtitle, Vector2 anchor, Vector2 anchoredPosition, out Image image, float rotationZ)
    {
        RectTransform panel = CreatePanel(parent, title.Replace(" ", string.Empty) + "Indicator", new Vector2(250f, 126f), anchor, IndicatorIdle, rotationZ);
        panel.anchoredPosition = anchoredPosition;
        panel.pivot = new Vector2(anchor.x, anchor.y);
        AddShadow(panel.gameObject, new Color(0f, 0.16f, 0.24f, 0.1f), new Vector2(2f, -2f));

        image = panel.GetComponent<Image>();

        TextMeshProUGUI titleText = CreateText(panel, title, 32, titleFont, FontStyles.Bold, Color.white);
        titleText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(22f, -16f);
        titleText.rectTransform.offsetMax = new Vector2(-22f, -18f);
        titleText.alignment = TextAlignmentOptions.TopLeft;

        TextMeshProUGUI subtitleText = CreateText(panel, subtitle, 22, bodyFont, FontStyles.Bold, new Color(0.88f, 0.95f, 1f, 0.92f));
        subtitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
        subtitleText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        subtitleText.rectTransform.offsetMin = new Vector2(22f, 12f);
        subtitleText.rectTransform.offsetMax = new Vector2(-22f, 2f);
        subtitleText.alignment = TextAlignmentOptions.BottomLeft;

        return panel;
    }

    private Slider CreateSlider(RectTransform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject sliderRoot = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        sliderRoot.transform.SetParent(parent, false);

        RectTransform rt = sliderRoot.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;

        Slider slider = sliderRoot.GetComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        Image background = CreateImage("Background", rt, size, new Color(0.16f, 0.26f, 0.42f, 0.35f), backgroundSprite);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(rt, false);
        Stretch(fillArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 16f), new Vector2(-18f, -16f));

        Image fill = CreateImage("Fill", fillArea, size, AccentCyan, slicedSprite);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fill.type = Image.Type.Sliced;

        RectTransform handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(rt, false);
        Stretch(handleArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 8f), new Vector2(-18f, -8f));

        Image handle = CreateImage("Handle", handleArea, new Vector2(42f, 42f), AccentOrange, knobSprite);
        handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        handle.rectTransform.anchoredPosition = Vector2.zero;
        handle.type = Image.Type.Simple;

        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.value = PlayerPrefs.GetFloat(VolumePrefKey, 0.85f);
        return slider;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

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

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}

internal static class PenguinGameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureFlowController()
    {
        if (Object.FindObjectOfType<PenguinGameFlowController>() != null)
        {
            return;
        }

        if (Object.FindObjectOfType<FlyingPenguinController>() == null)
        {
            return;
        }

        GameObject flowRoot = new GameObject("PenguinGameFlow");
        flowRoot.AddComponent<PenguinGameFlowController>();
    }
}

internal static class ProceduralAudioUtility
{
    public static AudioClip CreateSweep(string name, float startFrequency, float endFrequency, float durationSeconds, float volume, int sampleRate)
    {
        int samples = Mathf.CeilToInt(durationSeconds * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            float envelope = Mathf.Sin(Mathf.PI * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip CreateToneSequence(string name, float[] frequencies, float noteLengthSeconds, float volume, int sampleRate)
    {
        int noteSamples = Mathf.CeilToInt(noteLengthSeconds * sampleRate);
        int totalSamples = noteSamples * frequencies.Length;
        float[] data = new float[totalSamples];

        for (int noteIndex = 0; noteIndex < frequencies.Length; noteIndex++)
        {
            float frequency = frequencies[noteIndex];
            for (int i = 0; i < noteSamples; i++)
            {
                int sampleIndex = noteIndex * noteSamples + i;
                float t = i / (float)Mathf.Max(1, noteSamples - 1);
                float envelope = Mathf.Sin(Mathf.PI * t);
                data[sampleIndex] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
            }
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
