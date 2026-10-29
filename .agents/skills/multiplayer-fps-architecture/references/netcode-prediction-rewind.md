# Netcode Architecture: Client Prediction & History Rewind Lag Compensation

This document provides a production-grade algorithmic and mathematical reference for building competitive multiplayer netcode, derived from the core principles of Quake/Source/Unity's **FPSSample** and adapted for modern Unity C# architecture.

---

## 1. The Core Problem: Latency in Competitive Shooters

In a multiplayer game over the internet, network latency (RTT: Round-Trip Time, typically 30–120ms) creates two critical problems:
1. **Input Lag**: If a player presses 'W' and waits for the server to acknowledge movement before moving the camera/character, the game feels unresponsive and unplayable.
2. **Shot Discrepancy (Spatial Parallax)**: When Player A fires at Player B on Player A's screen, Player B was actually at that location $RTT / 2$ milliseconds in the past. If the server evaluates the shot against Player B's *current* position, the shot will miss, even though Player A aimed directly at Player B.

```
Timeline:
Client A (Render Time: t - 50ms): Sees Player B at position X. Fires shot!
Shot packet travels over network (50ms latency)...
Server (Current Time: t): Player B has already moved to position Y.
Without Lag Compensation: Server raycasts at current time t -> MISS! (Unfair)
With Lag Compensation: Server rewinds Player B back to position X at time (t - 50ms) -> HIT! (Fair)
```

---

## 2. Client Prediction & Server Reconciliation Loop

### 2.1 The Data Structures

Every client input is packaged into a sequential, tick-stamped struct called `UserCommand`:

```csharp
using System;
using UnityEngine;

[Serializable]
public struct UserCommand
{
    public uint tick;             // Monotonically increasing tick number (e.g. 60Hz)
    public float deltaTime;        // Fixed delta time (e.g. 1/60s = 0.01666f)
    public Vector2 moveInput;      // X: Horizontal, Y: Vertical
    public Vector2 lookAngles;     // Pitch & Yaw in degrees
    public bool jump;
    public bool sprint;
    public bool fire;
    public uint weaponSelect;
}

public struct CharacterState
{
    public uint tick;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public bool isGrounded;
}
```

### 2.2 Client-Side Prediction Algorithm

The client maintains:
- `uint currentClientTick`: Incremented each simulation tick.
- `CircularBuffer<UserCommand> inputBuffer`: Stores recent unacknowledged inputs.
- `CircularBuffer<CharacterState> stateBuffer`: Stores predicted states per tick.

```csharp
public class ClientPredictionController
{
    private const int BufferSize = 1024;
    private UserCommand[] inputHistory = new UserCommand[BufferSize];
    private CharacterState[] stateHistory = new CharacterState[BufferSize];
    private uint currentTick = 0;
    private uint lastAcknowledgedServerTick = 0;

    public void OnFixedUpdate(Vector2 rawMove, Vector2 look, bool jump, bool fire)
    {
        currentTick++;
        int bufferIndex = (int)(currentTick % BufferSize);

        // 1. Capture current input
        var cmd = new UserCommand
        {
            tick = currentTick,
            deltaTime = Time.fixedDeltaTime,
            moveInput = rawMove,
            lookAngles = look,
            jump = jump,
            fire = fire
        };
        inputHistory[bufferIndex] = cmd;

        // 2. Predict movement locally immediately
        CharacterState previousState = stateHistory[(int)((currentTick - 1) % BufferSize)];
        CharacterState predictedState = SimulateMovement(previousState, cmd);
        stateHistory[bufferIndex] = predictedState;

        // 3. Send command to server over network
        SendInputToServer(cmd);

        // 4. Update visual transform
        ApplyStateToLocalTransform(predictedState);
    }

    // 2.3 Server Reconciliation
    public void OnServerSnapshotReceived(CharacterState authoritativeServerState)
    {
        lastAcknowledgedServerTick = authoritativeServerState.tick;
        int ackIndex = (int)(authoritativeServerState.tick % BufferSize);
        CharacterState predictedState = stateHistory[ackIndex];

        // Check if predicted state diverges from authoritative server state
        float posError = Vector3.Distance(predictedState.position, authoritativeServerState.position);
        if (posError > 0.001f) // Discrepancy detected! Reconcile!
        {
            // Rewind state to server authoritative state
            CharacterState replayState = authoritativeServerState;
            stateHistory[ackIndex] = replayState;

            // Re-simulate all inputs from (serverTick + 1) up to currentTick
            for (uint tick = authoritativeServerState.tick + 1; tick <= currentTick; tick++)
            {
                int replayIndex = (int)(tick % BufferSize);
                UserCommand cmd = inputHistory[replayIndex];
                replayState = SimulateMovement(replayState, cmd);
                stateHistory[replayIndex] = replayState;
            }

            // Smoothly correct local transform
            ApplyStateToLocalTransform(replayState);
        }
    }

    private CharacterState SimulateMovement(CharacterState current, UserCommand cmd)
    {
        // Deterministic character kinematic simulation
        Vector3 moveDir = Quaternion.Euler(0, cmd.lookAngles.y, 0) * new Vector3(cmd.moveInput.x, 0, cmd.moveInput.y);
        Vector3 vel = moveDir * 6.0f; // 6 m/s walk speed
        Vector3 pos = current.position + (vel * cmd.deltaTime);

        return new CharacterState
        {
            tick = cmd.tick,
            position = pos,
            rotation = Quaternion.Euler(0, cmd.lookAngles.y, 0),
            velocity = vel,
            isGrounded = true
        };
    }

    private void SendInputToServer(UserCommand cmd) { /* Netcode transport stream */ }
    private void ApplyStateToLocalTransform(CharacterState state) { /* Update GameObject */ }
}
```

---

## 3. Server-Side Lag Compensation (History Rewind)

Lag compensation ensures fair hit registration by allowing the server to rewind collision geometry to the exact point in time when the client fired.

### 3.1 Historical Hurtbox Buffer

Every player character possesses a series of precision hitboxes/hurtboxes (Head, Torso, Arms, Legs). On each fixed server tick, the server records the world positions and rotations of every hurtbox in a circular ring buffer:

```csharp
public struct HurtboxSnapshot
{
    public Vector3 position;
    public Quaternion rotation;
}

public struct PlayerHurtboxHistory
{
    public uint tick;
    public double timestamp;
    public HurtboxSnapshot[] hurtboxes; // Indexed by body part ID
}
```

### 3.2 Production History Rewind System

```csharp
using System.Collections.Generic;
using UnityEngine;

public class LagCompensationServerManager : MonoBehaviour
{
    public static LagCompensationServerManager Instance;

    private const int HistoryTicks = 120; // 2 seconds of history at 60Hz
    private Dictionary<int, PlayerHurtboxHistory[]> playerHistories = new Dictionary<int, PlayerHurtboxHistory[]>();
    private Dictionary<int, Transform[]> playerColliderTransforms = new Dictionary<int, Transform[]>();

    private void Awake() => Instance = this;

    public void RegisterPlayer(int playerId, Transform[] colliders)
    {
        playerHistories[playerId] = new PlayerHurtboxHistory[HistoryTicks];
        playerColliderTransforms[playerId] = colliders;
    }

    public void RecordServerTick(uint currentTick, double currentTime)
    {
        int index = (int)(currentTick % HistoryTicks);

        foreach (var kvp in playerColliderTransforms)
        {
            int playerId = kvp.Key;
            Transform[] colliders = kvp.Value;
            var snapshot = new HurtboxSnapshot[colliders.Length];

            for (int i = 0; i < colliders.Length; i++)
            {
                snapshot[i] = new HurtboxSnapshot
                {
                    position = colliders[i].position,
                    rotation = colliders[i].rotation
                };
            }

            playerHistories[playerId][index] = new PlayerHurtboxHistory
            {
                tick = currentTick,
                timestamp = currentTime,
                hurtboxes = snapshot
            };
        }
    }

    /// <summary>
    /// Executes a hitscan raycast rewinded to the targetTick.
    /// </summary>
    public bool LagCompensatedRaycast(
        int shooterPlayerId,
        uint targetTick,
        Ray ray,
        float maxDistance,
        LayerMask hurtboxMask,
        out RaycastHit hitInfo)
    {
        // 1. Cache current live positions of all other players
        var savedCurrentTransforms = new Dictionary<int, HurtboxSnapshot[]>();

        foreach (var kvp in playerColliderTransforms)
        {
            int playerId = kvp.Key;
            if (playerId == shooterPlayerId) continue; // Don't rewind the shooter

            Transform[] transforms = kvp.Value;
            var currentPos = new HurtboxSnapshot[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                currentPos[i] = new HurtboxSnapshot
                {
                    position = transforms[i].position,
                    rotation = transforms[i].rotation
                };
            }
            savedCurrentTransforms[playerId] = currentPos;

            // 2. Rewind colliders to target historical tick
            int historyIndex = (int)(targetTick % HistoryTicks);
            PlayerHurtboxHistory historicalState = playerHistories[playerId][historyIndex];

            if (historicalState.hurtboxes != null)
            {
                for (int i = 0; i < transforms.Length; i++)
                {
                    transforms[i].position = historicalState.hurtboxes[i].position;
                    transforms[i].rotation = historicalState.hurtboxes[i].rotation;
                }
            }
        }

        // 3. Sync physics scene so raycasts hit rewound collider positions
        Physics.SyncTransforms();

        // 4. Perform authoritative raycast
        bool hit = Physics.Raycast(ray, out hitInfo, maxDistance, hurtboxMask);

        // 5. Restore all colliders back to current live server positions
        foreach (var kvp in savedCurrentTransforms)
        {
            int playerId = kvp.Key;
            Transform[] transforms = playerColliderTransforms[playerId];
            HurtboxSnapshot[] currentPos = kvp.Value;

            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].position = currentPos[i].position;
                transforms[i].rotation = currentPos[i].rotation;
            }
        }

        // 6. Resync physics scene back to current live state
        Physics.SyncTransforms();

        return hit;
    }
}
```

---

## 4. Remote Entity Interpolation & Jitter Buffering

Remote players (proxies) must not be rendered at raw packet arrival positions, as network packet arrival jitter causes visual stutter.

### The Interpolation Buffer Rule:
* The client maintains a queue of incoming server snapshots.
* Remote players are rendered at a delayed time:
  $$\text{RenderTime} = \text{CurrentClientTime} - \text{InterpolationDelay}$$
* $\text{InterpolationDelay} \approx \text{SnapshotInterval} \times 2 + \text{SafetyMargin}$ (typically 50ms – 100ms).
* For any $\text{RenderTime}$, the client finds the two bounding snapshots $S_0$ and $S_1$ ($t_0 \le \text{RenderTime} \le t_1$) and computes interpolation factor:
  $$\alpha = \frac{\text{RenderTime} - t_0}{t_1 - t_0}$$
* Positions are linearly interpolated (`Vector3.Lerp`) and rotations are spherically interpolated (`Quaternion.Slerp`). This produces guaranteed butter-smooth motion regardless of minor packet jitter.
