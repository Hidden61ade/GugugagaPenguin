using UnityEditor;
using UnityEngine;

public class AssignDartHitSfx
{
    [MenuItem("Tools/Assign Dart Hit SFX")]
    public static void Execute()
    {
        GameObject amObj = GameObject.Find("AudioManager");
        if (amObj == null) { Debug.LogError("AudioManager not found!"); return; }

        AudioManager am = amObj.GetComponent<AudioManager>();
        if (am == null) { Debug.LogError("AudioManager component not found!"); return; }

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/gu-gu-ga-ga_Hyvo7id.mp3");
        if (clip == null) { Debug.LogError("Audio clip not found!"); return; }

        SerializedObject so = new SerializedObject(am);
        so.FindProperty("dartHitSfx").objectReferenceValue = clip;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(am);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Assigned dartHitSfx: {clip.name} ({clip.length:F1}s)");
    }
}
