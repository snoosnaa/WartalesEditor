# Wartales Editor
## Project Snapshot

**Application Version:** 0.6.0  
**Documentation Version:** 0.7  
**Last Updated:** 2026-07-13

---

# Project Vision

Wartales Editor is a desktop WPF application for editing Wartales game data safely, intelligently, and efficiently.

The editor is designed to understand the structure of the game's data rather than simply exposing JSON. Whenever practical, it should guide users toward valid edits, prevent accidental mistakes, and provide workflows centered around gameplay concepts instead of raw data structures.

The long-term goal is to become a professional-quality editor capable of supporting both casual modders and advanced content creators while remaining maintainable, extensible, and reliable across future Wartales updates.

---

# Project Status

**Status:** Builds Successfully ✅

**Current Release:** Version 0.6.0

**Current Milestone Status:** Snapshot UI – Pass 1 completed

The application has evolved beyond a functional editor into a reusable editing platform with a growing collection of shared infrastructure.

The editor currently provides:

- Intelligent navigation
- Localization-aware searching
- Type-aware property editors
- Safe modification tracking
- Unlimited Undo / Redo
- Live Change Summary
- Direct navigation to modified properties
- Complete Snapshot workflow
- Constructor-injected services
- Dialog abstraction
- Workflow-based architecture

The next development milestone is **Mod Profiles & Change Migration**.

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

- .NET 10
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

Supporting models include:

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
- Reset support
- Validation support
- Reference lookup
- Modification notifications
- Value-change notifications
- Snapshot participation
- Display-ready original and current values

`PropertyModel.IsModified` remains the application's single source of truth for determining whether a property differs from its saved baseline.

---

# Core Services

## JsonDataService

Responsible for:

- Loading projects
- Saving projects
- Parsing JSON
- Building ProjectModel
- Capturing original values
- Establishing a new baseline after Save

---

## SearchService

Responsible for:

- Find Anything searching
- Search result generation
- Navigation metadata

---

## LocalizationService

Responsible for:

- English localization
- Localized searching
- Display names
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
- Shared lookup data

---

## EditHistoryService

Responsible for:

- Recording edits
- Undo
- Redo
- Session history
- History state notifications

This service answers the question:

> "What happened?"

---

## ModificationSnapshotWorkflowService

Responsible for:

- Snapshot export
- Snapshot preview
- Snapshot import
- Workflow orchestration
- Matching coordination
- Snapshot application

The workflow service coordinates existing systems rather than replacing them.

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

Concrete WPF implementations remain isolated from ViewModels.

---

# ViewModels

## MainViewModel

Coordinates:

- Project state
- Category selection
- Setting selection
- Property selection
- Find Anything
- Navigation
- Modification tracking
- Undo / Redo
- Change Summary
- Snapshot workflow
- Window commands
- Status reporting

All dependencies are supplied through constructor injection.

---

## ChangeSummaryViewModel

Responsible for:

- Presenting modified-property snapshots
- Category grouping
- Navigation
- Live updates
- Selection management

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

Editing always occurs directly against the loaded JSON document.

No replacement document is reconstructed during Save.

---

# Snapshot Workflow

Implemented:

- ✅ Export Snapshot
- ✅ Preview Snapshot
- ✅ Import Snapshot
- ✅ Snapshot capture
- ✅ Snapshot serialization
- ✅ Snapshot loading
- ✅ Snapshot matching
- ✅ Snapshot application
- ✅ Workflow orchestration

The complete end-to-end snapshot workflow is now operational and fully integrated with the editor.

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

The modification state remains the single source of truth throughout the editor.

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

- Programmatic Undo / Redo may reposition the caret within certain WPF text editors.
- This does not affect data integrity.
- Deferred until the UI Modernization milestone.

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

The Change Summary is generated entirely from the existing modification state rather than maintaining a second change-tracking system.

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
Export Snapshot
    ↓
Preview Snapshot
    ↓
Import Snapshot
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
- ✅ First Functional Editor
- ✅ Find Anything
- ✅ Smart Property Editors
- ✅ Safe Editing
- ✅ Change Summary
- ✅ Snapshot Workflow Foundation
- ✅ Constructor Injection
- ✅ Dialog Service Abstraction
- ✅ Snapshot UI – Pass 1

---

# Current Roadmap

## Active Milestone

### Mod Profiles & Change Migration

Primary objective:

Allow users to preserve their modifications across future Wartales updates.

Planned capabilities:

- Save reusable Mod Profiles
- Load existing Mod Profiles
- Intelligent snapshot matching
- Migration to newer game versions
- Conflict reporting
- Safe application
- Merge Preview (future)

---

## Priority 2 – Robust Validation

Planned capabilities:

- Missing reference detection
- Invalid references
- Invalid values
- Required property validation
- Duplicate detection
- Validation reports
- Navigable validation results

---

## Priority 3 – Content Creation Tools

Initial focus:

- Camp structures
- Crafting stations
- Camp anvil
- Guided gameplay content creation

---

## Priority 4 – UI Modernization

Planned improvements:

- Workflow-oriented toolbar
- Larger action buttons
- Improved command organization
- Layout polish
- Icon support
- Additional usability improvements

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
- In-game mod/profile notification with optional creator credits
- Specialized gameplay editors

---

# Coding Standards

Always:

- Use MVVM.
- Use `ObservableObject` where appropriate.
- Prefer small, focused classes.
- Keep reusable logic inside services.
- Keep editing behavior inside models.
- Keep coordination inside ViewModels.
- Keep code-behind view-specific.
- Preserve the original JSON document.
- Preserve original formatting whenever practical.
- Use the existing modification state as the application's single source of truth.
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
- Keep the project compiling after every implementation stage.
- Complete one milestone at a time.
- Stay focused on the active milestone.
- Work only from the latest files supplied by the user.
- Never reconstruct a current project file from memory.
- Ask for any current file before modifying it.
- Return complete replacements for small and medium files.
- Fully design large-file replacements before emitting them.
- Split large replacements only when required by response length.
- Update documentation before major commits.

---

# Current Task

Current documentation updates are complete.

Next actions:

1. Create the Version **0.6.0** Git commit.
2. Push the changes to GitHub.
3. Begin the **Mod Profiles & Change Migration** milestone.

The Snapshot workflow is now complete and provides the reusable foundation for future profile management, migration, validation, merge preview, and advanced editing workflows.