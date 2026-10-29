---
name: dots-graphics-animation
description: Implements, optimizes, and debugs graphics, rendering, mesh deformation, and animations in Unity DOTS / Entities using Entities Graphics (com.unity.entities.graphics), Shader Graph, compute/vertex skinning, and GPU instancing. Use when working with Entities Graphics, RenderMeshArray, RenderMeshUtility, MaterialMeshInfo, MaterialProperty overrides, GPU skinning, Linear Blend Skinning, Compute Deformation, bone transforms, or high-performance animated entities.
allowed-tools: WebFetch, WebSearch
metadata:
  version: 1.0.0
  systems: Unity.Entities.Graphics, Unity.Deformations, Unity.Rendering, Entities, URP, HDRP
---

# Skill: dots-graphics-animation (Entities Graphics & Animation)

Authoritative reference and diagnostic guide for **Entities Graphics** (`com.unity.entities.graphics`), **Mesh Deformation / Skinning** (`Unity.Deformations`), and high-performance animation architectures in Unity DOTS / ECS.

---

## Fast-Paths & Critical Rules (Read First)

### Fast-Path 1: Skinned Mesh Rendered as Static T-Pose
**Symptom**: Skinned mesh entities appear in the scene but stay frozen in T-pose or don't animate.
- **Cause**: Skin matrices are missing, not calculated, or not updated before the deformation pass.
- **Fix**:
  1. Ensure the entity possesses `DynamicBuffer<SkinMatrix>` (from `Unity.Deformations`).
  2. Ensure your animation system updates bone `LocalTransform` components *before* `PresentationSystemGroup`.
  3. Ensure a skin matrix calculation system updates in `[UpdateInGroup(typeof(PresentationSystemGroup))]` with `[UpdateBefore(typeof(DeformationsInPresentation))]`.
  4. See [mesh-deformation-and-skinning.md](./references/mesh-deformation-and-skinning.md).

### Fast-Path 2: Material Overrides Breaking GPU Batching
**Symptom**: High draw calls / batches when tinting or customizing per-entity colors and properties.
- **Cause**: Creating new Material instances at runtime breaks SRP Batcher and GPU Resident Drawer instancing.
- **Fix**:
  1. Do NOT use `new Material()`. Use a single shared material with a DOTS-compatible Shader Graph.
  2. Expose the property in Shader Graph with **"Override Property Declaration"** enabled or hybrid instancing.
  3. Create an ECS component marked with `[MaterialProperty("_PropertyName")]` implementing `IComponentData`.
  4. See [material-property-overrides.md](./references/material-property-overrides.md).

### Fast-Path 3: Entities Invisible in Game View (Culling or Bounds Zero)
**Symptom**: Entities exist in the hierarchy/world and have mesh/material components, but do not render on screen.
- **Cause**: `RenderBounds` is uninitialized (extents `0,0,0`), causing frustum culling to cull them every frame.
- **Fix**: Assign a valid `RenderBounds { Value = mesh.bounds.ToAABB() }` when creating entities programmatically.

### Fast-Path 4: Choosing Between Compute vs. Vertex Shader Skinning
**Symptom**: Unsure whether to use Compute Deformation or Vertex Linear Blend Skinning for character crowds.
- **Rules of thumb**:
  - **Vertex Skinning (`Linear Blend Skinning` node in Shader Graph)**: Lower memory bandwidth, no compute buffer dispatch overhead, ideal for mobile/Switch and crowds under 2,000 skinned instances.
  - **Compute Skinning (`Compute Deformation` node in Shader Graph)**: Evaluates once per vertex in compute shaders, supports blend shapes + skinning combined, writes to GPU vertex cache, superior for high-polygon characters (10k+ verts each) on PC/Consoles.

### Fast-Path 5: Spawning Millions of Rendered Entities via Burst
**Symptom**: Calling `RenderMeshUtility.AddComponents` in a loop causes GC spikes or cannot run inside Burst jobs.
- **Fix**:
  1. Set up a prototype entity once on the main thread using `RenderMeshUtility.AddComponents` with a `RenderMeshArray`.
  2. Inside your Burst `IJobParallelFor` or `IJobEntity`, use `EntityCommandBuffer.ParallelWriter.Instantiate(index, prototype)`.
  3. Set per-entity `MaterialMeshInfo.FromRenderMeshArrayIndices(matIndex, meshIndex)` and `LocalToWorld`.

---

## Core System Architecture & Update Flow

```text
SimulationSystemGroup
└── (Gameplay / AI / Physics)
└── AnimationSystemGroup (Updates bone LocalTransform / LocalToWorld)
PresentationSystemGroup
├── CalculateSkinMatrixSystemBase (Reads bone LocalToWorlds -> Writes DynamicBuffer<SkinMatrix>)
├── DeformationsInPresentation (Unity.Deformations executes compute/vertex skinning buffers)
└── EntitiesGraphicsSystemGroup (Culling, LOD selection, BatchRendererGroup dispatch)
```

---

## Component Taxonomy

| Component | Namespace | Role |
| :--- | :--- | :--- |
| `RenderMeshArray` | `Unity.Rendering` | Shared component holding arrays of `Mesh` and `Material` assets. |
| `MaterialMeshInfo` | `Unity.Rendering` | Identifies which Mesh and Material index from `RenderMeshArray` this entity renders. |
| `RenderBounds` | `Unity.Rendering` | Local AABB for frustum and occlusion culling. |
| `PostTransformMatrix` | `Unity.Transforms` | Optional non-uniform scale matrix ($4\times4$) combined with `LocalTransform`. |
| `SkinMatrix` | `Unity.Deformations` | Buffer element containing the final $3\times4$ or $4\times4$ bone skinning transformation. |
| `BoneTag` / `RootTag` | Custom / Authoring | Marks bone entities and skeleton root entities for fast query grouping. |
| `[MaterialProperty]` | `Unity.Rendering` | Attribute binding an `IComponentData` to an instanced GPU shader variable. |

---

## Detailed References

For comprehensive deep dives, consult the reference library:
- [Entities Graphics Overview](./references/entities-graphics-overview.md): BatchRendererGroup, GPU Resident Drawer, SRP Batcher compatibility, LODs.
- [Mesh Deformation & Skinning](./references/mesh-deformation-and-skinning.md): Compute vs Vertex shader skinning, Shader Graph nodes, skin matrices pipeline.
- [Material Property Overrides](./references/material-property-overrides.md): Zero-drawcall property overrides via `[MaterialProperty]`.
- [Lighting & Environment](./references/lighting-and-environment.md): Light Probes, lightmaps, shadows, and ambient probes on entities.
- [Runtime Rendering APIs](./references/runtime-rendering-apis.md): Procedural rendering via C#, `RenderMeshArray`, and Burst mass-instantiation.
- [Animation Systems & Patterns](./references/animation-systems-patterns.md): Driving skeletal rigs, blending, and GPU animation instancing in ECS.
- [Baking & Authoring](./references/baking-and-authoring.md): Subscene baking of Renderers, SkinnedMeshRenderers, and LOD groups.
