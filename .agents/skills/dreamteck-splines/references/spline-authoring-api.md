# Dreamteck Splines: Authoring, Interpolation & Mathematical Evaluation

This reference details the curve mathematics, sample caching, evaluation methods, and junction nodes of Dreamteck Splines.

---

## 1. Spline Curve Algorithms

Dreamteck Splines supports five mathematical curve interpolation models:

| Algorithm (`Spline.Type`) | Continuity | Tangent Controls | Best For |
| :--- | :--- | :--- | :--- |
| **`CatmullRom`** | $C^1$ continuous | Automated | Smooth paths, rollercoasters, racing tracks without manual handles. |
| **`Bezier`** | $C^1$ / $G^1$ continuous | Two tangent handles per point | Precise curve sculpting, stylized arcs, manual track profiling. |
| **`BSpline`** | $C^2$ continuous | Approximation control points | Super-smooth natural river paths, flight paths (does not pass through points). |
| **`Hermite`** | $C^1$ continuous | Single tangent vector | Keyframed cinematics, robotic arm interpolations. |
| **`Linear`** | $C^0$ continuous | No tangents (polygonal) | Grid paths, sharp angular turns, conveyor belts. |

---

## 2. Sample Modes & Performance

A `SplineComputer` computes and caches `SplineSample[]` tables to enable fast $O(1)$ runtime evaluation:

- **`SampleMode.Default`**: Samples based on control point density. Fast rebuild, but sample intervals vary on curved segments.
- **`SampleMode.Uniform`**: Resamples the curve so samples are equidistant in 3D arc-length. **Crucial for constant-speed followers and uniform mesh extrusion**.
- **`SampleMode.Optimized`**: Subdivides only where curvature exceeds a tolerance angle. Minimizes vertex count for procedural roads.

```csharp
spline.sampleMode = SplineComputer.SampleMode.Uniform;
spline.sampleRate = 30; // Samples per spline segment
spline.Rebuild();
```

---

## 3. Position & Direction Evaluation API

```csharp
using Dreamteck.Splines;
using UnityEngine;

public class SplineNavigator
{
    public void Navigate(SplineComputer spline, Vector3 currentPos)
    {
        // Project arbitrary point onto spline (find closest percent)
        double closestPercent = spline.Project(currentPos);
        Vector3 projectedPos = spline.EvaluatePosition(closestPercent);

        // Calculate distance from point to spline
        float distanceToCenter = Vector3.Distance(currentPos, projectedPos);

        // Travel 10 units forward along the curve
        double aheadPercent = spline.Travel(closestPercent, 10.0, Spline.Direction.Forward);
        Vector3 aheadPos = spline.EvaluatePosition(aheadPercent);
        Vector3 tangentForward = spline.Evaluate(aheadPercent).forward;
    }
}
```

---

## 4. Branching & Junctions with `Node`

To create rail switches, track forks, or road intersections:
1. Attach a `Node` component to a GameObject.
2. Add connections to multiple `SplineComputer` components.
3. In `SplineFollower`, subscribe to `onNode` callbacks to dynamically choose path branches:

```csharp
using Dreamteck.Splines;
using UnityEngine;

public class TrackSwitcher : MonoBehaviour
{
    public SplineFollower follower;

    private void Start()
    {
        follower.onNode += OnReachedJunction;
    }

    private void OnReachedJunction(List<SplineTracer.NodeConnection> connections)
    {
        // Select random branch or decision-based branch
        int branchIndex = Random.Range(0, connections.Count);
        follower.SetClipRange(0.0, 1.0);
        follower.spline = connections[branchIndex].computer;
    }
}
```
