using UnityEngine;
using UnityEditor;
using System.IO;

public class GetBuildErrors
{
    public static string Execute()
    {
        string result = "";

        // Check project path for problematic characters
        string projectPath = Application.dataPath;
        result += $"Project dataPath: {projectPath}\n";
        result += $"Project path: {Directory.GetCurrentDirectory()}\n";

        // Check if the DLL that previously failed exists
        string dllPath = Path.Combine(Directory.GetCurrentDirectory(),
            "Library", "PackageCache", "com.unity.collections@1.2.4",
            "Unity.Collections.LowLevel.ILSupport",
            "Unity.Collections.LowLevel.ILSupport.dll");
        result += $"Collections DLL exists: {File.Exists(dllPath)}\n";
        result += $"Collections DLL path: {dllPath}\n";

        // Check if OneDrive is in the path (common issue)
        if (projectPath.Contains("OneDrive"))
        {
            result += "WARNING: Project is on OneDrive! OneDrive file syncing can cause build issues.\n";
            result += "OneDrive may lock files or fail to sync long paths correctly.\n";
        }

        // Check path length
        result += $"Path length: {dllPath.Length} chars (Windows MAX_PATH is 260)\n";
        if (dllPath.Length > 250)
        {
            result += "WARNING: Path is very close to or exceeds Windows MAX_PATH limit!\n";
        }

        // List the EditorBuildSettings scene path  
        result += $"\nBuild scene: {EditorBuildSettings.scenes[0].path}\n";
        result += $"Scene exists: {File.Exists(EditorBuildSettings.scenes[0].path)}\n";

        // Check the Editor log by copying it
        string editorLogPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Unity", "Editor", "Editor.log");
        result += $"\nEditor.log path: {editorLogPath}\n";

        try
        {
            // Copy the file to temp to read it
            string tempCopy = Path.Combine(Path.GetTempPath(), "editor_log_copy.txt");
            File.Copy(editorLogPath, tempCopy, true);
            string[] lines = File.ReadAllLines(tempCopy);

            // Find the last build section
            result += $"Editor.log total lines: {lines.Length}\n";
            result += "\n=== Last Build Errors (from Editor.log) ===\n";

            int buildStart = -1;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].Contains("Build Report") || lines[i].Contains("Starting build") ||
                    lines[i].Contains("[Build]") || lines[i].Contains("BuildPlayerWindow"))
                {
                    buildStart = i;
                    break;
                }
            }

            if (buildStart >= 0)
            {
                int start = Mathf.Max(buildStart - 5, 0);
                int end = Mathf.Min(buildStart + 100, lines.Length);
                for (int i = start; i < end; i++)
                {
                    result += lines[i] + "\n";
                }
            }
            else
            {
                // Just get the last 80 lines
                int start = Mathf.Max(lines.Length - 80, 0);
                for (int i = start; i < lines.Length; i++)
                {
                    if (lines[i].Contains("error") || lines[i].Contains("Error") ||
                        lines[i].Contains("fail") || lines[i].Contains("Fail") ||
                        lines[i].Contains("Build") || lines[i].Contains("build"))
                    {
                        result += $"[L{i}] {lines[i]}\n";
                    }
                }
            }

            File.Delete(tempCopy);
        }
        catch (System.Exception e)
        {
            result += $"Could not read Editor.log: {e.Message}\n";
        }

        return result;
    }
}
