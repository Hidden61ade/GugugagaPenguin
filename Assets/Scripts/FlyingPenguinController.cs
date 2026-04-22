using UnityEngine;
using System;

/// <summary>
/// Executes flap actions for FlyingPenguin.
/// Input sources should call FlapLeft/FlapRight rather than reading input here.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FlyingPenguinController : MonoBehaviour
{
    public enum FlapSide
    {
        Left,
        Right
    }

    [Header("Physics Settings")]
    public float upwardForce = 10f;
    public float sidewaysForce = 5f;
    public float glideAcceleration = 4f;
    public float maxForwardSpeed = 11f;
    public Vector3 worldGlideDirection = Vector3.forward;

    [Header("State")]
    public bool inputEnabled = true;

    private Animator animator;
    private Rigidbody rb;

    private static readonly int SwingLeftTrigger = Animator.StringToHash("SwingLeft");
    private static readonly int SwingRightTrigger = Animator.StringToHash("SwingRight");
    private static readonly int SwingLegTrigger = Animator.StringToHash("SwingLeg");

    public event Action<FlapSide, float> Flapped;

    void Start()
    {
        EnsureReferences();
        if (worldGlideDirection.sqrMagnitude < 0.01f)
        {
            worldGlideDirection = Vector3.forward;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic || !inputEnabled)
        {
            return;
        }

        Vector3 glideDirection = worldGlideDirection.normalized;
        rb.AddForce(glideDirection * glideAcceleration, ForceMode.Acceleration);

        float forwardSpeed = Vector3.Dot(rb.velocity, glideDirection);
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
