# Architecture

**Version:** 0.6
**Status:** Active
**Last Updated:** 2026-07-12
**Applies To:** Entire Project

---

# Overview

Wartales Editor follows the Model-View-ViewModel (MVVM) architectural pattern.

The primary architectural goals are:

- Separate presentation from business logic.
- Keep models independent of the UI.
- Preserve the original Wartales CDB structure.
- Modify the original JSON document directly.
- Build reusable editing infrastructure before implementing advanced features.
- Favor extensible services over feature-specific implementations.

The editor has evolved beyond a simple property editor into a modular editing platform supporting intelligent editing, safe editing, undo/redo, and live change review.

---

# Project Structure

```
WartalesEditor
│
├── Docs
├── Helpers
├── Models
├── Services
├── ViewModels
└── Views
```

Each folder owns a specific responsibility.

---

# Model Hierarchy

```
ProjectModel
    ↓
SheetModel
    ↓
EntryModel
    ↓
PropertyModel
```

Supporting models:

```
SearchResultModel
ReferenceValueModel
PropertyValueChangedEventArgs
ChangeSummaryItemModel
PropertyEditAction
```

The internal model names intentionally mirror the Wartales data structure while the UI presents gameplay-oriented terminology.

| Internal Model | User Interface |
|----------------|----------------|
| SheetModel | Category |
| EntryModel | Setting |
| PropertyModel | Property |

---

# Navigation Flow

```
Project
    ↓
SelectedSheet
    ↓
Entries
    ↓
SelectedEntry
    ↓
Properties
```

Changing a higher-level selection automatically clears lower-level selections to prevent invalid editor state.

---

# Find Anything Architecture

Search remains completely independent of editing.

```
Search Text
        ↓
SearchService
        ↓
LocalizationService
        ↓
SearchResultModel
        ↓
SelectedSheet
        ↓
SelectedEntry
        ↓
SelectedProperty
```

Search is treated as navigation rather than filtering.

---

# Property Editing Pipeline

```
User edits Property

        ↓

PropertyModel.Value

        ↓

SourceProperty

        ↓

JProperty

        ↓

RootDocument

        ↓

JsonDataService.SaveProject()

        ↓

Modified CDB
```

The JSON document is modified directly.

No intermediate object graph is reconstructed.

---

# Modification Tracking Architecture

```
PropertyModel

        │

ModifiedChanged

        │

        ▼

MainViewModel

        │

        ├── Project.IsModified
        ├── ModifiedPropertyCount
        ├── WindowTitle
        ├── ModificationStatus
        └── ModifiedProperties
```

PropertyModel owns modification detection.

MainViewModel owns application state.

Presentation remains independent of modification logic.

---

# Undo / Redo Architecture

```
PropertyModel

        │

ValueChanged

        │

        ▼

EditHistoryService

        │

        ▼

PropertyEditAction

        │

        ├── Undo()
        └── Redo()
```

History recording is intentionally independent of editing logic.

Undo/Redo operates directly on PropertyModel rather than user interface controls.

---

# Change Summary Architecture

The Change Summary does **not** maintain its own change-tracking system.

Instead, it consumes the existing modification state.

```
PropertyModel

        │

IsModified

        │

        ▼

MainViewModel

        │

Build Snapshot

        │

        ▼

ChangeSummaryItemModel

        │

        ▼

ChangeSummaryViewModel

        │

        ▼

ChangeSummaryWindow
```

The summary is rebuilt from the current project state whenever modification state changes.

This guarantees that the summary always reflects the current editor state rather than the editing history.

---

# Architectural Principle

## Single Source of Truth

Modification state exists in exactly one place:

```
PropertyModel.IsModified
```

Every feature that answers:

> "What is currently different?"

must consume the existing modification state.

Future features should **not** introduce parallel change-tracking systems.

Examples include:

- Change Summary
- Batch Editing
- Import / Merge preview
- Validation reports
- Change Export
- Modified-only filtering

This keeps the architecture simple, consistent, and reliable.

---

# Responsibilities

## ProjectModel

Owns:

- RootDocument
- Sheets
- File metadata
- Project modification state

Acts as the root object for the editor.

---

## SheetModel

Represents one gameplay Category.

Owns:

- Entries

Future responsibilities:

- Visibility state
- Statistics
- Validation summaries

---

## EntryModel

Represents one gameplay Setting.

Owns:

- Properties

Stores:

- DisplayName
- Name
- Id

---

## PropertyModel

Represents one editable gameplay property.

Current responsibilities:

- Value editing
- Original value capture
- Modification detection
- Reset to original
- Type-aware editing
- Validation
- Raising modification events
- Raising value-change events
- Display-ready value formatting

PropertyModel intentionally owns editing behavior rather than UI behavior.

---

## ChangeSummaryItemModel

Represents one modified property.

Contains:

- Category
- Setting
- Property
- Original Value
- Current Value

Instances are immutable snapshots created from the current project state.

---

## SearchResultModel

Represents a Find Anything result.

Contains all information required to navigate directly into the editor.

---

## ReferenceValueModel

Represents one valid selectable value for dropdown editors.

Keeps display text independent from stored values.

---

## PropertyValueChangedEventArgs

Carries:

- Previous value
- New value

Allows EditHistoryService to remain independent from PropertyModel.

---

# Services

## JsonDataService

Responsible for:

- Loading JSON
- Parsing CDB
- Building ProjectModel
- Capturing original values
- Saving RootDocument

---

## SearchService

Responsible for:

- Global searching
- Property searching
- Result generation
- Navigation information

Contains no UI logic.

---

## LocalizationService

Responsible for:

- Loading localization XML
- Localized name lookup
- Future language support

---

## ReferenceDataService

Responsible for:

- Discovering valid references
- Populating dropdown editors
- Managing reference lookups

---

## EditHistoryService

Responsible for:

- Recording edits
- Undo
- Redo
- Session history
- History notifications

Future editing features will continue reusing this service.

---

# ViewModels

## MainViewModel

Coordinates:

- Project state
- Selection
- Search
- Navigation
- Modification tracking
- Undo / Redo
- Change Summary snapshots
- Commands
- Status reporting

---

## ChangeSummaryViewModel

Responsible for:

- Presenting Change Summary items
- Category grouping
- Selection
- Navigation commands
- Live refresh

Contains presentation logic only.

---

# User Interface

Current workflow:

```
Toolbar

↓

Search

↓

Find Anything

↓

Categories

↓

Settings

↓

Properties

↓

Status Bar
```

The Change Summary window is modeless and remains synchronized with the editor.

---

# Design Principles

## Gameplay First

The editor exists to improve the gameplay modding workflow.

---

## Preserve Original Data

Modify the existing JSON document whenever practical.

Avoid rebuilding data structures.

---

## Separation of Responsibilities

- Models own data and editing behavior.
- Services own reusable logic.
- ViewModels coordinate application state.
- Views present information.

---

## Infrastructure Before Features

Reusable infrastructure should be implemented before feature-specific functionality whenever practical.

---

## Single Source of Truth

Every subsystem should consume existing application state rather than duplicate it.

---

## Documentation First

Every completed milestone includes documentation updates before the associated Git commit.

---

# Current Architecture Status

The core architecture is now considered stable.

The existing infrastructure directly supports future implementation of:

- Batch Editing
- Import / Merge
- Validation
- Validation Summary
- Mod Profiles
- Change Migration
- Change Export
- Modified-only filtering
- Content Creation Tools

Future milestones should extend the existing architecture rather than introducing parallel systems.