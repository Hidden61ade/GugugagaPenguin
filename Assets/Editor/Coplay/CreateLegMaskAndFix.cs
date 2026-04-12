using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateLegMaskAndFix
{
    public static string Execute()
    {
        string result = "";

        // 1. Get all transforms from the FlyingPenguin model
        string fbxPath = "Assets/Mesh/FlyingPenguin.fbx";
        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
            return "Error: FBX not found!";

        var allTransforms = fbxAsset.GetComponentsInChildren<Transform>(true);
        result += $"Total transforms in FBX: {allTransforms.Length}\n";

        // Leg-related transform path keywords
        string[] legKeywords = new string[] {
            "Leg01.L", "Leg01.R", "Leg02.L", "Leg02.R",
            "Feet.L", "Feet.R",
            "LegIK.L", "LegIK.R",
            "IKPole.L", "IKPole.R"
        };

        // 2. Create AvatarMask
        var mask = new AvatarMask();
        mask.transformCount = allTransforms.Length;

        for (int i = 0; i < allTransforms.Length; i++)
        {
            string path = GetRelativePath(fbxAsset.transform, allTransforms[i]);
            mask.SetTransformPath(i, path);

            // Check if this is a leg bone
            bool isLeg = false;
            foreach (var keyword in legKeywords)
            {
                if (path.Contains(keyword))
                {
                    isLeg = true;
                    break;
                }
            }

            // Also enable the root so the mask works
            if (string.IsNullOrEmpty(path))
                isLeg = true;

            mask.SetTransformActive(i, isLeg);

            if (isLeg && !string.IsNullOrEmpty(path))
                result += $"  Leg bone enabled: {path}\n";
        }

        // Save the mask asset
        string maskPath = "Assets/Animation/LegMask.mask";
        AssetDatabase.CreateAsset(mask, maskPath);
        AssetDatabase.SaveAssets();
        result += $"Created AvatarMask at {maskPath}\n";

        // 3. Assign mask to Leg Layer
        string controllerPath = "Assets/Animation/FlyingPenguin.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
        {
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "Leg Layer")
                {
                    layers[i].avatarMask = mask;
                    result += "Assigned LegMask to Leg Layer.\n";
                }
            }
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        result += "\nDone! Leg Layer now only affects leg bones, wing animations will play fully.";
        return result;
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
