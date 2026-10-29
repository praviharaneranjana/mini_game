# Unity Agent Plugin and Skill Guidelines

This project integrates the official **Unity Agent Plugin** (`unity-agent-plugin`), providing automated, domain-specific coding and configuration workflows for Unity 6 and URP projects.

## Architecture and Discovery
- Plugins reside in `.agents/plugins/<plugin_name>/` and are registered in [plugins.json](file:///d:/Unity%20Projects/mini_game/.agents/plugins.json).
- Each plugin contains a root `plugin.json` manifest declaring metadata, keywords, and exposed skills.
- Custom skills are located in `.agents/skills/<skill_name>/` and inside `.agents/plugins/unity-agent-plugin/skills/`.
- Progressive disclosure is maintained: skill descriptions are indexed; full procedure files (`SKILL.md`, references, and scripts) are loaded on-demand when activated.

## Skill Domain Mapping

When handling project tasks, prefer consulting the appropriate specialized skill:

| Domain | Recommended Skill | Trigger Scenarios |
| :--- | :--- | :--- |
| **Graphics & Shaders** | `urp-postprocessing`, `validate-urp-render-graph-renderer-feature`, `shader-graph-create-custom-node`, `dots-graphics-animation`, `unity-toon-shader` | Volume overrides, ScriptableRendererFeatures, RenderGraph migration, HLSL nodes, cel-shading. |
| **Physics** | `physics-3d-collision`, `create-2d-physics`, `physics-dots` | PhysX collision debugging, Rigidbody2D/Physics2D setup, Unity.Physics DOTS simulation. |
| **User Interface** | `ui`, `ui-uitk`, `ui-ugui`, `ui-imgui` | UI framework selection, Flexbox/USS styling, Canvas/RectTransform layouts, custom Editor windows. |
| **2D & Tilemaps** | `create-tile-palette-or-rule-tiles`, `2d-pixel-perfect`, `manage-sprite-atlas`, `sprite-editor`, `sprite-segment-3x3grid` | Rule tiles, pixel perfection, sprite slicing, atlas packing. |
| **Networking & FPS** | `netcode-gameobjects`, `setup-multiplayer-services`, `multiplayer-fps-architecture`, `setup-vivox-voice-chat` | NGO state sync, RPCs, client prediction, matchmaking, lobbies, Vivox voice. |
| **Monetization & Live Ops** | `implement-in-app-purchases`, `levelplay-unity-integration`, `build-live-game`, `mobile-notifications` | IAP catalog, receipt validation, LevelPlay ad mediation, remote config, notifications. |
| **Editor & Pipeline** | `unity-cli`, `unity-package-management`, `build-gtk`, `generate-editor-search-query`, `unity-engine-internals` | CLI automation, UPM package resolution, Graph Toolkit editors, native lifecycle traps. |
| **Optimization** | `optimize-audio`, `optimize-text-mesh-pro`, `optimize-web` | Audio compression/mixing, TMP atlas multi-font stacks, WebGL/WebGPU build shrinking. |

## Plugin Extension Guidelines
1. **Preserve Frontmatter**: All `SKILL.md` files must include YAML frontmatter with `name` and `description` explaining explicit triggers.
2. **References Over Duplication**: Store extended API references, architecture blueprints, and code samples under `references/` within the skill folder.
3. **No Breaking Modifications**: Do not overwrite upstream plugin core manifests without verifying compatibility with Antigravity and Claude Code specifications.
