using UnityEngine;
using UnityEditor;

public class AttachScripts
{
    public static string Execute()
    {
        string result = "";

        // Find the Penguin
        var penguin = GameObject.Find("Penguin");
        if (penguin == null)
            return "Error: Penguin not found in scene.";

        // Find Main Camera
        var mainCamera = GameObject.Find("Main Camera");
        if (mainCamera == null)
            return "Error: Main Camera not found in scene.";

        // Add PenguinController to Penguin (if not already added)
        var pc = penguin.GetComponent<PenguinController>();
        if (pc == null)
        {
            pc = penguin.AddComponent<PenguinController>();
            result += "Added PenguinController to Penguin.\n";
        }
        else
        {
            result += "PenguinController already exists on Penguin.\n";
        }

        // Configure PenguinController
        pc.moveSpeed = 8f;
        pc.turnSmoothTime = 0.1f;
        pc.gravity = -20f;
        pc.cameraTransform = mainCamera.transform;
        EditorUtility.SetDirty(penguin);
        result += "Configured PenguinController.\n";

        // Add ThirdPersonCamera to Main Camera (if not already added)
        var cam = mainCamera.GetComponent<ThirdPersonCamera>();
        if (cam == null)
        {
            cam = mainCamera.AddComponent<ThirdPersonCamera>();
            result += "Added ThirdPersonCamera to Main Camera.\n";
        }
        else
        {
            result += "ThirdPersonCamera already exists on Main Camera.\n";
        }

        // Configure ThirdPersonCamera
        cam.target = penguin.transform;
        EditorUtility.SetDirty(mainCamera);
        result += "Configured ThirdPersonCamera.\n";

        // Set Penguin tag to Player
        penguin.tag = "Player";
        EditorUtility.SetDirty(penguin);
        result += "Set Penguin tag to Player.\n";

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        result += "Scene saved.\n";

        return result;
    }
}
