# Runtime Rendering APIs & Mass Instantiation

When procedurally generating meshes or spawning thousands of visual entities at runtime without pre-baked subscenes, Entities Graphics provides high-performance APIs designed to work seamlessly with the C# Job System and Burst.

---

## 1. Key Data Structures

### `RenderMeshArray` (ISharedComponentData)
Stores references to managed assets (`Mesh[]` and `Material[]`). Multiple entities share the same `RenderMeshArray` index, grouping them into contiguous memory chunks.

### `MaterialMeshInfo` (IComponentData)
Specifies which mesh and material index from the entity's `RenderMeshArray` to draw:
```csharp
var info = MaterialMeshInfo.FromRenderMeshArrayIndices(materialIndex: 0, meshIndex: 0);
```

### `RenderMeshDescription`
Struct configuring rendering flags on the main thread:
- `ShadowCastingMode`
- `ReceiveShadows`
- `Layer`
- `FilterSettings`

---

## 2. Setting Up a Rendered Entity on Main Thread

Use `RenderMeshUtility.AddComponents` to add all necessary rendering components in a single call:

```csharp
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public static Entity CreateRenderPrototype(
    EntityManager entityManager,
    Mesh[] meshes,
    Material[] materials)
{
    Entity prototype = entityManager.CreateEntity();

    // 1. Transform components
    entityManager.AddComponentData(prototype, LocalTransform.Identity);
    entityManager.AddComponentData(prototype, new LocalToWorld());

    // 2. Render Mesh Description
    var desc = new RenderMeshDescription
    {
        FilterSettings = RenderFilterSettings.Default,
        LightProbeUsage = LightProbeUsage.BlendProbes,
        ReceiveShadows = true,
        ShadowCastingMode = ShadowCastingMode.On
    };

    // 3. Shared RenderMeshArray
    var renderMeshArray = new RenderMeshArray(materials, meshes);

    // 4. Default MaterialMeshInfo (points to mesh 0, material 0)
    var matMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

    // 5. Add all required rendering components atomically
    RenderMeshUtility.AddComponents(
        prototype,
        entityManager,
        desc,
        renderMeshArray,
        matMeshInfo);

    return prototype;
}
```

---

## 3. Burst Mass-Instantiation Pattern (10,000+ Entities)

Because `RenderMeshUtility.AddComponents` allocates managed references, **it cannot be called inside a Burst job**.

### The Professional Pattern:
1. Create a **Prototype Entity** once on the main thread (using the pattern above).
2. Schedule a Burst-compiled `IJobParallelFor` using `EntityCommandBuffer.ParallelWriter`.
3. Clone the prototype via `ecb.Instantiate(index, prototype)` and set per-instance transforms and material/mesh indices:

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[BurstCompile]
public struct SpawnEntityGridJob : IJobParallelFor
{
    public Entity Prototype;
    public EntityCommandBuffer.ParallelWriter ECB;
    public int Columns;
    public float Spacing;
    public int TotalMaterialCount;

    public void Execute(int index)
    {
        // 1. Clone prototype with all rendering components intact
        Entity newEntity = ECB.Instantiate(index, Prototype);

        // 2. Calculate position
        int x = index % Columns;
        int z = index / Columns;
        float3 position = new float3(x * Spacing, 0f, z * Spacing);

        // 3. Set transform
        ECB.SetComponent(index, newEntity, LocalTransform.FromPosition(position));

        // 4. Optionally vary mesh/material index per instance without structural changes
        int matIndex = index % TotalMaterialCount;
        ECB.SetComponent(index, newEntity, MaterialMeshInfo.FromRenderMeshArrayIndices(matIndex, 0));
    }
}
```

This pattern spawns 500,000 units in milliseconds across worker threads with zero GC allocations.
