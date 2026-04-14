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

    [Header("Debug")]
    public bool logFlapDebug = true;
    public bool logTrackingStatus = true;
    public float trackingStatusLogInterval = 1.5f;

    private FlyingPenguinController flyingPenguinController;
    private readonly HandFlapState leftState = new HandFlapState();
    private readonly HandFlapState rightState = new HandFlapState();
    private bool warnedMissingProvider;
    private float nextTrackingStatusLogTime;

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
            LogTrackingStatus("provider_missing_or_frame_null");
            ResetHandState(leftState);
            ResetHandState(rightState);
            return;
        }

        Frame frame = leapProvider.CurrentFrame;
        Hand leftHand = frame.GetHand(Chirality.Left);
        Hand rightHand = frame.GetHand(Chirality.Right);

        if (leftHand == null && rightHand == null)
        {
            LogTrackingStatus("frame_ok_but_no_hands");
        }

        ProcessHand(leftHand, leftState, true);
        ProcessHand(rightHand, rightState, false);
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
        if (leapProvider != null)
        {
            Debug.Log($"LeapWingInputController assigned provider '{leapProvider.name}'.", this);
        }

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

        if (logFlapDebug)
        {
            LogFlapDebug(hand, isLeftHand, downwardSpeed, strength, sidewaysStrength, state);
        }

        state.armed = false;
        state.nextAllowedTime = Time.time + flapCooldown;
    }

    private bool IsHandValid(Hand hand)
    {
        if (hand == null || hand.Confidence < minConfidence)
        {
            if (hand != null)
            {
                LogTrackingStatus(
                    $"hand_low_confidence side={(hand.IsLeft ? "Left" : "Right")} confidence={hand.Confidence:F3} min={minConfidence:F3}");
            }
            return false;
        }

        if (requirePalmFacingDown && hand.PalmNormal.y > maxPalmNormalY)
        {
            LogTrackingStatus(
                $"hand_rejected_by_palm_normal side={(hand.IsLeft ? "Left" : "Right")} palmNormalY={hand.PalmNormal.y:F3} max={maxPalmNormalY:F3}");
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

    private void LogFlapDebug(Hand hand, bool isLeftHand, float downwardSpeed, float strength, float sidewaysStrength, HandFlapState state)
    {
        string side = isLeftHand ? "Left" : "Right";
        float normalizedSpeed = Mathf.InverseLerp(flapStartDownwardSpeed, maxTrackedDownwardSpeed, downwardSpeed);

        Debug.Log(
            $"[LeapFlap] side={side} " +
            $"downwardSpeed={downwardSpeed:F3} " +
            $"normalizedSpeed={normalizedSpeed:F3} " +
            $"upwardStrength={strength:F3} " +
            $"sidewaysStrength={sidewaysStrength:F3} " +
            $"confidence={hand.Confidence:F3} " +
            $"palmVelocity=({hand.PalmVelocity.x:F3}, {hand.PalmVelocity.y:F3}, {hand.PalmVelocity.z:F3}) " +
            $"palmNormal=({hand.PalmNormal.x:F3}, {hand.PalmNormal.y:F3}, {hand.PalmNormal.z:F3}) " +
            $"armedBeforeTrigger={state.armed} " +
            $"cooldown={flapCooldown:F3} " +
            $"nextAllowedTime={state.nextAllowedTime:F3}",
            this);
    }

    private static void ResetHandState(HandFlapState state)
    {
        state.armed = false;
    }

    private void LogTrackingStatus(string status)
    {
        if (!logTrackingStatus || Time.time < nextTrackingStatusLogTime)
        {
            return;
        }

        string providerName = leapProvider == null ? "null" : leapProvider.name;
        int handCount = leapProvider?.CurrentFrame?.Hands?.Count ?? -1;

        Debug.Log(
            $"[LeapTracking] status={status} provider={providerName} handCount={handCount}",
            this);

        nextTrackingStatusLogTime = Time.time + Mathf.Max(0.1f, trackingStatusLogInterval);
    }
}
