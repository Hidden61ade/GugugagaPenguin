using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Coplay.Controllers.Functions;

public class SetupIndependentWings
{
    public static string Execute()
    {
        string result = "";
        string fbxPath = "Assets/Mesh/FlyingPenguin.fbx";
        string controllerPath = "Assets/Animation/FlyingPenguin.controller";

        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) return "Error: FBX not found!";

        var allTransforms = fbxAsset.GetComponentsInChildren<Transform>(true);

        // =============================================
        // 1. Create Left Wing Mask
        // =============================================
        string[] leftWingKeywords = { "chest.L", "Arm02.L", "Arm01.L", "Hand.L" };
        var leftMask = CreateMask(allTransforms, fbxAsset.transform, leftWingKeywords);
        string leftMaskPath = "Assets/Animation/LeftWingMask.mask";
        AssetDatabase.CreateAsset(leftMask, leftMaskPath);
        result += "Created LeftWingMask.\n";

        // =============================================
        // 2. Create Right Wing Mask
        // =============================================
        string[] rightWingKeywords = { "chest.R", "Arm02.R", "Arm01.R", "Hand.R" };
        var rightMask = CreateMask(allTransforms, fbxAsset.transform, rightWingKeywords);
        string rightMaskPath = "Assets/Animation/RightWingMask.mask";
        AssetDatabase.CreateAsset(rightMask, rightMaskPath);
        result += "Created RightWingMask.\n";

        AssetDatabase.SaveAssets();

        // =============================================
        // 3. Modify Animator Controller
        // =============================================
        // Remove wing states from Base Layer
        string modifications = @"[
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Idle"", ""destination_state"": ""Swing Left Wing""},
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Idle"", ""destination_state"": ""Swing Right Wing""},
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Swing Left Wing"", ""destination_state"": ""Idle""},
            {""type"": ""remove_transition"", ""layer_name"": ""Base Layer"", ""source_state"": ""Swing Right Wing"", ""destination_state"": ""Idle""},
            {""type"": ""remove_state"", ""layer_name"": ""Base Layer"", ""state_name"": ""Swing Left Wing""},
            {""type"": ""remove_state"", ""layer_name"": ""Base Layer"", ""state_name"": ""Swing Right Wing""},

            {""type"": ""add_layer"", ""layer_name"": ""Left Wing Layer""},
            {""type"": ""add_state"", ""layer_name"": ""Left Wing Layer"", ""state_name"": ""Wing Idle""},
            {""type"": ""add_state"", ""layer_name"": ""Left Wing Layer"", ""state_name"": ""Swing Left Wing"", ""motion_clip_path"": ""Assets/Mesh/FlyingPenguin.fbx::Armature|leftWingAnimation""},
            {""type"": ""set_default_state"", ""layer_name"": ""Left Wing Layer"", ""state_name"": ""Wing Idle""},
            {""type"": ""add_transition"", ""layer_name"": ""Left Wing Layer"", ""source_state"": ""Wing Idle"", ""destination_state"": ""Swing Left Wing"", ""has_exit_time"": false, ""transition_duration"": 0.1, ""conditions"": [{""parameter"": ""SwingLeft"", ""mode"": ""If"", ""threshold"": 0}]},
            {""type"": ""add_transition"", ""layer_name"": ""Left Wing Layer"", ""source_state"": ""Swing Left Wing"", ""destination_state"": ""Wing Idle"", ""has_exit_time"": true, ""exit_time"": 0.9, ""transition_duration"": 0.1},

            {""type"": ""add_layer"", ""layer_name"": ""Right Wing Layer""},
            {""type"": ""add_state"", ""layer_name"": ""Right Wing Layer"", ""state_name"": ""Wing Idle""},
            {""type"": ""add_state"", ""layer_name"": ""Right Wing Layer"", ""state_name"": ""Swing Right Wing"", ""motion_clip_path"": ""Assets/Mesh/FlyingPenguin.fbx::Armature|rightWingAnimation""},
            {""type"": ""set_default_state"", ""layer_name"": ""Right Wing Layer"", ""state_name"": ""Wing Idle""},
            {""type"": ""add_transition"", ""layer_name"": ""Right Wing Layer"", ""source_state"": ""Wing Idle"", ""destination_state"": ""Swing Right Wing"", ""has_exit_time"": false, ""transition_duration"": 0.1, ""conditions"": [{""parameter"": ""SwingRight"", ""mode"": ""If"", ""threshold"": 0}]},
            {""type"": ""add_transition"", ""layer_name"": ""Right Wing Layer"", ""source_state"": ""Swing Right Wing"", ""destination_state"": ""Wing Idle"", ""has_exit_time"": true, ""exit_time"": 0.9, ""transition_duration"": 0.1}
        ]";

        result += CoplayTools.ModifyAnimatorController(controllerPath, modifications) + "\n";

        // =============================================
        // 4. Set layer weights and masks
        // =============================================
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
        {
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "Left Wing Layer")
                {
                    layers[i].defaultWeight = 1f;
                    layers[i].avatarMask = leftMask;
                    result += "Left Wing Layer: weight=1, mask=LeftWingMask\n";
                }
                else if (layers[i].name == "Right Wing Layer")
                {
                    layers[i].defaultWeight = 1f;
                    layers[i].avatarMask = rightMask;
                    result += "Right Wing Layer: weight=1, mask=RightWingMask\n";
                }
            }
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        result += "\nDone! Left and right wings can now play simultaneously.";
        return result;
    }

    static AvatarMask CreateMask(Transform[] allTransforms, Transform root, string[] keywords)
    {
        var mask = new AvatarMask();
        mask.transformCount = allTransforms.Length;

        for (int i = 0; i < allTransforms.Length; i++)
        {
            string path = GetRelativePath(root, allTransforms[i]);
            mask.SetTransformPath(i, path);

            bool include = false;
            if (string.IsNullOrEmpty(path))
            {
                include = true; // root
            }
            else
            {
                foreach (var kw in keywords)
                {
                    if (path.Contains(kw))
                    {
                        include = true;
                        break;
                    }
                }
            }
            mask.SetTransformActive(i, include);
        }
        return mask;
    }

    static string GetRelativePath(Transform root, Transform target)
    {
        if (target == root) return "";
        string path = target.name;
        Transform current = target.parent;
        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
