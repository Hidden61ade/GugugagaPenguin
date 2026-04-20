using UnityEngine;

/// <summary>
/// Runtime finish trigger that notifies the game flow controller when the penguin reaches the goal.
/// </summary>
public class PenguinFinishTrigger : MonoBehaviour
{
    public PenguinGameFlowController controller;

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null)
        {
            return;
        }

        if (other.GetComponentInParent<FlyingPenguinController>() != null)
        {
            controller.NotifyFinishReached();
        }
    }
}
