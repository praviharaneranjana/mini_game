# Dreamteck Splines Guidelines

This project utilizes **Dreamteck Splines** (`Dreamteck.Splines`) for curved path authoring, track geometry extrusion, rollercoaster/camera rails, and spline-based entity locomotion.

## Core Directives

1. **Curve Algorithm Selection**:
   - Use `Spline.Type.CatmullRom` for general tracks, camera rails, and smooth navigation curves without manual tangent authoring.
   - Use `Spline.Type.Bezier` when technical artists require discrete in/out control handles for precise architectural or level geometry.
   - Use `Spline.Type.Linear` for sharp grid-based paths, conveyor systems, or low-poly aesthetic tracks.

2. **Sample Modes & Arc-Length Consistency**:
   - Always set `spline.sampleMode = SplineComputer.SampleMode.Uniform` for entities using constant speed or for procedural tracks requiring equidistant UV repeats.
   - Keep `sampleRate` tuned: default to `20-30` samples per segment for high-speed followers; decrease to `10-15` for background ambient paths to reduce memory.
   - Call `spline.RebuildImmediate()` after modifying points programmatically before querying distance or evaluating samples.

3. **Follower Physics & Thread Safety**:
   - When a `SplineFollower` moves a physics-interacting object (e.g. minecart, vehicle with colliders), always set:
     - `follower.physicsMode = SplineFollower.PhysicsMode.Rigidbody`
     - `follower.updateMethod = SplineUser.UpdateMethod.FixedUpdate`
   - For camera rigs or visual particles, use `PhysicsMode.Transform` and `UpdateMethod.Update` for maximum frame-rate smoothness.

4. **Procedural Geometry (`SplineMesh`)**:
   - Reuse extruded meshes where possible. When generating dynamic roads or tubes via `SplineMesh`, ensure custom cross-sections are clean quad strips without internal non-manifold geometry.
   - Ensure the GameObject has an appropriate `MeshRenderer` and `MeshFilter` component initialized before calling `splineMesh.RebuildImmediate()`.
