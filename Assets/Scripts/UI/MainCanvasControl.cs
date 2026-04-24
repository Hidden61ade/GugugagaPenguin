using System.Collections;
using System.Collections.Generic;
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
        MainCanvasControl.Instance.TitleScreen?.SetActive(e == FlowControlClean.FlowState.Title);
        MainCanvasControl.Instance.PauseScreen?.SetActive(e == FlowControlClean.FlowState.Paused);
        MainCanvasControl.Instance.HudScreen?.SetActive(e == FlowControlClean.FlowState.Playing);
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
    }
}
