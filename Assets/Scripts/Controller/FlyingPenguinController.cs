using UnityEngine;
using System;
using Unity.VisualScripting;

/// <summary>
/// Executes flap actions for FlyingPenguin.
/// Input sources should call FlapLeft/FlapRight rather than reading input here.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public partial class FlyingPenguinController : MonoBehaviour
{
    public enum FlapSide
    {
        Left,
        Right
    }

    [Header("Physics Settings")]
    public float upwardForce = 10f;
    public float additionalGravity = 0f;
    public float sidewaysForce = 5f;
    public float glideAcceleration = 4f;
    public float maxForwardSpeed = 11f;
    public Vector3 worldGlideDirection = Vector3.forward;
    public float launchForwardSpeed = 8f;

    [Header("State")]
    public bool inputEnabled = true;

    private Animator animator;
    private Rigidbody rb;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private static readonly int SwingLeftTrigger = Animator.StringToHash("SwingLeft");
    private static readonly int SwingRightTrigger = Animator.StringToHash("SwingRight");
    private static readonly int SwingLegTrigger = Animator.StringToHash("SwingLeg");

    public event Action<FlapSide, float> Flapped;
    void Awake()
    {
        EnsureReferences();

        // 记录出生点
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        FlowControlClean.Instance.OnStartRun += () =>
        {
            // 从出生点重新开始（解冻物理）
            ResetRun(spawnPosition, spawnRotation, false);
        };

        FlowControlClean.Instance.OnEnterTitle += () =>
        {
            // 返回标题：传送回出生点并冻结
            ResetRun(spawnPosition, spawnRotation, true);
        };

        FlowControlClean.Instance.OnStateChanged += (state) =>
        {
            if (state == FlowControlClean.FlowState.Playing)
            {
                SetInputEnabled(true);
            }
            else if (state == FlowControlClean.FlowState.Title)
            {
                SetInputEnabled(false);
            }
        };
    }

    void Start()
    {
        EnsureReferences();
        if (worldGlideDirection.sqrMagnitude < 0.01f)
        {
            worldGlideDirection = Vector3.forward;
        }
    }

    [SerializeField] private float speedCatchUpThreshold = 0.9f; 
    [SerializeField] private float speedCatchUpDelay = 1.0f;
    [SerializeField] private float accelerationIncreaseRate = 0.5f;
    [SerializeField] private float maxAccelerationMultiplier = 3.0f;

    private float lowSpeedTimer = 0f;
    private float accelerationMultiplier = 1f;

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic || !inputEnabled)
        {
            return;
        }

        Vector3 glideDirection = worldGlideDirection.normalized;

        float forwardSpeed = Vector3.Dot(rb.velocity, glideDirection);

        float targetSpeedThreshold = maxForwardSpeed * speedCatchUpThreshold;

        if (forwardSpeed < targetSpeedThreshold)
        {
            lowSpeedTimer += Time.fixedDeltaTime;

            if (lowSpeedTimer >= speedCatchUpDelay)
            {
                accelerationMultiplier += accelerationIncreaseRate * Time.fixedDeltaTime;
                accelerationMultiplier = Mathf.Min(accelerationMultiplier, maxAccelerationMultiplier);
            }
        }
        else
        {
            lowSpeedTimer = 0f;
            accelerationMultiplier = 1f;
        }

        float currentGlideAcceleration = glideAcceleration * accelerationMultiplier;

        rb.AddForce(
            glideDirection * currentGlideAcceleration + Vector3.down * additionalGravity,
            ForceMode.Acceleration
        );

        forwardSpeed = Vector3.Dot(rb.velocity, glideDirection);

        if (forwardSpeed > maxForwardSpeed)
        {
            Vector3 excessVelocity = glideDirection * (forwardSpeed - maxForwardSpeed);
            rb.velocity -= excessVelocity;
        }
    }

    public void FlapLeft(float upwardStrength = 1f, float sidewaysStrength = 1f)
    {
        if (!inputEnabled)
        {
            return;
        }

        EnsureReferences();
        animator.SetTrigger(SwingLeftTrigger);
        animator.SetTrigger(SwingLegTrigger);

        // Left wing swings -> pushes penguin Right and Up
        // Note: The penguin model faces -Z, so its "Right" is -X (-transform.right)
        Vector3 forceDir = transform.up * (upwardForce * upwardStrength)
                         + transform.right * (sidewaysForce * sidewaysStrength);
        rb.AddForce(forceDir, ForceMode.Impulse);
        Flapped?.Invoke(FlapSide.Left, upwardStrength);
    }

    public void FlapRight(float upwardStrength = 1f, float sidewaysStrength = 1f)
    {
        if (!inputEnabled)
        {
            return;
        }

        EnsureReferences();
        animator.SetTrigger(SwingRightTrigger);
        animator.SetTrigger(SwingLegTrigger);

        // Right wing swings -> pushes penguin Left and Up
        // Note: The penguin model faces -Z, so its "Left" is +X (transform.right)
        Vector3 forceDir = transform.up * (upwardForce * upwardStrength)
                         - transform.right * (sidewaysForce * sidewaysStrength);
        rb.AddForce(forceDir, ForceMode.Impulse);
        Flapped?.Invoke(FlapSide.Right, upwardStrength);
    }

    public void ResetRun(Vector3 position, Quaternion rotation, bool freezeSimulation)
    {
        EnsureReferences();

        rb.isKinematic = false;
        rb.useGravity = true;
        transform.SetPositionAndRotation(position, rotation);
        rb.position = position;
        rb.rotation = rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = freezeSimulation;
        rb.useGravity = !freezeSimulation;
        inputEnabled = !freezeSimulation;

        if (freezeSimulation)
        {
            rb.Sleep();
        }
        else
        {
            rb.WakeUp();
            rb.AddForce(launchForwardSpeed * rb.mass * transform.forward, ForceMode.Impulse);
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    private void EnsureReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }
}
