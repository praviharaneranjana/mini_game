# Entities Graphics Architecture Overview

Entities Graphics (`com.unity.entities.graphics`) is the rendering engine for Unity's Data-Oriented Technology Stack (DOTS), enabling high-performance rendering of hundreds of thousands of dynamic, animated entities.

---

## 1. How Entities Graphics Works

Traditional GameObject rendering invokes individual draw calls or relies on CPU-side matrix arrays. In contrast, Entities Graphics leverages:

1. **BatchRendererGroup (BRG)**: The low-level graphics API that feeds entity data directly from native ECS chunk memory to the GPU via persistent ByteAddressBuffers (Structured Buffers).
2. **GPU Resident Drawer (Unity 6)**: Automatically uploads mesh instances, transforms, and material properties into persistent GPU buffers. The GPU draws instances directly with minimal CPU driver overhead.
3. **Chunk-Aligned Data**: Because entities sharing identical archetypes are stored contiguously in 16KB memory chunks, visibility culling and instanced draw command generation can be processed via SIMD and Burst parallel jobs.

---

## 2. Rendering Component Architecture

When an entity is rendered via Entities Graphics, it possesses these core components:

```
[Entity]
  ├── LocalToWorld (float4x4 world matrix)
  ├── LocalTransform (position, rotation, scale)
  ├── RenderMeshArray (Shared: Array of Mesh & Material assets)
  ├── MaterialMeshInfo (Indices into RenderMeshArray: meshIndex, materialIndex)
  ├── RenderBounds (Local AABB bounds for culling)
  └── [Optional MaterialProperty Overrides] (e.g., URPBaseColor, Tint)
```

### Key Differences from GameObjects:
- **No MeshRenderer / MeshFilter objects**: Replaced by lightweight value-type components.
- **`RenderMeshArray` is Shared**: Multiple entities point to the same `RenderMeshArray` instance, grouping them into identical chunk archetypes for batching.
- **Dynamic Swapping**: Change the rendered mesh or material simply by writing `MaterialMeshInfo.FromRenderMeshArrayIndices(newMatIdx, newMeshIdx)` without creating or destroying components.

---

## 3. Frustum, Occlusion Culling & LODs

### Frustum Culling
- Entities Graphics executes high-speed Burst jobs that test entity `RenderBounds` (transformed by `LocalToWorld`) against the active camera's view frustum.
- Culled entities are omitted from the GPU draw packet before submission.

### Level of Detail (LOD)
- LOD groups authored in subscenes are baked into `MeshLODGroupComponent` and per-LOD child entities.
- Entities Graphics evaluates camera distance / screen-relative metric per chunk or per instance to toggle appropriate LOD indices seamlessly.

---

## 4. Pipeline Compatibility

Entities Graphics supports both scriptable render pipelines:
- **Universal Render Pipeline (URP)**: Supported via URP Forward+, Forward, or Deferred paths. Fully compatible with Shader Graph and URP Lit / Simple Lit shaders.
- **High Definition Render Pipeline (HDRP)**: Supported with full exposure, volumetrics, ray tracing, and subsurface scattering.
