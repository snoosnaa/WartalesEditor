# Architecture

**Version:** 0.2
**Status:** Active
**Last Updated:** 2026-07-10
**Applies To:** Entire Project

---

# Overview

Wartales Editor follows the Model-View-ViewModel (MVVM) architectural pattern.

The primary goals are:

- Separate user interface from business logic.
- Keep models independent of the UI.
- Support incremental feature development.
- Preserve the original CDB structure.

---

# Project Structure

WartalesEditor

├── Helpers

├── Models

├── Services

├── ViewModels

├── Views

└── Docs

---

# Model Hierarchy

ProjectModel

↓

SheetModel

↓

EntryModel

↓

PropertyModel

Each level owns the level beneath it.

---

# Selection Flow

The editor always navigates in one direction.

Project

↓

SelectedSheet

↓

Entries

↓

SelectedEntry

↓

Properties

This flow should remain consistent throughout the project.

---

# Responsibilities

## ProjectModel

Represents the currently opened CDB project.

Owns:

- Sheets

---

## SheetModel

Represents one sheet within the project.

Owns:

- Entries

---

## EntryModel

Represents one record within a sheet.

Owns:

- Properties

Also stores:

- DisplayName

---

## PropertyModel

Represents one editable property.

Current members:

- Name
- Value

Future members may include:

- OriginalValue
- IsModified
- Description
- DataType
- EditorType

---

# Services

## JsonDataService

Responsible for:

- Loading CDB JSON.
- Parsing models.
- Saving models (future).

Services should never contain UI code.

---

# ViewModels

MainViewModel currently exposes:

- Project
- Sheets
- SelectedSheet
- Entries
- SelectedEntry
- Properties

Future ViewModels should follow the same MVVM principles.

---

# User Interface

Current layout

Sheets

|

Entries

|

Properties

The UI should remain focused on gameplay concepts rather than JSON structure.

---

# Design Principles

- Build incrementally.
- Prefer composition over duplication.
- Keep responsibilities small.
- Preserve user data.
- Avoid unnecessary complexity.
- Keep the UI approachable for modders.

---

# Future Expansion

The architecture is intentionally designed to support:

- Property editing
- Change tracking
- Batch operations
- Change migration
- Undo / Redo
- Additional editor types

without requiring major structural changes.