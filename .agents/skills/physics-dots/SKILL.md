---
name: physics-dots
description: Implements, diagnoses, optimizes, and writes code for Unity Physics (DOTS / ECS, Unity.Physics package) and professional game physics architecture. Use when working with Unity.Physics, PhysicsCollider, PhysicsVelocity, PhysicsMass, simulation system scheduling, collision queries, trigger/collision events, kinematics, joints, or DOTS physics baking.
allowed-tools: WebFetch, WebSearch
metadata:
  version: 1.0.0
  systems: Unity.Physics, DOTS, ECS, Entities
---

# Skill: physics-dots (Unity Physics / DOTS / ECS)

Authoritative reference and diagnostic guide for **Unity Physics** (`Unity.Physics`), the high-performance, stateless, data-oriented physics engine for Unity Entities / DOTS.

---

## Fast-Paths & Critical Rules (Read First)

### Fast-Path 1: Structural Changes During Simulation (Chunk Integrity Crash)
**Symptom**: `ExportPhysicsWorld` integrity check assertion fails, or crash during physics update after adding/removing components or destroying entities.
- **Cause**: Entities cannot change structural archetype or chunk locations between `PhysicsInitializeGroup` and `ExportPhysicsWorld`.
- **Fix**: Never make structural changes (add/remove components, instantiate, destroy entities) inside `PhysicsSystemGroup`. Use an `EntityCommandBuffer` scheduled for playback in `AfterPhysicsSystemGroup` or later.

### Fast-Path 2: Velocity Writes Overwritten / Ignored
**Symptom**: Writing to `PhysicsVelocity` or `LocalTransform` has no effect on rigid bodies.
- **Cause**: Writing to `PhysicsVelocity` between `PhysicsInitializeGroup` and `ExportPhysicsWorld` is clobbered when `ExportPhysicsWorld` writes solver results back to entities.
- **Fix**: Update velocities or apply impulses inside `[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]` or `BeforePhysicsSystemGroup`.

### Fast-Path 3: Collision Queries Return Zero Hits
**Symptom**: `CollisionWorld.CastRay()` or `CastCollider()` returns false even when pointing directly at colliders.
- **Cause**: `RaycastInput.Filter` or `ColliderCastInput.Filter` was left default-initialized (`default`), which has a 0 `CollidesWith` mask.
- **Fix**: Always assign `Filter = CollisionFilter.Default` or a valid layer mask.

### Fast-Path 4: Moving Static Colliders Causes Severe Frame Drops
**Symptom**: Significant CPU spikes when moving environment colliders (walls, doors, platforms).
- **Cause**: Modifying transforms of static colliders triggers a full, expensive rebuild of the static Bounding Volume Hierarchy (BVH).
- **Fix**: Convert moving objects to **kinematic dynamic bodies** (`PhysicsMass` absent or kinematic override) and move them by setting `PhysicsVelocity.Linear`.

### Fast-Path 5: Stateless Triggers (Missing OnEnter / OnExit)
**Symptom**: Need `OnTriggerEnter` or `OnTriggerExit` behavior in DOTS physics.
- **Cause**: Unity Physics is stateless; `sim.TriggerEvents` only reports current tick overlaps. It does not store previous frame state.
- **Fix**: Implement an Enter/Inside/Exit state tracker comparing `LastPhysicsUpdateCount` against the current update count (see [events-and-impulses.md](./references/events-and-impulses.md)).

### Fast-Path 6: Joints Do Not Break Automatically
**Symptom**: Setting `BreakForce` or `BreakTorque` on a joint does not sever the connection upon impact.
- **Cause**: Unlike PhysX, Unity Physics joints do not self-destruct. Exceeding break thresholds generates an `ImpulseEvent`.
- **Fix**: Read `sim.ImpulseEvents` in `AfterPhysicsSystemGroup` and destroy the joint entity via `EntityCommandBuffer.DestroyEntity(impulseEvent.JointEntity)`.

### Fast-Path 7: Resting Contact Jitter / False Hit Registration
**Symptom**: Continuous collision damage or audio triggers while objects rest on surfaces.
- **Cause**: Solver generates collision contacts every tick for resting bodies.
- **Fix**: Check `collisionEvent.CalculateDetails(ref physicsWorld).EstimatedImpulse` against a minimum impact threshold (e.g., `> 2.0f`).

---

## Core System Architecture & Update Loop

Unity Physics executes inside `FixedStepSimulationGroup` (running 0, 1, or multiple times per frame to maintain fixed delta time):

1. **`PhysicsInitializeGroup`**: Copies transforms and velocities from entities into the internal physics simulation world.
2. **`PhysicsSimulationGroup`**:
   - `BroadphaseSystem`: Builds dynamic BVH and finds overlapping AABBs.
   - `NarrowphaseSystem`: Performs exact geometric contact tests on overlapping pairs.
   - `CreateJacobiansSystem`: Generates constraint equations (Jacobians) from contacts and joints.
   - `SolveAndIntegrateSystem`: Solves velocities and updates positions.
3. **`ExportPhysicsWorld`**: Writes transforms and velocities back to ECS components (`LocalTransform`, `PhysicsVelocity`).
4. **`AfterPhysicsSystemGroup`**: **The correct group for game gameplay systems** that read events, modify velocity, or apply impulses.

---

## Component Taxonomy

| Component | Type | Description |
| :--- | :--- | :--- |
| `PhysicsCollider` | IComponentData | Contains `BlobAssetReference<Collider>` (geometry, filter, restitution, friction). |
| `PhysicsVelocity` | IComponentData | Linear velocity (world space) and Angular velocity (mass frame). |
| `PhysicsMass` | IComponentData | Mass, Center of Mass, Inertia Tensor, Angular Expansion. (Omit for kinematic body). |
| `PhysicsWorldIndex` | ISharedComponentData | Identifies physics world partition (default 0). Entities only interact within the same index. |
| `PhysicsDamping` | IComponentData | Linear and angular velocity decay per physics step. |
| `PhysicsGravityFactor` | IComponentData | Multiplier for global gravity on this specific body. |
| `PhysicsJoint` | IComponentData | Joint constraint definition between two bodies. |
| `PhysicsConstrainedBodyPair` | IComponentData | References the two connected entities (`EntityA`, `EntityB`). |
| `PhysicsWorldSingleton` | Singleton | Access to `PhysicsWorld` and `CollisionWorld` (queries). |
| `SimulationSingleton` | Singleton | Access to `Simulation`, `CollisionEvents`, `TriggerEvents`, `ImpulseEvents`. |

---

## Standard Code Patterns

### 1. Applying Impulses and Forces (Do Not Overwrite Velocity)
```csharp
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct ExplosionForceSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (velocity, collider, mass, transform) in 
            SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<PhysicsCollider>, RefRO<PhysicsMass>, RefRO<LocalTransform>>())
        {
            velocity.ValueRW.ApplyExplosionForce(
                mass.ValueRO, collider.ValueRO,
                transform.ValueRO.Position, transform.ValueRO.Rotation,
                100f, float3.zero, 15f, deltaTime, math.up());
        }
    }
}
```

### 2. Performing a Raycast Query
```csharp
var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
var rayInput = new RaycastInput
{
    Start = startPosition,
    End = endPosition,
    Filter = CollisionFilter.Default // MANDATORY: Never leave default(CollisionFilter)
};

if (collisionWorld.CastRay(rayInput, out RaycastHit hit))
{
    Entity hitEntity = hit.Entity;
    float3 hitPoint = hit.Position;
    float3 hitNormal = hit.SurfaceNormal;
}
```

### 3. Reading Collision Events (Main Thread)
```csharp
var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
sim.FinalJobHandle.Complete(); // Must complete outstanding simulation jobs

foreach (var collisionEvent in sim.CollisionEvents)
{
    // Always check both permutations: EntityA vs EntityB order is non-deterministic
    Entity targetEntity = Entity.Null;
    if (SystemAPI.HasComponent<Damageable>(collisionEvent.EntityA))
        targetEntity = collisionEvent.EntityA;
    else if (SystemAPI.HasComponent<Damageable>(collisionEvent.EntityB))
        targetEntity = collisionEvent.EntityB;

    if (targetEntity != Entity.Null)
    {
        var details = collisionEvent.CalculateDetails(ref physicsWorld);
        if (details.EstimatedImpulse > 2.0f) // Ignore resting contact
        {
            // Apply damage proportional to details.EstimatedImpulse
        }
    }
}
```

---

## Detailed References

For comprehensive deep dives, consult the reference documents:
- [Engine Fundamentals](./references/engine-fundamentals.md): 4-step solver loop, broadphase BVH, rigid bodies, colliders relative costs.
- [Components & Singletons](./references/components-and-singletons.md): Complete data layouts and properties.
- [Scheduling & Pipeline](./references/scheduling-and-pipeline.md): System groups, update order, ECB playback, thread safety.
- [Collision Queries](./references/collision-queries.md): Raycasts, sweeps, overlap checks, and distance queries.
- [Events & Impulses](./references/events-and-impulses.md): Collision, trigger, impulse events, enter/exit state machines, jobified processing.
- [Kinematics & Gameplay Patterns](./references/kinematics-and-bodies.md): Elevators, buoyancy, character interaction, dynamic vs kinematic.
- [Joints & Motors](./references/joints-and-motors.md): Constraints, driven motors, break thresholds.
- [Authoring & Baking](./references/authoring-and-baking.md): Subscene baking, PhysX mapping, runtime procedural generation.
