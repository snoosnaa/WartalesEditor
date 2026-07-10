# Project Vision

**Status:** Active  
**Last Updated:** 2026-07-10  
**Applies To:** Entire Project

---

# Wartales Editor

## Mission

Wartales Editor is a purpose-built Windows application for editing the Wartales `data.cdb` file.

The goal is to make modifying gameplay values simple, safe, and understandable without requiring users to understand the underlying JSON structure.

---

## Objectives

- Open the original `data.cdb` file.
- Display game data in a user-friendly interface.
- Allow gameplay values to be edited safely.
- Preserve the original file formatting as closely as possible when saving.
- Produce files that remain compatible with the game's packaging workflow.

---

## Design Principles

- Gameplay first, file format second.
- Preserve original data whenever possible.
- Make common tasks easy.
- Build incrementally with small, testable milestones.
- Document decisions and discoveries throughout development.
- Prefer clarity over cleverness.

---

## Non-Goals

The editor is not intended to be:

- A save-game editor.
- A mod manager.
- A QuickBMS replacement.
- A general-purpose JSON editor.

---

## Target Workflow

1. Open the original `data.cdb`.
2. Modify gameplay values.
3. Save the updated file.
4. Rebuild `res.pak` using external tools.
5. Start a new game using the modified data.

---

## Current Status

Project Phase: Early Development

Current Focus:
- Loading game data
- Building the editor interface
- Establishing a solid MVVM architecture