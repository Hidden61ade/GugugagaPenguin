using UnityEngine;
using UnityEditor;
using System.IO;

public class CleanBuildCache
{
    public static string Execute()
    {
        string result = "";
        
        string beePath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "Bee", "artifacts", "WinPlayerBuildProgram");
        
        if (Directory.Exists(beePath))
        {
            try
            {
                // Try to remove read-only attributes first
                var dirInfo = new DirectoryInfo(beePath);
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        file.Attributes = FileAttributes.Normal;
                    }
                    catch { }
                }

                Directory.Delete(beePath, true);
                result += $"Successfully deleted: {beePath}\n";
            }
            catch (System.Exception e)
            {
                result += $"Failed to delete {beePath}: {e.Message}\n";
                
                // Try individual problematic folder
                string asyncPath = Path.Combine(beePath, "AsyncPluginsFromLinker");
                if (Directory.Exists(asyncPath))
                {
                    try
                    {
                        var di = new DirectoryInfo(asyncPath);
                        foreach (var file in di.GetFiles("*", SearchOption.AllDirectories))
                        {
                            try { file.Attributes = FileAttributes.Normal; file.Delete(); }
                            catch (System.Exception ex) { result += $"  Cannot delete {file.Name}: {ex.Message}\n"; }
                        }
                        Directory.Delete(asyncPath, true);
                        result += "Successfully deleted AsyncPluginsFromLinker\n";
                    }
                    catch (System.Exception ex)
                    {
                        result += $"Failed to delete AsyncPluginsFromLinker: {ex.Message}\n";
                    }
                }
            }
        }
        else
        {
            result += "WinPlayerBuildProgram directory does not exist.\n";
        }

        result += "\nRecommendation: If this still fails, pause OneDrive sync before building.\n";
        result += "Right-click OneDrive tray icon -> Pause syncing -> Then build.\n";
        
        return result;
    }
}
