using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CleanBaseLayer
{
    public static string Execute()
    {
        string controllerPath = "Assets/Animation/FlyingPenguin.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) return "Error: Controller not found";

        var baseLayer = controller.layers[0];
        var stateMachine = baseLayer.stateMachine;

        // Remove the old wing states from Base Layer if they still exist
        var states = stateMachine.states;
        foreach (var state in states)
        {
            if (state.state.name == "Swing Left Wing" || state.state.name == "Swing Right Wing")
            {
                stateMachine.RemoveState(state.state);
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return "Cleaned up Base Layer.";
    }
}
