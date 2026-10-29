# First-Person & Third-Person Controller Mechanics

This reference details the internal logic of `ThirdPersonController.cs` and `FirstPersonController.cs`.

---

## 1. Ground Checking & SphereCast Calibration

Both controllers perform ground detection via `Physics.CheckSphere`:
```csharp
private void GroundedCheck()
{
    // Sphere position at character base
    Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
    Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

    // Update Animator parameter
    if (_hasAnimator)
    {
        _animator.SetBool(_animIDGrounded, Grounded);
    }
}
```

### Tuning Ground Detection
- `GroundedOffset`: Default `-0.14m`. Adjust if character hovers or gets stuck in fall states on stairs.
- `GroundedRadius`: Default `0.28m`. Should be slightly smaller than the character controller radius (`0.5m`) to avoid snagging on vertical walls.

---

## 2. Speed Interpolation & Rotation Smoothing

Third-person controller smoothly rotates towards target input relative to camera forward:
```csharp
// Target rotation based on camera angle + input angle
float targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref _rotationVelocity, RotationSmoothTime);

// Rotate character towards move direction
transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
```
