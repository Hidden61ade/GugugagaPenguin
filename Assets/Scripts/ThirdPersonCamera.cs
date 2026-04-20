using UnityEngine;

/// <summary>
/// Camera rig with title/gameplay/victory poses relative to a target.
/// The initial scene view is captured as the gameplay pose so existing framing is preserved.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    public enum CameraMode
    {
        Title,
        Gameplay,
        Victory
    }

    [Header("Target")]
    public Transform target;

    [Header("Capture")]
    public bool autoCaptureGameplayPose = true;

    [Header("Gameplay Pose")]
    public Vector3 gameplayLocalOffset;
    public Vector3 gameplayLocalEuler;

    [Header("Title Pose")]
    public Vector3 titleOffsetDelta = new Vector3(0f, 5f, -7f);
    public Vector3 titleEulerDelta = new Vector3(-8f, 0f, 0f);

    [Header("Victory Pose")]
    public Vector3 victoryOffsetDelta = new Vector3(0f, 2f, 5f);
    public Vector3 victoryEulerDelta = new Vector3(8f, 180f, 0f);

    [Header("Smoothing")]
    public float positionSmoothTime = 0.28f;
    public float rotationSmoothSpeed = 8f;

    private Vector3 currentLocalOffset;
    private Quaternion currentLocalRotation = Quaternion.identity;
    private Vector3 desiredLocalOffset;
    private Quaternion desiredLocalRotation = Quaternion.identity;
    private Vector3 currentVelocity;
    private bool poseInitialized;
    private CameraMode currentMode = CameraMode.Gameplay;

    void Start()
    {
        CaptureGameplayPoseFromCurrent();
        SetMode(currentMode, true);
    }

    public void CaptureGameplayPoseFromCurrent()
    {
        if (target == null || (!autoCaptureGameplayPose && poseInitialized))
        {
            return;
        }

        gameplayLocalOffset = target.InverseTransformPoint(transform.position);
        gameplayLocalEuler = (Quaternion.Inverse(target.rotation) * transform.rotation).eulerAngles;
        poseInitialized = true;
    }

    public void SetMode(CameraMode mode, bool snap = false)
    {
        currentMode = mode;

        if (!poseInitialized && target != null)
        {
            CaptureGameplayPoseFromCurrent();
        }

        desiredLocalOffset = ResolveOffset(mode);
        desiredLocalRotation = Quaternion.Euler(ResolveEuler(mode));

        if (snap)
        {
            currentLocalOffset = desiredLocalOffset;
            currentLocalRotation = desiredLocalRotation;
            currentVelocity = Vector3.zero;
            ApplyPoseImmediate();
        }
    }

    public CameraMode GetCurrentMode()
    {
        return currentMode;
    }

    void LateUpdate()
    {
        if (target == null) return;

        currentLocalOffset = Vector3.SmoothDamp(
            currentLocalOffset,
            desiredLocalOffset,
            ref currentVelocity,
            Mathf.Max(0.01f, positionSmoothTime),
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        float rotationT = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.unscaledDeltaTime);
        currentLocalRotation = Quaternion.Slerp(currentLocalRotation, desiredLocalRotation, rotationT);

        transform.position = target.TransformPoint(currentLocalOffset);
        transform.rotation = target.rotation * currentLocalRotation;
    }

    private void ApplyPoseImmediate()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.TransformPoint(currentLocalOffset);
        transform.rotation = target.rotation * currentLocalRotation;
    }

    private Vector3 ResolveOffset(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.Title:
                return gameplayLocalOffset + titleOffsetDelta;
            case CameraMode.Victory:
                return gameplayLocalOffset + victoryOffsetDelta;
            default:
                return gameplayLocalOffset;
        }
    }

    private Vector3 ResolveEuler(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.Title:
                return gameplayLocalEuler + titleEulerDelta;
            case CameraMode.Victory:
                return gameplayLocalEuler + victoryEulerDelta;
            default:
                return gameplayLocalEuler;
        }
    }
}
