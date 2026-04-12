using UnityEngine;

/// <summary>
/// Controller for FlyingPenguin.
/// A key: swing left wing + swing legs + apply force right/up
/// L key: swing right wing + swing legs + apply force left/up
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            animator.SetTrigger(SwingLeftTrigger);
            animator.SetTrigger(SwingLegTrigger);
            
            // Left wing swings -> pushes penguin Right and Up
            // Note: The penguin model faces -Z, so its "Right" is -X (-transform.right)
            Vector3 forceDir = transform.up * upwardForce - transform.right * sidewaysForce;
            rb.AddForce(forceDir, ForceMode.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            animator.SetTrigger(SwingRightTrigger);
            animator.SetTrigger(SwingLegTrigger);
            
            // Right wing swings -> pushes penguin Left and Up
            // Note: The penguin model faces -Z, so its "Left" is +X (transform.right)
            Vector3 forceDir = transform.up * upwardForce + transform.right * sidewaysForce;
            rb.AddForce(forceDir, ForceMode.Impulse);
        }
    }
}
