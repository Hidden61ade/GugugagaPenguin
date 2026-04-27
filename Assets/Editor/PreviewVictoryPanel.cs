using UnityEditor;
using UnityEngine;

public class PreviewVictoryPanel
{
    [MenuItem("Tools/Preview Victory Panel")]
    public static void Execute()
    {
        // 临时激活 VictoryPanel 预览
        GameObject vp = GameObject.Find("Canvas")?.transform.Find("VictoryPanel")?.gameObject;
        if (vp == null)
        {
            // 尝试查找非激活的
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform t = canvas.transform.Find("VictoryPanel");
                if (t != null) vp = t.gameObject;
            }
        }

        if (vp != null)
        {
            vp.SetActive(true);

            // 模拟填充数据
            VictoryManager vm = vp.GetComponent<VictoryManager>();
            if (vm != null)
            {
                vm.ShowVictory();
            }

            EditorUtility.SetDirty(vp);
            Debug.Log("VictoryPanel activated for preview");
        }
        else
        {
            Debug.LogError("VictoryPanel not found!");
        }
    }

    [MenuItem("Tools/Hide Victory Panel")]
    public static void Hide()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform t = canvas.transform.Find("VictoryPanel");
            if (t != null)
            {
                t.gameObject.SetActive(false);
                EditorUtility.SetDirty(t.gameObject);
                Debug.Log("VictoryPanel hidden");
            }
        }
    }
}
