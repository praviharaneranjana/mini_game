# 2D Game Development, Tilemaps & Pixel Art Guidelines

This rule establishes technical standards for 2D Tilemaps (`create-tile-palette-or-rule-tiles`), Pixel Perfection (`2d-pixel-perfect`), Sprite Atlases (`manage-sprite-atlas`), and 2D Physics (`create-2d-physics`).

## 1. Pixel Perfection & Resolution Calibration
- **Pixels Per Unit (PPU)**: Standardize all project sprites to a uniform PPU (e.g. 16, 32, or 64). Never mix different PPUs within the same visual layer.
- **PixelPerfectCamera**:
  - In URP, attach `PixelPerfectCamera` to the main rendering camera.
  - Set `Assets Pixels Per Unit` matching your sprite PPU.
  - Set `Reference Resolution` to target retro display (e.g. 320x180, 640x360).
  - Enable `Upscale Render Texture` for crisp pixel snap, and `Crop Frame (X/Y)` to eliminate subpixel shimmering during camera pan.

## 2. Tilemaps & Rule Tiles
- **Tile Palette Organization**: Organize Tile Palettes by biome/theme (e.g. `Grassland`, `Dungeon`, `Industrial`).
- **Rule Tiles**:
  - Use 8-neighbor Rule Tiles for autotiling terrain, platforms, and water edges.
  - Set Rule Tile collider type to `Grid` or `Sprite` depending on collision requirements.
- **Tilemap Colliders**:
  - Always attach `CompositeCollider2D` alongside `TilemapCollider2D` and check `Used By Composite` to merge adjacent tile colliders into a single polygon mesh, eliminating seam catching bugs.

## 3. Sprite Atlasing & Memory
- **SpriteAtlas Management**: Group sprites by scene and visual tier (e.g. `HUD_Atlas`, `Level1_Atlas`).
- **Tight Packing**: Enable `Allow Rotation` and set packing padding to at least `4` pixels to avoid texture bleeding between atlas sprites at mipmap transitions.

## 4. 2D Physics Integration
- Always use `Rigidbody2D` and `Collider2D` components; never mix 2D colliders with 3D PhysX `Rigidbody`.
- Set `Collision Detection` to `Continuous` on fast-moving projectiles or bullets to prevent tunneling through thin platform colliders.
