using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class FixFillSprite
{
    [MenuItem("Tools/Fix Fill Sprite")]
    public static void Execute()
    {
        // 找到 Fill Image
        GameObject canvas = GameObject.Find("Canvas");
        Transform fill = canvas?.transform.Find("ProgressBarRoot/ProgressBar/Fill");
        if (fill == null) { Debug.LogError("Fill not found!"); return; }

        Image img = fill.GetComponent<Image>();
        if (img == null) { Debug.LogError("Image not found on Fill!"); return; }

        // 使用内置 UI 白色 sprite
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (uiSprite == null)
        {
            // 备选：使用 Background
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        }

        img.sprite = uiSprite;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Vertical;
        img.fillOrigin = 0; // Bottom
        img.fillAmount = 0f; // 初始为空

        EditorUtility.SetDirty(img);

        // 保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Fill sprite set to: {img.sprite?.name ?? "null"}, fillAmount={img.fillAmount}");
    }
}
