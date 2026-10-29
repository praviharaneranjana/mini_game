---
name: virtual-joysticks
description: Implements and configures on-screen virtual joysticks for mobile touch input using Virtual Joystick Pack (Terresquall.VirtualJoystick). Covers Fixed, Floating, and Dynamic joystick modes, deadzone tuning, axis clamping, and integration with character controllers.
---

# Virtual Joystick Pack Skill

This skill guides setting up, customizing, and wiring mobile on-screen joysticks using the **Virtual Joystick Pack** (`Terresquall` namespace).

## Core Concepts & Joystick Modes

| Joystick Mode | Behavior | Best For |
| :--- | :--- | :--- |
| **Fixed Joystick** | Joystick stays at a fixed screen coordinate (e.g. bottom-left corner). Only touches starting within the base move the knob. | Standard twin-stick shooters, retro action games. |
| **Floating Joystick** | Positioned in a designated screen zone, but snaps to the touch contact position when a finger touches down. | Open-world RPGs, platformers with relaxed thumb positions. |
| **Dynamic Joystick** | Invisible until the user touches the screen zone; appears at touch location and moves with the finger if dragged beyond radius. | Fast-paced combat games and competitive mobile titles. |

---

## 1. Quick Setup in Scene

1. Add a Canvas to the scene (`Render Mode: Screen Space - Overlay` with `Canvas Scaler: Scale With Screen Size`).
2. Instantiate `Assets/VirtualJoystick/Prefabs/Fixed Joystick.prefab` (or `Floating Joystick.prefab`) as a child of the Canvas.
3. In your character locomotion script:

```csharp
using UnityEngine;
using Terresquall;

public class MobilePlayerMover : MonoBehaviour
{
    public VirtualJoystick MoveJoystick;
    public float MoveSpeed = 6f;

    private void Update()
    {
        // Query axis values [-1.0 to 1.0]
        Vector2 input = MoveJoystick.GetAxis();
        float horizontal = input.x;
        float vertical = input.y;

        // Apply movement relative to camera
        Vector3 direction = new Vector3(horizontal, 0f, vertical);
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.Translate(direction * MoveSpeed * Time.deltaTime, Space.World);
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
```

---

## 2. Integration with Unity Starter Assets
Connect `VirtualJoystick` directly to `StarterAssetsInputs`:
```csharp
using UnityEngine;
using StarterAssets;
using Terresquall;

public class VirtualJoystickToStarterAssets : MonoBehaviour
{
    public VirtualJoystick MoveJoystick;
    public StarterAssetsInputs TargetInputs;

    private void Update()
    {
        TargetInputs.MoveInput(MoveJoystick.GetAxis());
    }
}
```

---

## Detailed References
- [Virtual Joystick Integration & Deadzone Calibration](file:///d:/Unity%20Projects/mini_game/.agents/skills/virtual-joysticks/references/joystick-integration-patterns.md)
