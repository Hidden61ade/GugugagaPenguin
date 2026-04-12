using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class ForceCleanBee
{
    public static string Execute()
    {
        string result = "";
        
        // Get the problematic path
        string projectRoot = Directory.GetCurrentDirectory();
        string asyncPath = Path.Combine(projectRoot, "Library", "Bee", "artifacts", "WinPlayerBuildProgram", "AsyncPluginsFromLinker");
        
        result += $"Project root: {projectRoot}\n";
        result += $"Target path: {asyncPath}\n";
        result += $"Exists: {Directory.Exists(asyncPath)}\n";

        if (Directory.Exists(asyncPath))
        {
            // List what's inside
            try
            {
                var files = Directory.GetFiles(asyncPath, "*", SearchOption.AllDirectories);
                result += $"Files inside: {files.Length}\n";
                foreach (var f in files)
                {
                    var fi = new FileInfo(f);
                    result += $"  {fi.Name} - {fi.Length}bytes - ReadOnly:{fi.IsReadOnly} - Attr:{fi.Attributes}\n";
                }
            }
            catch (System.Exception e)
            {
                result += $"Cannot list files: {e.Message}\n";
            }

            // Try cmd /c rmdir /s /q
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c rmdir /s /q \"{asyncPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);
                proc.WaitForExit(5000);
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                result += $"rmdir exit code: {proc.ExitCode}\n";
                if (!string.IsNullOrEmpty(stdout)) result += $"stdout: {stdout}\n";
                if (!string.IsNullOrEmpty(stderr)) result += $"stderr: {stderr}\n";
                result += $"Still exists after rmdir: {Directory.Exists(asyncPath)}\n";
            }
            catch (System.Exception e)
            {
                result += $"rmdir failed: {e.Message}\n";
            }
        }

        // If still exists, try deleting the entire WinPlayerBuildProgram folder
        string winPlayerPath = Path.Combine(projectRoot, "Library", "Bee", "artifacts", "WinPlayerBuildProgram");
        if (Directory.Exists(asyncPath))
        {
            result += "\nAsyncPluginsFromLinker still exists. Trying to delete entire WinPlayerBuildProgram...\n";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c rmdir /s /q \"{winPlayerPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);
                proc.WaitForExit(5000);
                result += $"rmdir WinPlayerBuildProgram exit: {proc.ExitCode}\n";
                string stderr = proc.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(stderr)) result += $"stderr: {stderr}\n";
                result += $"Still exists: {Directory.Exists(winPlayerPath)}\n";
            }
            catch (System.Exception e)
            {
                result += $"Failed: {e.Message}\n";
            }
        }

        if (!Directory.Exists(asyncPath))
        {
            result += "\nSUCCESS: AsyncPluginsFromLinker has been deleted. Please try Build again.\n";
        }
        else
        {
            result += "\nFAILED: Directory is still locked. Manual steps needed:\n";
            result += "1. Close Unity\n";
            result += "2. Delete the entire 'Library' folder in the project root\n";
            result += "3. Re-open the project in Unity (it will regenerate Library)\n";
            result += "4. Try Build again\n";
        }

        return result;
    }
}
