using System.Collections;
using UnityEngine;

/// <summary>
/// 纯净的游戏流程状态机，负责管理游戏的主要状态转换。
/// 不包含任何 UI、Audio 或其他具体实现细节。
/// 通过事件回调与外部系统通信。
/// 采用懒汉式单例模式，确保全局唯一访问点。
/// </summary>
public class FlowControlClean : MonoBehaviour
{
    public enum FlowState
    {
        Title,
        Transition,
        Playing,
        Paused,
        Victory
    }

    // ── 懒汉式单例实现 ────────────────────────────────────────────────────────
    private static FlowControlClean instance;
    
    public static FlowControlClean Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FlowControlClean>(true);
                
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("FlowControlClean");
                    instance = singletonObject.AddComponent<FlowControlClean>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return instance;
        }
    }

    // ── 流程配置 ──────────────────────────────────────────────────────────────
    [Header("Flow Settings")]
    [SerializeField] private float runDistanceToWin = 70f;
    [SerializeField] private float failYDistance = 16f;
    [SerializeField] private float transitionDelay = 0.9f;
    [SerializeField] private float tutorialVisibleSeconds = 9f;

    // ── 状态事件回调 ──────────────────────────────────────────────────────────
    public event System.Action<FlowState> OnStateChanged;
    public event System.Action OnStartRun;
    public event System.Action OnResumeRun;
    public event System.Action OnPauseRun;
    public event System.Action OnEnterTitle;
    public event System.Action OnEnterVictory;
    public event System.Action OnRestartFromFall;
    public event System.Action<float> OnProgressUpdated;
    public event System.Action<string> OnHintUpdated;

    // ── 运行时状态 ────────────────────────────────────────────────────────────
    private FlowState currentState = FlowState.Title;
    private bool finishTriggered;
    private bool fallCheckDisabled; // 标靶命中后禁止坠落检测
    private bool leftSeen;
    private bool rightSeen;
    private float runStartUnscaledTime;

    private Transform penguinTransform;
    private Vector3 spawnPosition;
    private float spawnY;

    private Coroutine runRoutine;

    // ── 属性 ──────────────────────────────────────────────────────────────────
    public FlowState CurrentState => currentState;
    public bool IsPlaying => currentState == FlowState.Playing;
    public bool IsPaused => currentState == FlowState.Paused;
    public bool IsTitle => currentState == FlowState.Title;
    public bool IsVictory => currentState == FlowState.Victory;

    // ── 生命周期 ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            ResolveSceneReferences();
            EnterTitleState();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        EnterTitleState();
    }
    private void Update()
    {
        if (penguinTransform == null)
            return;

        HandleMenuHotkeys();

        if (currentState == FlowState.Playing)
        {
            UpdatePlayingState();
        }
    }

    // ── 场景引用解析 ──────────────────────────────────────────────────────────
    private void ResolveSceneReferences()
    {
        FlyingPenguinController penguinController = FindObjectOfType<FlyingPenguinController>(true);
        
        if (penguinController != null)
        {
            penguinTransform = penguinController.transform;
            spawnPosition = penguinTransform.position;
            spawnY = spawnPosition.y;
            penguinController.Flapped += OnPenguinFlapped;
        }
    }

    private void OnDestroy()
    {
        FlyingPenguinController penguinController = FindObjectOfType<FlyingPenguinController>(true);
        if (penguinController != null)
        {
            penguinController.Flapped -= OnPenguinFlapped;
        }
    }

    // ── 企鹅拍打事件 ──────────────────────────────────────────────────────────
    private void OnPenguinFlapped(FlyingPenguinController.FlapSide side, float strength)
    {
        if (side == FlyingPenguinController.FlapSide.Left)
            leftSeen = true;
        else
            rightSeen = true;
    }

    // ── 公共 API ──────────────────────────────────────────────────────────────
    public void StartRun()
    {
        if (runRoutine != null)
            StopCoroutine(runRoutine);

        currentState = FlowState.Transition;
        finishTriggered = false;
        leftSeen = false;
        rightSeen = false;
        Time.timeScale = 1f;

        OnStartRun?.Invoke();
        OnHintUpdated?.Invoke("Slide in... then flap left and right to steer");
        OnProgressUpdated?.Invoke(0f);

        runRoutine = StartCoroutine(BeginRunRoutine());
    }

    public void EnterPauseState()
    {
        if (currentState == FlowState.Paused)
            return;

        currentState = FlowState.Paused;
        Time.timeScale = 0f;
        
        OnStateChanged?.Invoke(currentState);
        OnPauseRun?.Invoke();
    }

    public void ResumeRun()
    {
        if (currentState != FlowState.Paused)
            return;

        currentState = FlowState.Playing;
        Time.timeScale = 1f;
        
        OnStateChanged?.Invoke(currentState);
        OnResumeRun?.Invoke();
    }

    public void ReturnToTitle()
    {
        EnterTitleState();
    }

    /// <summary>
    /// 禁止坠落检测（标靶命中后调用）。
    /// </summary>
    public void DisableFallCheck()
    {
        fallCheckDisabled = true;
    }

    public void NotifyFinishReached()
    {
        // 允许从 Playing 或 Transition 进入 Victory（兼容 RestartFromFall 循环）
        if (finishTriggered)
            return;

        if (currentState != FlowState.Playing && currentState != FlowState.Transition)
            return;

        // 停止可能正在运行的 Transition 协程
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }

        EnterVictoryState();
    }

    // ── 状态转换 ──────────────────────────────────────────────────────────────
    private void EnterTitleState()
    {
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }

        currentState = FlowState.Title;
        finishTriggered = false;
        fallCheckDisabled = false;
        leftSeen = false;
        rightSeen = false;
        Time.timeScale = 0f;

        OnStateChanged?.Invoke(currentState);
        OnEnterTitle?.Invoke();
        OnHintUpdated?.Invoke(GetTitleHint());
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
        Time.timeScale = 0f;

        OnStateChanged?.Invoke(currentState);
        OnEnterVictory?.Invoke();
    }

    private void RestartFromFall()
    {
        if (currentState != FlowState.Playing)
            return;

        if (runRoutine != null)
            StopCoroutine(runRoutine);

        currentState = FlowState.Transition;
        
        OnStateChanged?.Invoke(currentState);
        OnRestartFromFall?.Invoke();
        OnHintUpdated?.Invoke("Try again and keep your flaps balanced");
        OnProgressUpdated?.Invoke(0f);

        runRoutine = StartCoroutine(BeginRunRoutine());
    }

    // ── 游戏中状态更新 ────────────────────────────────────────────────────────
    private void UpdatePlayingState()
    {
        float distance = GetRunDistance();
        OnProgressUpdated?.Invoke(distance);

        if (!fallCheckDisabled && penguinTransform.position.y < spawnY - failYDistance)
        {
            RestartFromFall();
            return;
        }

        OnHintUpdated?.Invoke(GetPlayingHint());
    }

    private float GetRunDistance()
    {
        return Mathf.Max(0f, Vector3.Dot(penguinTransform.position - spawnPosition, Vector3.forward));
    }

    // ── 菜单热键处理 ──────────────────────────────────────────────────────────
    private void HandleMenuHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Return) && currentState == FlowState.Title)
        {
            StartRun();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
            else if (currentState == FlowState.Paused)
            {
                ResumeRun();
            }
        }
    }

    // ── Hint 字符串生成 ────────────────────────────────────────────────────────
    private string GetTitleHint()
    {
        return "Press START to begin";
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
}