# Custom Movement States: Crouching, Swimming, Sliding & Ladders in KCC

This reference demonstrates structuring a state-driven character controller implementing `ICharacterController`.

---

## 1. Character State Machine Pattern

```csharp
using UnityEngine;
using KinematicCharacterController;

public enum CharacterState
{
    Default,
    Crouching,
    Sliding,
    Swimming,
    Climbing
}

public class AdvancedKCCPlayer : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;
    public CharacterState CurrentState = CharacterState.Default;

    [Header("Speeds")]
    public float WalkSpeed = 6f;
    public float SprintSpeed = 10f;
    public float CrouchSpeed = 3f;

    [Header("Capsule Dimensions")]
    public float StandHeight = 2.0f;
    public float CrouchHeight = 1.0f;

    public void TransitionToState(CharacterState newState)
    {
        CharacterState previousState = CurrentState;
        CurrentState = newState;

        switch (newState)
        {
            case CharacterState.Crouching:
                Motor.SetCapsuleDimensions(radius: 0.45f, height: CrouchHeight, yOffset: CrouchHeight * 0.5f);
                break;

            case CharacterState.Default:
                // Stand up only if headroom is clear
                Motor.SetCapsuleDimensions(radius: 0.45f, height: StandHeight, yOffset: StandHeight * 0.5f);
                break;
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        switch (CurrentState)
        {
            case CharacterState.Default:
                // Normal walk/run logic
                break;
            case CharacterState.Crouching:
                // Reduced speed and custom friction
                break;
            case CharacterState.Sliding:
                // Slope gravity boost with low friction
                currentVelocity += Motor.GroundingStatus.GroundNormal * 15f * deltaTime;
                break;
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) { }
    public void BeforeCharacterUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
}
```
