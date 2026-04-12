using UnityEngine;

/// <summary>
/// Temporary keyboard input source for testing wing flaps.
/// A key triggers the left flap, L key triggers the right flap.
/// </summary>
[RequireComponent(typeof(FlyingPenguinController))]
public class KeyboardWingInputController : MonoBehaviour
{
    private FlyingPenguinController flyingPenguinController;

    void Start()
    {
        flyingPenguinController = GetComponent<FlyingPenguinController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            flyingPenguinController.FlapLeft();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            flyingPenguinController.FlapRight();
        }
    }
}
