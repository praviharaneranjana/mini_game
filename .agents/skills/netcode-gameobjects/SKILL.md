---
name: netcode-gameobjects
description: Implements, configures, and debugs multiplayer networking with Unity Netcode for GameObjects (NGO, com.unity.netcode.gameobjects). Covers NetworkManager, NetworkBehaviour, NetworkVariable, modern unified [Rpc(SendTo)], Distributed Authority, connection approval, and object spawning.
---

# Netcode for GameObjects (NGO) Guide

Architect, implement, and debug multiplayer games using Unity's official **Netcode for GameObjects (NGO)** package (`com.unity.netcode.gameobjects`).

---

## 1. Core NGO Architecture

```mermaid
graph TD
    subgraph Engine_Layer ["Session Management"]
        NM[NetworkManager Singleton] --> Transport[UnityTransport: UDP / Relay]
        NM --> Prefabs[NetworkPrefabs Registration]
        NM --> Approval[Connection Approval Callback]
    end

    subgraph Object_Layer ["Entity Lifecycle"]
        NO[NetworkObject] --> NB[NetworkBehaviour]
        NO --> Spawn[Server/Authority: no.Spawn()]
        NO --> Despawn[Server/Authority: no.Despawn()]
    end

    subgraph Data_Sync ["State & RPC Synchronization"]
        NB --> NV["NetworkVariable<T> (Persistent State)"]
        NB --> RPC["[Rpc(SendTo.X)] (Transient Events)"]
        NB --> NT[NetworkTransform / Interpolation]
        NB --> NA[NetworkAnimator]
    end
```

### The 4 Foundation Classes
1. **`NetworkManager`**: The global session coordinator singleton. Manages connection flows (`StartServer()`, `StartClient()`, `StartHost()`), registers network prefabs, and configures the transport layer (`UnityTransport`).
2. **`NetworkObject`**: Required on any GameObject that exists across the network. Assigns `NetworkObjectId`, tracks `OwnerClientId`, and manages visibility.
3. **`NetworkBehaviour`**: Subclasses `MonoBehaviour` to provide networking hooks (`IsServer`, `IsClient`, `IsOwner`, `OnNetworkSpawn`, `OnNetworkDespawn`).
4. **`NetworkVariable<T>`**: Synchronizes persistent state from server/authority to clients with automated late-joiner synchronization and `OnValueChanged` callbacks.

---

## 2. Technical References & Deep-Dives

Consult the specialized technical guides:

- [references/rpc-and-networkvariable.md](references/rpc-and-networkvariable.md) — The modern unified `[Rpc(SendTo.X)]` system, target routing, and `NetworkVariable<T>` permissions, serialization, and events.
- [references/lifecycle-spawning-approval.md](references/lifecycle-spawning-approval.md) — Connection approval handshakes, custom spawn points, dynamic runtime object spawning/despawning, and network pooling.
- [references/distributed-authority-guide.md](references/distributed-authority-guide.md) — Unity 6 Distributed Authority (DA) architecture: peer ownership, migrating from Server Authority to DA, and reducing server hosting costs.
- [references/serialization-custom-types.md](references/serialization-custom-types.md) — Implementing `INetworkSerializable`, `FastBufferReader`, and `FastBufferWriter` for zero-allocation high-performance payloads.

---

## 3. Fast Implementation Checklist

### Step 1: Session Setup
- [ ] Add `NetworkManager` and `UnityTransport` to a persistent GameObject in your startup scene.
- [ ] Register all spawnable network prefabs into the `NetworkPrefabs` list on `NetworkManager`.
- [ ] Implement `ConnectionApprovalCallback` on the server to validate client versions and passwords.

### Step 2: Character / Object Setup
- [ ] Attach `NetworkObject` to the root of the prefab.
- [ ] Subclass `NetworkBehaviour` instead of `MonoBehaviour`.
- [ ] Add `NetworkTransform` for position/rotation replication (configure interpolation for smooth proxy movement).

### Step 3: State & Events
- [ ] Use `NetworkVariable<T>` for persistent state (health, score, inventory, team).
- [ ] Use modern `[Rpc(SendTo.Server)]` or `[Rpc(SendTo.ClientsAndHost)]` for transient actions (attacks, gunshot sounds, particle triggers).
- [ ] Ensure all RPC methods end with the `Rpc` suffix (e.g. `PlayExplosionRpc()`).

---

## 4. Anti-Hallucination & Architecture Traps

| Pitfall | Why It Fails | Correct Architecture |
|---|---|---|
| **Calling `Destroy(gameObject)` on NetworkObject** | Desyncs clients and throws internal Netcode exceptions. | Call `networkObject.Despawn()` on the server/authority, or `Destroy` only after despawning. |
| **Omitting `Rpc` Suffix** | Unity's IL post-processor requires all methods with `[Rpc]` / `[ServerRpc]` / `[ClientRpc]` to end with `Rpc`. | Name methods like `TakeDamageRpc` or `FireWeaponRpc`. |
| **Writing to `NetworkVariable` on Client (ServerAuth)** | Throws `InvalidOperationException` if client attempts to write to a server-writable `NetworkVariable`. | Send an `[Rpc(SendTo.Server)]` requesting the server to modify the variable. |
| **Mutating Complex Reference Objects in `NetworkVariable`** | `NetworkVariable` only detects dirty state when assigning a new value to `.Value`. Mutating internal fields of an object does not trigger replication. | Reassign the struct: `playerData.Value = new PlayerData { ... };`. |
| **Accessing Network Properties in `Awake()` or `Start()`** | `IsOwner`, `IsServer`, and `NetworkObjectId` are not yet assigned during standard Unity `Awake()` / `Start()`. | Move networking initialization into `public override void OnNetworkSpawn()`. |
