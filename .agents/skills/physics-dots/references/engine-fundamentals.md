# Engine Fundamentals: Game Physics Architecture

This guide covers core concepts of game physics simulation and the internal architecture of modern physics engines like Unity Physics.

---

## 1. The Four-Stage Physics Simulation Loop

Every physics update tick, the engine performs four distinct operations in strict sequence:

```
[1. Spatial Acceleration (BVH)] 
             │
             ▼
[2. Collision Detection & Contacts] 
             │
             ▼
[3. Constraint Solver (Jacobians)] 
             │
             ▼
[4. Integration & Export]
```

### Stage 1: Spatial Data Structure Construction (Broadphase)
- The physics engine builds a spatial acceleration structure (typically a **Bounding Volume Hierarchy - BVH** or Octree) from the Axis-Aligned Bounding Boxes (AABBs) of all colliders.
- Dynamic and kinematic bodies are grouped into a dynamic BVH rebuilt frequently.
- Static colliders are organized into a separate static BVH.
- Broadphase tests check for overlapping AABBs in world space.

### Stage 2: Collision Detection & Contact Generation (Narrowphase)
- For pairs of bodies whose AABBs overlap in the broadphase, narrowphase algorithms perform exact geometric intersection tests (e.g., GJK, EPA, SAT).
- The engine calculates contact points, contact normals, and penetration depths.
- Contact constraints are created from these points.

### Stage 3: Constraint Solver (Jacobian Formulation)
- The solver formulates constraint equations (Jacobians) for all contact constraints and persistent joints/motors.
- It calculates the necessary corrective impulses to satisfy constraints, taking into account:
  - Linear and angular velocities
  - Mass and inertia tensors
  - Friction and restitution coefficients
  - Gravity and external impulses

### Stage 4: Position and Velocity Integration
- Objects' linear and angular velocities are updated based on solved impulses.
- New positions and orientations are calculated by integrating velocities over the fixed delta time ($x_{new} = x_{old} + v \cdot \Delta t$).
- Results are exported back to game entities/transforms.

---

## 2. Fixed Timestep vs. Variable Rendering Framerate

- Rendering runs on a variable delta time depending on GPU/CPU load.
- Physics engines require a **fixed time step** ($\Delta t$) for numerical stability, reproducibility, and conservation of energy.
- **Fixed Step Averaging**:
  - The physics loop (`FixedStepSimulationGroup`) runs **0 or more times per render frame**.
  - If a frame is fast (e.g., 144 FPS with 60 Hz physics), several frames will run 0 physics ticks.
  - If a frame lags (e.g., 20 FPS with 60 Hz physics), a single render frame may execute 3 physics ticks in succession.
- **Rule**: Never use `Time.deltaTime` from rendering inside physics solver logic. Use `SystemAPI.Time.DeltaTime` within fixed step groups.

---

## 3. Rigid Body Mechanics

A rigid body is an idealized solid object with non-deformable geometry and uniformly distributed mass.

### Core Data Elements:
- **Position & Orientation**: Transform in world space.
- **Linear Velocity**: Direction and speed of travel (world space vector).
- **Angular Velocity**: Direction and speed of rotation (expressed relative to the mass frame / center of mass).
- **Mass**: Resistance to linear acceleration ($F = m \cdot a$).
- **Center of Mass (COM)**: The balance point around which torques apply rotational acceleration.
- **Inertia Tensor**: A $3\times3$ matrix (or diagonal vector in principal axes) representing the body's resistance to rotational acceleration.

### Optional Properties:
- **Linear & Angular Damping**: Drag coefficients that gradually decay velocities over time, preventing runaway perpetual motion.
- **Gravity Factor**: Per-body scale factor applied to global gravitational acceleration.
- **Friction Coefficient**: Resistance to tangential sliding motion across contact surfaces.
- **Restitution (Bounciness)**: Ratio of relative velocity after impact to relative velocity before impact ($0 = \text{inelastic}$, $1 = \text{perfectly elastic}$).
- **Collision Filter**: Bitmasks determining which layers interact.

---

## 4. Collider Types and Performance Hierarchy

Collision geometry often differs from visual geometry. Using high-polygon visual meshes directly for collision is a primary cause of game physics performance degradation.

| Shape | Complexity | Relative Cost | Best For |
| :--- | :--- | :--- | :--- |
| **Sphere** | Center + Radius | Lowest ($\mathcal{O}(1)$) | Projectiles, ballistics, particles, wheels |
| **Box** | Dimensions (L, W, H) | Very Low | Crates, walls, floors, trigger zones |
| **Capsule** | Line segment + Radius | Low | Characters, humanoids, limbs, pillars |
| **Cylinder** | Height + Radius | Low | Barrels, columns, shafts |
| **Convex Hull** | Set of 3D points ($< 32$ verts) | Medium | Irregular debris, boulders, props |
| **Compound** | Composite of primitives | Medium | Vehicles, modular props, hitbox sets |
| **Terrain** | Heightfield grid | Medium-High | Open landscapes, heightmaps |
| **Mesh (Concave)** | Arbitrary triangles & quads | **Extremely High** | Static complex structures (NEVER for moving bodies) |

### Key Principles:
1. **Never use Mesh Colliders on dynamic moving bodies**. Pure Unity Physics requires dynamic bodies to be convex primitives or compounds.
2. **Compound Colliders over Mesh Colliders**: Form complex shapes by parenting multiple primitive colliders (spheres, boxes, capsules). They are dramatically faster to solve than concave triangle meshes and allow per-limb hit identification.
3. **Beware of Large AABBs**: Colliders that span huge world spaces (e.g., giant single floor planes) have massive AABBs that intersect dozens of objects in the broadphase, overwhelming the narrowphase with false-alarm pairwise tests. **Break expansive colliders into modular tiled segments**.
