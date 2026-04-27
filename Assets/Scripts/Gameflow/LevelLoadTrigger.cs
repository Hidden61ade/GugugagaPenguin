using UnityEngine;

public class LevelLoadTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            // 记录企鹅进入当前关卡时的位置和速度
            FlyingPenguinController penguin = other.GetComponent<FlyingPenguinController>();
            if (penguin != null)
            {
                penguin.RecordLevelEntry();
            }

            LevelController.Instance.MoveSceneAhead();
            hasTriggered = true;
        }
    }
}
