# Mesh Deformation & GPU Skinning in DOTS

Mesh deformation in Unity Entities (`Unity.Deformations`) allows thousands of animated characters with skeletal rigs and blend shapes to render with unprecedented performance using GPU skinning.

---

## 1. Skinning Methods: Vertex Shader vs. Compute Shader

Entities Graphics provides two distinct deformation paths:

| Feature | Vertex Shader Skinning | Compute Shader Skinning |
| :--- | :--- | :--- |
| **Shader Graph Node** | **`Linear Blend Skinning`** | **`Compute Deformation`** |
| **Execution Phase** | Inside the vertex shader during rasterization | Separate compute dispatch before rasterization |
| **Blend Shapes** | Not supported (or very limited) | Full support for multiple blend shape targets |
| **Normal & Tangent Recalculation** | Approximated on vertex shader | Accurately computed on GPU cache buffer |
| **Pass Reuse** | Recomputed in every pass (depth prepass, shadow pass, gbuffer) | Deforms vertices **once** into a cache buffer, reused across all passes |
| **Best Used For** | Large crowds, mobile devices, low-to-mid poly meshes ($< 3k$ vertices) | Hero characters, high-poly meshes ($> 10k$ vertices), characters with facial blendshapes |

---

## 2. The Skinning Pipeline

The data flow from bone entities to on-screen rendered meshes follows this exact sequence:

```
[1. Animation System] (Updates bone LocalTransform components)
        │
        ▼
[2. Transform System Group] (Computes bone LocalToWorld matrices)
        │
        ▼
[3. CalculateSkinMatrixSystem] (Evaluates SkinMatrix = Inv(Root) * Bone * BindPose)
        │
        ▼
[4. DeformationsInPresentation] (Uploads SkinMatrices to GPU ByteAddressBuffers)
        │
        ▼
[5. Draw Passes] (Shader Graph applies skinning via vertex or compute node)
```

---

## 3. Calculating Skin Matrices

The mesh entity contains a `DynamicBuffer<SkinMatrix>` where each element represents the transformation matrix for a specific bone.

### Calculation Formula:
$$SkinMatrix_i = \text{RootWorldToLocal} \times \text{BoneLocalToWorld}_i \times \text{BindPose}_i$$

### System Implementation Pattern:

```csharp
using Unity.Collections;
using Unity.Deformations;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(DeformationsInPresentation))]
public partial class CalculateSkinMatrixSystem : SystemBase
{
    private EntityQuery _boneQuery;
    private EntityQuery _rootQuery;

    protected override void OnCreate()
    {
        _boneQuery = GetEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<BoneTag>()
        );

        _rootQuery = GetEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<RootTag>()
        );
    }

    protected override void OnUpdate()
    {
        // 1. Gather bone matrices in a fast lookup map
        var boneCount = _boneQuery.CalculateEntityCount();
        var boneTransforms = new NativeParallelHashMap<Entity, float4x4>(boneCount, Allocator.TempJob);

        Entities
            .WithStoreEntityQueryInField(ref _boneQuery)
            .ForEach((Entity entity, in LocalToWorld ltw, in BoneTag tag) =>
            {
                boneTransforms.TryAdd(entity, ltw.Value);
            }).ScheduleParallel();

        // 2. Gather inverse root transforms
        var rootCount = _rootQuery.CalculateEntityCount();
        var rootTransforms = new NativeParallelHashMap<Entity, float4x4>(rootCount, Allocator.TempJob);

        Entities
            .WithStoreEntityQueryInField(ref _rootQuery)
            .ForEach((Entity entity, in LocalToWorld ltw, in RootTag tag) =>
            {
                rootTransforms.TryAdd(entity, math.inverse(ltw.Value));
            }).ScheduleParallel();

        // 3. Compute final SkinMatrices for each skinned mesh entity
        Dependency = new CalculateSkinMatricesJob
        {
            BonesMap = boneTransforms,
            RootsMap = rootTransforms
        }.ScheduleParallel(Dependency);

        boneTransforms.Dispose(Dependency);
        rootTransforms.Dispose(Dependency);
    }

    public partial struct CalculateSkinMatricesJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<Entity, float4x4> BonesMap;
        [ReadOnly] public NativeParallelHashMap<Entity, float4x4> RootsMap;

        public void Execute(
            ref DynamicBuffer<SkinMatrix> skinMatrices,
            in DynamicBuffer<SkinBoneEntity> bones,
            in DynamicBuffer<BindPose> bindPoses,
            in RootEntityReference rootRef)
        {
            if (!RootsMap.TryGetValue(rootRef.RootEntity, out float4x4 rootWorldToLocal))
                return;

            for (int i = 0; i < bones.Length; i++)
            {
                Entity boneEntity = bones[i].Value;
                if (BonesMap.TryGetValue(boneEntity, out float4x4 boneLtw))
                {
                    float4x4 finalMatrix = math.mul(rootWorldToLocal, math.mul(boneLtw, bindPoses[i].Value));
                    skinMatrices[i] = new SkinMatrix { Value = new float3x4(finalMatrix.c0.xyz, finalMatrix.c1.xyz, finalMatrix.c2.xyz, finalMatrix.c3.xyz) };
                }
            }
        }
    }
}
```

---

## 4. Setting Up Shader Graph for DOTS Skinning

To enable skinning in your custom Shader Graph:

### For Vertex Shader Skinning:
1. Open your Shader Graph (URP or HDRP Lit).
2. Add the **`Linear Blend Skinning`** node.
3. Connect the output **Position**, **Normal**, and **Tangent** into the **Vertex Stage** inputs of the Master Stack (`Vertex Position`, `Vertex Normal`, `Vertex Tangent`).
4. In Graph Settings, ensure **"Support LODs"** and **"DOTS Instancing"** are checked.

### For Compute Shader Skinning:
1. Add the **`Compute Deformation`** node.
2. Connect its outputs into the Vertex Stage.
3. On the character's authoring component, set Deformation Mode to **Compute**.
