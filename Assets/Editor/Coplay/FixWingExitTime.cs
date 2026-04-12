using UnityEngine;
using Coplay.Controllers.Functions;

public class FixWingExitTime
{
    public static string Execute()
    {
        string controllerPath = "Assets/Animation/FlyingPenguin.controller";

        string modifications = @"[
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Swing Left Wing"", ""destination_state"": ""Idle""},
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Swing Right Wing"", ""destination_state"": ""Idle""},
            {""type"": ""add_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Swing Left Wing"", ""destination_state"": ""Idle"", ""has_exit_time"": true, ""exit_time"": 0.9, ""transition_duration"": 0.1},
            {""type"": ""add_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Swing Right Wing"", ""destination_state"": ""Idle"", ""has_exit_time"": true, ""exit_time"": 0.9, ""transition_duration"": 0.1}
        ]";

        return CoplayTools.ModifyAnimatorController(controllerPath, modifications);
    }
}
