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
    [SerializeField] private Image penguinIcon;

    [Header("Settings")]
    [SerializeField] private float totalDistance = 15000f;

    private Transform penguinTransform;
    private float lastZ;
    private float accumulatedDistance;
    private bool wasPlaying;
    private bool initialized;

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
        // 延迟初始化，确保企鹅已生成
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

        if (FlowControlClean.Instance == null)
            return;

        bool isPlaying = FlowControlClean.Instance.IsPlaying;

        // 状态切换检测：刚进入 Playing → 重置
        if (isPlaying && !wasPlaying)
        {
            accumulatedDistance = 0f;
            if (penguinTransform != null)
                lastZ = penguinTransform.position.z;
            SetVisible(true);
            UpdateBar(0f);
            Debug.Log("[ProgressBar] 开始追踪");
        }
        else if (!isPlaying && wasPlaying)
        {
            // 离开 Playing 状态（Victory 等），保持当前进度但不再更新
        }

        wasPlaying = isPlaying;

        // 仅 Playing 状态更新
        if (!isPlaying || penguinTransform == null)
        {
            // 非 Playing 状态隐藏（Title 状态下）
            if (FlowControlClean.Instance.IsTitle)
                SetVisible(false);
            return;
        }

        float currentZ = penguinTransform.position.z;
        float deltaZ = currentZ - lastZ;

        // 正向移动才累加（忽略浮点原点修正导致的大幅负跳）
        if (deltaZ > 0f)
        {
            accumulatedDistance += deltaZ;
        }

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

        if (penguinIcon != null)
        {
            RectTransform iconRt = penguinIcon.GetComponent<RectTransform>();
            RectTransform barRt = fillImage.GetComponent<RectTransform>().parent as RectTransform;
            if (barRt != null && iconRt != null)
            {
                float barHeight = barRt.rect.height;
                iconRt.anchoredPosition = new Vector2(iconRt.anchoredPosition.x, progress * barHeight);
            }
        }
    }
}
