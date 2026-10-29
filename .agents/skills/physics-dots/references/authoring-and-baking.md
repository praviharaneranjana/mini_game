# Authoring & Baking in Unity Physics

This reference explains how physics assets are authored in the Unity Editor, converted into DOTS entities via baking, and instantiated programmatically at runtime.

---

## 1. Subscene Baking with Standard MonoBehaviours

Unity Physics automatically translates standard Built-in (PhysX) GameObjects inside subscenes into optimized DOTS components:

### Component Mapping Table

| MonoBehaviour | Baked DOTS Components | Details |
| :--- | :--- | :--- |
| `UnityEngine.BoxCollider`<br>`UnityEngine.SphereCollider`<br>`UnityEngine.CapsuleCollider`<br>`UnityEngine.MeshCollider` | `PhysicsCollider`<br>`PhysicsWorldIndex` | Generates a `BlobAssetReference<Collider>`. Layer settings and `Is Trigger` / `Provides Contacts` translate directly to `CollisionFilter` and `CollisionResponsePolicy`. |
| `UnityEngine.Rigidbody` | `PhysicsVelocity`<br>`PhysicsMass`<br>`PhysicsDamping` | Generates mass and inertia tensor. If **"Is Kinematic"** is checked, `PhysicsMass` is omitted or configured with zero inverse mass. |
| `UnityEngine.FixedJoint`<br>`UnityEngine.HingeJoint` | `PhysicsJoint`<br>`PhysicsConstrainedBodyPair` | Translates anchor frames and limits to DOTS constraints. |

---

## 2. Automatic Compound Collider Baking

To create complex, irregular colliders without paying the massive performance cost of concave mesh colliders:
1. Place a root GameObject with a `Rigidbody` and a primitive collider (e.g., `SphereCollider`).
2. Add multiple child GameObjects beneath the root, each with primitive colliders (e.g., `BoxCollider` wings, arms, attachments).
3. **Baking behavior**:
   - The baker lifts all child colliders to assemble a single **Compound Collider** on the root entity.
   - The child entities retain their visual transforms and meshes for rendering, but their colliders are merged into the root's `PhysicsCollider`.

---

## 3. Collision Filtering via Layers

To configure collision masks using standard Unity layers:
1. Assign objects to specific Unity Layers (e.g., `Red`, `Yellow`, `Player`, `Debris`).
2. Under the Collider inspector, use **Layer Overrides → Exclude Layers** to declare layers that should be ignored.
3. The baker automatically configures the baked `CollisionFilter.BelongsTo` and `CollisionFilter.CollidesWith` bitmasks.

---

## 4. Runtime Procedural Entity Creation

When generating physics entities from pure C# code without subscenes:

### 1. Creating a Static Collider Entity
```csharp
public static Entity CreateStaticBox(EntityManager em, float3 position, float3 size)
{
    Entity entity = em.CreateEntity();

    // 1. Transforms
    em.AddComponentData(entity, LocalTransform.FromPosition(position));
    em.AddComponentData(entity, new LocalToWorld());

    // 2. Physics World Index
    em.AddSharedComponent(entity, new PhysicsWorldIndex { Value = 0 });

    // 3. Blob Collider
    var boxGeometry = new BoxGeometry
    {
        Center = float3.zero,
        Size = size,
        Orientation = quaternion.identity
    };
    BlobAssetReference<Collider> colliderBlob = BoxCollider.Create(boxGeometry, CollisionFilter.Default);
    em.AddComponentData(entity, new PhysicsCollider { Value = colliderBlob });

    return entity;
}
```

### 2. Creating a Dynamic Rigid Body Entity
```csharp
public static Entity CreateDynamicSphere(EntityManager em, float3 position, float radius, float massValue)
{
    Entity entity = em.CreateEntity();

    // 1. Transforms
    em.AddComponentData(entity, LocalTransform.FromPosition(position));
    em.AddComponentData(entity, new LocalToWorld());

    // 2. Physics World
    em.AddSharedComponent(entity, new PhysicsWorldIndex { Value = 0 });

    // 3. Collider Blob
    var sphereGeometry = new SphereGeometry { Center = float3.zero, Radius = radius };
    BlobAssetReference<Collider> colliderBlob = SphereCollider.Create(sphereGeometry, CollisionFilter.Default);
    em.AddComponentData(entity, new PhysicsCollider { Value = colliderBlob });

    // 4. Dynamic Velocity & Mass
    em.AddComponentData(entity, new PhysicsVelocity { Linear = float3.zero, Angular = float3.zero });
    em.AddComponentData(entity, PhysicsMass.CreateDynamic(colliderBlob.Value.MassProperties, massValue));

    // 5. Optional Damping
    em.AddComponentData(entity, new PhysicsDamping { Linear = 0.05f, Angular = 0.05f });

    return entity;
}
```

### Available Collider Factory Methods:
- `BoxCollider.Create()`
- `SphereCollider.Create()`
- `CapsuleCollider.Create()`
- `CylinderCollider.Create()`
- `ConvexCollider.Create()`
- `CompoundCollider.Create()`
- `MeshCollider.Create()`
- `TerrainCollider.Create()`
