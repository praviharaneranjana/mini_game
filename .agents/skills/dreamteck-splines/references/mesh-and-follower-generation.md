# SplineMesh, Procedural Tracks & Follower Generation

This reference covers procedurally extruding 3D geometry with `SplineMesh`, generating roads and tubes, and configuring `SplineFollower` physics.

---

## 1. Procedural Roads & Tubes with `SplineMesh`

`SplineMesh` extrudes 2D profiles along the 3D spline trajectory, generating dynamic vertex, normal, and UV buffers:

```csharp
using Dreamteck.Splines;
using UnityEngine;

public static class RoadGenerator
{
    public static void GenerateRoad(SplineComputer spline, Material roadMaterial)
    {
        var splineMesh = spline.gameObject.GetComponent<SplineMesh>() ?? spline.gameObject.AddComponent<SplineMesh>();
        splineMesh.spline = spline;

        // Create a custom extruded channel (e.g. flat road)
        var channel = splineMesh.AddChannel("RoadSurface");
        channel.minPercent = 0.0;
        channel.maxPercent = 1.0;

        // Configure profile cross-section (flat quad)
        channel.customMesh = CreateFlatQuadProfile(width: 4f);
        channel.clipFrom = 0.0;
        channel.clipTo = 1.0;

        // Material assignment
        var renderer = spline.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = roadMaterial;

        splineMesh.RebuildImmediate();
    }

    private static Mesh CreateFlatQuadProfile(float width)
    {
        var mesh = new Mesh();
        float half = width * 0.5f;
        mesh.vertices = new Vector3[] { new Vector3(-half, 0, 0), new Vector3(half, 0, 0) };
        return mesh;
    }
}
```

---

## 2. Advanced `SplineFollower` Modes

### Physics vs Transform Update
- **`UpdateMethod.Update`**: Best for smooth camera rails and cosmetic effects.
- **`UpdateMethod.FixedUpdate`**: Mandatory for physics objects interacting with Rigidbodies and colliders.

### Follower Physics Modes
- **`PhysicsMode.Transform`**: Directly updates `transform.position` and `transform.rotation`.
- **`PhysicsMode.Rigidbody`**: Moves via `Rigidbody.MovePosition()` and `Rigidbody.MoveRotation()`, preserving continuous collision detection.
- **`PhysicsMode.Rigidbody2D`**: Moves via 2D physics engine.

```csharp
using Dreamteck.Splines;
using UnityEngine;

public class MinecartController : MonoBehaviour
{
    private SplineFollower _follower;
    private Rigidbody _rb;

    private void Start()
    {
        _follower = GetComponent<SplineFollower>();
        _rb = GetComponent<Rigidbody>();

        _follower.physicsMode = SplineFollower.PhysicsMode.Rigidbody;
        _follower.updateMethod = SplineUser.UpdateMethod.FixedUpdate;
        _follower.motion.applyRotationX = true;
        _follower.motion.applyRotationY = true;
        _follower.motion.applyRotationZ = true;
    }
}
```

---

## 3. Spatial Event Triggers with `SplineTrigger`

Triggers trigger gameplay events when a `SplineFollower` passes a specific percentile mark along the spline:

```csharp
using Dreamteck.Splines;
using UnityEngine;

public class SplineTriggerSetup : MonoBehaviour
{
    public void AddSpeedBoostTrigger(SplineComputer spline, double percent)
    {
        var triggerGroup = spline.triggerGroups[0];
        var trigger = new SplineTrigger(SplineTrigger.Type.Double);
        trigger.position = percent;
        trigger.name = "SpeedBoost";

        // Subscribe to trigger event
        trigger.AddListener((user) =>
        {
            if (user is SplineFollower follower)
            {
                follower.followSpeed *= 1.5f;
                Debug.Log($"Boost activated for {follower.name} at percent {percent:P0}!");
            }
        });
    }
}
```
