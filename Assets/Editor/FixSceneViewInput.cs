using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 修复 Input System 导致 Scene View WASD 飞行失效的问题。
/// 在编辑器加载时自动设置 Input System 的 backgroundBehavior。
/// </summary>
[InitializeOnLoad]
public static class FixSceneViewInput
{
    static FixSceneViewInput()
    {
        // 让 Input System 在编辑器失焦时不重置输入状态
        // 这可以防止它抢占 Scene View 的键盘事件
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Debug.Log("[FixSceneViewInput] Input System background behavior set to IgnoreFocus");
    }
}
