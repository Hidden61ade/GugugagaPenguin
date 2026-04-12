using System;
using UnityEngine;
using UnityEditor;
using Coplay.Controllers.Functions;

public class SetupPenguinController
{
    public static string Execute()
    {
        string result = "";

        // =============================================
        // 1. Modify Animator Controller
        // =============================================
        string controllerPath = "Assets/Animation/Penguin.controller";
        string motionPath = "Assets/Mesh/Penguin.fbx::Armature|ArmatureAction";

        // Add Speed parameter, add Idle state, set transitions
        string modifications = @"[
            {""type"": ""add_parameter"", ""parameter_name"": ""Speed"", ""parameter_type"": ""Float"", ""default_value"": 0},
            {""type"": ""add_state"", ""layer_name"": ""Base Layer"", ""state_name"": ""Idle""},
            {""type"": ""set_default_state"", ""layer_name"": ""Base Layer"", ""state_name"": ""Idle""},
            {""type"": ""set_state_motion"", ""layer_name"": ""Base Layer"", ""state_name"": ""run"", ""motion_clip_path"": """ + motionPath + @"""},
            {""type"": ""add_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Idle"", ""destination_state"": ""run"", ""has_exit_time"": false, ""transition_duration"": 0.15, ""conditions"": [{""parameter"": ""Speed"", ""mode"": ""Greater"", ""threshold"": 0.1}]},
            {""type"": ""add_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""run"", ""destination_state"": ""Idle"", ""has_exit_time"": false, ""transition_duration"": 0.15, ""conditions"": [{""parameter"": ""Speed"", ""mode"": ""Less"", ""threshold"": 0.1}]}
        ]";

        result += "Animator: " + CoplayTools.ModifyAnimatorController(controllerPath, modifications) + "\n";

        // =============================================
        // 2. Add CharacterController to Penguin
        // =============================================
        // Penguin scale is 4.33. The model is about 1.45 local units tall.
        // CharacterController values are in local space, scaled by transform.
        result += "AddCC: " + CoplayTools.AddComponent("Penguin", "CharacterController") + "\n";

        // Set CharacterController properties
        // Penguin world height ~6.3, scale 4.33 -> local height ~1.45
        // center (0, 0.72, 0), radius ~0.3, height ~1.45
        // slopeLimit and stepOffset for navigating ramps
        result += CoplayTools.SetProperty("Penguin", "CharacterController", "center", "0,0.72,0") + "\n";
        result += CoplayTools.SetProperty("Penguin", "CharacterController", "height", "1.45") + "\n";
        result += CoplayTools.SetProperty("Penguin", "CharacterController", "radius", "0.35") + "\n";
        result += CoplayTools.SetProperty("Penguin", "CharacterController", "slopeLimit", "45") + "\n";
        result += CoplayTools.SetProperty("Penguin", "CharacterController", "stepOffset", "0.5") + "\n";

        // =============================================
        // 3. Add PenguinController script to Penguin
        // =============================================
        result += "AddPC: " + CoplayTools.AddComponent("Penguin", "PenguinController") + "\n";
        result += CoplayTools.SetProperty("Penguin", "PenguinController", "moveSpeed", "8") + "\n";
        result += CoplayTools.SetProperty("Penguin", "PenguinController", "cameraTransform", "Main Camera") + "\n";

        // =============================================
        // 4. Add ThirdPersonCamera script to Main Camera
        // =============================================
        result += "AddCam: " + CoplayTools.AddComponent("Main Camera", "ThirdPersonCamera") + "\n";
        result += CoplayTools.SetProperty("Main Camera", "ThirdPersonCamera", "target", "Penguin") + "\n";
        result += CoplayTools.SetProperty("Main Camera", "ThirdPersonCamera", "distance", "12") + "\n";
        result += CoplayTools.SetProperty("Main Camera", "ThirdPersonCamera", "heightOffset", "4") + "\n";
        result += CoplayTools.SetProperty("Main Camera", "ThirdPersonCamera", "mouseSensitivity", "3") + "\n";

        // =============================================
        // 5. Set Penguin tag to Player
        // =============================================
        var penguin = GameObject.Find("Penguin");
        if (penguin != null)
        {
            penguin.tag = "Player";
            EditorUtility.SetDirty(penguin);
        }

        // =============================================
        // 6. Save scene
        // =============================================
        result += "Save: " + CoplayTools.SaveScene("SampleScene") + "\n";

        return result;
    }
}
