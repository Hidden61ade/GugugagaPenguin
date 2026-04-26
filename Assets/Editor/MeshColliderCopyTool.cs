using UnityEngine;
using UnityEditor;

public class MeshColliderCopyTool : EditorWindow
{
    private Transform source;
    private Transform target;
    private bool clearOldGenerated = true;

    [MenuItem("Tools/Mesh Collider Copy Tool")]
    public static void ShowWindow()
    {
        GetWindow<MeshColliderCopyTool>("Mesh Collider Copy");
    }

    private void OnGUI()
    {
        GUILayout.Label("Copy MeshFilters from Source to MeshColliders under Target", EditorStyles.boldLabel);

        source = (Transform)EditorGUILayout.ObjectField("Source", source, typeof(Transform), true);
        target = (Transform)EditorGUILayout.ObjectField("Target", target, typeof(Transform), true);

        clearOldGenerated = EditorGUILayout.Toggle("Clear Old Generated", clearOldGenerated);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate MeshColliders"))
        {
            Generate();
        }
    }

    private void Generate()
    {
        if (source == null || target == null)
        {
            Debug.LogError("Source or Target is null.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        if (clearOldGenerated)
        {
            ClearGeneratedObjects();
        }

        MeshFilter[] meshFilters = source.GetComponentsInChildren<MeshFilter>(true);

        int count = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null)
                continue;

            Transform src = mf.transform;

            GameObject colliderObj = new GameObject(src.name + "_MeshCollider");
            Undo.RegisterCreatedObjectUndo(colliderObj, "Create MeshCollider Object");

            Transform dst = colliderObj.transform;
            dst.SetParent(target, false);

            /*
             * 关键点：
             *
             * 这里不是复制 src.localPosition / localRotation / localScale。
             *
             * src.localToWorldMatrix 已经包含：
             * Source -> 子层级A -> 子层级B -> ... -> src
             * 的完整变换。
             *
             * source.worldToLocalMatrix * src.localToWorldMatrix
             * 得到的就是：
             * src 相对于 source 的最终局部矩阵。
             *
             * 由于 Target 自身是标准 Transform，
             * 把这个矩阵分解后设给 Target 下的直接子物体，
             * 视觉上就会和 Source 里的 MeshFilter 物体一致。
             */
            Matrix4x4 localToSourceMatrix = source.worldToLocalMatrix * src.localToWorldMatrix;

            SetTransformFromMatrix(dst, localToSourceMatrix);

            MeshCollider meshCollider = colliderObj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mf.sharedMesh;

            count++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"Generated {count} MeshCollider objects under Target.");
    }

    private void ClearGeneratedObjects()
    {
        for (int i = target.childCount - 1; i >= 0; i--)
        {
            Transform child = target.GetChild(i);

            if (child.name.EndsWith("_MeshCollider"))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    private static void SetTransformFromMatrix(Transform transform, Matrix4x4 matrix)
    {
        Vector3 position = matrix.GetColumn(3);

        Vector3 x = matrix.GetColumn(0);
        Vector3 y = matrix.GetColumn(1);
        Vector3 z = matrix.GetColumn(2);

        Vector3 scale = new Vector3(
            x.magnitude,
            y.magnitude,
            z.magnitude
        );

        if (scale.x != 0) x /= scale.x;
        if (scale.y != 0) y /= scale.y;
        if (scale.z != 0) z /= scale.z;

        Quaternion rotation = Quaternion.LookRotation(z, y);

        transform.SetLocalPositionAndRotation(position, rotation);
        transform.localScale = scale;
    }
}
