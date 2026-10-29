# Kinematics, Bodies & Gameplay Patterns in Unity Physics

This guide explains the physical and performance differences between Static Colliders, Kinematic Bodies, and Dynamic Bodies, along with patterns for character controllers, elevators, and fluid buoyancy.

---

## 1. Body Types Comparison

| Attribute | Static Collider | Kinematic Body | Dynamic Body |
| :--- | :--- | :--- | :--- |
| **Mass & Inertia** | Conceptual Infinite | Infinite ($m = \infty, I = \infty$) | Finite (configured in `PhysicsMass`) |
| **Velocity** | None | User-controlled (`PhysicsVelocity`) | Solver-computed + impulses |
| **Affected by Forces?** | No | No | Yes (gravity, friction, collisions) |
| **Affects Dynamic Bodies?** | Yes (solid immovable obstacle) | Yes (pushes with velocity momentum) | Yes (two-way momentum exchange) |
| **BVH Location** | Static BVH | Dynamic BVH | Dynamic BVH |
| **Runtime Movement Cost** | **Catastrophic** (rebuilds entire static BVH) | **Zero penalty** (built into dynamic step) | **Zero penalty** |
| **Interactions with Resting Bodies** | Treated as sudden teleportation | Transfers continuous momentum | Full momentum exchange |

---

## 2. Moving Platforms and Elevators (The Kinematic Pattern)

### Why Moving Static Colliders is a Critical Mistake:
1. **BVH Rebuild Spikes**: Moving a static collider forces Unity Physics to destroy and reconstruct the static AABB Bounding Volume Hierarchy, stalling the CPU.
2. **Teleportation Artifacts**: The solver perceives static collider motion as a zero-time teleport. Bodies resting on top do not inherit momentum, causing them to slide off, jitter, or tunnel through the floor.

### The Professional Kinematic Elevator Pattern
Mark the elevator platform as **Kinematic** (`PhysicsMass` omitted, or `PhysicsMassOverride.IsKinematic = 1`).
**Control movement exclusively by setting `PhysicsVelocity.Linear`**, not by writing `LocalTransform`:

```csharp
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct ElevatorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (elevator, transform, velocity) in
            SystemAPI.Query<RefRO<ElevatorConfig>, RefRO<LocalTransform>, RefRW<PhysicsVelocity>>())
        {
            float currentY = transform.ValueRO.Position.y;

            // Maintain constant upward velocity
            if (velocity.ValueRW.Linear.y >= 0 && currentY < elevator.ValueRO.MaxHeight)
            {
                velocity.ValueRW.Linear.y = elevator.ValueRO.TravelSpeed;
            }
            // Reverse direction at top
            else if (currentY >= elevator.ValueRO.MaxHeight)
            {
                velocity.ValueRW.Linear.y = -elevator.ValueRO.TravelSpeed;
            }
            // Reverse direction at bottom
            else if (currentY <= elevator.ValueRO.MinHeight)
            {
                velocity.ValueRW.Linear.y = elevator.ValueRO.TravelSpeed;
            }
        }
    }
}
```

By letting the physics engine integrate the kinematic velocity:
- Boxes resting on the platform smoothly travel with it.
- When reaching walls or obstacles, the physics solver correctly resolves contact friction and constraints.

---

## 3. Applying Forces vs. Overwriting Velocity

### The Anti-Pattern: Hard Direct Assignment
```csharp
// BAD: Overwrites all existing physical forces, wipes out momentum, causes clipping
velocity.ValueRW.Linear = moveDirection * speed;
```

### The Professional Pattern: Impulses and DeltaTime Scaling
Use `ApplyLinearImpulse` or `ApplyExplosionForce` to accumulate acceleration on top of existing physical interactions:

```csharp
// GOOD: Adds impulse while preserving momentum and collision response
velocity.ValueRW.ApplyLinearImpulse(mass.ValueRO, impulseVector);
```

### Explosion Force Pattern
`ApplyExplosionForce` automatically calculates surface contact point and induces realistic rotational torque:

```csharp
velocity.ValueRW.ApplyExplosionForce(
    mass.ValueRO,
    collider.ValueRO,
    transform.ValueRO.Position,
    transform.ValueRO.Rotation,
    explosionStrength,  // Negative value makes this an implosion/gravity well!
    explosionEpicenter,
    explosionRadius,    // Radius (0 makes reach infinite with no falloff)
    deltaTime,
    math.up());
```

---

## 4. Buoyancy & Water Zone Simulation

Implementing fluid dynamics using an enableable `Buoyancy` component and a trigger volume:

```csharp
public struct Buoyancy : IComponentData, IEnableableComponent
{
    public float WaterSurfaceY;
    public float BuoyancyForce;
    public float Drag;
}

[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct FluidSimulationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (buoyant, transform, velocity, mass) in
            SystemAPI.Query<RefRO<Buoyancy>, RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRO<PhysicsMass>>())
        {
            float3 currentPos = transform.ValueRO.Position;
            float immersionDepth = buoyant.ValueRO.WaterSurfaceY - currentPos.y;

            if (immersionDepth > 0)
            {
                // Upward Archimedes thrust proportional to immersion depth
                float thrust = immersionDepth * buoyant.ValueRO.BuoyancyForce * dt;
                velocity.ValueRW.ApplyLinearImpulse(mass.ValueRO, new float3(0, thrust, 0));

                // Hydrodynamic linear damping / drag
                velocity.ValueRW.Linear *= math.clamp(1.0f - buoyant.ValueRO.Drag * dt, 0f, 1f);
            }
        }
    }
}
```
