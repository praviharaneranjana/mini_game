---
trigger: "When authoring, modifying, or configuring toon shaders, anime materials, cel-shading, outlines, or stylized lighting in Unity."
---

# Anime & Cel-Shading Visual Rules (Unity Toon Shader)

## 1. Palette & Shading Standards
- **Three-Color Model Discipline**: Always define distinct swatches for **Base Color**, **1st Shade Color**, and **2nd Shade Color**. Never use pure black or desaturated dark grays for skin or hair shadows; use warm, saturated tones (e.g. peach/apricot for skin, rich indigo/violet for blue hair).
- **Step & Feather Tuning**: Set `_BaseShade_Feather` to `0.0001–0.001` for crisp 2D anime cel lines, or `0.05–0.15` for soft illustrated styling.
- **Position Maps for Facial Features**: Use `1st Shading Position Map` and `2nd Shading Position Map` to lock in anatomical shadows under bangs, nose, and chin so facial expressions do not flatten when lit from the front.

## 2. Inverted-Hull Outlines
- **Outline Mode**: Prefer `Normal Direction` over `Position Scaling` on character meshes to maintain uniform line thickness.
- **Color Blending**: Always enable `Blend Base Color to Outline` (0.3–0.6) so outlines harmonize with local garment and skin colors rather than appearing as stark black wireframes.
- **Masking Facial Features**: Assign an `Outline Width Map` with pure black UV pixels over eyes and inner lips to eliminate distracting outline clutter on the face.
- **Z-Axis Depth Offset**: Tune `Offset Outline with Camera Z-axis` (`_Offset_Z`) if outline backfaces pierce through fingers, nose, or chin geometry.

## 3. Post-Processing & Lighting Integration
- **Mandatory Neutral Tonemapping**: Always set `Tonemapping.mode = TonemappingMode.Neutral` in URP Volume profiles when using toon shaders. Never use ACES tonemapping for anime games, as its filmic S-curve darkens and desaturates delicate pastel swatches.
- **Shadow Cascade Resolution**: Configure URP directional shadows with 4 cascades and close-range split distances (e.g. 2m, 6m) to prevent jagged shadow stair-stepping across character faces.
- **Stencil Layering**: Use `StencilMode.StencilMask` on eyes/eyebrows and `StencilMode.StencilOut` on front bangs to allow facial expressions to be viewed cleanly through overlapping hair.
