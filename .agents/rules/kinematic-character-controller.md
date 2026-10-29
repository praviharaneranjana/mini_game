# Kinematic Character Controller (KCC) Guidelines

This project utilizes **Kinematic Character Controller** (`KinematicCharacterController`) for responsive, physics-independent character locomotion, moving platforms, and dynamic gravity.

## Core Directives

1. **Controller Separation of Concerns**:
   - `KinematicCharacterMotor` strictly handles capsule sweeps, collision projections, step climbing, and moving platform velocity inheritance. Never place gameplay input logic directly inside `KinematicCharacterMotor`.
   - Your custom script must implement `ICharacterController` and assign itself to `Motor.CharacterController`.
   - Perform rotation updates exclusively in `UpdateRotation()`, and velocity/gravity calculations in `UpdateVelocity()`.

2. **Ground & Slope Stability**:
   - Ensure walkable geometry layers are included in `Motor.StableGroundLayers`.
   - Never manually set `transform.position` or `transform.rotation` on a KCC character during gameplay; always call `Motor.SetPosition()` and `Motor.SetRotation()` to maintain continuous swept collision integrity.

3. **Moving Platforms**:
   - All moving platforms, elevators, and rotating doors that carry characters must use `PhysicsMover` implementing `IMoverController`.
   - Never move platforms via standard `Transform.Translate` or raw `Rigidbody.velocity` if characters need to stand on them without slipping.
