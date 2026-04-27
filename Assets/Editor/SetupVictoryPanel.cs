using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetupVictoryPanel
{
    [MenuItem("Tools/Setup Victory Panel")]
    public static void Execute()
    {
        // 找到 VictoryPanel
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found!"); return; }

        Transform vpTransform = canvas.transform.Find("VictoryPanel");
        if (vpTransform == null) { Debug.LogError("VictoryPanel not found!"); return; }

        GameObject victoryPanel = vpTransform.gameObject;

        // 设置背景为半透明黑色
        Image bg = victoryPanel.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0f, 0f, 0f, 0.85f);
        }

        // ── 创建得分摘要文本 ──
        Transform existingText = vpTransform.Find("ScoreSummaryText");
        GameObject textObj;
        if (existingText != null)
        {
            textObj = existingText.gameObject;
        }
        else
        {
            textObj = new GameObject("ScoreSummaryText");
            textObj.transform.SetParent(vpTransform, false);
        }

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = textObj.AddComponent<TextMeshProUGUI>();

        tmp.text = "<size=42><b>GAME OVER</b></size>\n\nFinal score will appear here.";
        tmp.color = Color.white;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.1f, 0.3f);
        textRt.anchorMax = new Vector2(0.9f, 0.9f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = Vector2.zero;

        // ── 创建返回按钮 ──
        Transform existingBtn = vpTransform.Find("ReturnButton");
        GameObject btnObj;
        if (existingBtn != null)
        {
            btnObj = existingBtn.gameObject;
        }
        else
        {
            btnObj = new GameObject("ReturnButton");
            btnObj.transform.SetParent(vpTransform, false);
        }

        Image btnImage = btnObj.GetComponent<Image>();
        if (btnImage == null)
            btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.6f, 1f, 1f);

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null)
            btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.3f, 0.08f);
        btnRt.anchorMax = new Vector2(0.7f, 0.18f);
        btnRt.anchoredPosition = Vector2.zero;
        btnRt.sizeDelta = Vector2.zero;

        // 按钮文字
        Transform existingBtnText = btnObj.transform.Find("Text");
        GameObject btnTextObj;
        if (existingBtnText != null)
        {
            btnTextObj = existingBtnText.gameObject;
        }
        else
        {
            btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
        }

        TextMeshProUGUI btnTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
        if (btnTmp == null)
            btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "RETURN TO TITLE";
        btnTmp.color = Color.white;
        btnTmp.fontSize = 24;
        btnTmp.alignment = TextAlignmentOptions.Center;

        RectTransform btnTextRt = btnTextObj.GetComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.anchoredPosition = Vector2.zero;
        btnTextRt.sizeDelta = Vector2.zero;

        // ── 删除旧的 LeftHandIndicator（VictoryPanel 里的残留） ──
        Transform oldIndicator = vpTransform.Find("LeftHandIndicator");
        if (oldIndicator != null)
            Object.DestroyImmediate(oldIndicator.gameObject);

        // ── 挂载 VictoryManager 脚本 ──
        VictoryManager vm = victoryPanel.GetComponent<VictoryManager>();
        if (vm == null)
            vm = victoryPanel.AddComponent<VictoryManager>();

        // 通过 SerializedObject 赋值引用
        SerializedObject so = new SerializedObject(vm);
        so.FindProperty("scoreSummaryText").objectReferenceValue = tmp;
        so.FindProperty("returnButton").objectReferenceValue = btn;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(victoryPanel);

        // 保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("VictoryPanel setup complete!");
    }
}
