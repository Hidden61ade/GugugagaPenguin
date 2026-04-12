using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Coplay.Controllers.Functions;

public class SetupFlyingPenguin
{
    public static string Execute()
    {
        string result = "";
        string controllerPath = "Assets/Animation/FlyingPenguin.controller";
        string legMotionPath = "Assets/Mesh/FlyingPenguin.fbx::Armature|legAnimation";

        // =============================================
        // 1. Modify Animator Controller
        // =============================================
        string modifications = @"[
            {""type"": ""add_parameter"", ""parameter_name"": ""SwingLeft"", ""parameter_type"": ""Trigger""},
            {""type"": ""add_parameter"", ""parameter_name"": ""SwingRight"", ""parameter_type"": ""Trigger""},
            {""type"": ""add_parameter"", ""parameter_name"": ""SwingLeg"", ""parameter_type"": ""Trigger""},

            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Idle"", ""destination_state"": ""Swing Left Wing""},
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Idle"", ""destination_state"": ""Swing Right Wing""},
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Idle"", ""destination_state"": ""Swing Leg""},
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Swing Leg"", ""destination_state"": ""Idle""},
            {""type"": ""remove_state"", ""layer_name"": ""Base Layer"", ""state_name"": ""Swing Leg""},

            {""type"": ""add_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Idle"", ""destination_state"": ""Swing Left Wing"", ""has_exit_time"": false, ""transition_duration"": 0.1, ""conditions"": [{""parameter"": ""SwingLeft"", ""mode"": ""If"", ""threshold"": 0}]},
            {""type"": ""add_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Idle"", ""destination_state"": ""Swing Right Wing"", ""has_exit_time"": false, ""transition_duration"": 0.1, ""conditions"": [{""parameter"": ""SwingRight"", ""mode"": ""If"", ""threshold"": 0}]},

            {""type"": ""add_layer"", ""layer_name"": ""Leg Layer""},
            {""type"": ""add_state"", ""layer_name"": ""Leg Layer"", ""state_name"": ""Leg Idle""},
            {""type"": ""add_state"", ""layer_name"": ""Leg Layer"", ""state_name"": ""Swing Leg"", ""motion_clip_path"": """ + legMotionPath + @"""},
            {""type"": ""set_default_state"", ""layer_name"": ""Leg Layer"", ""state_name"": ""Leg Idle""},
            {""type"": ""add_transition"", ""layer_name"": ""Leg Layer"", ""source_state"": ""Leg Idle"", ""destination_state"": ""Swing Leg"", ""has_exit_time"": false, ""transition_duration"": 0.1, ""conditions"": [{""parameter"": ""SwingLeg"", ""mode"": ""If"", ""threshold"": 0}]},
            {""type"": ""add_transition"", ""layer_name"": ""Leg Layer"", ""source_state"": ""Swing Leg"", ""destination_state"": ""Leg Idle"", ""has_exit_time"": true, ""exit_time"": 0.9, ""transition_duration"": 0.1}
        ]";

        result += "Animator: " + CoplayTools.ModifyAnimatorController(controllerPath, modifications) + "\n";

        // =============================================
        // 2. Set Leg Layer weight to 1 (so it actually plays)
        // =============================================
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
        {
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "Leg Layer")
                {
                    layers[i].defaultWeight = 1f;
                    result += "Set Leg Layer weight to 1.\n";
                }
            }
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        // =============================================
        // 3. Add FlyingPenguinController to FlyingPenguin
        // =============================================
        var flyingPenguin = GameObject.Find("FlyingPenguin");
        if (flyingPenguin == null)
            return result + "Error: FlyingPenguin not found!";

        var fpc = flyingPenguin.GetComponent<FlyingPenguinController>();
        if (fpc == null)
        {
            fpc = flyingPenguin.AddComponent<FlyingPenguinController>();
            result += "Added FlyingPenguinController to FlyingPenguin.\n";
        }

        var keyboardInput = flyingPenguin.GetComponent<KeyboardWingInputController>();
        if (keyboardInput == null)
        {
            keyboardInput = flyingPenguin.AddComponent<KeyboardWingInputController>();
            result += "Added KeyboardWingInputController to FlyingPenguin.\n";
        }

        var leapInput = flyingPenguin.GetComponent<LeapWingInputController>();
        if (leapInput == null)
        {
            leapInput = flyingPenguin.AddComponent<LeapWingInputController>();
            result += "Added LeapWingInputController to FlyingPenguin.\n";
        }
        EditorUtility.SetDirty(flyingPenguin);

        // =============================================
        // 4. Update ThirdPersonCamera to follow FlyingPenguin
        // =============================================
        var mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
        {
            var tpc = mainCamera.GetComponent<ThirdPersonCamera>();
            if (tpc == null)
            {
                tpc = mainCamera.AddComponent<ThirdPersonCamera>();
                result += "Added ThirdPersonCamera to Main Camera.\n";
            }

            tpc.target = flyingPenguin.transform;
            EditorUtility.SetDirty(mainCamera);
            result += "Set ThirdPersonCamera target to FlyingPenguin.\n";
        }

        // =============================================
        // 5. Save
        // =============================================
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        result += "Scene saved.\n";

        return result;
    }
}
