# Virtual Joystick Integration & Deadzone Calibration

This reference details axis sensitivity, deadzone thresholding, and twin-stick setups using `VirtualJoystick`.

---

## 1. Deadzone & Sensitivity Tuning

- **Deadzone**: Value between `0.0` and `0.5` (default `0.1`). Prevents character drift caused by slight thumb resting tremors.
- **Radius / Handle Clamping**: Limits how far the joystick handle can travel from the center base.
- **Normalized Vector**: `GetAxis()` returns a clamped vector with max magnitude `1.0`.

```csharp
using UnityEngine;
using Terresquall;

public class TwinStickSetup : MonoBehaviour
{
    public VirtualJoystick LeftMoveJoystick;
    public VirtualJoystick RightAimJoystick;

    private void Update()
    {
        Vector2 moveVector = LeftMoveJoystick.GetAxis();
        Vector2 aimVector = RightAimJoystick.GetAxis();

        // 1. Locomotion
        if (moveVector.magnitude > 0.1f)
        {
            // Move character
        }

        // 2. Aiming / Firing
        if (aimVector.magnitude > 0.2f)
        {
            Vector3 aimDirection = new Vector3(aimVector.x, 0, aimVector.y);
            transform.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            // Fire weapon
        }
    }
}
```
