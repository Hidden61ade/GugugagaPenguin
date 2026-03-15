using UnityEngine;
using UnityEditor;

public class SetupTerrainColliders
{
    [MenuItem("Tools/Setup Terrain Colliders")]
    public static void Execute()
    {
        // 1. 创建 Ground Layer（如果不存在）
        int groundLayer = CreateLayer("Ground");
        Debug.Log($"Ground layer index: {groundLayer}");

        // 2. 创建滑行游戏专用物理材质
        string matPath = "Assets/PhysicsMaterials/SlipperyGround.physicMaterial";
        AssetDatabase.CreateFolder("Assets", "PhysicsMaterials");

        PhysicMaterial slideMat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(matPath);
        if (slideMat == null)
        {
            slideMat = new PhysicMaterial("SlipperyGround");
            slideMat.dynamicFriction = 0.05f;   // 极低动摩擦，保持滑行感
            slideMat.staticFriction = 0.1f;      // 低静摩擦，容易起滑
            slideMat.bounciness = 0.0f;           // 无弹性
            slideMat.frictionCombine = PhysicMaterialCombine.Minimum; // 取最小摩擦值
            slideMat.bounceCombine = PhysicMaterialCombine.Minimum;
            AssetDatabase.CreateAsset(slideMat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created SlipperyGround PhysicMaterial");
        }

        // 3. 地形对象名称列表
        string[] terrainObjects = new string[]
        {
            "Ground",
            "GM-Base-URP",
            "GM-Ramp-URP",
            "GM-Ramp-URP (1)",
            "GM-Ramp-URP (2)",
            "GM-Ramp-URP (3)",
            "GM-Slope-URP",
            "GM-Slope-URP (1)",
            "GM-Slope-URP (2)",
            "GM-Slope-URP (3)",
            "GM-Cube-URP",
            "GM-Cube-URP (1)",
            "GM-Cube-URP (3)",
            "GM-Cube-URP (4)"
        };

        int successCount = 0;
        foreach (string objName in terrainObjects)
        {
            GameObject go = GameObject.Find(objName);
            if (go == null)
            {
                Debug.LogWarning($"GameObject not found: {objName}");
                continue;
            }

            // 设置 Layer
            if (groundLayer >= 0)
                go.layer = groundLayer;

            // 设置 Static（地形不会移动，标记为 Static 优化物理和渲染）
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.ReflectionProbeStatic);

            // 应用物理材质到 MeshCollider
            MeshCollider col = go.GetComponent<MeshCollider>();
            if (col != null)
            {
                col.sharedMaterial = slideMat;
                // 确保 convex = false（地形需要精确碰撞）
                col.convex = false;
                EditorUtility.SetDirty(go);
                successCount++;
                Debug.Log($"Setup complete: {objName}");
            }
            else
            {
                Debug.LogWarning($"No MeshCollider on: {objName}");
            }
        }

        // 4. 保存场景
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Terrain setup complete! {successCount}/{terrainObjects.Length} objects configured.");
    }

    private static int CreateLayer(string layerName)
    {
        // 检查是否已存在
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
            return existing;

        // 在 TagManager 中添加新 Layer
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++) // 0-7 是内置层
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"Created layer '{layerName}' at index {i}");
                return i;
            }
        }

        Debug.LogError("No empty layer slots available!");
        return -1;
    }
}
