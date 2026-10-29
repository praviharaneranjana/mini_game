# Collision Queries in Unity Physics

Collision queries allow your game code to probe the physics world for spatial relationships, line-of-sight, obstacle detection, and trajectory sweeps without needing permanent colliders.

All queries are executed against the `CollisionWorld`, obtained from the `PhysicsWorldSingleton`.

---

## 1. Obtaining `CollisionWorld`

```csharp
var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
ref readonly CollisionWorld collisionWorld = ref physicsWorldSingleton.CollisionWorld;
```

---

## 2. The Five Query Types

### 1. Ray Cast (`CastRay`)
Intersects a directed line segment with physics colliders. Ideal for bullets, laser sights, line-of-sight checks, and ground detection.

```csharp
var rayInput = new RaycastInput
{
    Start = originPosition,
    End = originPosition + forwardDirection * maxDistance,
    // CRITICAL: Filter must have valid BelongsTo and CollidesWith masks!
    Filter = CollisionFilter.Default 
};

// 1. Single closest hit:
if (collisionWorld.CastRay(rayInput, out RaycastHit closestHit))
{
    Entity hitEntity = closestHit.Entity;
    float3 hitPoint = closestHit.Position;
    float3 hitNormal = closestHit.SurfaceNormal;
    float hitDistance = math.distance(rayInput.Start, closestHit.Position);
}

// 2. All hits along ray:
var allHits = new NativeList<RaycastHit>(Allocator.Temp);
if (collisionWorld.CastRay(rayInput, ref allHits))
{
    for (int i = 0; i < allHits.Length; i++)
    {
        RaycastHit hit = allHits[i];
        // Process each penetrated entity
    }
}
allHits.Dispose();
```

---

### 2. Collider Cast / Sweep (`CastCollider`)
Sweeps a 3D collider geometry along a linear path. Essential for character movement checks, vehicle wheel sweeps, and thick projectile tests.

```csharp
unsafe
{
    // Create a sphere or capsule collider for the sweep
    var sphereGeometry = new SphereGeometry { Center = float3.zero, Radius = 0.5f };
    using var sphereCollider = SphereCollider.Create(sphereGeometry, CollisionFilter.Default);

    var sweepInput = new ColliderCastInput
    {
        Collider = (Collider*)sphereCollider.GetUnsafePtr(),
        Orientation = quaternion.identity,
        Start = currentPosition,
        End = targetPosition
    };

    if (collisionWorld.CastCollider(sweepInput, out ColliderCastHit castHit))
    {
        float fractionTraveled = castHit.Fraction; // 0.0 to 1.0 along the path
        float3 contactPoint = castHit.Position;
        float3 surfaceNormal = castHit.SurfaceNormal;
        Entity hitObstacle = castHit.Entity;
    }
}
```

---

### 3. Overlap Query (`OverlapAabb` / `OverlapPoint`)
Finds all colliders whose bounding boxes or volumes touch or encompass a specific region. Used for area-of-effect explosions, proximity detection, or item pickup radii.

```csharp
var aabbInput = new OverlapAabbInput
{
    Aabb = new Aabb { Min = center - extents, Max = center + extents },
    Filter = CollisionFilter.Default
};

var overlappedBodies = new NativeList<int>(Allocator.Temp);
if (collisionWorld.OverlapAabb(aabbInput, ref overlappedBodies))
{
    foreach (int bodyIndex in overlappedBodies)
    {
        Entity bodyEntity = collisionWorld.Bodies[bodyIndex].Entity;
    }
}
overlappedBodies.Dispose();
```

---

### 4. Collider Distance Query (`CalculateDistance`)
Finds the shortest line segment connecting a query shape to colliders within a maximum distance radius.

```csharp
unsafe
{
    var boxGeometry = new BoxGeometry { Center = float3.zero, Size = new float3(1, 1, 1), Orientation = quaternion.identity };
    using var boxCollider = BoxCollider.Create(boxGeometry, CollisionFilter.Default);

    var distanceInput = new ColliderDistanceInput
    {
        Collider = (Collider*)boxCollider.GetUnsafePtr(),
        Transform = new RigidTransform(quaternion.identity, searchCenter),
        MaxDistance = 5.0f
    };

    if (collisionWorld.CalculateDistance(distanceInput, out DistanceHit closestHit))
    {
        float distance = closestHit.Distance; // Negative if penetrating!
        float3 surfacePoint = closestHit.Position;
        Entity nearestEntity = closestHit.Entity;
    }
}
```

---

### 5. Point Distance Query (`CalculateDistance`)
Finds nearest colliders from a specific single point within a search sphere.

```csharp
var pointInput = new PointDistanceInput
{
    Position = queryPoint,
    MaxDistance = 10.0f,
    Filter = CollisionFilter.Default
};

if (collisionWorld.CalculateDistance(pointInput, out DistanceHit distanceHit))
{
    float distance = distanceHit.Distance;
    float3 hitPoint = distanceHit.Position;
}
```

---

## 3. Collision Filtering Masks

A `CollisionFilter` consists of three integers:
- `BelongsTo`: Bitmask indicating the layers this object occupies.
- `CollidesWith`: Bitmask indicating which layers this query or body should intersect.
- `GroupIndex`: Override filter (positive = always collide within group; negative = never collide within group; zero = use bitmasks).

> [!WARNING]
> **Zero Filter Trap**: If you write `var input = new RaycastInput { Start = a, End = b };` without setting `Filter`, `Filter.CollidesWith` defaults to `0`. **The raycast will pass through every collider and return false.** Always explicitly set `Filter = CollisionFilter.Default` or custom bitmasks.
