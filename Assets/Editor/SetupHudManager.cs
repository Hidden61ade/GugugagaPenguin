using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SetupHudManager
{
    [MenuItem("Tools/Setup HudManager References")]
    public static void Execute()
    {
        // Find HudPanel
        GameObject hudPanel = GameObject.Find("Canvas/HudPanel");
        if (hudPanel == null)
        {
            Debug.LogError("Canvas/HudPanel not found!");
            return;
        }

        HudManager hud = hudPanel.GetComponent<HudManager>();
        if (hud == null)
        {
            Debug.LogError("HudManager component not found on HudPanel!");
            return;
        }

        SerializedObject so = new SerializedObject(hud);

        // 1. Back Button
        Transform buttonTr = hudPanel.transform.Find("Button");
        if (buttonTr != null)
        {
            Button btn = buttonTr.GetComponent<Button>();
            so.FindProperty("backButton").objectReferenceValue = btn;
            Debug.Log("Assigned backButton");
        }

        // 2. Left Hand Indicator
        Transform leftTr = hudPanel.transform.Find("LeftHandIndicator");
        if (leftTr != null)
        {
            so.FindProperty("leftHandIndicator").objectReferenceValue = leftTr.GetComponent<RectTransform>();
            Debug.Log("Assigned leftHandIndicator");
        }

        // 3. Right Hand Indicator
        Transform rightTr = hudPanel.transform.Find("RightHandIndicator");
        if (rightTr != null)
        {
            so.FindProperty("rightHandIndicator").objectReferenceValue = rightTr.GetComponent<RectTransform>();
            Debug.Log("Assigned rightHandIndicator");
        }

        // 4. Score Text
        Transform scoreTr = hudPanel.transform.Find("score");
        if (scoreTr != null)
        {
            TextMeshProUGUI tmp = scoreTr.GetComponent<TextMeshProUGUI>();
            so.FindProperty("scoreText").objectReferenceValue = tmp;
            Debug.Log("Assigned scoreText");
        }

        // 5. Hearts - find all heart children
        Transform heartsTr = hudPanel.transform.Find("Hearts");
        if (heartsTr != null)
        {
            SerializedProperty heartsList = so.FindProperty("hearts");
            heartsList.ClearArray();

            for (int i = 0; i < heartsTr.childCount; i++)
            {
                heartsList.InsertArrayElementAtIndex(i);
                heartsList.GetArrayElementAtIndex(i).objectReferenceValue = heartsTr.GetChild(i).gameObject;
            }
            Debug.Log($"Assigned {heartsTr.childCount} hearts");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(hud);

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("HudManager setup complete!");
    }
}
