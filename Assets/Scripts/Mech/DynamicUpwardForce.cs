using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicUpwardForce : MonoBehaviour
{
    public FlyingPenguinController FlyingPenguinController;
    public float TargetHeight = 110;
    [SerializeField] Rigidbody rb;
    float _newUpwardForce = 0f;
    float _originalUpwardForce = 0f;
    void Awake()
    {
        _originalUpwardForce = FlyingPenguinController.upwardForce;
    }
    void FixedUpdate()
    {
        _newUpwardForce = Mathf.Max(0, Mathf.Min(_originalUpwardForce,
        rb.mass * (Mathf.Sqrt(2 * Physics.gravity.magnitude * Mathf.Max(0, TargetHeight - transform.position.y)) - rb.velocity.y)));
        FlyingPenguinController.upwardForce = _newUpwardForce;
    }
}
