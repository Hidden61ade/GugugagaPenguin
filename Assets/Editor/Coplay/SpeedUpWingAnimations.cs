using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SpeedUpWingAnimations
{
    public static string Execute()
    {
        string result = "";
        string controllerPath = "Assets/Animation/FlyingPenguin.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        
        if (controller == null) return "Error: Controller not found";

        // The original animation is ~0.96s long.
        // To make it finish in 0.25s, we need a speed multiplier of 0.96 / 0.25 = 3.84
        float targetSpeed = 3.84f;

        foreach (var layer in controller.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.name == "Swing Left Wing" || state.state.name == "Swing Right Wing")
                {
                    state.state.speed = targetSpeed;
                    result += $"Set speed of '{state.state.name}' to {targetSpeed}\n";
                }
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return result + "Wing animations sped up successfully.";
    }
}
