# URP Integration, Lighting & Stencil Rendering

Integrating **Unity Toon Shader (UTS3)** into the **Universal Render Pipeline (URP 14+ / Unity 6 URP 17+)** requires deliberate configuration of lighting passes, shadow cascades, post-processing tonemappers, and stencil buffers.

---

## 1. URP Lighting & Forward+ Configuration

UTS3 fully supports URP's **Forward** and **Forward+** rendering paths:
* **Forward+**: Recommended for scenes with multiple dynamic lights (torches, streetlamps, magic spells). UTS3 supports clustering and point/spot lights seamlessly under Forward+.
* **Light Layers**: Use URP Light Layers to isolate character lighting from background environment lights. Characters can be illuminated by a dedicated soft key-light without overexposing the surrounding environment.

### Shadow Cascade Optimization for Cel-Shading
In PBR rendering, soft shadow filtering masks low-resolution cascades. In cel-shading, however, low shadow resolution creates jagged, pixelated stair-stepping along face and chin shadow lines:
- **URP Asset Setting**: Set **Shadow Resolution** to High/Very High (2048 or 4096) for main directional lights.
- **Cascade Count**: Use **4 Cascades** with tight cascade split distances (e.g. 2m, 6m, 15m, 40m) to maximize shadow map texel density across character faces during close-up camera angles.

---

## 2. Tonemapping Compatibility: ACES vs Neutral

A common blunder when using UTS3 in modern URP is applying default cinematic post-processing:

```
[HDR Scene Buffer] ---> ACES Tonemapping ---> Darkened, Desaturated, Muddy Anime Colors!
[HDR Scene Buffer] ---> Neutral Tonemapping -> Clean, Vibrant, Intended 2D Pastel Swatches!
```

### The Rule for Anime Visuals:
- **Avoid ACES Tonemapping**: ACES applies a heavy filmic S-curve roll-off designed for photographic PBR live-action film. It crushes delicate shadow pastels (like warm peach skin or pale blonde hair) into murky grays.
- **Use Neutral Tonemapping**:
  ```csharp
  // In your URP Volume Profile:
  profile.TryGet<Tonemapping>(out var tonemapping);
  tonemapping.mode.value = TonemappingMode.Neutral;
  ```
- **Pair with Subtle Bloom**: Set `Bloom.threshold = 0.9` and `intensity = 0.3` to give high-contrast anime characters a soft magical glow without blowing out cel lines.

---

## 3. The Anime Eye-Through-Hair Stencil Technique

In classic 2D anime, characters' eyes and eyebrows are visibly drawn *through* their overlapping front bangs so their facial expressions are never obscured:

```
      ( ( ( Bangs ) ) )
       \  \  |  /  /
        [ Eye Visible Through Hair! ]
```

UTS3 supports native hardware stencil operations (`_StencilMode`) to achieve this effect with zero performance overhead:

### Stencil Setup:
1. **Eyes & Eyebrows Material**:
   - Set **`_StencilMode` = `StencilMask` (Mode 2)**.
   - Set `_StencilNo = 1`.
   - `_StencilComp = 8` (`Always`).
   - `_StencilOpPass = 2` (`Replace`).
   - **Result**: Eyes write value `1` into the hardware stencil buffer.
2. **Hair Bangs Material**:
   - Set **`_StencilMode` = `StencilOut` (Mode 1)**.
   - Set `_StencilNo = 1`.
   - `_StencilComp = 6` (`NotEqual`).
   - **Result**: Hair is only rendered where stencil is NOT `1`. The eyes and eyebrows show through the hair cleanly!

---

## 4. Exposure & EV Compensation (`ModelToonEvAdjustment`)

When placing anime characters into HDR environments with physically based skyboxes and sun intensities ($10,000+\text{ lux}$), standard materials blow out to blinding white.

UTS3 includes the **`ModelToonEvAdjustment`** component (`Runtime/ModelToonEvAdjustment.cs`):
- Attach `ModelToonEvAdjustment` to the root GameObject of your character.
- Automatically calculates and applies an exposure compensation multiplier to all character materials.
- Ensures the character remains perfectly exposed and color-balanced regardless of whether the environment is a dark nighttime alley or blinding midday sunlight.
