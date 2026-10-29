---
trigger: "When working with multiplayer netcode, networking, client prediction, lag compensation, FPS character controllers, or weapons synchronization in Unity."
---

# Authoritative Multiplayer & Netcode Rules

## 1. Core Authority & Trust Principles
- **Absolute Server Authority**: The server is the sole arbiter of gameplay simulation, player positions, ammo depletion, health, and damage. Never trust client claims of damage or hit locations.
- **Client Input Streaming**: Clients must only send raw, validated inputs (`UserCommand` or `IInputComponentData`) stamped with sequential tick indices. Never send final world positions from clients.

## 2. Client Prediction & Reconciliation
- **Zero Input Latency Requirement**: Always predict local character movement immediately on the client using the current input command.
- **State Buffer & Replay**: Maintain a circular ring buffer of past unacknowledged inputs and predicted states. When authoritative server state arrives, if the predicted position deviates beyond tolerance (>1mm), rewind to the authoritative state and replay all subsequent inputs up to the present tick.
- **Interpolation for Remote Proxies**: Never render remote players directly at raw packet arrival coordinates. Use an interpolation buffer with an intentional delay ($\sim 50\text{–}100\text{ms}$) to interpolate smoothly between consecutive snapshots.

## 3. Lag Compensation (History Rewind)
- **Fair Hit Registration**: Implement server-side history rewind for hitscan and fast projectile weapons. When a shot command from tick $T$ arrives at server tick $T + \Delta t$, rewind target hurtboxes to tick $T$, execute the raycast, register hits, and restore colliders to the live tick.
- **Bounded Rewind Windows**: Cap the maximum rewind window (e.g. 200–250ms) to prevent high-ping players from shooting targets far behind solid cover.

## 4. Weapon Architecture
- **Decouple Simulation and Presentation**: Keep bullet math, ammo, and fire intervals strictly separate from presentation (audio, muzzle flash, procedural viewmodel kick, shell eject).
- **Ghost Field Counter**: Replicate shots fired using a monotonically increasing integer (`totalShotsCount`). Remote clients trigger visuals when `replicatedCount > lastVisualCount`, preventing dropped effects during packet loss.
- **Camera-Aligned Raycasting**: Raycasts must originate from the player camera view center, while visual tracers emanate from the physical gun muzzle toward the hit point.

## 5. Headless Server Optimization
- **Strip Server Visuals**: Dedicated server builds must strip all `MeshRenderer`, `SkinnedMeshRenderer`, `AudioSource`, `ParticleSystem`, and non-essential `Animator` components to achieve maximum server tick rates and minimal CPU/memory footprint.
- **Multi-Prefab Workflow**: Separate character setups into First-Person (local camera only), Third-Person (remote proxies and local shadow), Server (headless collision capsule), Hurtboxes (lag compensation), and Ragdolls.
