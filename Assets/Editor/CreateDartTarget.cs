using UnityEditor;
using UnityEngine;

public class CreateDartTarget
{
    [MenuItem("Tools/Create Dart Target")]
    public static void Execute()
    {
        // ── 配置 ──
        float targetZ = 9400f;      // Level1(1) Track 末端附近
        float targetY = 60f;        // 浮空高度，企鹅飞行高度范围
        float targetX = 0f;         // 赛道中央
        float outerRadius = 40f;    // 最外圈半径
        int ringCount = 5;

        // 环的分数（从外到内）
        int[] scores = { 10, 20, 30, 40, 50 };

        // 环的颜色（从外到内：红白交替）
        Color[] colors = {
            new Color(0.9f, 0.1f, 0.1f, 1f),  // 红
            Color.white,                         // 白
            new Color(0.9f, 0.1f, 0.1f, 1f),  // 红
            Color.white,                         // 白
            new Color(0.9f, 0.1f, 0.1f, 1f),  // 红（靶心）
        };

        // ── 创建父对象 ──
        GameObject parent = new GameObject("DartTarget");
        parent.transform.position = new Vector3(targetX, targetY, targetZ);
        // 靶子面朝 -Z（面向飞来的企鹅）
        parent.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // ── 确保材质文件夹 ──
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // ── 创建同心圆环（从外到内，用 Cylinder 薄片） ──
        // 每个环是一个扁平 Cylinder，半径递减
        // Unity Cylinder 默认：直径 1，高度 2，竖直方向
        // 我们旋转 90° 让它平面朝 Z 轴

        float ringStep = outerRadius / ringCount;
        float thickness = 1.0f; // 靶子厚度

        for (int i = 0; i < ringCount; i++)
        {
            float radius = outerRadius - (i * ringStep);
            string ringName = $"Ring_{i}_{scores[i]}pts";

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = ringName;
            ring.transform.SetParent(parent.transform, false);

            // Cylinder 默认高度 2, 半径 0.5
            // scale.x 和 scale.z 控制直径，scale.y 控制高度（厚度）
            float diameter = radius * 2f;
            ring.transform.localScale = new Vector3(diameter, thickness * 0.5f, diameter);

            // Cylinder 默认竖直，旋转使其面朝 Z
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // 每个环在 Z 方向微偏，内圈稍前，防止 Z-fighting
            ring.transform.localPosition = new Vector3(0f, 0f, i * 0.05f);

            // ── 材质 ──
            string matPath = $"Assets/Materials/DartRing_{i}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.color = colors[i];
            // 关闭金属感和光泽，显示纯色
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0.1f);
            EditorUtility.SetDirty(mat);

            MeshRenderer mr = ring.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // ── Collider 设置 ──
            // 移除默认 CapsuleCollider，改用 MeshCollider 或保留 CapsuleCollider
            // Cylinder 默认带 CapsuleCollider
            // 不过为了检测碰撞分区，保留各自的 collider
            // CapsuleCollider 对于扁平 cylinder 不太准确，替换为 BoxCollider
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            BoxCollider box = ring.AddComponent<BoxCollider>();
            // BoxCollider 的本地空间是经过 scale 之前的
            // Cylinder mesh 的本地空间：半径 0.5, 高度 2, 中心在原点
            box.center = Vector3.zero;
            box.size = new Vector3(1f, 0.2f, 1f); // 扁平检测区域
            box.isTrigger = false;

            // ── 挂载计分脚本 ──
            DartTarget dt = ring.AddComponent<DartTarget>();
            dt.scoreValue = scores[i];

            EditorUtility.SetDirty(ring);
        }

        // ── 添加靶子边框（装饰用，可选） ──
        // 外圈加个略大的深色边框
        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        border.name = "Border";
        border.transform.SetParent(parent.transform, false);
        float borderDiameter = (outerRadius + 2f) * 2f;
        border.transform.localScale = new Vector3(borderDiameter, thickness * 0.6f, borderDiameter);
        border.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        border.transform.localPosition = new Vector3(0f, 0f, -0.1f);

        Object.DestroyImmediate(border.GetComponent<Collider>());

        string borderMatPath = "Assets/Materials/DartBorder.mat";
        Material borderMat = AssetDatabase.LoadAssetAtPath<Material>(borderMatPath);
        if (borderMat == null)
        {
            borderMat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(borderMat, borderMatPath);
        }
        borderMat.color = new Color(0.2f, 0.15f, 0.1f, 1f); // 深棕色木框
        borderMat.SetFloat("_Metallic", 0f);
        borderMat.SetFloat("_Glossiness", 0.2f);
        EditorUtility.SetDirty(borderMat);
        border.GetComponent<MeshRenderer>().sharedMaterial = borderMat;

        // ── 添加支架 ──
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "Stand";
        stand.transform.SetParent(parent.transform, false);
        stand.transform.localPosition = new Vector3(0f, -(targetY * 0.5f + 5f), -1f);
        stand.transform.localScale = new Vector3(4f, targetY + 10f, 4f);

        Object.DestroyImmediate(stand.GetComponent<Collider>());

        stand.GetComponent<MeshRenderer>().sharedMaterial = borderMat;

        AssetDatabase.SaveAssets();

        // 放入 Level1(1) 下
        GameObject level = GameObject.Find("Level1 (1)");
        if (level != null)
        {
            parent.transform.SetParent(level.transform, true);
        }

        // 保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("DartTarget created successfully!");
    }
}
