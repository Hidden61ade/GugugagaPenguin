using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 竖向进度条：显示企鹅到 Level3 终点标靶的距离进度。
/// 使用增量累加避免浮点原点修正干扰。
/// </summary>
public class ProgressBarManager : MonoBehaviour
{
    public static ProgressBarManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private float totalDistance = 15000f;

    private Transform penguinTransform;
    private float lastZ;
    private float accumulatedDistance;
    private bool initialized;
    private bool runStarted; // 本轮游戏是否已经开始（从 Title 切换出来后为 true）

    public float Progress => Mathf.Clamp01(accumulatedDistance / totalDistance);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // 延迟初始化
        if (!initialized)
        {
            FlyingPenguinController penguin = FindObjectOfType<FlyingPenguinController>(true);
            if (penguin != null)
            {
                penguinTransform = penguin.transform;
                lastZ = penguinTransform.position.z;
                initialized = true;
            }
        }

        if (FlowControlClean.Instance == null || penguinTransform == null)
            return;

        var state = FlowControlClean.Instance.CurrentState;

        // Title 状态 → 重置一切，准备下一轮
        if (state == FlowControlClean.FlowState.Title)
        {
            if (runStarted)
            {
                accumulatedDistance = 0f;
                runStarted = false;
                UpdateBar(0f);
            }
            SetVisible(false);
            return;
        }

        // Victory 状态 → 保持当前进度，不再更新
        if (state == FlowControlClean.FlowState.Victory)
        {
            // 保持显示
            return;
        }

        // Playing 或 Transition → 显示并追踪
        bool isGameplay = state == FlowControlClean.FlowState.Playing
                       || state == FlowControlClean.FlowState.Transition;

        if (!isGameplay)
            return;

        // 首次进入游戏（从 Title 来）
        if (!runStarted)
        {
            runStarted = true;
            accumulatedDistance = 0f;
            lastZ = penguinTransform.position.z;
            SetVisible(true);
            UpdateBar(0f);
            Debug.Log("[ProgressBar] 开始追踪");
        }

        // 追踪 Z 轴正向移动
        float currentZ = penguinTransform.position.z;
        float deltaZ = currentZ - lastZ;

        if (deltaZ > 0f)
        {
            accumulatedDistance += deltaZ;
        }
        // 浮点原点修正导致的大幅负跳不累加（自动忽略）

        lastZ = currentZ;

        float progress = Mathf.Clamp01(accumulatedDistance / totalDistance);
        UpdateBar(progress);
    }

    private void SetVisible(bool visible)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);
    }

    private void UpdateBar(float progress)
    {
        if (fillImage != null)
            fillImage.fillAmount = progress;
    }
}
