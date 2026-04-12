using UnityEngine;

/// <summary>
/// Executes flap actions for FlyingPenguin.
/// Input sources should call FlapLeft/FlapRight rather than reading input here.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FlyingPenguinController : MonoBehaviour
{
    [Header("Physics Settings")]
    public float upwardForce = 10f;
    public float sidewaysForce = 5f;

    private Animator animator;
    private Rigidbody rb;

    private static readonly int SwingLeftTrigger = Animator.StringToHash("SwingLeft");
    private static readonly int SwingRightTrigger = Animator.StringToHash("SwingRight");
    private static readonly int SwingLegTrigger = Animator.StringToHash("SwingLeg");

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    public void FlapLeft(float upwardStrength = 1f, float sidewaysStrength = 1f)
    {
        animator.SetTrigger(SwingLeftTrigger);
        animator.SetTrigger(SwingLegTrigger);

        // Left wing swings -> pushes penguin Right and Up
        // Note: The penguin model faces -Z, so its "Right" is -X (-transform.right)
        Vector3 forceDir = transform.up * (upwardForce * upwardStrength)
                         - transform.right * (sidewaysForce * sidewaysStrength);
        rb.AddForce(forceDir, ForceMode.Impulse);
    }

    public void FlapRight(float upwardStrength = 1f, float sidewaysStrength = 1f)
    {
        animator.SetTrigger(SwingRightTrigger);
        animator.SetTrigger(SwingLegTrigger);

        // Right wing swings -> pushes penguin Left and Up
        // Note: The penguin model faces -Z, so its "Left" is +X (transform.right)
        Vector3 forceDir = transform.up * (upwardForce * upwardStrength)
                         + transform.right * (sidewaysForce * sidewaysStrength);
        rb.AddForce(forceDir, ForceMode.Impulse);
    }
}
