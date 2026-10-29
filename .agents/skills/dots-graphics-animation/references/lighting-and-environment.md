# Lighting & Environment in Entities Graphics

Entities Graphics seamlessly integrates with Unity's lighting pipelines (URP and HDRP), including baked lightmaps, real-time directional and local shadows, light probes, and reflection probes.

---

## 1. Light Probes for Dynamic Entities

Dynamic moving entities (such as characters, vehicles, and flying projectiles) cannot use static lightmaps. Instead, they sample the world's **Light Probe** tetrahedral mesh.

### How Entities Graphics Samples Probes:
- In GameObject rendering, each `MeshRenderer` calculates Spherical Harmonics (SH) coefficients on the CPU.
- In **Entities Graphics**, probe sampling is calculated across entities using vectorized SIMD Burst jobs.
- The interpolated ambient probe coefficients (L0, L1, L2 spherical harmonics) are uploaded directly into GPU ByteAddressBuffers for the batch.

### Baked Light Probe Components:
- When an entity with a `MeshRenderer` is baked from a subscene, the baker adds:
  - `AmbientProbeOverride`: Stores ambient SH lighting per entity.
  - `LightProbeUsage`: Specifies probe interpolation mode (`BlendProbes` vs `Off`).

---

## 2. Lightmaps on Static Entities

Static geometry placed in subscenes can be fully baked using the Unity Progressive Lightmapper:

1. Mark GameObjects in your subscene as **Contribute GI**.
2. Run standard scene light baking (**Window > Rendering > Lighting > Generate Lighting**).
3. **Baking output into DOTS**:
   - The baker attaches a `LightmapInfo` component storing:
     - `LightmapIndex`: Target lightmap texture index.
     - `LightmapST`: Scale and offset ($x, y, z, w$) into the lightmap atlas.
4. **Result**: Static entities render with full global illumination, lightmaps, and directional ambient occlusion while benefiting from the BatchRendererGroup.

---

## 3. Shadows: Casting & Receiving

Shadow flags are baked into the entity's `RenderFilterSettings` or material flags:

- **Shadow Casting Modes**:
  - `On`: Casts dynamic shadows into shadow maps.
  - `Off`: Does not render during the shadow caster pass (saves GPU vertex processing).
  - `ShadowsOnly`: Invisible in primary camera pass, renders exclusively into shadow map.
- **Shadow Receiving**:
  - Toggled per material/shader or via render filter.
- **Performance Tip**: For massive crowds (e.g., 20,000 units), set distant LODs to `ShadowCastingMode = Off` to cut shadow pass draw calls in half.

---

## 4. Reflection Probes

- Entities automatically sample the nearest active `ReflectionProbe` in world space.
- Box projection and probe blending are evaluated in the fragment shader per instance, matching standard URP/HDRP GameObject visual fidelity.
