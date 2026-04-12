using UnityEngine;
using UnityEditor;

public class SetupFlyingPenguinPhysics
{
    public static string Execute()
    {
        string result = "";
        var flyingPenguin = GameObject.Find("FlyingPenguin");
        
        if (flyingPenguin == null)
            return "Error: FlyingPenguin not found!";

        // Add Rigidbody
        var rb = flyingPenguin.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = flyingPenguin.AddComponent<Rigidbody>();
            result += "Added Rigidbody.\n";
        }
        
        // Configure Rigidbody
        rb.mass = 1f;
        rb.drag = 1f; // Add some drag so it doesn't fly forever
        rb.angularDrag = 2f;
        // Freeze rotation so it doesn't tumble out of control
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Add Collider
        var col = flyingPenguin.GetComponent<CapsuleCollider>();
        if (col == null)
        {
            col = flyingPenguin.AddComponent<CapsuleCollider>();
            result += "Added CapsuleCollider.\n";
        }
        
        // Configure Collider (based on bounds)
        col.center = new Vector3(0, 0.72f, 0);
        col.radius = 0.35f;
        col.height = 1.45f;

        EditorUtility.SetDirty(flyingPenguin);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        
        result += "Physics setup complete and scene saved.";
        return result;
    }
}
