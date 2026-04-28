using UnityEngine;

public class MainCanvasControl : MonoBehaviour
{
    public static MainCanvasControl Instance { get; private set; }
    public GameObject TitleScreen;
    public GameObject PauseScreen;
    public GameObject HudScreen;
    public GameObject VictoryScreen;

    public System.Action<FlowControlClean.FlowState> OnFlowStateChanged = (FlowControlClean.FlowState e) =>
    {
        if (MainCanvasControl.Instance == null) return;

        // Title 状态：只显示 TitleScreen
        MainCanvasControl.Instance.TitleScreen?.SetActive(e == FlowControlClean.FlowState.Title);

        // Playing 和 Transition 状态都显示 HudScreen（防止 RestartFromFall 导致闪烁）
        bool showHud = e == FlowControlClean.FlowState.Playing
                    || e == FlowControlClean.FlowState.Transition;
        MainCanvasControl.Instance.HudScreen?.SetActive(showHud);

        MainCanvasControl.Instance.PauseScreen?.SetActive(e == FlowControlClean.FlowState.Paused);
        MainCanvasControl.Instance.VictoryScreen?.SetActive(e == FlowControlClean.FlowState.Victory);
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        FlowControlClean.Instance.OnStateChanged += OnFlowStateChanged;
        FlowControlClean.Instance.OnStartRun += () => TitleScreen?.SetActive(false);

        // 主动同步当前状态
        ApplyCurrentState();
    }

    private void ApplyCurrentState()
    {
        FlowControlClean.FlowState state = FlowControlClean.Instance.CurrentState;
        OnFlowStateChanged(state);
        Debug.Log($"[MainCanvasControl] Applied initial state: {state}");
    }
}
