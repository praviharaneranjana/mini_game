# Baking & Authoring Graphics in DOTS

Subscenes and Bakers convert artist-authored GameObjects and scenes into optimized, unmanaged ECS entities during Editor edit-mode or build time.

---

## 1. Automatic Baking of Standard Renderers

When a GameObject resides within a Subscene:

### Static `MeshRenderer` + `MeshFilter`:
- The baker extracts mesh and material assets into a `RenderMeshArray`.
- Produces an entity with:
  - `MaterialMeshInfo`
  - `RenderBounds`
  - `LocalToWorld` (and `LocalTransform` if dynamic)

### `SkinnedMeshRenderer`:
- The baker detects the skeleton and mesh bones.
- Produces:
  - A mesh entity with `DynamicBuffer<SkinMatrix>`, `DynamicBuffer<BindPose>`, and `DynamicBuffer<SkinBoneEntity>`.
  - Bone entities corresponding to each transform bone in the rig hierarchy.
  - Automatically hooks up `DeformationMode` based on the assigned Material / Shader Graph.

---

## 2. Optimizing Transforms with `TransformUsageFlags`

When writing custom Bakers, always choose the most restrictive `TransformUsageFlags` to minimize chunk memory:

```csharp
class MyVisualBaker : Baker<MyVisualAuthoring>
{
    public override void Bake(MyVisualAuthoring authoring)
    {
        // Options:
        // 1. None: Entity has NO transform components (pure data entity)
        // 2. Renderable: Kept for static rendering (only LocalToWorld)
        // 3. Dynamic: Fully movable at runtime (LocalTransform + LocalToWorld)
        // 4. WorldSpace: Ignores parent hierarchy, optimizes root position
        
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        
        // Add custom component data
        AddComponent(entity, new MyVisualTag());
    }
}
```

---

## 3. Baking Dependencies (`DependsOn`)

To ensure entities re-bake automatically whenever source assets (materials, textures, or configs) are modified in the Editor:

```csharp
class CharacterVisualBaker : Baker<CharacterVisualAuthoring>
{
    public override void Bake(CharacterVisualAuthoring authoring)
    {
        // Declare dependency on the referenced material
        DependsOn(authoring.CustomMaterial);

        var entity = GetEntity(TransformUsageFlags.Renderable);
        // ...
    }
}
```
