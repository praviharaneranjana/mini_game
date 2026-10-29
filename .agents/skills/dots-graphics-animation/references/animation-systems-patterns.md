# Animation Architectures & Patterns in DOTS

To achieve both high visual fidelity and maximum rendering throughput, DOTS supports several animation paradigms depending on entity count and gameplay requirements.

---

## 1. Paradigm Comparison Matrix

| Approach | Max Instances | CPU Overhead | GPU Overhead | Flexibility | Best For |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Hybrid Animator** | 50 – 200 | High (MonoBehaviour / Mecanim) | Low | Maximum (Unity Animator, Mecanim graphs) | Player characters, boss enemies, cutscenes |
| **DOTS Skeletal Skinning (`Unity.Deformations`)** | 2,000 – 10,000 | Low (Burst bone evaluation) | Medium | High (supports IK, ragdoll blending, physics) | RTS units, action RPG combatants, squads |
| **Vertex Animation Textures (VAT / Instancing)** | **100,000+** | **Zero** (1 float per instance) | Very Low | Fixed clips (cannot easily do procedural IK) | Massive zombie hordes, armies, birds, fish |

---

## 2. DOTS Skeletal Animation Architecture

In pure DOTS skeletal animation:
1. Each character rig is an entity hierarchy:
   - Root entity with character logic.
   - Child entities representing individual bones (`BoneTag`, `Parent`, `Child`, `LocalTransform`).
2. An **Animation Job** updates bone local rotations and translations using Burst:

```csharp
[BurstCompile]
public partial struct WalkCycleAnimationJob : IJobEntity
{
    public float Time;

    public void Execute(ref LocalTransform transform, in BoneAnimationData animData)
    {
        // Procedural gait / clip evaluation in Burst
        float angle = math.sin(Time * animData.Frequency + animData.Phase) * animData.Amplitude;
        transform.Rotation = math.mul(animData.RestRotation, quaternion.AxisAngle(math.right(), angle));
    }
}
```

3. `TransformSystemGroup` converts bone `LocalTransform` to `LocalToWorld`.
4. `CalculateSkinMatrixSystem` computes skin matrices for `DeformationsInPresentation`.

---

## 3. Vertex Animation Textures (VAT) for 100,000+ Entities

For massive battlefields, zombie swarms, or background crowds where skeletal bone hierarchies would overwhelm transform memory:

### Mechanism:
1. Pre-bake character animations (positions and normals of all vertices over time) into an HDR 2D texture map.
   - $X$-axis: Vertex index
   - $Y$-axis: Animation frame time
2. The entity only requires:
   - `LocalTransform` (world position/rotation)
   - `MaterialMeshInfo` (standard static mesh)
   - `[MaterialProperty("_AnimTime")] struct AnimTimeOverride : IComponentData { public float Value; }`
3. A Shader Graph vertex shader samples the VAT texture at `(vertexID, _AnimTime)` to deform vertices on the GPU.
4. **Performance**: Zero CPU bone calculation, zero skin matrix buffer updates, **100,000+ fully animated unique characters running at 120 FPS**.

---

## 4. Animation Blending & Physics-to-Animation Transitions

Because ECS transforms are standard value components:
- **Ragdoll Blend**: Lerp between simulated `PhysicsVelocity`/`PhysicsMass` rigid body transforms and animated keyframe transforms in a single system.
- **Root Motion**: Read the root bone's delta position during the animation update and apply it directly to `PhysicsVelocity.Linear` in `AfterPhysicsSystemGroup`.
