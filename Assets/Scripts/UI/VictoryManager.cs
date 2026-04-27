using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VictoryPanel 管理器：显示最终得分公式并提供返回标题按钮。
/// 挂载在 Canvas/VictoryPanel 上。
/// </summary>
public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreSummaryText;
    [SerializeField] private Button returnButton;

    // ── 得分追踪（静态，跨 panel 激活保留） ──
    private static int dartBonus = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (returnButton != null)
            returnButton.onClick.AddListener(OnReturnClicked);
    }

    private void OnDisable()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveListener(OnReturnClicked);
    }

    /// <summary>
    /// 记录标靶得分（由 DartTarget 调用）。
    /// </summary>
    public static void SetDartBonus(int bonus)
    {
        dartBonus = bonus;
    }

    public static void ResetDartBonus()
    {
        dartBonus = 0;
    }

    /// <summary>
    /// 显示最终结算面板。由外部调用。
    /// </summary>
    public void ShowVictory()
    {
        int coinScore = HudManager.Instance != null ? HudManager.Instance.CurrentScore : 0;
        int lives = HudManager.Instance != null ? HudManager.Instance.CurrentLives : 0;
        int lifeBonus = lives * 50;
        int finalScore = coinScore + lifeBonus + dartBonus;

        if (scoreSummaryText != null)
        {
            scoreSummaryText.text =
                $"<size=42><b>GAME OVER</b></size>\n\n" +
                $"<size=28>Coin Score:    {coinScore}</size>\n" +
                $"<size=28>Life Bonus:    {lives} × 50 = {lifeBonus}</size>\n" +
                $"<size=28>Dart Bonus:    {dartBonus}</size>\n\n" +
                $"<size=24>───────────────────</size>\n" +
                $"<size=36><b>Final Score = {coinScore} + {lifeBonus} + {dartBonus} = {finalScore}</b></size>";
        }

        // 确保面板激活
        gameObject.SetActive(true);
    }

    private void OnReturnClicked()
    {
        ResetDartBonus();
        FlowControlClean.Instance.ReturnToTitle();
    }
}
