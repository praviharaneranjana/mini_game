# FPSSample to Modern Unity: Netcode for Entities & NGO Migration

This reference provides a comprehensive bridge mapping patterns from Unity's 2018 **FPSSample** to modern production packages in Unity 2022 LTS and Unity 6:
1. **Netcode for Entities (`com.unity.netcode`)** — Pure DOTS / ECS authoritative multiplayer.
2. **Netcode for GameObjects (NGO - `com.unity.netcode.gameobjects`)** — High-level GameObject-centric multiplayer.

---

## 1. Architectural Evolution Matrix

| FPSSample Pattern (2018.3) | Modern Netcode for Entities (DOTS 1.3+ / Unity 6) | Modern Netcode for GameObjects (NGO) |
|---|---|---|
| **ReplicatedEntity** Component | `GhostAuthoringComponent` + `[GhostComponent]` attribute | `NetworkObject` + `NetworkBehaviour` |
| **ReplicatedEntityRegistry** | Subscenes + Ghost Collection baking | NetworkPrefabs list on `NetworkManager` |
| **UserCommand** struct | `IInputComponentData` struct | `INetworkSerializable` struct sent via RPC / NetworkVariable |
| **Client Prediction Loop** | Built-in `PredictedTick` & `GhostPredictionSystemGroup` | Client Network Transform with client prediction or manual replay |
| **History Rewind / Lag Compensation** | Built-in `PhysicsWorldHistorySystem` & `RaycastCommand` | `NetworkLagCompensation` or custom physics rewind buffer |
| **Multi-Gameloop (`Game.cs`)** | Unity Multi-World (`ServerWorld`, `ClientWorld`, `ThinClientWorld`) | `NetworkManager.StartServer()`, `StartClient()`, `StartHost()` |
| **Network Transport** | Early raw `com.unity.transport` (sockets / byte streams) | Modern `UnityTransport` (with Relay & DTLS encryption support) |

---

## 2. Implementing Authoritative Character in Netcode for Entities (DOTS)

In modern DOTS, Unity automates ghost serialization, interpolation, and client-side prediction through the Ghost framework.

### 2.1 The Input Component (`IInputComponentData`)
Inputs are defined as unmanaged structs implementing `IInputComponentData`:

```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerInput : IInputComponentData
{
    public float2 move;
    public float2 look;
    public InputEvent jump;
    public InputEvent fire;
}
```

### 2.2 The Character State Component
```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerCharacterData : IComponentData
{
    [GhostField(Quantization = 100)] public float3 position;
    [GhostField] public quaternion rotation;
    [GhostField(Quantization = 100)] public float3 velocity;
    [GhostField] public uint totalShotsCount;
}
```

### 2.3 The Predicted Simulation System
This system runs on both the server (authoritative) and client (predicted):

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerMovementSimulationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        foreach (var (input, charData, transform) in
                 SystemAPI.Query<RefRO<PlayerInput>, RefRW<PlayerCharacterData>, RefRW<LocalTransform>>()
                          .WithAll<Simulate>())
        {
            float3 moveDirection = new float3(input.ValueRO.move.x, 0, input.ValueRO.move.y);
            float moveSpeed = 6.0f;
            float dt = SystemAPI.Time.DeltaTime;

            charData.ValueRW.velocity = moveDirection * moveSpeed;
            charData.ValueRW.position += charData.ValueRW.velocity * dt;

            transform.ValueRW.Position = charData.ValueRW.position;
            transform.ValueRW.Rotation = quaternion.Euler(0, math.radians(input.ValueRO.look.y), 0);
        }
    }
}
```

---

## 3. Lag Compensation in Modern DOTS

Netcode for Entities provides the `PhysicsWorldHistorySystem`, which automatically stores snapshots of physics colliders for all simulated entities across network ticks.

### Performing a Lag-Compensated Raycast in DOTS:
```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using RaycastHit = Unity.Physics.RaycastHit;

public static class DotsLagCompensationUtility
{
    public static bool PerformRewoundRaycast(
        EntityManager em,
        NetworkTick shotTick,
        float3 rayStart,
        float3 rayEnd,
        CollisionFilter filter,
        out RaycastHit hit)
    {
        // 1. Get the historical physics world at shotTick
        var historySystem = em.World.GetExistingSystemManaged<PhysicsWorldHistorySystem>();
        var historicalPhysicsWorld = historySystem.GetPhysicsWorldFromTick(shotTick);

        var rayInput = new RaycastInput
        {
            Start = rayStart,
            End = rayEnd,
            Filter = filter
        };

        // 2. Cast against rewound world
        return historicalPhysicsWorld.CastRay(rayInput, out hit);
    }
}
```

---

## 4. When to Use Netcode for Entities vs Netcode for GameObjects

| Project Criteria | Recommended Package |
|---|---|
| **Max Concurrent Players $\ge 32$** (Battle Royale, Massive Battlefield) | **Netcode for Entities (DOTS)** |
| **Max Concurrent Players $\le 16$** (Arena FPS, Co-op Shooter, Tactical 5v5) | **Netcode for GameObjects (NGO)** or Netcode for Entities |
| **Physics-Heavy Environment** (Thousands of rigidbodies, destructible covers) | **Netcode for Entities** (Burst + Unity Physics) |
| **Complex Character Rigging & Third-Party Mecanim Controllers** | **Netcode for GameObjects (NGO)** |
| **Fast Time-to-Market / Small Indie Team** | **Netcode for GameObjects (NGO)** |
