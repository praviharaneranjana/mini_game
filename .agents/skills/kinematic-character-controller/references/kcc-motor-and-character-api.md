# Kinematic Character Motor Configuration & Collision API

This reference details the settings, ground stability algorithms, and collision parameters of `KinematicCharacterMotor`.

---

## 1. Key Inspector Settings in `KinematicCharacterMotor`

| Setting Category | Field | Recommended Value | Impact |
| :--- | :--- | :--- | :--- |
| **Capsule Geometry** | `CapsuleRadius` | `0.45m` - `0.5m` | Radius of the character collision volume. |
| | `CapsuleHeight` | `1.8m` - `2.0m` | Total standing capsule height. |
| | `CapsuleYOffset` | `Height / 2` | Centers the pivot at the feet for easy ground alignment. |
| **Grounding & Slopes** | `MaxStableSlopeAngle` | `45°` - `50°` | Slopes steeper than this angle cause the character to slip/slide. |
| | `MaxStepHeight` | `0.3m` - `0.5m` | Maximum vertical obstacle height the motor can climb without jumping. |
| | `StableGroundLayers` | `Default, Environment` | LayerMask determining which colliders the motor treats as walkable terrain. |
| **Step & Snapping** | `StepHandling` | `Extra` | Prevents camera jitter when stepping up ledges. |
| | `LedgeHandling` | `True` | Allows smooth ground detachment when walking off ledges. |
| | `InteractiveRigidbodyHandling`| `True` | Pushes dynamic rigidbodies with mass-proportional forces. |

---

## 2. Stability & Collision Callbacks

### `ProcessHitStabilityReport`
Fires whenever the motor sweeps against a surface to decide whether it is stable ground:
```csharp
public void ProcessHitStabilityReport(
    Collider hitCollider,
    Vector3 hitNormal,
    Vector3 hitPoint,
    Vector3 atCharacterPosition,
    Quaternion atCharacterRotation,
    ref HitStabilityReport hitStabilityReport)
{
    // Example: Treat specific tags as slippery / non-walkable regardless of angle
    if (hitCollider.CompareTag("Ice"))
    {
        hitStabilityReport.IsStable = false;
    }
}
```

### `OnMovementHit`
Fires when character movement is blocked by an obstacle:
```csharp
public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
{
    // Apply damage if colliding with hazard, or deflect velocity
    if (hitCollider.TryGetComponent<DamageHazard>(out var hazard))
    {
        hazard.TakeDamage(10);
    }
}
```

---

## 3. Dynamic Gravity & Spherical Worlds

KCC supports non-vertical gravity orientations (e.g. planetary walking, wall running):
```csharp
// Set character up vector towards planet center
Vector3 toPlanetCenter = (planetOrigin - transform.position).normalized;
Motor.SetRotation(Quaternion.FromToRotation(transform.up, -toPlanetCenter) * transform.rotation);
```
