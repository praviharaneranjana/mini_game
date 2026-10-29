# UVCS (Plastic SCM) vs Git + Git LFS for Game Development

Understanding the fundamental trade-offs between **Unity Version Control (UVCS / Plastic SCM)** and **Git + Git LFS** is critical when choosing or managing game studio version control pipelines.

---

## 1. High-Level Comparison Matrix

| Capability | Unity Version Control (UVCS) | Git + Git LFS |
|---|---|---|
| **Primary Design Focus** | Game development (Code + Heavy Binaries) | Software development (Source code / Text) |
| **Large Binary Storage** | Native first-class binary storage and chunking | External LFS pointer server sidecar |
| **Partial Workspaces (Gluon)** | **Yes**: Artists can check out single folders/assets without downloading whole repo | **No**: Developers must clone the whole tree (sparse checkout is fragile) |
| **Exclusive File Locking** | **Native**: Automatic checkout locks on `.unity`, `.prefab`, `.psd` | Manual `git lfs lock`, prone to human error and race conditions |
| **Branching Performance** | Instantaneous: Metadata-only branches, fast across 500GB+ repos | Fast for code, can be very slow when switching branches with large LFS assets |
| **Visual History Graph** | **Branch Explorer**: GPU-accelerated interactive tree | Linear Git log or external visual tools (Fork, GitKraken, Sourcetree) |
| **Merge Engine** | Advanced 3-way merge with directory renames and moved code tracking | Standard 3-way diff (struggles with simultaneous rename + modify) |
| **Unity Editor Integration** | First-party (`com.unity.collab-proxy`), native in Project window | Third-party or basic Git plugins |

---

## 2. Deep-Dive: Why Game Studios Adopt UVCS

### 2.1 The Artist Problem: Gluon Mode
In AAA and mid-sized game studios, artists (3D modelers, concept artists, sound designers) often struggle with complex Git command lines, detached HEAD states, rebasing, and merge conflict resolution.
- **Gluon Mode in UVCS**: A simplified workspace mode designed specifically for non-programmers. Artists can:
  - Browse the repository tree and check out *only* the specific asset folders they work on (e.g. `/Assets/Art/Characters/`), saving hundreds of gigabytes of local disk space.
  - Automatically acquire exclusive locks on binary files upon editing.
  - Submit check-ins with a single "Checkin" button without dealing with branches, merges, or pull requests.

### 2.2 Exclusive Locking vs The Scene Merge Nightmare
In Git, two developers can edit `Scene_Main.unity` on separate branches simultaneously. When merging:
- Git tries to merge the multi-megabyte YAML text.
- Merge conflicts in internal Unity GameObject IDs (`fileID`, `guid`) often result in scene corruption, missing object references, or phantom prefabs.
- **UVCS Solution**: `lock.conf` enforces that only **one person** can edit `Scene_Main.unity` at a time. The first person to edit gets the lock; others are blocked from checking in until the lock is released.

### 2.3 Storage & Bandwidth Scalability
- **Git LFS**: Every modification to a 500MB `.psd` creates a separate 500MB blob on the LFS storage server. Bandwidth costs and clone times grow rapidly.
- **UVCS**: Uses content-addressable storage with native deduplication and delta compression for binary assets, significantly reducing cloud storage footprint.

---

## 3. When Git + Git LFS is Still Preferred
- **Open-Source & Multi-Engine Projects**: Broadest ecosystem compatibility, widespread familiarity among programmers, and integration with GitHub/GitLab CI pipelines.
- **Code-Heavy Projects with Minimal Binaries**: Small 2D games, tool packages, or libraries where binary assets account for less than a few gigabytes.
- **Existing Enterprise Tooling**: Studios with deep investment in GitHub Actions, GitLab CI, or proprietary Git infrastructure.
