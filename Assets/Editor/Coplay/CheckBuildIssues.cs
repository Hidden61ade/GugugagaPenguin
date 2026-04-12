using UnityEngine;
using UnityEditor;
using System.Linq;

public class CheckBuildIssues
{
    public static string Execute()
    {
        string result = "";

        // 1. Check Build Settings - Scenes
        var scenes = EditorBuildSettings.scenes;
        result += $"=== Build Scenes ({scenes.Length}) ===\n";
        if (scenes.Length == 0)
        {
            result += "WARNING: No scenes in Build Settings!\n";
        }
        foreach (var scene in scenes)
        {
            result += $"  [{(scene.enabled ? "ON" : "OFF")}] {scene.path}\n";
        }

        // 2. Check Build Target
        result += $"\n=== Build Target ===\n";
        result += $"Active: {EditorUserBuildSettings.activeBuildTarget}\n";
        result += $"Platform: {EditorUserBuildSettings.selectedBuildTargetGroup}\n";

        // 3. Check for scripts with compile issues
        result += $"\n=== Script Check ===\n";
        var allScripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" });
        foreach (var guid in allScripts)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null)
            {
                var klass = script.GetClass();
                result += $"  {path} -> Class: {(klass != null ? klass.FullName : "NULL (could not load class)")}\n";
            }
        }

        // 4. Check for any editor-only scripts outside Editor folders
        var allNonEditorScripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
        result += $"\n=== All Scripts Outside Editor ===\n";
        foreach (var guid in allNonEditorScripts)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("/Editor/"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null)
                {
                    var klass = script.GetClass();
                    result += $"  {path} -> {(klass != null ? klass.FullName : "NULL")}\n";
                }
            }
        }

        // 5. Try to validate build
        result += $"\n=== Build Validation ===\n";
        try
        {
            var report = BuildPipeline.BuildPlayer(
                scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                "Builds/test_validation",
                EditorUserBuildSettings.activeBuildTarget,
                BuildOptions.BuildScriptsOnly
            );
            result += $"Build result: {report.summary.result}\n";
            result += $"Total errors: {report.summary.totalErrors}\n";
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                    {
                        result += $"  ERROR: {msg.content}\n";
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            result += $"Build validation exception: {e.Message}\n";
        }

        return result;
    }
}
