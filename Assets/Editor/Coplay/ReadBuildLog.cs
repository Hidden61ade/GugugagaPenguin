using UnityEngine;
using System.IO;

public class ReadBuildLog
{
    public static string Execute()
    {
        string result = "";
        string editorLogPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Unity", "Editor", "Editor.log");

        try
        {
            string tempCopy = Path.Combine(Path.GetTempPath(), "editor_log_copy2.txt");
            File.Copy(editorLogPath, tempCopy, true);
            string[] lines = File.ReadAllLines(tempCopy);

            // Find the last "Build" or "Building" related section
            // Search from end for "Build completed with a result of 'Failed'"
            int failLine = -1;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].Contains("Build completed with a result of 'Failed'"))
                {
                    failLine = i;
                    break;
                }
            }

            if (failLine >= 0)
            {
                result += $"Found build failure at line {failLine}\n";
                // Get 150 lines before the failure
                int start = Mathf.Max(failLine - 150, 0);
                for (int i = start; i <= failLine + 5 && i < lines.Length; i++)
                {
                    // Only include lines with useful info
                    string line = lines[i];
                    if (line.Contains("error") || line.Contains("Error") ||
                        line.Contains("fail") || line.Contains("Fail") ||
                        line.Contains("Build") || line.Contains("build") ||
                        line.Contains("Exception") || line.Contains("exception") ||
                        line.Contains("Warning") || line.Contains("Cannot") ||
                        line.Contains("could not") || line.Contains("Could not") ||
                        line.Contains("not found") || line.Contains("missing") ||
                        line.Contains("Missing") || line.Contains("FAILED") ||
                        line.Contains("DirectoryNotFoundException") ||
                        line.Contains("Starting") || line.Contains("ExitCode"))
                    {
                        result += $"[L{i}] {line}\n";
                    }
                }
            }
            else
            {
                result += "No 'Build completed with a result of Failed' found in log.\n";
                // Show last 50 lines
                int start = Mathf.Max(lines.Length - 50, 0);
                for (int i = start; i < lines.Length; i++)
                {
                    result += $"[L{i}] {lines[i]}\n";
                }
            }

            File.Delete(tempCopy);
        }
        catch (System.Exception e)
        {
            result += $"Error: {e.Message}\n{e.StackTrace}\n";
        }

        return result;
    }
}
