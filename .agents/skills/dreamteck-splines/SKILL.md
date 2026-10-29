---
name: dreamteck-splines
description: Authors, modifies, evaluates, and optimizes curved paths, roads, tracks, spline followers, and procedural meshes using Dreamteck Splines (Dreamteck.Splines). Covers SplineComputer point manipulation, Bezier/Hermite/CatmullRom interpolation, SplineFollower physics/movement, SplineMesh extrusion, triggers, junctions, and runtime pathfinding.
---

# Dreamteck Splines Skill

This skill guides the implementation, mathematical evaluation, and procedural generation of curved spline paths using **Dreamteck Splines** (`Dreamteck.Splines`) in Unity.

## Quick Reference & Core Types

| Class / Component | Namespace | Core Purpose |
| :--- | :--- | :--- |
| **`SplineComputer`** | `Dreamteck.Splines` | The central spline generator. Stores `SplinePoint[]`, curve type, sample mode, and sample caching. |
| **`SplinePoint`** | `Dreamteck.Splines` | Individual spline control point holding position, tangents (tangent, tangent2), normal, color, and size. |
| **`SplineFollower`** | `Dreamteck.Splines` | Moves GameObjects along splines with constant speed, physics update, looping, or ping-pong. |
| **`SplineMesh`** | `Dreamteck.Splines` | Procedurally extrudes 2D profiles or tiles meshes along the spline trajectory (roads, pipes, tracks). |
| **`SplineProjector`** | `Dreamteck.Splines` | Projects arbitrary 3D positions onto the closest point on the spline curve. |
| **`SplineTrigger`** | `Dreamteck.Splines` | Spatial event trigger along the curve at a normalized percent `[0.0, 1.0]`. |
| **`Node`** | `Dreamteck.Splines` | Connects multiple splines together into intersections, branches, and rail switches. |

---

## Core Workflows

### 1. Procedural Spline Generation in C#
```csharp
using Dreamteck.Splines;
using UnityEngine;

public static class ProceduralSplineFactory
{
    public static SplineComputer CreateCurvedTrack(Vector3[] waypoints, bool closed = false)
    {
        var go = new GameObject("ProceduralSpline");
        var spline = go.AddComponent<SplineComputer>();

        // Set interpolation mode: CatmullRom, Bezier, BSpline, Hermite, Linear
        spline.type = Spline.Type.CatmullRom;
        spline.sampleMode = SplineComputer.SampleMode.Uniform;

        SplinePoint[] points = new SplinePoint[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            points[i] = new SplinePoint(waypoints[i]);
            points[i].size = 1f;
            points[i].color = Color.white;
            points[i].normal = Vector3.up;
        }

        spline.SetPoints(points);
        if (closed) spline.Close();
        spline.RebuildImmediate();

        return spline;
    }
}
```

### 2. Moving Objects with `SplineFollower`
```csharp
using Dreamteck.Splines;
using UnityEngine;

[RequireComponent(typeof(SplineFollower))]
public class RollercoasterCart : MonoBehaviour
{
    private SplineFollower _follower;

    private void Awake()
    {
        _follower = GetComponent<SplineFollower>();
        _follower.updateMethod = SplineUser.UpdateMethod.FixedUpdate;
        _follower.physicsMode = SplineFollower.PhysicsMode.Rigidbody;
        _follower.wrapMode = SplineFollower.Wrap.Loop;
        _follower.followSpeed = 15f; // meters per second
    }

    public void Boost(float multiplier)
    {
        _follower.followSpeed *= multiplier;
    }
}
```

### 3. Sampling Curves at Runtime
```csharp
using Dreamteck.Splines;
using UnityEngine;

public static class SplineEvaluator
{
    public static void SamplePath(SplineComputer spline, double percent)
    {
        // 1. Evaluate position, tangent forward, and normal vector
        SplineSample result = spline.Evaluate(percent);

        Vector3 worldPos = result.position;
        Vector3 forwardDir = result.forward;
        Vector3 upDir = result.up;

        // 2. Travel distance along curve
        double targetPercent = spline.Travel(percent, 5.0, Spline.Direction.Forward);
        Vector3 futurePos = spline.EvaluatePosition(targetPercent);
    }
}
```

---

## Detailed References
- [Spline Authoring, Interpolation & API Reference](file:///d:/Unity%20Projects/mini_game/.agents/skills/dreamteck-splines/references/spline-authoring-api.md)
- [SplineMesh, Procedural Tracks & Follower Generation](file:///d:/Unity%20Projects/mini_game/.agents/skills/dreamteck-splines/references/mesh-and-follower-generation.md)
