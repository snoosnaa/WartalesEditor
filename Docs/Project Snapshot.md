# Wartales Editor
## Project Snapshot

**Application Version:** 0.4.0
**Documentation Version:** 0.5
**Last Updated:** 2026-07-12

---

# Project Vision

Wartales Editor is a desktop WPF application for editing Wartales game data safely, intelligently, and efficiently.

The editor is intended to understand the structure of the game's data rather than simply exposing JSON. Whenever possible it should guide users toward valid edits, prevent accidental mistakes, and provide workflows tailored to gameplay concepts instead of raw data structures.

The long-term goal is a professional-quality editor that supports both casual modders and advanced creators.

---

# Project Status

**Status:** Builds Successfully ✅

**Current Milestone**

**Change Summary – Pass 1**

The project has successfully transitioned from building editor features to building reusable editing infrastructure.

Current focus is improving the editing workflow rather than simply adding additional editors.

---

# Development Philosophy

Whenever possible:

- Prevent mistakes instead of correcting them later.
- Infer behavior from game data instead of hardcoding values.
- Present gameplay concepts instead of raw JSON.
- Build reusable infrastructure before specialized features.
- Keep MVVM separation clean.
- Favor maintainability over quick implementations.

---

# Technology

### Language

- C#

### Framework

- .NET
- WPF
- MVVM

### IDE

- Visual Studio Community

### Version Control

- Git
- GitHub

### JSON

- Newtonsoft.Json

---

# Current Architecture

## Models

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

- SearchResultModel
- ReferenceValueModel
- PropertyValueChangedEventArgs

---

## PropertyModel

Current responsibilities:

- Type-aware editing
- Automatic editor selection
- Original value capture
- Modification tracking
- Reset to original
- Validation support
- Reference lookup
- Property change notifications

---

## Services

### JsonDataService

Responsible for:

- Loading projects
- Saving projects
- Parsing JSON
- Building ProjectModel
- Capturing original values

---

### SearchService

Responsible for:

- Global navigation
- Property searching
- Result generation

---

### LocalizationService

Responsible for:

- English localization
- Localized searching
- Future language support

---

### PropertyDefinitionService

Responsible for:

- Property metadata
- Read-only rules
- Editor overrides
- Property descriptions

---

### ReferenceDataService

Responsible for:

- Reference discovery
- Dropdown population
- Reference lookup

---

### EditHistoryService

Responsible for:

- Recording edits
- Undo
- Redo
- Session history

This service is intentionally reusable for future editing workflows.

---

# Current Editing Pipeline

```
User Edit

↓

PropertyModel

↓

RootDocument

↓

Modification Tracking

↓

Edit History

↓

Save
```

Editing occurs directly against the original JSON document.

---

# Property Editors

Implemented

- ✅ Text
- ✅ Number
- ✅ Boolean
- ✅ Dropdown
- ✅ Read Only
- ✅ Complex Placeholder

Editors are selected automatically using property metadata and JSON types.

---

# Safe Editing

Implemented

- ✅ Original value capture
- ✅ Property modification tracking
- ✅ Project modification tracking
- ✅ Reset Property
- ✅ Modified indicators
- ✅ Modification counter
- ✅ Window title dirty indicator

---

# Undo / Redo

Implemented

- ✅ Unlimited session undo
- ✅ Unlimited session redo
- ✅ Toolbar commands
- ✅ Ctrl+Z
- ✅ Ctrl+Y
- ✅ Automatic history reset when opening a project

---

# Search

Implemented

Find Anything supports:

- Categories
- Settings
- Properties
- Internal IDs
- English localization
- Property names
- Property values

Selecting a result automatically navigates directly to the matching editor location.

---

# Current User Workflow

```
Open

↓

Find Anything

↓

Edit

↓

Track Changes

↓

Undo / Redo

↓

Save

↓

Package

↓

Play
```

This workflow has been verified through live gameplay testing.

---

# Completed Milestones

## Foundation

- ✅ Project loading
- ✅ Project saving
- ✅ Three-pane editor

---

## Navigation

- ✅ Global Find Anything
- ✅ Localization-aware searching
- ✅ Automatic navigation

---

## Smart Editing

- ✅ Type-aware editors
- ✅ Validation framework
- ✅ Reference-aware dropdowns
- ✅ Smart editor selection

---

## Safe Editing

- ✅ Property tracking
- ✅ Project tracking
- ✅ Reset Property
- ✅ Edit history
- ✅ Unlimited Undo / Redo

---

# Current Roadmap

## Active

### Change Summary – Pass 1

- Review pending changes
- Group changes
- Navigate to modified properties
- Foundation for future batch editing

---

## Planned

### Workflow

- QuickBMS integration
- Recent Files
- Save & Exit
- Backup on Save

---

### Advanced Editing

- Batch Editing
- Import / Merge
- Property History
- Change filtering

---

### Gameplay Editors

- Starting Party Editor
- Camp Editor
- Profession Editor
- Skills
- Recipes
- Factions

---

### Long-Term

- Automatic migration
- Mod comparison
- Plugin system
- Rule-based validation

---

# Coding Standards

Always:

- MVVM
- ObservableObject
- Small focused classes
- Services own reusable logic
- Models own editing behavior
- ViewModels coordinate application state

Avoid:

- Business logic in XAML
- Business logic in code-behind
- Duplicate editing infrastructure
- Hardcoded gameplay values whenever possible

---

# Development Workflow

1. Complete one milestone.
2. Build after every implementation step.
3. Test thoroughly.
4. Update documentation.
5. Commit.
6. Push.
7. Begin the next milestone.

---

# AI Development Rules

The AI acts as lead software architect and senior developer.

Required practices:

- Design complete implementations before generating code.
- Prefer extensible architecture over shortcuts.
- Keep every build compiling.
- Work from the latest supplied files only.
- Never assume file contents.
- Return complete file replacements whenever practical.
- Split only large files when required.
- Update documentation before commits.

---

# Current Task

Implement **Change Summary – Pass 1**.

The feature must build directly upon the existing modification tracking and edit history infrastructure without introducing duplicate change-tracking systems.

The next milestone should extend the platform rather than redesign it.