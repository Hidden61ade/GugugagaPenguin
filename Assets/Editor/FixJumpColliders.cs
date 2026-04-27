using UnityEditor;
using UnityEngine;

public class FixJumpColliders
{
    [MenuItem("Tools/Fix Jump Colliders")]
    public static void Execute()
    {
        GameObject jump = GameObject.Find("Level0/Jump");
        if (jump == null)
        {
            Debug.LogError("Level0/Jump not found!");
            return;
        }

        // 移除父对象上引用空网格的 MeshCollider
        MeshCollider parentCol = jump.GetComponent<MeshCollider>();
        if (parentCol != null)
        {
            // 检查父对象自身是否有 MeshFilter
            MeshFilter parentMf = jump.GetComponent<MeshFilter>();
            if (parentMf == null || parentMf.sharedMesh == null || parentMf.sharedMesh.vertexCount == 0)
            {
                Object.DestroyImmediate(parentCol);
                Debug.Log("Removed empty MeshCollider from Jump parent");
            }
        }

        // 为所有有实际几何体的子对象添加 MeshCollider
        PhysicMaterial slideMat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>("Assets/PhysicsMaterials/SlipperyGround.physicMaterial");

        int addedCount = 0;
        MeshFilter[] meshFilters = jump.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0)
            {
                Debug.Log($"Skipped {mf.gameObject.name} (no geometry)");
                continue;
            }

            // 跳过已有 Collider 的对象
            if (mf.GetComponent<Collider>() != null)
            {
                Debug.Log($"Skipped {mf.gameObject.name} (already has collider)");
                continue;
            }

            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;

            if (slideMat != null)
                mc.sharedMaterial = slideMat;

            EditorUtility.SetDirty(mf.gameObject);
            addedCount++;
            Debug.Log($"Added MeshCollider to: {mf.gameObject.name} ({mf.sharedMesh.vertexCount} verts)");
        }

        // 保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Jump collider fix complete! Added {addedCount} MeshColliders.");
    }
}
