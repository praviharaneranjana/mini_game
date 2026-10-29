# Unity Version Control (UVCS): cm CLI Reference

The **`cm`** command-line interface is the foundational engine behind Unity Version Control (formerly Plastic SCM). It powers both official desktop applications (`uvcs-desktop-client`) and the Unity Editor integration package (`com.unity.collab-proxy`).

---

## 1. Workspace Management

```bash
# List all workspaces on this computer
cm workspace list
# Alias:
cm lswk

# Create a new workspace connected to a repository
cm workspace create my_game_wk /path/to/local/folder rep:MyGameRepo@my_org@cloud
# Alias:
cm mkwk my_game_wk . rep:MyGameRepo@my_org@cloud

# Delete workspace configuration (preserves local files on disk)
cm workspace delete my_game_wk
# Alias:
cm rmwk my_game_wk
```

---

## 2. Status, Inspection & Diff

```bash
# View pending changes in the current workspace (Added, Changed, Deleted, Private)
cm status

# Compact short status output
cm status --short

# Output status in machine-readable JSON format (ideal for scripts/tools)
cm status --format=json

# Show diff of a specific file against its repository base
cm diff Assets/Scripts/PlayerController.cs

# View history / revision log of a file
cm history Assets/Scripts/PlayerController.cs

# Annotate/blame file lines by author and changeset
cm annotate Assets/Scripts/PlayerController.cs
```

---

## 3. Check-in, Update & Undo

```bash
# Update workspace to latest changeset on current branch
cm update
# Alias:
cm up

# Check in all pending changes with a commit comment
cm checkin -c="Implement player jump and coyote time logic"
# Alias:
cm ci -c="Implement player jump and coyote time logic"

# Check in specific files only
cm checkin Assets/Scripts/Player.cs Assets/Scripts/Player.cs.meta -c="Update player script"

# Undo local uncommitted changes to a file (revert to last changeset)
cm undo Assets/Scripts/PlayerController.cs

# Undo all pending local changes in the workspace
cm undo --all
```

---

## 4. Branching & Switching

UVCS encourages a **Task-Branching Pattern**, where every feature, bugfix, or asset pass is created on a dedicated child branch:

```bash
# List all branches in the repository
cm branch list
# Alias:
cm lsb

# Create a new branch originating from current branch/changeset
cm branch create br:/main/task-player-combat -c="Combat system development"
# Alias:
cm mkb br:/main/task-player-combat

# Switch workspace to target branch (updates local files)
cm switch br:/main/task-player-combat

# Switch branch while preserving uncommitted local changes
cm switch br:/main/task-player-combat --applychanges
```

---

## 5. Merging & Integration

```bash
# Step 1: Ensure workspace is on destination branch (e.g. /main)
cm switch br:/main

# Step 2: Merge feature branch into current branch
cm merge br:/main/task-player-combat

# Preview merge result without modifying workspace files
cm merge br:/main/task-player-combat --preview

# Step 3: Check in the completed merge changeset
cm checkin -c="Merge task-player-combat into main"
```

---

## 6. Exclusive Locking (Binary File Protection)

To prevent multiple team members from creating unmergeable conflicts on scenes, prefabs, or textures:

```bash
# Lock a file exclusively
cm lock Assets/Scenes/MainLevel.unity -c="Editing lighting and level layout"

# List all currently active locks in the repository
cm lock list

# Release/unlock a file
cm unlock Assets/Scenes/MainLevel.unity
```

---

## 7. Shelving (Temporary Work Staging)

Shelving saves uncommitted changes to the server without creating a formal changeset (similar to git stash, but securely backed up to the cloud):

```bash
# Shelve all current uncommitted changes
cm shelve -c="WIP boss attack animation pass"

# List all shelves on the server
cm shelve list

# Apply/restore a shelf to the current workspace
cm shelve apply sh:12

# Delete a shelf after use
cm shelve delete sh:12
```

---

## 8. CLI Automation & Scripting Flags

When invoking `cm` from automated build scripts, CI/CD runners, or Antigravity tools:
- `--machinereadable`: Eliminates ASCII art, formatting bars, and progress animations.
- `--format=json`: Generates standard JSON strings for robust programmatic parsing.
- `--noninteractive`: Prevents the CLI from pausing for terminal prompts (e.g., credentials or merge questions).
