using UnityEngine;

/// <summary>
/// Third-person character controller for the Penguin.
/// WASD movement relative to camera direction, character rotates to face movement direction.
/// Uses CharacterController for physics-based movement.
/// </summary>
public class PenguinController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSmoothTime = 0.1f;
    public float gravity = -20f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private Vector3 velocity;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Lock and hide cursor for mouse look
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleGravity();
        HandleMovement();

        // Press Escape to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click to re-lock cursor
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
        float vertical = Input.GetAxisRaw("Vertical");     // W/S

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Calculate target angle based on input direction + camera facing
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                                + cameraTransform.eulerAngles.y;

            // Smoothly rotate character to face movement direction
            // Add 180° offset because the penguin model faces -Z by default
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetAngle + 180f,
                ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            // Move in the target direction
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);

            // Set animator speed for walk animation
            animator.SetFloat(SpeedHash, 1f);
        }
        else
        {
            // No input - idle
            animator.SetFloat(SpeedHash, 0f);
        }
    }
}
