# Joints and Motors in Unity Physics

Joints establish persistent kinematic constraints between two rigid body entities, restricting their relative Degrees of Freedom (DoFs). Motors add active, powered constraints that exert forces to drive bodies toward target positions, angles, or velocities.

---

## 1. Joint Types Reference

| Joint Type | Allowed Motion | Real-World Analog |
| :--- | :--- | :--- |
| **Fixed** | None (locks 3 linear + 3 angular DoFs) | Welded beams, glued parts, armor attachments |
| **Hinge** | Free rotation around a single axis | Revolving doors, wheels, spinning carousels |
| **Limited Hinge** | Articulated rotation within min/max angular limits | Knees, elbows, finger knuckles |
| **Ball and Socket** | Free 3-axis spherical rotation | Shoulders, hips, trailer hitches |
| **Prismatic** | Linear translation along a single axis | Elevators, sliding doors, hydraulic pistons |
| **Stiff Spring** | Maintains a target rest distance with restorative force | Suspension, bungee cords, suspension bridges |
| **Ragdoll** | Multi-axis conical cone and twist limits | Humanoid and creature skeletons |

---

## 2. Motor Types Reference

Motors actively inject energy into the simulation to reach and sustain mechanical targets:

| Motor Type | Mechanism | Usage |
| :--- | :--- | :--- |
| **Rotation Motor** | Drives a hinge toward a specific target relative angle | Robotic servo arms, automated gates |
| **Angular Velocity Motor** | Drives a hinge at a sustained angular speed (rad/sec) | Vehicle engines, propellers, turbine blades |
| **Position Motor** | Drives a prismatic slider toward a specific offset position | Power actuators, press machines |
| **Linear Velocity Motor** | Drives a prismatic slider at a sustained velocity (m/sec) | Conveyor belts, high-speed rail doors |

---

## 3. Entity Representation & Components

A joint is represented by placing two components on a dedicated Joint Entity (or directly on one of the connected rigid body entities):

1. **`PhysicsConstrainedBodyPair`**:
   - `EntityA`, `EntityB`: The two entities tethered by the constraint.
   - `EnableCollision` (0 or 1): Determines whether the two constrained bodies continue to collide with each other.
2. **`PhysicsJoint`**:
   - Stores the constraint mathematical equations, anchor transforms (`BodyFrames`), and joint limits.

```csharp
public static Entity CreateHingeJointEntity(
    EntityManager entityManager,
    Entity bodyA,
    Entity bodyB,
    RigidTransform frameA,
    RigidTransform frameB)
{
    Entity jointEntity = entityManager.CreateEntity();

    entityManager.AddComponentData(jointEntity, new PhysicsWorldIndex { Value = 0 });

    entityManager.AddComponentData(jointEntity, new PhysicsConstrainedBodyPair
    {
        EntityA = bodyA,
        EntityB = bodyB,
        EnableCollision = 0 // Disable collision between linked segments
    });

    // Create the PhysicsJoint using the built-in factory
    PhysicsJoint joint = PhysicsJoint.CreateHinge(frameA, frameB);
    entityManager.AddComponentData(jointEntity, joint);

    return jointEntity;
}
```

---

## 4. Breakable Joints and Impulse Events

Unlike PhysX `FixedJoint.breakForce` which automatically destroys the component internally, Unity Physics is strictly stateless and data-driven:

1. When forces across a joint exceed its configured break threshold, Unity Physics generates an **`ImpulseEvent`**.
2. An event listener system must consume the event and queue entity destruction in an ECB.

```csharp
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct BreakableJointSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
        sim.FinalJobHandle.Complete();

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var impulseEvent in sim.ImpulseEvents)
        {
            // Sever the joint by destroying the joint entity
            ecb.DestroyEntity(impulseEvent.JointEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

## 5. Architectural Tip: Compound Colliders vs. Fixed Joints

> [!TIP]
> **Avoid Chains of Fixed Joints for Rigid Multi-Part Objects**:
> Iterative velocity solvers have inherent numerical compliance (elasticity). Under violent impacts or heavy loads, chains of rigid bodies connected by fixed joints will slightly bend, flex, or oscillate before coming to rest.
> 
> **Better Design**:
> For destructible structures (e.g., stickmen, breaking bridges, barricades):
> 1. Represent the intact object as a single dynamic body with a **Compound Collider** (rigid and computationally cheap).
> 2. When a component breaks off, remove that child collider from the compound and spawn a separate dynamic rigid body for the broken debris.
