# Unity Character Controllers and Starter Assets Guidelines

This rule establishes technical standards for authoring player character controllers using Unity Starter Assets (`StarterAssets`).

## Core Directives

1. **Input System Architecture**:
   - Route all input events through `StarterAssetsInputs`. Never query legacy `Input.GetAxis()` directly in gameplay character scripts.
   - For cross-platform gameplay (Keyboard/Mouse, Gamepad, Touch), bind inputs to the `Player` action map in `StarterAssets.inputactions`.

2. **Ground Detection & Physics Tuning**:
   - Tune `GroundedOffset` and `GroundedRadius` carefully to match the character model's foot alignment.
   - Ensure `GroundLayers` exclude triggers, invisible boundaries, and dynamic pick-up colliders to avoid false grounded states while jumping over objects.

3. **Cinemachine Look & Camera Smoothing**:
   - First-person and Third-person cameras must target `CinemachineCameraTarget` on the player root.
   - Clamp pitch angles (`BottomClamp` to `TopClamp`) to prevent gimbal lock and camera flipping.
