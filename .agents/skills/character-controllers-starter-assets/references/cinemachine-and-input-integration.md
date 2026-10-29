# Cinemachine & Input System Integration for Starter Assets

This reference details wiring `StarterAssetsInputs` with the Unity Input System and Cinemachine virtual cameras.

---

## 1. Input Actions Mapping

`StarterAssets.inputactions` exposes four primary gameplay actions:
- `Move` (`Vector2` continuous - WASD / Left Stick)
- `Look` (`Vector2` delta - Mouse Delta / Right Stick)
- `Jump` (`Button` - Space / South Button)
- `Sprint` (`Button` - Left Shift / Left Stick Press)

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        public void OnMove(InputValue value) => MoveInput(value.Get<Vector2>());
        public void OnLook(InputValue value) => LookInput(value.Get<Vector2>());
        public void OnJump(InputValue value) => JumpInput(value.isPressed);
        public void OnSprint(InputValue value) => SprintInput(value.isPressed);
    }
}
```

---

## 2. Camera Root Pitch & Yaw Management

First-person and Third-person cameras follow `CinemachineCameraTarget` on the character:
- Pitch is clamped between `BottomClamp` and `TopClamp`.
- Yaw is unbounded (0° to 360°).
- In first-person mode, yaw rotates the entire character body.
- In third-person mode, yaw rotates only the camera rig, allowing the character body to rotate independently.
