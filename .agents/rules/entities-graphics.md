# Entities Graphics & Animation Architecture Rule

When authoring, generating, or refactoring rendering and animations in Unity DOTS / ECS:

## 1. Zero-Drawcall Material Overrides
- **Never instantiate materials at runtime** (`new Material()` or `renderer.material`).
- Always use a single shared material with a DOTS-compatible Shader Graph.
- For per-instance color, tint, emission, or custom floats, define an `IComponentData` decorated with `[MaterialProperty("_PropertyName")]`.
- All instances sharing the same mesh and material will render in a **single GPU Resident Drawer batch**.

## 2. Proper Skinned Mesh System Ordering
- Bone animations must update bone `LocalTransform` components **before** `PresentationSystemGroup`.
- `CalculateSkinMatrixSystem` must update in `[UpdateInGroup(typeof(PresentationSystemGroup))]` and `[UpdateBefore(typeof(DeformationsInPresentation))]`.
- Skinned mesh entities must have `DynamicBuffer<SkinMatrix>`.
- In Shader Graph, connect `Linear Blend Skinning` (for vertex skinning) or `Compute Deformation` (for compute skinning) into the Vertex Stage.

## 3. Burst Mass Instantiation
- Never invoke `RenderMeshUtility.AddComponents` inside loops or Burst jobs (it performs managed allocations).
- Create a single **prototype entity** on the main thread with `RenderMeshArray` and `MaterialMeshInfo`.
- In Burst jobs (`IJobParallelFor`), clone the prototype with `EntityCommandBuffer.ParallelWriter.Instantiate()` and set per-instance `LocalTransform` and `MaterialMeshInfo`.

## 4. Culling & Render Bounds
- Every rendered entity must have a valid `RenderBounds` containing local mesh extents. An uninitialized `RenderBounds` (`0,0,0`) will cause immediate frustum culling.

## 5. Choosing the Right Animation Architecture
- **Hero Characters / Cutscenes ($< 100$)**: Hybrid Animator or DOTS skeletal skinning with IK.
- **Armies & Squads ($1,000 - 10,000$)**: DOTS Skeletal Skinning (`Unity.Deformations`) with Burst bone jobs.
- **Massive Crowds & Swarms ($10,000 - 100,000+$)**: Vertex Animation Textures (VAT) sampled directly in Shader Graph.
