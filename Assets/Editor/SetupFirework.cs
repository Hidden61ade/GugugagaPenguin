using UnityEditor;
using UnityEngine;

public class SetupFirework
{
    [MenuItem("Tools/Setup Firework Object")]
    public static void Execute()
    {
        // 1. 创建 "Firework" Tag
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");

        bool tagExists = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "Firework")
            {
                tagExists = true;
                break;
            }
        }

        if (!tagExists)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Firework";
            tagManager.ApplyModifiedProperties();
            Debug.Log("Created tag: Firework");
        }
        else
        {
            Debug.Log("Tag 'Firework' already exists");
        }

        // 2. 找到 firework_ 对象
        GameObject firework = GameObject.Find("firework_");
        if (firework == null)
        {
            Debug.LogError("firework_ not found in scene!");
            return;
        }

        // 3. 设置 Tag
        firework.tag = "Firework";
        Debug.Log("Set tag 'Firework' on firework_");

        // 4. 添加 BoxCollider (Trigger)
        BoxCollider col = firework.GetComponent<BoxCollider>();
        if (col == null)
        {
            col = firework.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider to firework_");
        }
        col.isTrigger = true;

        // 根据 Bounds 自动设置 collider 大小
        Renderer[] renderers = firework.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            foreach (var r in renderers)
                combined.Encapsulate(r.bounds);

            // 转换到本地空间
            col.center = firework.transform.InverseTransformPoint(combined.center);
            col.size = combined.size;
            Debug.Log($"BoxCollider size set to: {col.size}, center: {col.center}");
        }

        // 5. 添加 CoinSpin（自转）
        if (firework.GetComponent<CoinSpin>() == null)
        {
            firework.AddComponent<CoinSpin>();
            Debug.Log("Added CoinSpin to firework_");
        }

        EditorUtility.SetDirty(firework);

        // 保存场景
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("Firework setup complete!");
    }
}
