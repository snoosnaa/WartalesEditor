# Architecture

**Version:** 0.5
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

The editor has evolved beyond a simple property editor into a modular editing platform that supports intelligent editing, safe editing, and reusable editing history.

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

Search is treated as a navigation system rather than a filtering system.

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

No intermediate data model is reconstructed.

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

Property models own modification detection.

The ViewModel owns application state.

This keeps editing logic independent from presentation.

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

The history service records every property edit while remaining independent from the user interface.

History actions operate directly on PropertyModel rather than UI controls.

This architecture is reusable for future editing features.

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
- Id

Future responsibilities:

- Validation summary
- Change summary

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

PropertyModel intentionally owns editing behavior rather than UI behavior.

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

Allows editing history to remain independent from PropertyModel.

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

Remains language independent.

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

Contains no UI logic.

Future features such as Batch Editing and Import/Merge will reuse this service.

---

# ViewModels

MainViewModel coordinates:

- Project state
- Selection
- Search
- Navigation
- Modification tracking
- Undo/Redo
- Commands
- Status reporting

Business logic remains inside Models and Services whenever practical.

---

# User Interface

Current layout:

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

The interface intentionally exposes gameplay concepts while hiding implementation details.

---

# Design Principles

## Gameplay First

The editor exists to improve the gameplay modding workflow.

---

## Preserve Original Data

Modify the existing JSON document whenever possible.

Avoid rebuilding data structures.

---

## Separation of Responsibilities

- Models own data and editing behavior.
- Services own reusable logic.
- ViewModels coordinate application state.
- Views present information.

---

## Infrastructure Before Features

Whenever practical, reusable infrastructure should be implemented before feature-specific functionality.

This reduces future refactoring and keeps the architecture consistent.

---

## Documentation First

Every completed milestone should include documentation updates before the associated Git commit.

---

# Current Architecture Status

The core architecture is now considered stable.

Current infrastructure already supports future implementation of:

- Change Summary
- Batch Editing
- Import / Merge
- Validation Summary
- Property History
- Modified-only filtering
- Advanced Undo/Redo
- Mod Profiles

Future milestones should primarily extend existing services rather than introducing parallel architectures.