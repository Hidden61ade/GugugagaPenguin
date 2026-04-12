using Leap;
using UnityEngine;

/// <summary>
/// Converts Ultraleap hand tracking data into left/right wing flap actions.
/// Each hand triggers a flap when its palm starts moving downward fast enough.
/// </summary>
[RequireComponent(typeof(FlyingPenguinController))]
public class LeapWingInputController : MonoBehaviour
{
    [Header("References")]
    public LeapProvider leapProvider;

    [Header("Provider Discovery")]
    public bool autoFindProvider = true;

    [Header("Flap Detection")]
    public float minConfidence = 0.3f;
    public bool requirePalmFacingDown = false;
    [Range(-1f, 1f)] public float maxPalmNormalY = 0.4f;
    public float flapStartDownwardSpeed = 0.7f;
    public float flapResetDownwardSpeed = 0.2f;
    public float flapCooldown = 0.12f;

    [Header("Flap Strength")]
    public float maxTrackedDownwardSpeed = 2.5f;
    public float minFlapStrength = 1f;
    public float maxFlapStrength = 2f;
    public bool scaleSidewaysForceWithStrength = true;
    public AnimationCurve speedToStrengthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private FlyingPenguinController flyingPenguinController;
    private readonly HandFlapState leftState = new HandFlapState();
    private readonly HandFlapState rightState = new HandFlapState();
    private bool warnedMissingProvider;

    private class HandFlapState
    {
        public bool armed;
        public float nextAllowedTime;
    }

    void Awake()
    {
        flyingPenguinController = GetComponent<FlyingPenguinController>();
        TryAssignProvider();
    }

    void Update()
    {
        if (!TryAssignProvider() || leapProvider.CurrentFrame == null)
        {
            ResetHandState(leftState);
            ResetHandState(rightState);
            return;
        }

        Frame frame = leapProvider.CurrentFrame;
        ProcessHand(frame.GetHand(Chirality.Left), leftState, true);
        ProcessHand(frame.GetHand(Chirality.Right), rightState, false);
    }

    private bool TryAssignProvider()
    {
        if (leapProvider != null)
        {
            return true;
        }

        if (!autoFindProvider)
        {
            return false;
        }

        leapProvider = Hands.Provider;
        if (leapProvider == null && !warnedMissingProvider)
        {
            Debug.LogWarning("LeapWingInputController could not find a LeapProvider in the scene.", this);
            warnedMissingProvider = true;
        }

        return leapProvider != null;
    }

    private void ProcessHand(Hand hand, HandFlapState state, bool isLeftHand)
    {
        if (!IsHandValid(hand))
        {
            ResetHandState(state);
            return;
        }

        float downwardSpeed = Mathf.Max(0f, -hand.PalmVelocity.y);

        if (downwardSpeed <= flapResetDownwardSpeed)
        {
            state.armed = true;
        }

        if (!state.armed || Time.time < state.nextAllowedTime || downwardSpeed < flapStartDownwardSpeed)
        {
            return;
        }

        float strength = EvaluateStrength(downwardSpeed);
        float sidewaysStrength = scaleSidewaysForceWithStrength ? strength : 1f;

        if (isLeftHand)
        {
            flyingPenguinController.FlapLeft(strength, sidewaysStrength);
        }
        else
        {
            flyingPenguinController.FlapRight(strength, sidewaysStrength);
        }

        state.armed = false;
        state.nextAllowedTime = Time.time + flapCooldown;
    }

    private bool IsHandValid(Hand hand)
    {
        if (hand == null || hand.Confidence < minConfidence)
        {
            return false;
        }

        if (requirePalmFacingDown && hand.PalmNormal.y > maxPalmNormalY)
        {
            return false;
        }

        return true;
    }

    private float EvaluateStrength(float downwardSpeed)
    {
        float normalizedSpeed = Mathf.InverseLerp(flapStartDownwardSpeed, maxTrackedDownwardSpeed, downwardSpeed);
        float curveValue = speedToStrengthCurve.Evaluate(normalizedSpeed);
        return Mathf.Lerp(minFlapStrength, maxFlapStrength, curveValue);
    }

    private static void ResetHandState(HandFlapState state)
    {
        state.armed = false;
    }
}
