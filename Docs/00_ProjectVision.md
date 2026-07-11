# Project Vision

**Version:** 0.3  
**Status:** Active  
**Last Updated:** 2026-07-11  
**Applies To:** Entire Project

---

# Wartales Editor

## Mission

Wartales Editor is a purpose-built Windows application for editing the Wartales `data.cdb` file.

The goal is to make modifying gameplay values simple, safe, understandable, and fast without requiring users to understand the underlying JSON structure.

The editor is designed to let players spend their time creating and testing mods instead of manually editing JSON files.

---

# Vision

The long-term vision is to provide a complete modding environment for Wartales that allows users to:

- Discover game data.
- Understand game mechanics.
- Modify gameplay values safely.
- Test changes quickly.
- Reuse modifications across future game versions.

The editor should become the primary tool for creating and maintaining personal gameplay mods.

---

# Objectives

## Core Objectives

- Open the original `data.cdb`.
- Display game data in a gameplay-oriented interface.
- Allow gameplay values to be edited safely.
- Preserve the original data structure whenever possible.
- Save valid CDB files.
- Produce files compatible with the game's packaging workflow.

## Workflow Objectives

Reduce the time required to perform common gameplay edits.

The desired workflow is:

Open

↓

Find

↓

Edit

↓

Save

↓

Package

↓

Play

---

# Design Principles

## Gameplay First

Gameplay concepts should always take priority over JSON implementation details.

Users should think in terms of:

- Categories
- Settings
- Properties

not:

- Sheets
- Lines
- JSON Objects

---

## Preserve Original Data

Whenever practical:

- Preserve unknown properties.
- Preserve document structure.
- Modify existing JSON instead of rebuilding it.
- Avoid destructive transformations.

---

## Incremental Development

Develop the application through small, testable milestones.

Every feature should:

- Build successfully.
- Be tested.
- Be documented.
- Be committed before moving forward.

---

## Practical Modding

Features should directly improve the process of creating mods.

Avoid spending significant effort on features that do not make gameplay editing faster or easier.

---

## Documentation

Documentation is considered part of the project.

Every major milestone should include documentation updates before creating a Git commit.

---

# Current Status

## Project Phase

**Version 0.1 – Functional Editor**

The editor now successfully supports:

- Opening the game's CDB.
- Browsing Categories.
- Browsing Settings.
- Viewing Properties.
- Editing property values.
- Saving modified CDB files.
- Reloading saved files.
- Successfully verifying gameplay changes inside Wartales.

The editor has transitioned from a proof of concept into a functional modding tool.

---

# Current Development Priorities

## Highest Priority

- Improve search capabilities.
- Expand editable property support.
- Continue validating gameplay edits.
- Streamline the modding workflow.

## Secondary Priority

- QuickBMS integration.
- Localization support.
- Property validation.
- Batch editing.

## Long-Term Priority

- Change templates.
- Version migration.
- Specialized editors.
- Advanced search.
- Undo / Redo.

---

# Target Workflow

The intended workflow is now:

1. Open the original `data.cdb`.
2. Search for the desired gameplay setting.
3. Modify one or more properties.
4. Save the modified CDB.
5. Package the modified file.
6. Launch Wartales.
7. Verify gameplay changes.
8. Repeat.

The goal is to complete this workflow in only a few minutes.

---

# Roadmap

## Phase 1 — Functional Editor ✅

Completed:

- Open CDB
- Three-pane interface
- Categories / Settings / Properties
- Property viewing
- Property editing
- Save modified CDB
- Reload verification
- Successful in-game verification

---

## Phase 2 — Better Editing

Planned:

- Improved global search
- Search across all Categories
- Search localized names
- Search property names
- Search property values
- Type-aware editors
- Property validation
- Unsaved changes detection
- Save & Exit
- Backup on Save

---

## Phase 3 — Better Modding

Planned:

- QuickBMS integration
- Localization import
- Change templates
- Import/export templates
- Version migration
- Batch editing
- Compare CDB files

---

## Success Criteria

The project will be considered successful if it allows a user to:

- Discover gameplay values quickly.
- Modify those values safely.
- Save valid game data.
- Verify the changes in Wartales with minimal effort.

Every new feature should move the editor closer to that goal.