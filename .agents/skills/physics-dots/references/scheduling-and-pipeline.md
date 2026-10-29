# Scheduling & System Pipeline in Unity Physics

Understanding how Unity Physics executes within the ECS system hierarchy is essential for building stable simulations and avoiding memory corruption or integrity check assertions.

---

## 1. System Group Hierarchy

Below is the execution flow inside the ECS frame loop (Unity Physics 1.x):

```text
SimulationSystemGroup
└── FixedStepSimulationGroup (executes 0, 1, or multiple times per frame)
    ├── BeforePhysicsSystemGroup
    │   └── EnsureUniqueColliderSystem
    └── PhysicsSystemGroup
        ├── PhysicsInitializeGroup
        │   └── PhysicsBuildWorldGroup (BuildPhysicsWorld, copies transforms/velocities to internal buffers)
        ├── PhysicsSimulationGroup
        │   ├── PhysicsCreateBodyPairsGroup (BroadphaseSystem)
        │   ├── PhysicsCreateContactsGroup (NarrowphaseSystem)
        │   ├── PhysicsCreateJacobiansGroup (CreateJacobiansSystem)
        │   └── PhysicsSolveAndIntegrateGroup (SolveAndIntegrateSystem)
        ├── ExportPhysicsWorld (Writes solved transforms and velocities back to ECS components)
        └── AfterPhysicsSystemGroup (User gameplay systems, event listeners, impulse handlers)
└── TransformSystemGroup
    └── SmoothRigidBodiesGraphicalMotion (Interpolates visual entities between ticks)
LateSimulationSystemGroup
└── PhysicsDebugDisplayGroup (Visual debugging wireframes)
```

> [!NOTE]
> When using **Netcode for Entities**, `PhysicsSystemGroup` is placed inside `PredictedFixedStepSimulationGroup` for client-side prediction and server reconciliation.

---

## 2. The Golden Rules of Physics Scheduling

### Rule 1: No Structural Changes Between Initialize and Export
From the moment `PhysicsInitializeGroup` runs until `ExportPhysicsWorld` completes:
- **Entities must remain in the exact same archetypes and chunk locations**.
- **Forbidden actions**:
  - `EntityManager.CreateEntity()` or `DestroyEntity()`
  - `EntityManager.AddComponent()` or `RemoveComponent()`
  - `EntityManager.SetSharedComponentManaged()`
  - Instantiating prefabs
- **Failure symptom**: `ExportPhysicsWorld` performs archetype/chunk integrity checks in Editor and Development builds; violating this throws an integrity check error or causes memory corruption.
- **Solution**: Record structural commands using an `EntityCommandBuffer` (ECB) and play it back in `AfterPhysicsSystemGroup` or during the frame boundary.

### Rule 2: Never Write to Velocity or Transform Between Initialize and Export
- `PhysicsBuildWorldGroup` reads `LocalTransform` and `PhysicsVelocity` into native internal physics buffers at the start of the simulation.
- `ExportPhysicsWorld` writes the solved results back to `LocalTransform` and `PhysicsVelocity` at the end of the simulation.
- **Any direct write made to these components between these systems is overwritten and lost**.

### Rule 3: Put Gameplay Response Systems in `AfterPhysicsSystemGroup`
- Always place custom systems that:
  - Process `CollisionEvents`, `TriggerEvents`, or `ImpulseEvents`
  - Modify body velocities based on triggers or controls
  - Apply physical forces or impulses
  - Break joints or destroy entities upon impact
- Pattern:
  ```csharp
  [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
  public partial struct MyPhysicsGameplaySystem : ISystem
  {
      // Safe to read events, safe to write velocities, safe to schedule ECBs
  }
  ```

---

## 3. System Ordering Attributes Reference

| Attribute | Execution Frequency | Timing | Best For |
| :--- | :--- | :--- | :--- |
| `[UpdateBefore(typeof(FixedStepSimulationGroup))]` | Once per render frame | Before any physics steps run | Gathering player input, camera updates |
| `[UpdateAfter(typeof(FixedStepSimulationGroup))]` | Once per render frame | After all physics steps run | Updating HUD, graphics sync |
| `[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]` | Every physics tick | Immediately before physics builds world | Kinematic pathing, procedural collider prep |
| `[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]` | Every physics tick | Immediately after physics exports world | **Collision reaction, applying forces, trigger logic** |

---

## 4. Time and DeltaTime Management

When updating physics bodies, the passage of time must align with the simulation rate:

```csharp
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct WaterBuoyancySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // SystemAPI.Time.DeltaTime correctly evaluates to FixedDeltaTime inside FixedStepSimulationGroup
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (velocity, mass, transform, buoyancy) in
            SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<PhysicsMass>, RefRO<LocalTransform>, RefRO<Buoyancy>>())
        {
            float3 upwardForce = math.up() * buoyancy.ValueRO.Strength * dt;
            velocity.ValueRW.ApplyLinearImpulse(mass.ValueRO, upwardForce);
        }
    }
}
```

If the system was erroneously placed in `SimulationSystemGroup` instead of `AfterPhysicsSystemGroup`:
- It would run once per render frame instead of once per physics update.
- During frame drops (where 3 physics updates happen in 1 frame), forces would only be applied once instead of three times, causing simulation inconsistency and tunneling.
