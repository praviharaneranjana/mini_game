# NGO Lifecycle, Spawning & Connection Approval

This reference details the connection approval handshake, object spawning, ownership management, scene loading, and network object pooling in **Netcode for GameObjects (NGO)**.

---

## 1. Connection Approval Handshake

Before a client is allowed to join the game session, the server can inspect authentication payloads (e.g. game versions, passwords, auth tokens, character indices) via `ConnectionApprovalCallback`.

### 1.1 Client Payload Configuration
The client sends payload bytes before calling `StartClient()`:

```csharp
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class NetworkConnectClient : MonoBehaviour
{
    public void ConnectWithAuth(string password, int selectedCharacterId)
    {
        string payloadJson = $"{{\"password\":\"{password}\",\"character\":{selectedCharacterId}}}";
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        // Attach payload to NetworkManager
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
        NetworkManager.Singleton.StartClient();
    }
}
```

### 1.2 Server Connection Approval Implementation
The server validates the payload and approves or denies the connection:

```csharp
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class ServerApprovalManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback = ApproveConnection;
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        string payload = Encoding.UTF8.GetString(request.Payload);
        Debug.Log($"Validating connection from client {request.ClientNetworkId} with payload: {payload}");

        // 1. Validate payload (e.g. check password / version)
        bool isAuthorized = true; // Parse JSON and validate

        if (!isAuthorized)
        {
            response.Approved = false;
            response.Reason = "Invalid password or incompatible game version.";
            return;
        }

        // 2. Approve client
        response.Approved = true;
        response.CreatePlayerObject = true; // Auto-spawn player object
        response.PlayerPrefabHash = null;   // Use default player prefab or specify hash

        // 3. Assign spawn position
        int spawnIndex = (int)(request.ClientNetworkId % (ulong)spawnPoints.Length);
        response.Position = spawnPoints[spawnIndex].position;
        response.Rotation = spawnPoints[spawnIndex].rotation;

        // 4. Optionally register pending scene loading
        response.Pending = false;
    }
}
```

---

## 2. Dynamic Object Spawning & Ownership

Only the server (or the designated authority in Distributed Authority) can spawn `NetworkObject`s.

### 2.1 Spawning with Ownership
```csharp
public void SpawnLootBox(GameObject lootPrefab, Vector3 position, ulong targetOwnerClientId)
{
    if (!NetworkManager.Singleton.IsServer) return;

    GameObject instance = Instantiate(lootPrefab, position, Quaternion.identity);
    NetworkObject netObj = instance.GetComponent<NetworkObject>();

    // Spawns object across all clients and assigns ownership to target client
    netObj.SpawnWithOwnership(targetOwnerClientId);
}
```

### 2.2 Despawning Objects
Never call standard `Destroy(gameObject)` directly on a spawned network object!
```csharp
public void CollectLoot(NetworkObject netObj)
{
    if (!netObj.IsServer) return;

    // Despawn removes object from all connected clients
    // Pass 'destroy: true' to destroy the local GameObject on server as well
    netObj.Despawn(destroy: true);
}
```

### 2.3 Ownership Transfer
The server can transfer control of an object (e.g. passing a ball, entering a vehicle):
```csharp
public void EnterVehicle(NetworkObject vehicleNetObj, ulong driverClientId)
{
    if (!IsServer) return;
    vehicleNetObj.ChangeOwnership(driverClientId);
}
```

---

## 3. Synchronized Scene Management

When switching levels or lobbies in multiplayer, all clients must load the scene synchronously.
Use `NetworkSceneManager` instead of `UnityEngine.SceneManagement.SceneManager`:

```csharp
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public void LoadGameLevel(string sceneName)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Automatically instructs all connected clients to load this scene
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
```
