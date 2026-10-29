---
trigger: "When checking in code, managing version control, modifying .meta files, creating branches, or configuring UVCS / Git."
---

# Unity Version Control & Asset Discipline Rules

## 1. Strict Meta File Integrity
- **Always Commit Assets and `.meta` Files Together**: Every asset in `Assets/` has an associated `.meta` file holding its unique GUID. Never check in, move, or rename an asset without including its `.meta` file in the exact same commit or changeset.
- **Never Manually Regenerate GUIDs**: Deleting and regenerating `.meta` files destroys all serialized references to that asset across scenes and prefabs, causing missing references and pink shaders.
- **Match Deletions**: Deleting an asset requires deleting its `.meta` file immediately.

## 2. Ignore Configuration Discipline
- **Mandatory Exclusions**: Ensure `ignore.conf` (or `.gitignore`) excludes machine-generated, local cache folders:
  - `/[Ll]ibrary/`
  - `/[Tt]emp/`
  - `/[Oo]bj/`
  - `/[Bb]uild/` and `/[Bb]uilds/`
  - `/[Uu]ser[Ss]ettings/`
  - `*.csproj` and `*.sln`
- **Never Ignore `.meta` Files**: Ensure ignore rules never match `*.meta`.

## 3. Binary Asset Protection & Exclusive Locking
- **Lock Unmergeable Assets**: In UVCS, configure `lock.conf` for all binary and complex asset formats (`.unity`, `.prefab`, `.mat`, `.fbx`, `.psd`, `.png`, `.wav`) to prevent unmergeable conflicts.
- **Never Attempt Blind Scene Merges**: If two developers modify the same `.unity` scene, never attempt a blind manual text merge. Revert one, export changes as a prefab or package, and re-integrate sequentially.

## 4. Atomic Check-in Standards
- **Descriptive Changeset Comments**: Always provide clear, descriptive commit/changeset comments describing *why* and *what* changed.
- **Task-Branching Pattern**: Isolate gameplay features, bugs, or asset overhauls on dedicated task branches (`br:/main/task-player-mechanics`) rather than committing directly to `/main`.
