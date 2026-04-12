using UnityEngine;

/// <summary>
/// Camera that locks to its initial relative position and rotation to the target.
/// This perfectly preserves the view you set up in the Scene view.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.05f;

    private Vector3 localOffset;
    private Quaternion localRotation;
    private Vector3 currentVelocity;

    void Start()
    {
        if (target != null)
        {
            // Record the exact offset and rotation of the camera relative to the target at start
            localOffset = target.InverseTransformPoint(transform.position);
            localRotation = Quaternion.Inverse(target.rotation) * transform.rotation;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate desired position based on target's current position and rotation
        Vector3 desiredPosition = target.TransformPoint(localOffset);
        
        // Smoothly move to desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
        
        // Match the target's rotation plus the initial relative rotation
        transform.rotation = target.rotation * localRotation;
    }
}
