# Custom Serialization: INetworkSerializable & FastBuffer

In **Netcode for GameObjects (NGO)**, transmitting custom complex data structures inside `NetworkVariable<T>` or as RPC parameters requires implementing `INetworkSerializable` or using `FastBufferWriter` / `FastBufferReader`.

---

## 1. Implementing `INetworkSerializable`

Any custom `struct` can be synchronized across the network by implementing `INetworkSerializable`.
The method `NetworkSerialize<T>` handles both reading (deserialization) and writing (serialization) through a unified generic `BufferSerializer`:

```csharp
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerInventoryItem : INetworkSerializable
{
    public int itemId;
    public int quantity;
    public FixedString32Bytes itemName; // Prefer FixedString over System.String to avoid GC allocation!
    public Vector3 worldPosition;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Unified read/write pattern
        serializer.SerializeValue(ref itemId);
        serializer.SerializeValue(ref quantity);
        serializer.SerializeValue(ref itemName);
        serializer.SerializeValue(ref worldPosition);
    }
}
```

### Using Custom Structs in `NetworkVariable`:
```csharp
public class PlayerInventory : NetworkBehaviour
{
    public NetworkVariable<PlayerInventoryItem> activeEquippedItem = new NetworkVariable<PlayerInventoryItem>();

    public void EquipItem(int id, string name)
    {
        if (!IsServer) return;

        // Struct reassignment ensures dirty flag is triggered
        activeEquippedItem.Value = new PlayerInventoryItem
        {
            itemId = id,
            quantity = 1,
            itemName = new FixedString32Bytes(name),
            worldPosition = transform.position
        };
    }
}
```

---

## 2. Dynamic Arrays & Strings in `INetworkSerializable`

When serializing dynamic arrays or lists, you must serialize the count first, check whether reading or writing, and allocate the array if deserializing:

```csharp
using Unity.Netcode;

public struct QuestData : INetworkSerializable
{
    public int questId;
    public int[] objectiveProgress;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref questId);

        // Array Serialization Pattern
        int length = 0;
        if (!serializer.IsReader)
        {
            length = objectiveProgress != null ? objectiveProgress.Length : 0;
        }

        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            objectiveProgress = new int[length];
        }

        for (int i = 0; i < length; i++)
        {
            serializer.SerializeValue(ref objectiveProgress[i]);
        }
    }
}
```

---

## 3. High-Performance Raw Streams: `FastBufferWriter` & `FastBufferReader`

For custom low-level network messaging or custom packet protocols, use `FastBufferWriter` and `FastBufferReader`. They operate directly on unmanaged memory with zero GC allocations:

```csharp
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public static class FastBufferPayloadUtility
{
    public static void SendRawBinaryMessage(Vector3 position, int score)
    {
        // Allocate writer on stack (Allocator.Temp)
        using (var writer = new FastBufferWriter(128, Allocator.Temp))
        {
            writer.WriteValueSafe(position);
            writer.WriteValueSafe(score);

            // Send via Custom Messaging Manager
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                "CustomCombatMessage", writer);
        }
    }

    public static void OnReceiveRawBinaryMessage(ulong senderId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out Vector3 position);
        reader.ReadValueSafe(out int score);

        Debug.Log($"Received position {position} and score {score} from {senderId}");
    }
}
```

### Zero-Allocation Serialization Best Practices
- **Prefer `FixedString32Bytes` / `FixedString64Bytes`**: `System.String` triggers heap allocations during serialization. `FixedString` is an unmanaged value type.
- **Pre-Allocate Buffers**: Reuse `NativeArray` or stack buffers where possible.
- **Use Bit Packing**: For booleans and small enums, pack flags into a single `byte` or `int` bitmask before serializing.
