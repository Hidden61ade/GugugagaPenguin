using UnityEditor;
using UnityEngine;

public class FixPrefabTriggers
{
    [MenuItem("Tools/Fix Prefab Triggers")]
    public static void Execute()
    {
        FixPrefab("Assets/Prefabs/Levels/Level0.prefab");
        FixPrefab("Assets/Prefabs/Levels/Level1.prefab");
        FixPrefab("Assets/Prefabs/Levels/Level3.prefab");
        Debug.Log("All prefab triggers fixed!");
    }

    private static void FixPrefab(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogError($"Prefab not found: {path}");
            return;
        }

        Transform trigger = root.transform.Find("ScensLoadAheadTrigger");
        if (trigger == null)
        {
            Debug.LogWarning($"No ScensLoadAheadTrigger in {path}");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        // 确保有 BoxCollider (isTrigger)
        BoxCollider col = trigger.GetComponent<BoxCollider>();
        if (col == null)
        {
            col = trigger.gameObject.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            col.center = Vector3.zero;
        }
        col.isTrigger = true;

        // 确保有 LevelLoadTrigger 脚本
        LevelLoadTrigger llt = trigger.GetComponent<LevelLoadTrigger>();
        if (llt == null)
        {
            trigger.gameObject.AddComponent<LevelLoadTrigger>();
            Debug.Log($"Added LevelLoadTrigger to {path}");
        }
        else
        {
            Debug.Log($"{path} already has LevelLoadTrigger");
        }

        // 检查是否有重复的 ScensLoadAheadTrigger
        int triggerCount = 0;
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = root.transform.GetChild(i);
            if (child.name == "ScensLoadAheadTrigger")
            {
                triggerCount++;
                if (triggerCount > 1)
                {
                    Debug.Log($"Removed duplicate ScensLoadAheadTrigger in {path}");
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
