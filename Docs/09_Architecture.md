# Architecture

**Version:** 0.4
**Status:** Active
**Last Updated:** 2026-07-11
**Applies To:** Entire Project

---

# Overview

Wartales Editor follows the Model-View-ViewModel (MVVM) architectural pattern.

The primary goals are:

- Separate user interface from business logic.
- Keep models independent of the UI.
- Support incremental feature development.
- Preserve the original CDB structure.
- Modify the original JSON document directly without reconstructing game data.

The editor now supports complete gameplay editing together with localization-aware global navigation.

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

Each folder has a clearly defined responsibility.

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
```

The internal model names intentionally match the structure of Wartales.

The user interface presents these objects using gameplay-oriented terminology.

| Internal Model | User Interface |
|----------------|----------------|
| SheetModel | Category |
| EntryModel | Setting |
| PropertyModel | Property |

---

# Selection Flow

Navigation always flows downward.

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

Changing a higher-level selection clears selections beneath it to prevent invalid state.

---

# Find Anything Flow

The global navigation system is implemented independently of the editor hierarchy.

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

This architecture keeps searching completely independent from editing while allowing direct navigation into the editor.

---

# Editing Pipeline

```
User edits TextBox
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
Modified data.cdb
```

The original document is modified directly without rebuilding the JSON structure.

---

# Responsibilities

## ProjectModel

Owns:

- RootDocument
- Sheets
- Project metadata

Acts as the root object for the editor.

---

## SheetModel

Represents one Category.

Owns:

- Entries

Future responsibilities:

- Entry counts
- Visibility state

---

## EntryModel

Represents one Setting.

Owns:

- Properties

Stores:

- DisplayName
- Id

Future responsibilities:

- Validation state
- Modified state

---

## PropertyModel

Represents one editable property.

Current members:

- Name
- Value
- SourceProperty

Future members:

- OriginalValue
- IsModified
- DataType
- EditorType
- ValidationState

---

## SearchResultModel

Represents one Find Anything result.

Contains the information required to navigate directly to the correct editor location.

---

# Services

## JsonDataService

Responsible for:

- Loading JSON
- Parsing CDB
- Building ProjectModel
- Saving RootDocument

---

## SearchService

Responsible for:

- Global searching
- Property searching
- Result generation
- Navigation data

Contains no UI code.

---

## LocalizationService

Responsible for:

- Loading localization XML
- Looking up localized names
- Supporting future language packs

The service remains language-independent.

---

# ViewModels

MainViewModel exposes:

- Project
- Categories
- Settings
- Properties
- Find Anything
- Search
- Localization status
- Editor state

The ViewModel coordinates interaction between the UI and Services.

Business logic remains inside Models and Services.

---

# User Interface

Current layout:

```
Find Anything

↓

Categories

↓

Settings

↓

Properties
```

The interface intentionally emphasizes gameplay concepts over internal implementation details.

---

# Design Principles

## Gameplay First

The editor exists to simplify gameplay modding.

Technical implementation should remain largely invisible to users.

---

## Preserve Original Data

Modify existing JSON whenever possible.

Avoid rebuilding or normalizing structures unnecessarily.

---

## Separation of Responsibilities

- Models represent data.
- Services implement logic.
- ViewModels coordinate state.
- Views present information.

---

## Documentation First

Every completed milestone should include documentation updates before committing changes.

---

# Current Architecture Status

The core architecture is now considered stable.

Future features such as:

- Validation
- Type-aware editors
- Mod Profiles
- Batch editing
- Change migration

can all build upon the existing architecture without requiring major restructuring.