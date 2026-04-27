using UnityEditor;
using UnityEngine;

public class FixLevelController
{
    [MenuItem("Tools/Fix Level Controller")]
    public static void Execute()
    {
        // ── 1. 修复 InitialLevelsInstances：包含场景中全部 3 个关卡 ──
        GameObject lcObj = GameObject.Find("LevelController");
        if (lcObj == null) { Debug.LogError("LevelController not found!"); return; }

        LevelController lc = lcObj.GetComponent<LevelController>();
        if (lc == null) { Debug.LogError("LevelController component not found!"); return; }

        GameObject level0 = GameObject.Find("Level0");
        GameObject level1 = GameObject.Find("Level1");
        GameObject level3 = GameObject.Find("Level3");

        if (level0 == null || level1 == null || level3 == null)
        {
            Debug.LogError($"Missing levels! L0={level0 != null} L1={level1 != null} L3={level3 != null}");
            return;
        }

        // 通过 SerializedObject 修改
        SerializedObject so = new SerializedObject(lc);

        // 设置 InitialLevelsInstances = [Level0, Level1, Level3]
        SerializedProperty initProp = so.FindProperty("InitialLevelsInstances");
        initProp.arraySize = 3;
        initProp.GetArrayElementAtIndex(0).objectReferenceValue = level0;
        initProp.GetArrayElementAtIndex(1).objectReferenceValue = level1;
        initProp.GetArrayElementAtIndex(2).objectReferenceValue = level3;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lc);

        Debug.Log($"InitialLevelsInstances updated to [Level0, Level1, Level3]");

        // ── 2. 修复 Level0 的 ScensLoadAheadTrigger 位置 ──
        // Level0 Track 范围: Z=11 ~ Z=3411
        // 触发器应该放在关卡末尾附近（Z≈2900），而不是 Z=766
        Transform trigger = level0.transform.Find("ScensLoadAheadTrigger");
        if (trigger != null)
        {
            // Level0 在 world Z=0，所以 local Z=2900 ≈ 关卡末尾
            trigger.localPosition = new Vector3(0f, 190f, 2900f);
            EditorUtility.SetDirty(trigger.gameObject);
            Debug.Log($"Level0 trigger moved to local Z=2900 (was Z={766})");
        }

        // ── 3. 确认 Level1 的触发器位置 ──
        Transform trigger1 = level1.transform.Find("ScensLoadAheadTrigger");
        if (trigger1 != null)
        {
            Debug.Log($"Level1 trigger at local Z={trigger1.localPosition.z}");
            // Level1 的 Track 也是 3000 长，触发器也应该在末尾附近
            // Level1 的 localPos.z = 3300, Track localPos.z = 1610
            // Track world end ≈ 3300 + 1610 + 1500 = 6410
            // 触发器 local Z 应该在 ≈ 2800
            if (trigger1.localPosition.z < 1000f)
            {
                trigger1.localPosition = new Vector3(0f, 190f, 2800f);
                EditorUtility.SetDirty(trigger1.gameObject);
                Debug.Log("Level1 trigger also moved to local Z=2800");
            }
        }

        // ── 4. 确认 Level3 有 ScensLoadAheadTrigger ──
        Transform trigger3 = level3.transform.Find("ScensLoadAheadTrigger");
        if (trigger3 == null)
        {
            GameObject t3 = new GameObject("ScensLoadAheadTrigger");
            t3.transform.SetParent(level3.transform, false);
            t3.transform.localPosition = new Vector3(0f, 190f, 2800f);
            t3.transform.localScale = new Vector3(600f, 400f, 100f);

            BoxCollider col = t3.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = Vector3.one;
            col.center = Vector3.zero;

            t3.AddComponent<LevelLoadTrigger>();
            EditorUtility.SetDirty(t3);
            Debug.Log("Added ScensLoadAheadTrigger to Level3 at local Z=2800");
        }
        else
        {
            Debug.Log($"Level3 trigger already at local Z={trigger3.localPosition.z}");
        }

        // 保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("Level Controller fix complete!");
    }
}
