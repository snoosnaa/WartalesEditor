# Roadmap

**Document Version:** 1.1
**Status:** Active
**Last Updated:** 2026-07-11
**Applies To:** Entire Project

---

# Project Roadmap

The roadmap is a living document and will evolve as the project grows. Priorities may change based on development experience, user feedback, and discoveries about Wartales' data structures.

---

# Mission

> **Empower players to easily customize Wartales to match the way they want to play, without requiring them to understand the game's internal data files.**

The editor should make creating, maintaining, **sharing** gameplay modifications simple, safe, approachable, and sustainable across future game updates.

---

# Current Status

**Application Version:** 0.2.0 — Find Anything

The editor currently supports:

- Opening the game's CDB.
- Browsing Categories, Settings, and Properties.
- Editing gameplay values.
- Saving modified CDB files.
- Reloading edited files.
- Searching every Category.
- Searching English display names.
- Searching internal IDs.
- Searching property names.
- Searching property values.
- Localization-aware searching.
- Direct navigation to search results.
- Successfully verifying gameplay changes inside Wartales.

The project has now transitioned from a functional editor into a powerful gameplay editing tool.

---

# Completed Milestones

## Foundation

Completed

- WPF application
- MVVM architecture
- Git repository
- GitHub integration
- Documentation framework
- JSON parsing

---

## Data Browser

Completed

- Three-pane interface
- Categories
- Settings
- Properties
- Search scopes
- Internal ID display

---

## Functional Editor

Completed

- Editable properties
- RootDocument synchronization
- Save support
- Reload verification
- First successful gameplay modification
- Pane headers
- Show Empty Categories

---

## Find Anything v1

Completed

- Global search across all Categories
- Search internal IDs
- Search English display names
- Search property names
- Search property values
- Search all fields simultaneously
- Find Anything panel
- Direct navigation to matching Categories and Settings
- Automatic property selection
- Localization-aware searching

---

# Current Priorities

## Goal 2 — Edit Safely

### Objective

Allow users to confidently modify game data while minimizing mistakes.

### Property Editing

- Type-aware editors.
- Numeric controls.
- Boolean checkboxes.
- Drop-down editors.
- Enum support.
- Property descriptions.
- Tooltips.
- Default values.
- Known value ranges.
- Reset property to original value.
- Read-only handling for unsupported complex objects.

### Validation

- Syntax validation.
- Save validation.
- Type validation.
- Schema validation.
- Missing-field detection.

### Visual Feedback

- Highlight modified values.
- Modified indicator.
- Change summaries.

---

## Goal 3 — Build Mods

### Objective

Allow users to create reusable collections of gameplay modifications.

### Mod Profiles

- Save Mod Profiles.
- Load Mod Profiles.
- Apply Mod Profiles.
- Rename Mod Profiles.
- Delete Mod Profiles.
- Organize Mod Profiles.
- Compare Mod Profiles.
- Preview Mod Profile contents.
- Share Mod Profiles.
- Apply Mod Profiles to updated game versions.

---

## Goal 4 — Streamline the Workflow

### Objective

Reduce repetitive work between editing and playing.

### File Workflow

- Save As.
- Save & Exit.
- Exit confirmation.
- Unsaved changes prompt.
- Backup on Save.
- Recent Files.
- Remember last opened project.
- Remember user preferences.

### Packaging

- QuickBMS extraction.
- QuickBMS packaging.
- Configure QuickBMS location.
- Verify required files exist.
- One-click Package.
- Long-term Package & Launch Wartales.

---

## Goal 5 — Advanced Editing

### Objective

Allow efficient modification and long-term maintenance of gameplay modifications.

### Batch Editing

- Set values.
- Add values.
- Multiply values.
- Divide values.
- Find & Replace.
- Preview batch operations.
- Apply changes to selected Categories.
- Apply changes to matching property types.

### Migration

- Compare CDB versions.
- Import previous edits.
- Conflict detection.
- Preview migration.
- Merge changes.

---

# Research

Topics requiring continued investigation.

- Unknown CDB structures.
- Property meanings.
- Hidden gameplay mechanics.
- Relationships between game systems.
- Localization mappings.
- Undocumented data structures.

---

# Future Ideas

Ideas intentionally postponed until the core editor reaches Version 1.0.

- Undo / Redo.
- Raw JSON viewer.
- Developer Mode.
- Performance diagnostics.
- Compare projects.
- Plugin architecture.
- Scriptable actions.
- Theme support.
- Dark mode.
- Dockable panes.
- Adjustable fonts.
- Favorites.
- Bookmarks.

---

# Design Philosophy

Every decision should support one or more of the following principles:

- Focus on gameplay rather than JSON.
- Preserve original game data whenever possible.
- Prefer discoverability over exposing raw structures.
- Keep the editor approachable for users with little or no programming experience.
- Build incrementally using small, testable improvements.
- Validate major features through in-game testing.
- Documentation is part of the project.
- Solve real modding problems before adding complexity.