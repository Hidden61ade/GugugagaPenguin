using UnityEngine;
using UnityEditor;

public class InspectAnimClips
{
    public static string Execute()
    {
        string result = "";
        string fbxPath = "Assets/Mesh/FlyingPenguin.fbx";

        // Load all sub-assets from the FBX
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        result += $"Total sub-assets in FBX: {allAssets.Length}\n\n";

        foreach (var asset in allAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                result += $"=== Clip: {clip.name} ===\n";
                result += $"  Length: {clip.length}s, FrameRate: {clip.frameRate}\n";
                result += $"  WrapMode: {clip.wrapMode}\n";
                result += $"  Legacy: {clip.legacy}\n";
                result += $"  IsLooping: {clip.isLooping}\n";

                // Get all curve bindings
                var bindings = AnimationUtility.GetCurveBindings(clip);
                result += $"  Curve bindings: {bindings.Length}\n";

                foreach (var binding in bindings)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve != null && curve.keys.Length > 0)
                    {
                        float minVal = float.MaxValue, maxVal = float.MinValue;
                        foreach (var key in curve.keys)
                        {
                            if (key.value < minVal) minVal = key.value;
                            if (key.value > maxVal) maxVal = key.value;
                        }
                        float range = maxVal - minVal;
                        // Only show curves with actual movement
                        if (range > 0.001f)
                        {
                            result += $"    {binding.path}/{binding.propertyName}: keys={curve.keys.Length}, range=[{minVal:F4} ~ {maxVal:F4}], delta={range:F4}\n";
                        }
                    }
                }
                result += "\n";
            }
        }

        return result;
    }
}
