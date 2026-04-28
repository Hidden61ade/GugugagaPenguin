using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VictoryPanel 管理器：显示最终得分，按钮直接重启游戏。
/// </summary>
public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreSummaryText;
    [SerializeField] private Button returnButton;

    private static int dartBonus = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OnRestartClicked);
        }
    }

    private void OnDisable()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveAllListeners();
    }

    public static void SetDartBonus(int bonus) => dartBonus = bonus;
    public static void ResetDartBonus() => dartBonus = 0;

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

        gameObject.SetActive(true);
    }

    private void OnRestartClicked()
    {
        ResetDartBonus();
        FlowControlClean.RestartGame();
    }
}
