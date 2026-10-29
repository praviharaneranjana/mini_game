# UVCS Ignore & Lock Configuration Guide

Properly configuring **`ignore.conf`** and **`lock.conf`** is essential for maintaining clean, high-performance Unity repositories and eliminating merge conflicts on unmergeable binary assets.

---

## 1. Production `ignore.conf` for Unity

The `ignore.conf` file resides at the root of the UVCS workspace (analogous to `.gitignore`). It prevents temporary, machine-generated cache files from polluting the repository.

### Recommended `ignore.conf`:
```
# =========================================================================
# Unity Cache & Temporary Directories (NEVER check these in!)
# =========================================================================
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/
/[Rr]ecordings/

# =========================================================================
# Asset Store Packages & Cache
# =========================================================================
/Assets/AssetStoreTools*

# =========================================================================
# IDE & Operating System Files
# =========================================================================
.vs/
.idea/
.gradle/
*.csproj
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.opendb
*.VC.db

# OS specific
.DS_Store
Thumbs.db
desktop.ini

# =========================================================================
# CRITICAL RULE: NEVER IGNORE .meta FILES!
# =========================================================================
# Unity relies on .meta files for asset GUIDs. Excluding .meta files will
# completely destroy scene object and material links across teammates!
```

---

## 2. Production `lock.conf` (Exclusive Binary Checkout)

Unmergeable files (scenes, prefabs, 3D models, textures, audio) cannot be cleanly resolved via 3-way text merges. If two developers modify a `.unity` scene simultaneously, merging them often leads to corrupted hierarchies.

UVCS solves this via **Exclusive Checkout (Locking)**. When configured, opening or editing a listed file automatically acquires an exclusive lock on the server. Other teammates are notified that the file is locked and cannot check in changes to it until the lock is released.

### Recommended `lock.conf`:
Place `lock.conf` at the workspace root or configure it in the UVCS Admin Console:

```
# =========================================================================
# Unity Scene & Asset Prefabs (Highest conflict risk)
# =========================================================================
*.unity
*.prefab
*.mat
*.physicMaterial
*.physicsMaterial2D
*.terrainlayer
*.mixer

# =========================================================================
# 3D Models & DCC Source Files
# =========================================================================
*.fbx
*.obj
*.blend
*.max
*.ma
*.mb

# =========================================================================
# Textures & Artwork
# =========================================================================
*.psd
*.png
*.tga
*.jpg
*.jpeg
*.exr
*.hdr
*.tif
*.tiff

# =========================================================================
# Audio & Video Media
# =========================================================================
*.wav
*.mp3
*.ogg
*.aif
*.aiff
*.mp4
*.mov
```

### How Exclusive Checkout Works in Practice:
1. **Developer A** opens `MainLevel.unity` in the Unity Editor or checks it out.
2. The UVCS server grants an exclusive lock to Developer A.
3. **Developer B** attempts to modify `MainLevel.unity`. The Unity Editor displays a locked indicator (padlock icon) showing "Locked by Developer A".
4. Developer B cannot check in changes to `MainLevel.unity` until Developer A checks in their work and releases the lock.
5. **Zero merge conflicts on scenes and binary assets!**

---

## 3. The `.plastic` Folder & Workspace Config

At the root of every UVCS workspace, a hidden directory called `.plastic` stores workspace metadata:
- **`plastic.workspace`**: Identifies the workspace GUID and mapped repository.
- **`plastic.selector`**: Defines the active branch and view rules (e.g. `repository "MyRepo@cloud" path "/" smartbranch "/main"`).

> [!WARNING]
> Never manually delete or edit files inside `.plastic` unless directed by official troubleshooting workflows, as it can de-synchronize local workspace tracking from the server.
