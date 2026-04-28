using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CreateProgressBar
{
    [MenuItem("Tools/Create Progress Bar")]
    public static void Execute()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found!"); return; }

        Transform hudPanel = canvas.transform.Find("HudPanel");
        if (hudPanel == null) { Debug.LogError("HudPanel not found!"); return; }

        // HudPanel 缩放 1.74，所以锚点范围需要缩小到 1/1.74 ≈ 0.575 才能刚好占满屏幕
        // 左侧进度条在屏幕坐标 2%-5%，对应 HudPanel 内部锚点需要额外偏移
        // 使用 HudPanel 的 anchorMin/Max (0,0)-(1,1) 加上 scale 1.74
        // 实际可见范围约为 HudPanel 锚点 0.12 ~ 0.88（中心向外 1/1.74）

        // 清理旧的
        Transform existing = hudPanel.Find("ProgressBar");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        // 移除旧的 ProgressBarManager
        ProgressBarManager oldPbm = hudPanel.GetComponent<ProgressBarManager>();
        if (oldPbm != null)
            Object.DestroyImmediate(oldPbm);

        // ── 在 Canvas 根层创建进度条（不受 HudPanel 缩放影响） ──
        Transform existingRoot = canvas.transform.Find("ProgressBarRoot");
        if (existingRoot != null)
            Object.DestroyImmediate(existingRoot.gameObject);

        GameObject progressRoot = new GameObject("ProgressBarRoot");
        progressRoot.transform.SetParent(canvas.transform, false);
        // 确保在 HudPanel 之上
        progressRoot.transform.SetSiblingIndex(canvas.transform.childCount - 1);

        RectTransform rootRt = progressRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        // ── 进度条容器（左侧竖条） ──
        GameObject progressBar = new GameObject("ProgressBar");
        progressBar.transform.SetParent(progressRoot.transform, false);

        RectTransform barRt = progressBar.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.015f, 0.15f);
        barRt.anchorMax = new Vector2(0.035f, 0.85f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = Vector2.zero;

        // ── 背景（深色圆角） ──
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(progressBar.transform, false);

        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = Vector2.zero;

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bgImage.type = Image.Type.Sliced;

        // ── 填充条 ──
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(progressBar.transform, false);

        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0.1f, 0.02f);
        fillRt.anchorMax = new Vector2(0.9f, 0.98f);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.75f, 1f, 0.95f); // 冰蓝色
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = 0; // Bottom
        fillImage.fillAmount = 0.3f; // 预览用

        // ── 顶部标靶标记 ──
        GameObject targetMark = new GameObject("TargetMark");
        targetMark.transform.SetParent(progressBar.transform, false);

        RectTransform targetRt = targetMark.AddComponent<RectTransform>();
        targetRt.anchorMin = new Vector2(-1f, 0.98f);
        targetRt.anchorMax = new Vector2(2f, 1.06f);
        targetRt.anchoredPosition = Vector2.zero;
        targetRt.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI targetText = targetMark.AddComponent<TMPro.TextMeshProUGUI>();
        targetText.text = "◎";
        targetText.fontSize = 18;
        targetText.color = new Color(1f, 0.3f, 0.3f, 1f);
        targetText.alignment = TMPro.TextAlignmentOptions.Center;
        targetText.enableAutoSizing = true;
        targetText.fontSizeMin = 8;
        targetText.fontSizeMax = 18;

        // ── 底部起点标记 ──
        GameObject startMark = new GameObject("StartMark");
        startMark.transform.SetParent(progressBar.transform, false);

        RectTransform startRt = startMark.AddComponent<RectTransform>();
        startRt.anchorMin = new Vector2(-1f, -0.06f);
        startRt.anchorMax = new Vector2(2f, 0.02f);
        startRt.anchoredPosition = Vector2.zero;
        startRt.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI startText = startMark.AddComponent<TMPro.TextMeshProUGUI>();
        startText.text = "▲";
        startText.fontSize = 14;
        startText.color = Color.white;
        startText.alignment = TMPro.TextAlignmentOptions.Center;
        startText.enableAutoSizing = true;
        startText.fontSizeMin = 6;
        startText.fontSizeMax = 14;

        // ── 挂载 ProgressBarManager 到 ProgressBarRoot ──
        ProgressBarManager pbm = progressRoot.GetComponent<ProgressBarManager>();
        if (pbm == null)
            pbm = progressRoot.AddComponent<ProgressBarManager>();

        SerializedObject so = new SerializedObject(pbm);
        so.FindProperty("fillImage").objectReferenceValue = fillImage;
        so.FindProperty("totalDistance").floatValue = 15000f;
        so.ApplyModifiedProperties();

        // ── ProgressBarRoot 需要跟随 HudPanel 的激活状态 ──
        // 最简单的方法是让它始终显示，由脚本控制
        // 或者我们可以把显隐逻辑放在 ProgressBarManager 里

        EditorUtility.SetDirty(progressRoot);

        // 保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("Progress bar created at Canvas root (scale-independent)!");
    }
}
