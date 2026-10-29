# Cel-Shading & The Three-Color Pipeline

The core mechanism of **Unity Toon Shader (UTS3)** is the **Three-Color Shading Model**. Unlike Physically Based Rendering (PBR)—which calculates shadow colors by multiplying ambient light and surface normals—UTS allows artists to specify exact RGB palettes for unshaded, partially shaded, and deeply shaded regions.

---

## 1. The Three Basic Colors

In 2D anime and manga production, color designers specify distinct swatches for every surface:

```
[Lit / Highlight Area]   <--- Base Map & Base Color
        |
        v  (Base Color Step & Feather Threshold)
[1st Shadow Region]      <--- 1st Shading Map & 1st Shading Color
        |
        v  (Shading Color Step & Feather Threshold)
[2nd Core Shadow Region] <--- 2nd Shading Map & 2nd Shading Color
```

### 1.1 Base Map & Base Color
* **Shader Properties**: `_MainTex` (Texture) and `_BaseColor` (Color).
* **Role**: The primary unshaded color of the character. When light strikes the mesh directly, this texture and color are rendered.

### 1.2 1st Shading Map & Color
* **Shader Properties**: `_1st_ShadeMap` and `_1st_ShadeColor`.
* **Role**: The lighter shadow area. 
* **Workflow Option**: Enabling `Apply to 1st Shading Map` (`_Use_BaseAs1st`) reuses the Base Map texture and multiplies it by `_1st_ShadeColor`, saving texture memory and draw call complexity.
* **Tuning**: For anime skin, never use muddy brown or black. Use a warm peach, light apricot, or soft rose hue.

### 1.3 2nd Shading Map & Color
* **Shader Properties**: `_2nd_ShadeMap` and `_2nd_ShadeColor`.
* **Role**: The deep core shadow in occluded areas (creases, under the chin, inner ears, deep hair layers).
* **Workflow Option**: Enabling `Apply to 2nd Shading Map` (`_Use_1stAs2nd`) reuses the 1st Shade Map texture multiplied by `_2nd_ShadeColor`.

---

## 2. Step & Feather Mathematics

The transition between colors is determined by calculating the dot product of surface normal and light direction ($\mathbf{N} \cdot \mathbf{L}$), evaluated against numerical step and feather thresholds:

```
Light Intensity: 0.0 (Back) ---------------------> 1.0 (Front)
Color Bands:    [2nd Shade]  |  [1st Shade]  |  [Base Color]
                             ^               ^
                    Shading Color Step   Base Color Step
```

### 2.1 Base Color Step (`_BaseColor_Step`)
* **Range**: `0.0` to `1.0` (Default: `0.5`).
* **Function**: Controls how much of the mesh remains in the Base Color vs 1st Shade Color.
* Decreasing this value expands the lit Base Color area; increasing it expands the shadow area.

### 2.2 Base Shading Feather (`_BaseShade_Feather`)
* **Range**: `0.0001` to `1.0`.
* **Sharp Anime Cel Style**: Set to `0.0001` - `0.001`. Creates a crisp, razor-sharp boundary line typical of traditional Japanese 2D cel animation.
* **Soft Illustrated Style**: Set to `0.05` - `0.15`. Blurs the step into a subtle gradient, resembling hand-painted light novel illustrations or Ghibli backgrounds.

### 2.3 Shading Color Step (`_ShadeColor_Step`) & Feather (`_1st2nd_Shades_Feather`)
* Controls the boundary and softness between the 1st Shade Color and the 2nd Shade Color.

---

## 3. Fixed Shadow Control Maps

In anime character modeling, certain shadows must **always** be present regardless of lighting angles (e.g. shadow under the bangs, nose shadow, nostrils, eyelid creases, ear cavities).

UTS provides two dedicated linear control maps:
* **`1st Shading Position Map` (`_Set_1st_ShadePosition`)**: White pixels in this UV map are permanently forced into the 1st Shade Color, regardless of scene light direction.
* **`2nd Shading Position Map` (`_Set_2nd_ShadePosition`)**: White pixels are permanently forced into the 2nd Shade Color.

### Benefits of Position Maps:
1. Prevents the "flat face" look when directional lights shine straight into a character's face.
2. Eliminates the need to model complex sub-geometry for facial shadows.
3. Preserves hand-drawn anime facial aesthetics under dynamic 360° rotating cameras.

---

## 4. Production Recipe: Anime Character Skin

| Property | Value | Rationale |
|---|---|---|
| **Base Color** | `RGB(255, 235, 225)` | Bright porcelain anime skin tone |
| **1st Shade Color** | `RGB(255, 195, 175)` | Warm peach tone; avoids dirty gray shadows |
| **2nd Shade Color** | `RGB(230, 155, 140)` | Saturated soft coral core shadow |
| **Base Color Step** | `0.52` | Balanced front-face illumination |
| **Base Shading Feather** | `0.001` | Razor-sharp cel line |
| **Shading Color Step** | `0.25` | Deep shadow only in true occlusions |
| **Shading Feather** | `0.005` | Clean transition |
| **Position Maps** | Attached for nose & neck | Permanent neck and facial crease definitions |
