# Wartales Editor
## Project Snapshot

**Application Version:** 0.5.1  
**Documentation Version:** 0.6  
**Last Updated:** 2026-07-12

---

# Project Vision

Wartales Editor is a desktop WPF application for editing Wartales game data safely, intelligently, and efficiently.

The editor is intended to understand the structure of the game's data rather than simply exposing JSON. Whenever possible, it should guide users toward valid edits, prevent accidental mistakes, and provide workflows tailored to gameplay concepts instead of raw data structures.

The long-term goal is a professional-quality editor that supports both casual modders and advanced creators.

---

# Project Status

**Status:** Builds Successfully ✅

**Current Release:** Version 0.5.1

**Current Milestone Status:** Snapshot Workflow Foundation completed

The project has transitioned from a functional editor into a reusable editing platform.

The current application supports:

- Intelligent navigation
- Localization-aware searching
- Type-aware editing
- Safe modification tracking
- Unlimited Undo / Redo
- Live Change Summary
- Direct navigation to modified properties
- Snapshot workflow infrastructure
- Constructor-injected services
- Dialog abstraction

The immediate focus is implementing Snapshot UI – Pass 1 using the completed workflow infrastructure.

---

# Development Philosophy

Whenever practical:

- Prevent mistakes instead of correcting them later.
- Infer behavior from game data instead of hardcoding values.
- Present gameplay concepts instead of raw JSON.
- Build reusable infrastructure before specialized features.
- Keep MVVM separation clean.
- Favor maintainability over quick implementations.
- Extend existing application state instead of creating parallel systems.
- Runtime-test completed behavior before committing.

---

# Technology

## Language

- C#

## Framework

- .NET
- WPF
- MVVM

## IDE

- Visual Studio Community

## Version Control

- Git
- GitHub

## JSON

- Newtonsoft.Json

---

# Current Architecture

## Primary Model Hierarchy

```text
ProjectModel
    ↓
SheetModel
    ↓
EntryModel
    ↓
PropertyModel
```

User interface terminology:

| Internal Model | User Interface |
|---|---|
| SheetModel | Category |
| EntryModel | Setting |
| PropertyModel | Property |

Supporting models:

- SearchResultModel
- ReferenceValueModel
- PropertyValueChangedEventArgs
- PropertyEditAction
- ChangeSummaryItemModel
- ModificationSnapshotModel
- ModificationSnapshotImportResultModel
- ModificationSnapshotExportResultModel

---

# PropertyModel

Current responsibilities:

- Type-aware editing
- Automatic editor selection
- Original value capture
- Modification detection
- Reset to original
- Validation support
- Reference lookup
- Property modification notifications
- Property value-change notifications
- Snapshot support
- Display-ready original and current values

`PropertyModel.IsModified` remains the single source of truth for determining whether a property currently differs from its saved baseline.

---

# Services

## JsonDataService

Responsible for:

- Loading projects
- Saving projects
- Parsing JSON
- Building ProjectModel
- Capturing original property values
- Establishing a new baseline after saving

---

## SearchService

Responsible for:

- Global Find Anything searching
- Property searching
- Search result generation
- Navigation metadata

---

## LocalizationService

Responsible for:

- English localization
- Localized searching
- Localized display names
- Future language support

---

## PropertyDefinitionService

Responsible for:

- Property metadata
- Read-only rules
- Editor overrides
- Property descriptions

---

## ReferenceDataService

Responsible for:

- Reference discovery
- Dropdown population
- Reference lookup
- Shared reference data through a singleton instance

---

## EditHistoryService

Responsible for:

- Recording edits
- Undo
- Redo
- Session history
- History state notifications

Edit history answers:

> What happened?

---

## ModificationSnapshotWorkflowService

Responsible for:

- Snapshot export orchestration
- Snapshot preview orchestration
- Snapshot import orchestration
- Coordinating matching, preview, serialization, and application services

The workflow service coordinates existing services rather than duplicating their responsibilities.

---

## Dialog Services

### IFileDialogService

Responsible for:

- Opening files
- Saving files

### IMessageDialogService

Responsible for:

- Information dialogs
- Warning dialogs
- Error dialogs
- Confirmation dialogs

WPF-specific implementations remain isolated from the ViewModel.

---

# ViewModels

## MainViewModel

Coordinates:

- Project state
- Category, Setting, and Property selection
- Find Anything
- Navigation
- Modification state
- Undo / Redo
- Change Summary
- Snapshot orchestration
- Window commands
- Status reporting

All required services are supplied through constructor injection.

---

## ChangeSummaryViewModel

Responsible for:

- Presenting modified-property snapshots
- Category grouping
- Selection
- Navigation commands
- Live refresh while the window remains open

---

# Current Editing Pipeline

```text
User Edit
    ↓
PropertyModel.Value
    ↓
SourceProperty
    ↓
RootDocument
    ↓
Modification Tracking
    ↓
Edit History
    ↓
Change Summary
    ↓
Snapshot Workflow
    ↓
Save
```

Editing occurs directly against the original JSON document.

No replacement JSON document is reconstructed for saving.

---

# Snapshot Workflow

Implemented:

- ✅ Snapshot capture
- ✅ Snapshot serialization
- ✅ Snapshot loading
- ✅ Snapshot matching
- ✅ Snapshot preview
- ✅ Snapshot application
- ✅ Workflow orchestration

Current status:

- Infrastructure complete
- UI integration pending

---

# Modification Tracking

Implemented:

- ✅ Original value capture
- ✅ Property modification tracking
- ✅ Project modification tracking
- ✅ Reset Property
- ✅ Modified row indicators
- ✅ Modified-property counter
- ✅ Window title dirty indicator
- ✅ Modification status reporting
- ✅ New baseline after Save

The modification state remains the single source of truth for the entire editor.

---

# Undo / Redo

Implemented:

- ✅ Unlimited session Undo
- ✅ Unlimited session Redo
- ✅ Toolbar commands
- ✅ Ctrl+Z
- ✅ Ctrl+Y
- ✅ History reset when opening another project
- ✅ Undo after Save

Known minor issue:

- Programmatic Undo / Redo may move the caret to the beginning of certain WPF text editors.
- Does not affect data integrity.
- Deferred until future UI modernization.

---

# Change Summary

Implemented:

- ✅ Live review of pending changes
- ✅ Category grouping
- ✅ Original values
- ✅ Current values
- ✅ Navigate button
- ✅ Double-click navigation
- ✅ Automatic refresh after Edit
- ✅ Undo
- ✅ Redo
- ✅ Reset Property
- ✅ Save
- ✅ Opening another project

Built entirely from the existing modification state.

---

# Property Editors

Implemented:

- ✅ Text
- ✅ Number
- ✅ Boolean
- ✅ Dropdown
- ✅ Read Only
- ✅ Complex Placeholder

Editors are selected automatically using property metadata, reference availability, and JSON token types.

---

# Search

Find Anything supports:

- Categories
- Settings
- Properties
- Internal IDs
- English localized names
- Property names
- Property values

Selecting a result automatically navigates to the matching Category, Setting, and Property.

---

# Current User Workflow

```text
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
Review Change Summary
    ↓
Export Snapshot (next)
    ↓
Preview Snapshot (next)
    ↓
Import Snapshot (next)
    ↓
Save
    ↓
Package
    ↓
Play
```

---

# Completed Milestones

- ✅ Project Foundation
- ✅ Data Browser
- ✅ Functional Editing
- ✅ Find Anything
- ✅ Smart Property Editors
- ✅ Safe Editing
- ✅ Change Summary
- ✅ Snapshot Workflow Foundation
- ✅ Constructor Injection
- ✅ Dialog Service Abstraction

---

# Current Roadmap

## Immediate Task

Implement **Snapshot UI – Pass 1**:

- Export Snapshot
- Preview Snapshot
- Import Snapshot

using the completed workflow infrastructure.

---

## Priority 1 – Mod Profiles and Change Migration

Primary goal:

Preserve user modifications across future Wartales updates.

Expected capabilities:

- Save reusable mod profiles
- Export modified values
- Import edits into newer `data.cdb`
- Intelligent matching
- Merge preview
- Conflict detection
- Safe application

---

## Priority 2 – Robust Validation

- Missing references
- Invalid references
- Required properties
- Duplicate detection
- Validation summaries
- Navigable validation results

---

## Priority 3 – Content Creation Tools

Initial targets:

- Camp structures
- Crafting stations
- Camp anvil
- Guided content creation

---

## Priority 4 – UI Modernization

- Workflow-oriented toolbar
- Improved command organization
- Better layout
- Icon support

---

# Additional Planned Features

- Batch Editing
- Import / Merge
- Modified-only filtering
- Change Summary filtering
- Change Summary export
- Recent Files
- Backup on Save
- QuickBMS workflow integration
- Property descriptions
- In-game mod/profile notification with creator credits
- Specialized gameplay editors

---

# Coding Standards

Always:

- Use MVVM.
- Use `ObservableObject` where appropriate.
- Prefer small, focused classes.
- Keep reusable logic in services.
- Keep editing behavior in models.
- Keep coordination in ViewModels.
- Keep code-behind view-specific.
- Preserve the original JSON document.
- Preserve original formatting whenever practical.
- Use the existing modification state as the single source of truth.
- Build after every logical implementation step.

Avoid:

- Business logic in XAML.
- Business logic in code-behind.
- Duplicate change tracking.
- Duplicate original-value storage.
- Hardcoded gameplay values whenever data-driven discovery is practical.
- Reconstructing current project files from memory.

---

# Development Workflow

1. Complete one logical implementation stage.
2. Build.
3. Fix all errors before continuing.
4. Runtime-test completed behavior.
5. Perform regression testing.
6. Update documentation.
7. Commit.
8. Push.
9. Begin the next approved milestone.

---

# AI Development Rules

The AI acts as lead software architect and senior developer.

Required practices:

- Design complete implementations before generating code.
- Prefer extensible architecture over shortcuts.
- Keep the project compiling after every stage.
- Complete one milestone at a time.
- Stay focused on the active milestone.
- Work only from the latest files supplied by the user.
- Never reconstruct a current file from memory.
- Ask for any current file needed before modifying it.
- Return complete replacements for small and medium files.
- Fully design large-file replacements before emitting them.
- Split large replacements only when required by response length.
- Update documentation before major commits.

---

# Current Task

Implement **Snapshot UI – Pass 1**:

- Export Snapshot
- Preview Snapshot
- Import Snapshot

using the completed Snapshot workflow infrastructure.