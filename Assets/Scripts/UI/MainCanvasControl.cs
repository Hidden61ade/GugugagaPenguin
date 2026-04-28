using UnityEngine;

public class MainCanvasControl : MonoBehaviour
{
    public static MainCanvasControl Instance { get; private set; }
    public GameObject TitleScreen;
    public GameObject PauseScreen;
    public GameObject HudScreen;
    public GameObject VictoryScreen;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        FlowControlClean.Instance.OnStateChanged += OnFlowStateChanged;
        FlowControlClean.Instance.OnStartRun += () => TitleScreen?.SetActive(false);

        // 主动同步当前状态
        OnFlowStateChanged(FlowControlClean.Instance.CurrentState);
    }

    private void OnFlowStateChanged(FlowControlClean.FlowState e)
    {
        TitleScreen?.SetActive(e == FlowControlClean.FlowState.Title);

        bool showHud = e == FlowControlClean.FlowState.Playing
                    || e == FlowControlClean.FlowState.Transition;
        HudScreen?.SetActive(showHud);

        PauseScreen?.SetActive(e == FlowControlClean.FlowState.Paused);
        VictoryScreen?.SetActive(e == FlowControlClean.FlowState.Victory);
    }
}
