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
    public float positionSmoothFactor = 0.5f;
    public float rotationSmoothFactor = 0.5f;

    private Vector3 desiredLocalOffset;
    private Quaternion desiredLocalRotation = Quaternion.identity;
    private Vector3 currentWorldPosition;
    private Quaternion currentWorldRotation = Quaternion.identity;
    private bool poseInitialized;
    private CameraMode currentMode = CameraMode.Gameplay;

    bool inited = false;
    bool oneReached = false;

    void Awake()
    {
        CaptureGameplayPoseFromCurrent();
        FlowControlClean.Instance.OnEnterTitle += () =>
        {
            inited = false;
            SetMode(CameraMode.Title, true);
        };
        FlowControlClean.Instance.OnStartRun += () =>
        {
            inited = true;
            SetMode(CameraMode.Gameplay);
        };
        FlowControlClean.Instance.OnStateChanged += (state) =>
        {
            if (state == FlowControlClean.FlowState.Playing)
            {
                // positionSmoothFactor = 1f;
            }
        };
        FlowControlClean.Instance.OnEnterVictory += () => SetMode(CameraMode.Victory);
    }
    void Start()
    {
        SetMode(currentMode, true);
        if (target != null)
        {
            currentWorldPosition = transform.position;
            currentWorldRotation = transform.rotation;
        }
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

        if (snap && target != null)
        {
            currentWorldPosition = target.TransformPoint(desiredLocalOffset);
            currentWorldRotation = target.rotation * desiredLocalRotation;
            ApplyPoseImmediate();
        }
    }

    public CameraMode GetCurrentMode()
    {
        return currentMode;
    }

    void Update()
    {
        if (target == null) return;

        if( !oneReached && inited && positionSmoothFactor < 1f)
        {
            positionSmoothFactor += positionSmoothFactor * 0.01f; // Gradually increase smoothing factor
            if(positionSmoothFactor > 1f)
            {
                positionSmoothFactor = 1f;
                oneReached = true;
            }
        }

        // Step 1: Calculate desired world position and rotation (no smoothing)
        Vector3 desiredWorldPosition = target.TransformPoint(desiredLocalOffset);
        Quaternion desiredWorldRotation = target.rotation * desiredLocalRotation;

        // Step 2: Linear interpolation smoothing
        currentWorldPosition = Vector3.Lerp(currentWorldPosition, desiredWorldPosition, positionSmoothFactor);
        currentWorldRotation = Quaternion.Slerp(currentWorldRotation, desiredWorldRotation, rotationSmoothFactor);

        // Step 3: Apply final position and rotation
        transform.position = currentWorldPosition;
        transform.rotation = currentWorldRotation;
    }

    private void ApplyPoseImmediate()
    {
        if (target == null)
        {
            return;
        }

        transform.position = currentWorldPosition;
        transform.rotation = currentWorldRotation;
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
