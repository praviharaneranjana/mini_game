# Material Property Overrides in DOTS

In GameObject development, varying colors or parameters per instance required `MaterialPropertyBlock` (clunky and memory intensive) or creating new material instances (`renderer.material`), which breaks batching and causes severe draw call spikes.

In **Entities Graphics**, every per-instance material override is an unmanaged **ECS component** decorated with the `[MaterialProperty]` attribute. The GPU Resident Drawer uploads these components directly into GPU instancing buffers, enabling **thousands of uniquely tinted or parameterized entities to render in a single draw call**.

---

## 1. Built-In URP Material Properties

Entities Graphics includes pre-baked override components for the standard URP Lit shader:

| Override Component | Underlying Shader Property | Data Type | Usage |
| :--- | :--- | :--- | :--- |
| `URPMaterialPropertyBaseColor` | `_BaseColor` | `float4` | Per-entity albedo / diffuse tint |
| `URPMaterialPropertyMetallic` | `_Metallic` | `float` | Per-entity metallic reflection |
| `URPMaterialPropertySmoothness` | `_Smoothness` | `float` | Per-entity surface glossiness |
| `URPMaterialPropertyEmissionColor` | `_EmissionColor` | `float4` | Per-entity glow / emission |

### Example: Dynamically Pulsing Entity Colors
```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct PulseColorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float time = (float)SystemAPI.Time.ElapsedTime;

        foreach (var color in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>>())
        {
            float green = (math.sin(time * 3f) + 1f) * 0.5f;
            color.ValueRW.Value = new float4(1f, green, 0.2f, 1f);
        }
    }
}
```

---

## 2. Creating Custom Shader Graph Overrides

To expose your own custom parameters (e.g., dissolve progress, team color, shield hit glow) to ECS:

### Step 1: Configure Shader Graph
1. Open your Shader Graph.
2. In the Blackboard, create a property (e.g., **Float** named `DissolveAmount`).
3. In Graph Inspector -> Node Settings, set the **Reference** name to `_DissolveAmount` (prefix with underscore).
4. Ensure **DOTS Instancing** is enabled in Graph Settings.

### Step 2: Define the ECS Component
```csharp
using Unity.Entities;
using Unity.Rendering;

[MaterialProperty("_DissolveAmount")]
public struct DissolveAmountOverride : IComponentData
{
    public float Value;
}
```

### Step 3: Add Authoring Baker (Optional for Editor Workflow)
```csharp
using Unity.Entities;
using UnityEngine;

public class DissolveAuthoring : MonoBehaviour
{
    [Range(0f, 1f)] public float InitialDissolve = 0f;

    class Baker : Baker<DissolveAuthoring>
    {
        public override void Bake(DissolveAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new DissolveAmountOverride
            {
                Value = authoring.InitialDissolve
            });
        }
    }
}
```

---

## 3. High-Performance Best Practices

1. **Keep Component Types Structurally Aligned**: If 10,000 entities have `URPMaterialPropertyBaseColor` and only 5 have `DissolveAmountOverride`, they belong to two different archetypes. For optimal chunk density, apply the same set of override components across uniform entity collections.
2. **Use Shared Materials**: All entities must reference the exact same `Material` asset. The shader reads the per-entity value from the GPU instancing buffer.
3. **Zero Draw Call Penalty**: 100,000 units with distinct colors, metallic values, and dissolve amounts will render as **1 single batch** via GPU Resident Drawer if they share the same mesh and material!
