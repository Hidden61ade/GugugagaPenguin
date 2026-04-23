using UnityEngine;

public class LevelLoadTrigger : MonoBehaviour
{
    private bool hasTriggered = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            LevelController.Instance.MoveSceneAhead();
            hasTriggered = true;
        }
    }
}