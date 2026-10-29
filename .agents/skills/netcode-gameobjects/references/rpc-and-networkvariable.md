# RPCs & NetworkVariables in Netcode for GameObjects

This reference details state synchronization and remote messaging in modern **Netcode for GameObjects (NGO)** using `NetworkVariable<T>` and the unified `[Rpc(SendTo)]` attribute system.

---

## 1. Persistent State: `NetworkVariable<T>`

`NetworkVariable<T>` is used for persistent game state (e.g. player health, ammo, score, team ID, inventory).
- **Automatic Late-Join Sync**: Newly connected clients immediately receive current values upon spawning.
- **Dirty State Detection**: The variable only serializes across the network when its value changes.
- **Supported Types**: Primitive types (`int`, `float`, `bool`), Unity unmanaged types (`Vector3`, `Quaternion`), enums, and custom structs implementing `INetworkSerializable`.

### 1.1 Read/Write Permissions
By default, `NetworkVariable` is **Server-Authoritative** (Server writes, Everyone reads).

```csharp
using Unity.Netcode;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    // Server-write, Everyone-read (Standard Authoritative pattern)
    public NetworkVariable<int> health = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Owner-write, Everyone-read (e.g. Cosmetic customization)
    public NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        // Subscribe to value change callbacks
        health.OnValueChanged += HandleHealthChanged;
        playerColor.OnValueChanged += HandleColorChanged;

        // Apply initial state
        HandleHealthChanged(0, health.Value);
        HandleColorChanged(Color.black, playerColor.Value);
    }

    public override void OnNetworkDespawn()
    {
        // Always unsubscribe to prevent memory leaks!
        health.OnValueChanged -= HandleHealthChanged;
        playerColor.OnValueChanged -= HandleColorChanged;
    }

    private void HandleHealthChanged(int previous, int current)
    {
        Debug.Log($"Health updated from {previous} to {current}");
    }

    private void HandleColorChanged(Color previous, Color current)
    {
        GetComponent<Renderer>().material.color = current;
    }
}
```

> [!WARNING]
> **Struct Reassignment Rule**:
> `NetworkVariable` tracks dirty state via its setter: `myVariable.Value = ...`.
> If using a custom struct, mutating an internal property (`myVar.Value.health = 50;`) will NOT trigger network replication. You must reassign the struct: `myVar.Value = new MyStruct { health = 50 };`.

---

## 2. Transient Events: The Unified `[Rpc(SendTo)]` System

In modern NGO (Unity 2022+ / Unity 6), the legacy `[ServerRpc]` and `[ClientRpc]` attributes are replaced/unified by the flexible **`[Rpc(SendTo)]`** attribute.

### 2.1 The `SendTo` Routing Targets

| `SendTo` Target | Who Executes It | Typical Use Case |
|---|---|---|
| **`SendTo.Server`** | The Server only | Client requests an action (interact, jump, fire). |
| **`SendTo.NotServer`** | All connected clients (excluding the server) | Server notifies clients without repeating host work. |
| **`SendTo.Everyone`** | All clients and the server | Global announcements, universal chat broadcasts. |
| **`SendTo.ClientsAndHost`** | All clients and the host machine | Visual/Audio effects (gunshot, explosion particles). |
| **`SendTo.Owner`** | The client that owns the `NetworkObject` | Private notifications, inventory pickup feedback. |
| **`SendTo.NotOwner`** | All observers *except* the owner | Third-person animations, remote muzzle flash. |
| **`SendTo.Authority`** | Whichever process currently holds authority | Works seamlessly across Server-Auth and Distributed Authority. |
| **`SendTo.NotAuthority`** | All clients without authority | Broadcasts from the authority to remote peers. |

### 2.2 Production RPC Example

```csharp
using Unity.Netcode;
using UnityEngine;

public class CombatController : NetworkBehaviour
{
    // 1. Client requests an attack on the server
    [Rpc(SendTo.Server)]
    public void RequestAttackRpc(Vector3 aimDirection, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Received attack request from client: {senderClientId}");

        // Validate ammo and cooldowns on server
        bool validAttack = true; 

        if (validAttack)
        {
            // 2. Server notifies everyone to play visual/audio effects
            BroadcastAttackVisualsRpc(transform.position, aimDirection);
        }
    }

    // 2. Play gunshot visual/audio on all clients and host
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    private void BroadcastAttackVisualsRpc(Vector3 origin, Vector3 direction)
    {
        // Spawn muzzle flash particle
        // Play gunshot audio
    }

    // 3. Private feedback sent ONLY to the attacker
    [Rpc(SendTo.Owner)]
    public void NotifyHitMarkerRpc(int damageDealt)
    {
        // Display red crosshair hit marker on UI
    }
}
```

### 2.3 RPC Naming & Delivery Rules
1. **Mandatory `Rpc` Suffix**: All methods decorated with `[Rpc]` must end with `Rpc` (e.g. `DoSomethingRpc()`). Omitting the suffix will cause compilation or IL weaving errors.
2. **Delivery Mode**:
   - `RpcDelivery.Reliable` (Default): Guaranteed delivery in order. Use for gameplay-critical events.
   - `RpcDelivery.Unreliable`: Faster, no resends if dropped. Ideal for high-frequency cosmetic effects (sparks, audio, bullet impact puffs).
