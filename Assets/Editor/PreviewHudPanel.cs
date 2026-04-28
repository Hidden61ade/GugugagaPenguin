using UnityEditor;
using UnityEngine;

public class PreviewHudPanel
{
    [MenuItem("Tools/Preview HudPanel")]
    public static void Execute()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) return;
        Transform hp = canvas.transform.Find("HudPanel");
        if (hp != null) { hp.gameObject.SetActive(true); Debug.Log("HudPanel activated"); }
        // Hide others
        Transform tp = canvas.transform.Find("TitlePanel");
        if (tp != null) tp.gameObject.SetActive(false);
        Transform vp = canvas.transform.Find("VictoryPanel");
        if (vp != null) vp.gameObject.SetActive(false);
        Transform pp = canvas.transform.Find("PausePanel");
        if (pp != null) pp.gameObject.SetActive(false);
    }

    [MenuItem("Tools/Hide HudPanel")]
    public static void Hide()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) return;
        Transform hp = canvas.transform.Find("HudPanel");
        if (hp != null) { hp.gameObject.SetActive(false); Debug.Log("HudPanel hidden"); }
    }
}
