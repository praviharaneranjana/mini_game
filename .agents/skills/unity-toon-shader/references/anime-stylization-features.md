# Anime Stylization Features: Angel Ring, MatCap & Rim Lighting

Unity Toon Shader includes specialized visual components engineered to reproduce iconic Japanese 2D animation tropes directly in 3D real-time rendering.

---

## 1. Angel Ring Hair Projection

The **Angel Ring** (天使の輪, *Tenshi no wa*) is the characteristic horizontal halo highlight that crowns anime hair strands.

In 2D hand-drawn animation, this highlight remains relatively fixed to the character's head while subtly shifting with camera perspective:

```
      (   (   )   )
    (  ==========  )  <--- Horizontal Angel Ring Highlight
   (  |   o   o  |  )
      |     ^    |
```

### 1.1 How It Works in UTS3
* Rather than requiring complex anisotropic hair specular maps, UTS projects a specialized texture (`_AngelRing_Sampler`) along the hair mesh using screen or cylindrical coordinates.
* **Offset U / V (`_AR_OffsetU`, `_AR_OffsetV`)**: Adjusts the vertical and horizontal position of the ring on the character's scalp.
* **Alpha Clipping (`_Use_AR_Alpha`)**: Allows using the alpha channel of the hair texture to ensure the highlight only appears on outer strands and does not bleed into inner skull shadows.
* **Camera Responsiveness**: As the camera rotates around the character, the Angel Ring smoothly glides across hair geometry, maintaining an authentic anime sheen.

---

## 2. MatCap (Material Capture / Sphere Mapping)

**MatCap** projects a pre-rendered spherical texture directly onto surface normals in view space. It allows artists to achieve complex metallic, latex, silk, or glossy reflections with **zero runtime lighting or raytracing cost**.

### 2.1 Common Anime Use Cases
1. **Metallic Armor & Weapons**: Gold, silver, chrome, or polished brass sphere textures.
2. **Sci-Fi Bodysuits & Latex**: High-gloss specular rims with tinted reflections.
3. **Anime Eyes (Gems & Highlights)**: Multi-colored internal glints that stay vibrant regardless of scene lighting.

### 2.2 Key MatCap Controls in UTS3
* **MatCap Map (`_MatCap_Sampler`)**: A square texture containing a rendered sphere.
* **Color Blending Mode (`_BlendMode`)**:
  * `Multiply`: Darkens surfaces with metallic reflections.
  * `Add` / `Screen`: Produces glowing, luminous highlights.
* **Scale & Rotate MatCap UV (`_Scale_MatCapUV`, `_Rotate_MatCapUV`)**: Scales or tilts the reflection reflection vector.
* **Camera Roll Stabilization (`_CameraRolling_Stabilizer`)**: Prevents the reflection from unnaturally spinning when the player tilts or rolls the camera.
* **MatCap Mask (`_Set_MatCapMask`)**: Restricts the reflection to specific UV areas (e.g. metal belt buckles and shoulder pauldrons while leaving cloth untouched).

---

## 3. Rim Lighting & Edge Illumination

Rim lighting highlights the edges of a character's silhouette, separating them visually from dark or noisy background environments.

```
       Light Source (Behind)
                |
                v
       +-----------------+
  ==== | Character Mesh  | ====  <--- Bright Rim Highlights
       +-----------------+
                ^
                |
          Camera View
```

### 3.1 Key Rim Light Parameters
* **Rim Light Color (`_RimLightColor`)**: Typically a bright complementary color (cool cyan or pale gold) or tinted to match the ambient sky.
* **Rim Light Power (`_RimLight_Power`)**: Controls the sharpness and falloff of the rim along the outer silhouette edges.
* **Adjust Rim Light Area (`_RimLight_Area`)**: Controls how far the edge glow wraps around the front of the mesh.
* **Inverted Light Direction Rim Light (`_Invert_RimLight`)**: Creates dramatic rim lighting even when the light source is in front of the character, ensuring silhouettes remain clear in any scene lighting.
* **Rim Light Mask (`_Set_RimLightMask`)**: Prevents rim lighting on areas that should remain unlit (e.g. inside nostrils or armpits).

---

## 4. Dynamic Emissive Animation

For magical runes, glowing mecha visors, cybernetic conduits, or pulsating magical weapons, UTS includes a built-in procedural emission animator:
* **Scroll U / V Direction (`_Base_Speed_U`, `_Base_Speed_V`)**: Scrolls the emission texture across UV coordinates to simulate flowing energy.
* **Ping-Pong Movement (`_Pingpong_U`, `_Pingpong_V`)**: Creates rhythmic breathing or pulsating glow patterns without writing custom C# scripts.
* **Color Shifting with Time (`_ColorShift_Speed`)**: Dynamically shifts emission colors across the RGB spectrum over time.
* **Color Shifting with View Angle (`_ColorShift_ViewAngle`)**: Produces iridescent holographic effects that change color depending on camera angle.

---

## 5. Scene Light vs Artist Palette Discipline

A pervasive problem when placing anime characters into realistic 3D scenes is that intense white sunlight or colorful ambient point lights overpower the character's carefully crafted hand-drawn colors.

UTS provides **Scene Light Effectiveness Settings (`_Unlit_Intensity`)**:
* **High Influence (`1.0`)**: Character reacts like standard geometry to scene lights.
* **Low / Controlled Influence (`0.0` - `0.3`)**: Character preserves their authentic 2D anime color design, remaining legible and vibrant in both dim dungeons and blinding desert sunlight.
