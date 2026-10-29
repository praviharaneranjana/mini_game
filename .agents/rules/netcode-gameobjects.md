---
trigger: "When writing, modifying, or debugging Netcode for GameObjects (NGO) scripts, NetworkBehaviour, NetworkVariable, RPCs, or network prefabs."
---

# Netcode for GameObjects (NGO) Coding Rules

## 1. Lifecycle & Spawning Rules
- **No Early Network Access**: Never access `IsOwner`, `IsServer`, `IsClient`, or `NetworkObjectId` inside `Awake()` or `Start()`. Move all network initialization into `public override void OnNetworkSpawn()`.
- **Despawn Before Destroy**: Never call `Destroy(gameObject)` on an active network entity. Call `networkObject.Despawn(destroy: true)` on the server or designated authority.
- **Event Unsubscription**: Always unsubscribe from `NetworkVariable.OnValueChanged` events inside `public override void OnNetworkDespawn()` to avoid dangling delegate references.

## 2. RPC Standards
- **Mandatory `Rpc` Suffix**: All methods decorated with `[Rpc]` must end with the `Rpc` suffix (e.g. `FireWeaponRpc`, `TakeDamageRpc`).
- **Use Modern `[Rpc(SendTo)]`**: Prefer the unified `[Rpc(SendTo.Server)]`, `[Rpc(SendTo.ClientsAndHost)]`, or `[Rpc(SendTo.Authority)]` syntax over legacy `[ServerRpc]` / `[ClientRpc]`.
- **Delivery Selection**: Use `Delivery = RpcDelivery.Unreliable` for non-critical, high-frequency visual/audio cues (e.g., muzzle flashes, footsteps, dust particles) to preserve bandwidth.

## 3. NetworkVariable Best Practices
- **Struct Reassignment**: When synchronizing custom structs via `NetworkVariable<T>`, never mutate internal fields directly (`myVar.Value.health = 10;`). Always reassign the struct (`myVar.Value = new MyStruct { health = 10 };`) to trigger the dirty replication flag.
- **Permission Discipline**: Maintain server authority by defaulting to `NetworkVariableWritePermission.Server`. Use `NetworkVariableWritePermission.Owner` only for non-critical player cosmetic state or in Distributed Authority mode.

## 4. Performance & Serialization
- **Zero-Allocation Payloads**: Avoid `System.String` in `INetworkSerializable` structs. Use `FixedString32Bytes`, `FixedString64Bytes`, or `FixedString128Bytes` to eliminate managed GC heap allocations.
- **Batching & Frequency**: Avoid synchronizing high-frequency variables every frame. Use `NetworkTransform` with threshold limits and interpolation enabled for smooth motion.
