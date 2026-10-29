# Components and Singletons in Unity Physics

This reference outlines all core ECS data components, shared components, dynamic buffers, and singletons used in the `Unity.Physics` package.

---

## 1. Core Collider & Body Components

### `PhysicsCollider` (IComponentData)
Represents the collision geometry and physical surface material of an entity.
- **`Value` (`BlobAssetReference<Collider>`)**: Reference to the immutable binary blob containing the collider's shape data.
- **Properties baked inside the Blob**:
  - Geometry (Box, Sphere, Capsule, Cylinder, Convex, Mesh, Compound, Terrain)
  - `CollisionFilter` (`BelongsTo`, `CollidesWith`, `GroupIndex`)
  - Friction and Restitution (bounciness) coefficients
  - Collision response policy (`Collide`, `CollideRaiseCollisionEvents`, `RaiseTriggerEvents`)
> [!NOTE]
> Because `BlobAssetReference<Collider>` can be shared among multiple entities (e.g., all spawned crates), modifying a shared blob asset in place alters every entity referencing it. Create separate blob copies if per-instance runtime modifications are required.

### `PhysicsWorldIndex` (ISharedComponentData)
- Specifies which physics world partition the entity belongs to (integer, defaults to `0`).
- Physics entities only test collisions and generate constraints with entities sharing the identical `PhysicsWorldIndex`.
- Used to isolate parallel sub-simulations (e.g., UI 3D viewport, secondary simulation, prediction worlds).

### `PhysicsVelocity` (IComponentData)
Stores linear and angular momentum.
- **`Linear` (`float3`)**: Velocity vector expressed in **world space**.
- **`Angular` (`float3`)**: Rotational velocity vector expressed **relative to the body's mass frame** (principal axes of inertia), NOT world space.

### `PhysicsMass` (IComponentData)
Defines inertial properties for dynamic bodies.
- **`InverseMass` (`float`)**: Reciprocal of mass ($1/m$). A value of `0` denotes infinite mass (kinematic).
- **`InverseInertia` (`float3`)**: Reciprocal of principal moments of inertia ($1/I_x, 1/I_y, 1/I_z$).
- **`CenterOfMass` (`float3`)**: Offset from entity origin to physical center of mass.
- **`InertiaOrientation` (`quaternion`)**: Orientation of the principal axes of inertia relative to entity rotation.
- **`AngularExpansionFactor` (`float`)**: Expansion metric used in swept volume bounding box calculations to prevent tunneling.
> [!IMPORTANT]
> **Omitting `PhysicsMass`**: If an entity possesses `PhysicsCollider` and `PhysicsVelocity` but lacks `PhysicsMass`, Unity Physics treats the body as **Kinematic** with conceptually infinite mass.

---

## 2. Additional Modifiers & Physics Flags

### `PhysicsDamping` (IComponentData)
- **`Linear` (`float`)**: Fraction of linear velocity drained per second.
- **`Angular` (`float`)**: Fraction of angular velocity drained per second.

### `PhysicsGravityFactor` (IComponentData)
- **`Value` (`float`)**: Multiplier applied to the world's global gravity vector for this entity ($1.0 = \text{normal}$, $0.0 = \text{weightless}$, $-1.0 = \text{inverted}$).

### `PhysicsMassOverride` (IComponentData)
- Contains a bitfield flag (`IsKinematic`) that dynamically toggles a body between kinematic and dynamic without needing to remove or add `PhysicsMass` components (which would cause structural changes).

### `PhysicsCustomTags` (IComponentData)
- User-definable custom bitmask/byte tags. These tags are automatically copied into `CollisionEvent` and `TriggerEvent` structs, enabling zero-lookup body categorization in event-processing loops.

---

## 3. Graphical Smoothing & Decoupling

Because physics runs at fixed intervals while graphics render at display refresh rates, fast-moving physics bodies can visually stutter without interpolation.

### `PhysicsRenderEntity` (IComponentData)
- Links a physics entity to a separate graphical visual entity.

### `PhysicsGraphicalSmoothing` (IComponentData)
- Signals the `SmoothRigidBodiesGraphicalMotion` system to interpolate/extrapolate visual positions between physics ticks when rendering frame rate exceeds physics fixed rate.

### `PhysicsGraphicalInterpolationBuffer` (IComponentData)
- Stores rigid body position and orientation history from the previous physics tick. Combined with smoothing, this yields jitter-free visual interpolation (at the expense of one tick of visual latency).

---

## 4. Joints and Constraints

### `PhysicsJoint` (IComponentData)
- Encapsulates joint type, body frame local transforms, and solver constraint equations.
- Static helper constructors:
  - `PhysicsJoint.CreateFixed()`
  - `PhysicsJoint.CreateHinge()`
  - `PhysicsJoint.CreateBallAndSocket()`
  - `PhysicsJoint.CreatePrismatic()`
  - `PhysicsJoint.CreateRagdoll()`
  - `PhysicsJoint.CreateRotationalMotor()`
  - `PhysicsJoint.CreateAngularVelocityMotor()`
  - `PhysicsJoint.CreatePositionMotor()`
  - `PhysicsJoint.CreateLinearVelocityMotor()`

### `PhysicsConstrainedBodyPair` (IComponentData)
- References the two constrained entities:
  - `EntityA`, `EntityB`
  - `EnableCollision` (0 or 1): Dictates whether the constrained bodies should continue to collide with each other.

### `PhysicsJointCompanion` (DynamicBuffer)
- Used when multiple joint entities operate in tandem to form complex compound articulators.

---

## 5. Singletons

Singletons are instantiated automatically or created once on a designated configuration entity.

| Singleton | Created By | Primary Use |
| :--- | :--- | :--- |
| `PhysicsWorldSingleton` | Auto-generated by engine | Provides `PhysicsWorld` (bodies, colliders, joints) and `CollisionWorld` (raycasts, sweeps, distance queries). |
| `SimulationSingleton` | Auto-generated by engine | Provides `AsSimulation()` to access streams of `CollisionEvents`, `TriggerEvents`, and `ImpulseEvents`. |
| `PhysicsStep` | User / Authoring | Global configuration: Gravity vector, solver iteration counts, multi-threading flags, synchronous vs async dispatch. |
| `PhysicsDebugDisplayData` | User / Debugger | Runtime flags toggling visual wireframe overlays for AABBs, colliders, contacts, joints, and query rays. |
