using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器工具：将运行时生成的UI保存为预制体
/// 使用方法：进入Play模式运行游戏后，从菜单选择 Tools > Save Runtime UI
/// </summary>
public static class SaveRuntimeUiTool
{
    [MenuItem("Tools/Save Runtime UI")]
    public static void SaveRuntimeUi()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("提示", "请先进入Play模式运行游戏，让UI生成出来", "确定");
            return;
        }

        // 查找运行时生成的UI根对象
        GameObject penguinGameFlow = GameObject.Find("PenguinGameFlow");
        if (penguinGameFlow == null)
        {
            EditorUtility.DisplayDialog("提示", "未找到 PenguinGameFlow 对象", "确定");
            return;
        }

        // 创建保存目录
        string prefabPath = "Assets/Prefabs/RuntimeGenerated/";
        if (!System.IO.Directory.Exists(prefabPath))
        {
            System.IO.Directory.CreateDirectory(prefabPath);
        }

        // 保存 PenguinGameFlow 为预制体
        string flowPrefabPath = prefabPath + "PenguinGameFlow.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(penguinGameFlow, flowPrefabPath, InteractionMode.UserAction);

        if (prefab != null)
        {
            EditorUtility.DisplayDialog("成功", $"UI已保存到:\n{flowPrefabPath}", "确定");
            Selection.activeObject = prefab;
        }
        else
        {
            EditorUtility.DisplayDialog("失败", "保存预制体失败", "确定");
        }
    }

    [MenuItem("Tools/Save Runtime UI", true)]
    public static bool ValidateSaveRuntimeUi()
    {
        return Application.isPlaying;
    }

    [MenuItem("Tools/Save Finish Gate")]
    public static void SaveFinishGate()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("提示", "请先进入Play模式运行游戏", "确定");
            return;
        }

        GameObject finishGate = GameObject.Find("RuntimeFinishGate");
        if (finishGate == null)
        {
            EditorUtility.DisplayDialog("提示", "未找到 RuntimeFinishGate 对象", "确定");
            return;
        }

        string prefabPath = "Assets/Prefabs/RuntimeGenerated/";
        if (!System.IO.Directory.Exists(prefabPath))
        {
            System.IO.Directory.CreateDirectory(prefabPath);
        }

        string gatePrefabPath = prefabPath + "FinishGate.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(finishGate, gatePrefabPath, InteractionMode.UserAction);

        if (prefab != null)
        {
            EditorUtility.DisplayDialog("成功", $"终点门已保存到:\n{gatePrefabPath}", "确定");
            Selection.activeObject = prefab;
        }
        else
        {
            EditorUtility.DisplayDialog("失败", "保存预制体失败", "确定");
        }
    }

    [MenuItem("Tools/Save Finish Gate", true)]
    public static bool ValidateSaveFinishGate()
    {
        return Application.isPlaying;
    }
}