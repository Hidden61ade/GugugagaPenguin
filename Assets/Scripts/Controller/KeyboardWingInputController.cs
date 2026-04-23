using UnityEngine;

/// <summary>
/// Temporary keyboard input source for testing wing flaps.
/// A key triggers the left flap, L key triggers the right flap.
/// </summary>
[RequireComponent(typeof(FlyingPenguinController))]
public class KeyboardWingInputController : MonoBehaviour
{
    [Header("Debug")]
    public bool logFlapDebug = true;

    private FlyingPenguinController flyingPenguinController;

    void Start()
    {
        flyingPenguinController = GetComponent<FlyingPenguinController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            flyingPenguinController.FlapLeft();
            if (logFlapDebug)
            {
                Debug.Log("[KeyboardFlap] side=Left key=A/LeftArrow upwardStrength=1.000 sidewaysStrength=1.000", this);
            }
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            flyingPenguinController.FlapRight();
            if (logFlapDebug)
            {
                Debug.Log("[KeyboardFlap] side=Right key=D/RightArrow upwardStrength=1.000 sidewaysStrength=1.000", this);
            }
        }
    }
}
