using UnityEngine;
using System.IO;
using System.Diagnostics;

public class ForceCleanAll
{
    public static string Execute()
    {
        string result = "";
        string projectRoot = Directory.GetCurrentDirectory();

        // Delete the entire WinPlayerBuildProgram folder via cmd
        string winPlayerPath = Path.Combine(projectRoot, "Library", "Bee", "artifacts", "WinPlayerBuildProgram");
        result += $"Target: {winPlayerPath}\n";
        result += $"Exists: {Directory.Exists(winPlayerPath)}\n";

        if (Directory.Exists(winPlayerPath))
        {
            // List subdirs
            try
            {
                foreach (var dir in Directory.GetDirectories(winPlayerPath))
                {
                    result += $"  Subdir: {Path.GetFileName(dir)}\n";
                }
            }
            catch { }

            // Use cmd to force delete
            result += RunCmd($"/c rmdir /s /q \"{winPlayerPath}\"");
            result += $"Still exists after delete: {Directory.Exists(winPlayerPath)}\n";
        }

        // Also clean the .dag files that reference this path
        string[] dagFiles = new string[]
        {
            "Library/Bee/Player0bda21e4.dag",
            "Library/Bee/Playerdfb5c570.dag"
        };
        foreach (var dag in dagFiles)
        {
            string fullPath = Path.Combine(projectRoot, dag);
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    result += $"Deleted {dag}\n";
                }
                catch (System.Exception e)
                {
                    result += $"Cannot delete {dag}: {e.Message}\n";
                }
            }
        }

        // Verify clean state
        if (!Directory.Exists(winPlayerPath))
        {
            result += "\nSUCCESS: WinPlayerBuildProgram completely removed. Please Build again.\n";
        }
        else
        {
            // Nuclear option - try to take ownership
            result += "\nStill locked. Trying takeown...\n";
            result += RunCmd($"/c takeown /f \"{winPlayerPath}\" /r /d y & icacls \"{winPlayerPath}\" /grant %username%:F /t & rmdir /s /q \"{winPlayerPath}\"");
            result += $"Final check - exists: {Directory.Exists(winPlayerPath)}\n";

            if (Directory.Exists(winPlayerPath))
            {
                result += "\nSTILL LOCKED. Please:\n";
                result += "1. Close Unity completely\n";
                result += "2. Delete the 'Library' folder manually\n";
                result += "3. Re-open the project\n";
            }
        }

        return result;
    }

    static string RunCmd(string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            proc.WaitForExit(10000);
            string err = proc.StandardError.ReadToEnd();
            string output = proc.StandardOutput.ReadToEnd();
            string r = $"cmd exit: {proc.ExitCode}\n";
            if (!string.IsNullOrEmpty(output)) r += $"  out: {output}\n";
            if (!string.IsNullOrEmpty(err)) r += $"  err: {err}\n";
            return r;
        }
        catch (System.Exception e)
        {
            return $"cmd exception: {e.Message}\n";
        }
    }
}
