using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 管理器：负责管理游戏中 HUD 面板的所有交互逻辑。
/// 挂载在 Canvas/HudPanel 上，通过 Inspector 拖入引用。
/// </summary>
public class HudManager : MonoBehaviour
{
    // ── 单例 ──────────────────────────────────────────────────────────────────
    public static HudManager Instance { get; private set; }

    // ── Inspector 引用 ────────────────────────────────────────────────────────
    [Header("Button - 返回开始界面")]
    [SerializeField] private Button backButton;

    [Header("Hand Indicators - 拍打反馈")]
    [SerializeField] private RectTransform leftHandIndicator;
    [SerializeField] private RectTransform rightHandIndicator;
    [SerializeField] private float flapAnimDuration = 0.1f;
    [SerializeField] private float flapAngle = 45f;

    [Header("Score - 分数系统")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Hearts - 生命值系统")]
    [SerializeField] private List<GameObject> hearts;

    // ── 运行时状态 ────────────────────────────────────────────────────────────
    private int currentScore;
    private int maxLives;
    private int currentLives;

    private FlyingPenguinController penguinController;
    private Coroutine leftFlapCoroutine;
    private Coroutine rightFlapCoroutine;
    private float leftBaseZ;
    private float rightBaseZ;

    // ── 公共属性 ──────────────────────────────────────────────────────────────
    public int CurrentScore => currentScore;
    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;

    // ── 生命周期 ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        // 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 记录手部指示器的初始 Z 旋转角度
        if (leftHandIndicator != null)
            leftBaseZ = leftHandIndicator.localEulerAngles.z;
        if (rightHandIndicator != null)
            rightBaseZ = rightHandIndicator.localEulerAngles.z;

        // 初始化生命值
        maxLives = hearts != null ? hearts.Count : 0;
        currentLives = maxLives;

        // 初始化分数
        currentScore = 0;
        UpdateScoreDisplay();
    }

    private void Start()
    {
        // 绑定按钮事件
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // 查找企鹅控制器并订阅拍打事件
        penguinController = FindObjectOfType<FlyingPenguinController>(true);
        if (penguinController != null)
        {
            penguinController.Flapped += OnPenguinFlapped;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (penguinController != null)
        {
            penguinController.Flapped -= OnPenguinFlapped;
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonClicked);
        }
    }

    // ── Button：返回开始界面 ──────────────────────────────────────────────────
    private void OnBackButtonClicked()
    {
        FlowControlClean.Instance.ReturnToTitle();
    }

    // ── Hand Indicators：拍打旋转反馈 ────────────────────────────────────────
    private void OnPenguinFlapped(FlyingPenguinController.FlapSide side, float strength)
    {
        if (side == FlyingPenguinController.FlapSide.Left)
        {
            TriggerLeftFlapAnim();
        }
        else
        {
            TriggerRightFlapAnim();
        }
    }

    private void TriggerLeftFlapAnim()
    {
        if (leftHandIndicator == null) return;

        if (leftFlapCoroutine != null)
            StopCoroutine(leftFlapCoroutine);

        // 向右旋转 = 顺时针 = Z 轴减小
        leftFlapCoroutine = StartCoroutine(FlapRotateCoroutine(leftHandIndicator, leftBaseZ, leftBaseZ - flapAngle));
    }

    private void TriggerRightFlapAnim()
    {
        if (rightHandIndicator == null) return;

        if (rightFlapCoroutine != null)
            StopCoroutine(rightFlapCoroutine);

        // 向左旋转 = 逆时针 = Z 轴增大
        rightFlapCoroutine = StartCoroutine(FlapRotateCoroutine(rightHandIndicator, rightBaseZ, rightBaseZ + flapAngle));
    }

    /// <summary>
    /// 旋转动画协程：从 baseAngle 旋转到 targetAngle，再回到 baseAngle。
    /// 总时长为 flapAnimDuration（前半程旋转到目标，后半程回正）。
    /// </summary>
    private IEnumerator FlapRotateCoroutine(RectTransform target, float baseAngle, float targetAngle)
    {
        float halfDuration = flapAnimDuration * 0.5f;

        // 前半程：旋转到目标角度
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float angle = Mathf.LerpAngle(baseAngle, targetAngle, t);
            SetZRotation(target, angle);
            yield return null;
        }
        SetZRotation(target, targetAngle);

        // 后半程：回正到初始角度
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float angle = Mathf.LerpAngle(targetAngle, baseAngle, t);
            SetZRotation(target, angle);
            yield return null;
        }
        SetZRotation(target, baseAngle);
    }

    private static void SetZRotation(RectTransform target, float zAngle)
    {
        Vector3 euler = target.localEulerAngles;
        euler.z = zAngle;
        target.localEulerAngles = euler;
    }

    // ── Score：分数系统接口 ──────────────────────────────────────────────────
    /// <summary>
    /// 设置分数为指定值。
    /// </summary>
    public void SetScore(int score)
    {
        currentScore = score;
        UpdateScoreDisplay();
    }

    /// <summary>
    /// 在当前分数基础上增加指定数值。
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreDisplay();
    }

    /// <summary>
    /// 重置分数为 0。
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    // ── Hearts：生命值系统接口 ────────────────────────────────────────────────
    /// <summary>
    /// 减少一条生命。返回剩余生命值。
    /// </summary>
    public int LoseLife()
    {
        if (currentLives <= 0) return 0;

        currentLives--;
        UpdateHeartsDisplay();
        return currentLives;
    }

    /// <summary>
    /// 设置生命值为指定值（会自动 clamp 到 [0, maxLives]）。
    /// </summary>
    public void SetLives(int lives)
    {
        currentLives = Mathf.Clamp(lives, 0, maxLives);
        UpdateHeartsDisplay();
    }

    /// <summary>
    /// 重置生命值为最大值（恢复所有心）。
    /// </summary>
    public void ResetLives()
    {
        currentLives = maxLives;
        UpdateHeartsDisplay();
    }

    private void UpdateHeartsDisplay()
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] != null)
            {
                hearts[i].SetActive(i < currentLives);
            }
        }
    }

    // ── Shake：受击震动 ──────────────────────────────────────────────────────
    private Coroutine shakeCoroutine;

    /// <summary>
    /// 触发 HUD 面板震动。
    /// </summary>
    public void Shake(float duration = 0.3f, float magnitude = 12f)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 originalPos = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - (elapsed / duration); // 衰减
            float x = Random.Range(-1f, 1f) * magnitude * t;
            float y = Random.Range(-1f, 1f) * magnitude * t;
            rt.anchoredPosition = originalPos + new Vector2(x, y);
            yield return null;
        }

        rt.anchoredPosition = originalPos;
        shakeCoroutine = null;
    }
}
