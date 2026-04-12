using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;

public class DisablePlasticAndInspect
{
    public static string Execute()
    {
        string result = "";

        // 1. Disable Plastic SCM / Version Control
        try
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/VersionControlSettings.asset")[0]);
            var modeProp = settings.FindProperty("m_Mode");
            if (modeProp != null)
            {
                result += $"Current VCS mode: {modeProp.stringValue}\n";
                modeProp.stringValue = "Visible Meta Files";
                settings.ApplyModifiedPropertiesWithoutUndo();
                result += "Disabled Plastic SCM -> set to 'Visible Meta Files'.\n";
            }
        }
        catch (System.Exception e)
        {
            result += $"VCS disable error: {e.Message}\n";
        }

        return result;
    }
}
