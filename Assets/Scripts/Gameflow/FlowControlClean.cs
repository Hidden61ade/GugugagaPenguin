using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏流程状态机。单场景模式，不使用 DontDestroyOnLoad。
/// 重启 = 直接 SceneManager.LoadScene(0)。
/// </summary>
public class FlowControlClean : MonoBehaviour
{
    public enum FlowState { Title, Transition, Playing, Paused, Victory }

    // ── 单例（场景级，不跨场景） ──
    private static FlowControlClean instance;
    public static FlowControlClean Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<FlowControlClean>(true);
            return instance;
        }
    }

    [Header("Flow Settings")]
    [SerializeField] private float failYDistance = 16f;
    [SerializeField] private float transitionDelay = 0.9f;
    [SerializeField] private float tutorialVisibleSeconds = 9f;

    // ── 事件 ──
    public event System.Action<FlowState> OnStateChanged;
    public event System.Action OnStartRun;
    public event System.Action OnResumeRun;
    public event System.Action OnPauseRun;
    public event System.Action OnEnterTitle;
    public event System.Action OnEnterVictory;
    public event System.Action OnRestartFromFall;
    public event System.Action<float> OnProgressUpdated;
    public event System.Action<string> OnHintUpdated;

    // ── 状态 ──
    private FlowState currentState = FlowState.Title;
    private bool finishTriggered;
    private bool fallCheckDisabled;
    private bool leftSeen, rightSeen;
    private float runStartUnscaledTime;
    private Transform penguinTransform;
    private Vector3 spawnPosition;
    private float spawnY;
    private Coroutine runRoutine;

    // ── 属性 ──
    public FlowState CurrentState => currentState;
    public bool IsPlaying => currentState == FlowState.Playing;
    public bool IsPaused => currentState == FlowState.Paused;
    public bool IsTitle => currentState == FlowState.Title;
    public bool IsVictory => currentState == FlowState.Victory;

    private void Awake()
    {
        instance = this;
        ResolveSceneReferences();
    }

    private void Start()
    {
        EnterTitleState();
    }

    private void Update()
    {
        if (penguinTransform == null) return;
        HandleMenuHotkeys();
        if (currentState == FlowState.Playing)
            UpdatePlayingState();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // ── 场景引用 ──
    private void ResolveSceneReferences()
    {
        var ctrl = FindObjectOfType<FlyingPenguinController>(true);
        if (ctrl != null)
        {
            penguinTransform = ctrl.transform;
            spawnPosition = penguinTransform.position;
            spawnY = spawnPosition.y;
            ctrl.Flapped += OnPenguinFlapped;
        }
    }

    private void OnPenguinFlapped(FlyingPenguinController.FlapSide side, float strength)
    {
        if (side == FlyingPenguinController.FlapSide.Left) leftSeen = true;
        else rightSeen = true;
    }

    // ── 公共 API ──
    public void StartRun()
    {
        if (runRoutine != null) StopCoroutine(runRoutine);
        currentState = FlowState.Transition;
        finishTriggered = false;
        leftSeen = rightSeen = false;
        Time.timeScale = 1f;
        OnStartRun?.Invoke();
        OnHintUpdated?.Invoke("Slide in... then flap left and right to steer");
        OnProgressUpdated?.Invoke(0f);
        runRoutine = StartCoroutine(BeginRunRoutine());
    }

    public void EnterPauseState()
    {
        if (currentState == FlowState.Paused) return;
        currentState = FlowState.Paused;
        Time.timeScale = 0f;
        OnStateChanged?.Invoke(currentState);
        OnPauseRun?.Invoke();
    }

    public void ResumeRun()
    {
        if (currentState != FlowState.Paused) return;
        currentState = FlowState.Playing;
        Time.timeScale = 1f;
        OnStateChanged?.Invoke(currentState);
        OnResumeRun?.Invoke();
    }

    public void DisableFallCheck() => fallCheckDisabled = true;

    public void NotifyFinishReached()
    {
        if (finishTriggered) return;
        if (currentState != FlowState.Playing && currentState != FlowState.Transition) return;
        if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; }
        EnterVictoryState();
    }

    /// <summary>
    /// 重启游戏：恢复 timeScale 并重载场景。
    /// </summary>
    public static void RestartGame()
    {
        Time.timeScale = 1f;
        instance = null;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    // ── 状态转换 ──
    private void EnterTitleState()
    {
        if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; }
        currentState = FlowState.Title;
        finishTriggered = false;
        fallCheckDisabled = false;
        leftSeen = rightSeen = false;
        Time.timeScale = 0f;
        OnStateChanged?.Invoke(currentState);
        OnEnterTitle?.Invoke();
        OnHintUpdated?.Invoke("Press START to begin");
    }

    private IEnumerator BeginRunRoutine()
    {
        yield return new WaitForSecondsRealtime(transitionDelay);
        currentState = FlowState.Playing;
        runStartUnscaledTime = Time.unscaledTime;
        OnStateChanged?.Invoke(currentState);
        OnHintUpdated?.Invoke(GetPlayingHint());
    }

    private void EnterVictoryState()
    {
        finishTriggered = true;
        currentState = FlowState.Victory;
        OnStateChanged?.Invoke(currentState);
        OnEnterVictory?.Invoke();
    }

    private void RestartFromFall()
    {
        if (currentState != FlowState.Playing) return;
        if (runRoutine != null) StopCoroutine(runRoutine);
        currentState = FlowState.Transition;
        OnStateChanged?.Invoke(currentState);
        OnRestartFromFall?.Invoke();
        OnHintUpdated?.Invoke("Try again and keep your flaps balanced");
        OnProgressUpdated?.Invoke(0f);
        runRoutine = StartCoroutine(BeginRunRoutine());
    }

    private void UpdatePlayingState()
    {
        float distance = Mathf.Max(0f, Vector3.Dot(penguinTransform.position - spawnPosition, Vector3.forward));
        OnProgressUpdated?.Invoke(distance);
        if (!fallCheckDisabled && penguinTransform.position.y < spawnY - failYDistance)
        {
            RestartFromFall();
            return;
        }
        OnHintUpdated?.Invoke(GetPlayingHint());
    }

    private void HandleMenuHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Return) && currentState == FlowState.Title) { StartRun(); return; }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == FlowState.Playing) { EnterPauseState(); return; }
            if (currentState == FlowState.Paused) { ResumeRun(); return; }
            if (currentState == FlowState.Victory) { RestartGame(); }
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentState == FlowState.Playing) EnterPauseState();
            else if (currentState == FlowState.Paused) ResumeRun();
        }
    }

    private string GetPlayingHint()
    {
        if (Time.unscaledTime - runStartUnscaledTime > tutorialVisibleSeconds || (leftSeen && rightSeen))
            return "Hold your line and ride the ice to the finish gate.";
        if (!leftSeen && !rightSeen) return "Flap either hand to light up its indicator and steer.";
        if (!leftSeen) return "Try a LEFT flap to bump across the lane.";
        if (!rightSeen) return "Now try a RIGHT flap to balance the glide.";
        return "Keep alternating your hands to stay on course.";
    }
}
