# Events and Impulses in Unity Physics

Unity Physics provides three event streams during simulation:
1. **`CollisionEvents`**: Raised when solid physical colliders touch or impact.
2. **`TriggerEvents`**: Raised when a body overlaps a trigger volume.
3. **`ImpulseEvents`**: Raised when forces acting across a joint exceed its configured break threshold.

All event processing belongs inside systems updating in `[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]`.

---

## 1. Enabling Event Generation on Colliders

By default, Unity Physics skips event generation for performance. Colliders must be configured with specific response policies:

| Event Type | Authoring Setting | Baked Policy (`CollisionResponsePolicy`) |
| :--- | :--- | :--- |
| **Solid Collision** | Check **"Provides Contacts"** in Collider | `CollisionResponsePolicy.CollideRaiseCollisionEvents` |
| **Trigger Overlap** | Check **"Is Trigger"** in Collider | `CollisionResponsePolicy.RaiseTriggerEvents` |

An event is raised only if at least one of the colliding bodies has enabled event generation.

---

## 2. Collision Events (`sim.CollisionEvents`)

### Main Thread Processing Pattern

```csharp
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct BrickDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        // CRITICAL: Synchronize background simulation jobs before reading events
        sim.FinalJobHandle.Complete();

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        const float MinImpactThreshold = 2.0f; // Filter resting contacts!

        foreach (var collisionEvent in sim.CollisionEvents)
        {
            // CRITICAL: Entity ordering is non-deterministic.
            // EntityA may be the Ball and EntityB the Brick, or vice-versa!
            Entity brickEntity = Entity.Null;
            Entity ballEntity = Entity.Null;

            if (SystemAPI.HasComponent<Brick>(collisionEvent.EntityA) && SystemAPI.HasComponent<Ball>(collisionEvent.EntityB))
            {
                brickEntity = collisionEvent.EntityA;
                ballEntity = collisionEvent.EntityB;
            }
            else if (SystemAPI.HasComponent<Brick>(collisionEvent.EntityB) && SystemAPI.HasComponent<Ball>(collisionEvent.EntityA))
            {
                brickEntity = collisionEvent.EntityB;
                ballEntity = collisionEvent.EntityA;
            }
            else
            {
                continue; // Not a ball-vs-brick collision
            }

            // Calculate impact details (impulses, normals, contact points)
            var details = collisionEvent.CalculateDetails(ref physicsWorld);

            // Filter out resting/sliding contacts
            if (details.EstimatedImpulse < MinImpactThreshold)
                continue;

            var brick = SystemAPI.GetComponentRW<Brick>(brickEntity);
            brick.ValueRW.Hitpoints -= details.EstimatedImpulse;

            if (brick.ValueRO.Hitpoints <= 0)
            {
                ecb.DestroyEntity(brickEntity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

## 3. Trigger Events and Stateless Enter/Exit State Machine

Because Unity Physics is **completely stateless**, it does NOT provide `OnTriggerEnter` or `OnTriggerExit` callbacks. If an entity remains inside a trigger for 100 ticks, a `TriggerEvent` is produced on all 100 ticks.

To detect **Enter** and **Exit** transitions, track the physics update tick:

### State Machine Model

```csharp
public enum ZoneState : byte
{
    Outside, // Player is not inside zone
    Enter,   // Player entered this tick (was not inside last tick)
    Inside,  // Player remains inside zone (was inside last tick)
    Exit     // Player exited this tick (was inside last tick, but not this tick)
}

public struct TriggerZone : IComponentData
{
    public ZoneState State;
    public uint LastPhysicsUpdateCount;
}
```

### Complete Update Logic

```csharp
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct TriggerZoneSystem : ISystem
{
    private uint _physicsTick;

    public void OnUpdate(ref SystemState state)
    {
        _physicsTick++;
        var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
        sim.FinalJobHandle.Complete();

        // 1. Process active triggers for this tick -> mark ENTER or INSIDE
        foreach (var triggerEvent in sim.TriggerEvents)
        {
            Entity zoneEntity = Entity.Null;
            if (SystemAPI.HasComponent<TriggerZone>(triggerEvent.EntityA))
                zoneEntity = triggerEvent.EntityA;
            else if (SystemAPI.HasComponent<TriggerZone>(triggerEvent.EntityB))
                zoneEntity = triggerEvent.EntityB;

            if (zoneEntity == Entity.Null) continue;

            var zone = SystemAPI.GetComponentRW<TriggerZone>(zoneEntity);
            zone.ValueRW.LastPhysicsUpdateCount = _physicsTick;

            if (zone.ValueRO.State == ZoneState.Enter)
                zone.ValueRW.State = ZoneState.Inside;
            else if (zone.ValueRO.State == ZoneState.Outside || zone.ValueRO.State == ZoneState.Exit)
                zone.ValueRW.State = ZoneState.Enter;
        }

        // 2. Identify inactive triggers -> mark EXIT or OUTSIDE
        foreach (var zone in SystemAPI.Query<RefRW<TriggerZone>>())
        {
            if (zone.ValueRO.LastPhysicsUpdateCount == _physicsTick)
                continue; // Was active this tick

            if (_physicsTick - zone.ValueRO.LastPhysicsUpdateCount == 1)
                zone.ValueRW.State = ZoneState.Exit; // Active last tick, gone this tick
            else
                zone.ValueRW.State = ZoneState.Outside;
        }
    }
}
```

---

## 4. Impulse Events & Joint Breaking (`sim.ImpulseEvents`)

In PhysX, joints break automatically when exceeding break limits. **In Unity Physics, joints DO NOT auto-break**.
When a joint exceeds `BreakForce` or `BreakTorque`, the solver raises an `ImpulseEvent`. The user's code is responsible for severing the joint.

```csharp
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct JointBreakSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
        sim.FinalJobHandle.Complete();

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var impulseEvent in sim.ImpulseEvents)
        {
            // Note: An impulse event is produced for both bodies connected by the joint.
            // Calling DestroyEntity multiple times on the same JointEntity in one ECB is completely safe.
            ecb.DestroyEntity(impulseEvent.JointEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

## 5. Jobified Event Streams (`ICollisionEventsJob`, `ITriggerEventsJob`)

For optimal performance in high-density scenes, avoid iterating event streams on the main thread. Instead, schedule event jobs directly on the simulation:

```csharp
[BurstCompile]
public struct TriggerProcessingJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<CharacterTag> CharacterLookup;
    public EntityCommandBuffer ECB;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity character = Entity.Null;
        if (CharacterLookup.HasComponent(triggerEvent.EntityA))
            character = triggerEvent.EntityA;
        else if (CharacterLookup.HasComponent(triggerEvent.EntityB))
            character = triggerEvent.EntityB;

        if (character != Entity.Null)
        {
            // Enqueue lightweight buffer event or tag
        }
    }
}

// Scheduled via:
// JobHandle jobHandle = new TriggerProcessingJob { ... }.Schedule(sim, ref physicsWorld, state.Dependency);
```
