# Wartales Editor
## Project Snapshot

**Application Version:** 0.5.0  
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

**Current Release:** Version 0.5.0

**Current Milestone Status:** Change Summary – Pass 1 completed

The project has transitioned from a functional editor into a reusable editing platform.

The current application supports:

- Intelligent navigation
- Localization-aware searching
- Type-aware editing
- Safe modification tracking
- Unlimited Undo / Redo
- Live Change Summary
- Direct navigation to modified properties

The immediate focus is completing the Version 0.5.0 documentation, commit, and push before beginning the next milestone.

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
- Display-ready original and current values

`PropertyModel.IsModified` is the single source of truth for determining whether a property currently differs from its saved baseline.

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

This service remains intentionally independent from the Change Summary.

Edit history answers:

> What happened?

Change Summary answers:

> What is currently different?

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
- Change Summary snapshot generation
- Window commands
- Status reporting

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
Save
```

Editing occurs directly against the original JSON document.

No replacement JSON document is reconstructed for saving.

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

Current-state comparison is based on `JToken.DeepEquals`.

A property is no longer considered modified when its current value again matches its saved baseline, regardless of how many edits occurred.

---

# Undo / Redo

Implemented:

- ✅ Unlimited session Undo
- ✅ Unlimited session Redo
- ✅ Toolbar commands
- ✅ Ctrl+Z
- ✅ Ctrl+Y
- ✅ History reset when opening another project
- ✅ Undo and Redo integration with modification tracking
- ✅ Undo permitted after saving

Undo after saving intentionally creates a new unsaved change relative to the newly saved baseline.

Known minor issue:

- Programmatic Undo / Redo may move the caret to the beginning of certain WPF text editors.
- This does not affect data integrity.
- Deferred to a future UI modernization milestone.

---

# Change Summary

Implemented:

- ✅ Read-only Change Summary window
- ✅ Live modified-property review
- ✅ Category grouping
- ✅ Localized Setting names
- ✅ Property names
- ✅ Original values
- ✅ Current values
- ✅ Navigate button
- ✅ Double-click navigation
- ✅ Main editor focus after navigation
- ✅ Working Close button
- ✅ Automatic refresh after Edit
- ✅ Automatic refresh after Undo
- ✅ Automatic refresh after Redo
- ✅ Automatic refresh after Reset Property
- ✅ Automatic refresh after Save
- ✅ Automatic refresh after opening another project
- ✅ Correct empty state
- ✅ Correct window reopening behavior

The Change Summary is built from temporary snapshots of the current project state.

It does not maintain its own modification history or duplicate original-value storage.

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

Selecting a result automatically navigates to the matching Category and Setting and selects the matching Property when applicable.

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
Save
    ↓
Package
    ↓
Play
```

The editing and packaging workflow has been verified through live gameplay testing.

---

# Completed Milestones

## Project Foundation

- ✅ WPF application
- ✅ MVVM architecture
- ✅ Git and GitHub integration
- ✅ Documentation system
- ✅ JSON loading

---

## Data Browser

- ✅ Three-pane editor
- ✅ Categories
- ✅ Settings
- ✅ Properties
- ✅ Selection synchronization

---

## Functional Editing

- ✅ Editable properties
- ✅ Direct RootDocument synchronization
- ✅ Save modified CDB
- ✅ Reload saved files
- ✅ In-game verification

---

## Find Anything and Smart Editing

- ✅ Global Find Anything
- ✅ Localization-aware searching
- ✅ Automatic navigation
- ✅ Type-aware editors
- ✅ Reference-aware dropdowns
- ✅ Validation framework foundation

---

## Safe Editing

- ✅ Property modification tracking
- ✅ Project modification tracking
- ✅ Reset Property
- ✅ Modified indicators
- ✅ Unlimited Undo / Redo
- ✅ Reusable history infrastructure

---

## Change Summary

- ✅ Live review of pending changes
- ✅ Original and current value comparison
- ✅ Category grouping
- ✅ Localized Setting display
- ✅ Navigation to modified properties
- ✅ Reusable snapshot architecture

---

# Current Roadmap

## Immediate Task

Complete the Version 0.5.0 release process:

- Update remaining documentation
- Confirm version consistency
- Create Git commit
- Push to GitHub

No additional Version 0.5.0 code changes are planned unless a critical issue is discovered.

---

## Priority 1 – Mod Profiles and Change Migration

Primary goal:

Preserve user modifications across future Wartales updates.

Expected capabilities:

- Save reusable mod profiles
- Export modified values
- Import edits into a newer `data.cdb`
- Intelligent matching of Categories, Settings, and Properties
- Merge preview
- Conflict detection
- Safe application of compatible changes

---

## Priority 2 – Robust Validation

Primary goal:

Detect invalid or unsafe modifications before saving or packaging.

Expected capabilities:

- Missing reference detection
- Invalid reference detection
- Required property validation
- Duplicate detection
- Invalid value detection
- Validation summaries
- Navigable validation results

---

## Priority 3 – Content Creation Tools

Primary goal:

Provide guided actions for adding game content without manually reproducing complex JSON structures.

Initial targets:

- Add camp structures
- Add crafting stations
- Add the anvil to the player camp
- Future guided content-creation tools

---

## Priority 4 – UI Modernization

Planned improvements:

- Move editing actions into clearer button-based workflows
- Simplify the top toolbar
- Improve command organization
- Improve spacing and resizing
- Add visual polish
- Prepare for future icon support

---

## Additional Planned Features

- Batch Editing
- Import / Merge
- Modified-only filtering
- Change Summary filtering
- Change Summary export
- Recent Files
- Backup on Save
- QuickBMS workflow integration
- Property descriptions
- Additional specialized gameplay editors

---

# Coding Standards

Always:

- Use MVVM.
- Use `ObservableObject` for bindable ViewModels and models where appropriate.
- Prefer small, focused classes.
- Keep reusable logic in services.
- Keep editing behavior in models.
- Keep application coordination in ViewModels.
- Keep code-behind limited to view-specific interaction.
- Preserve the original JSON document.
- Use the existing modification state as the single source of truth.
- Build after every logical implementation step.

Avoid:

- Business logic in XAML.
- Business logic in code-behind.
- Duplicate change-tracking systems.
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

Complete the Version 0.5.0 documentation, commit, and push.

After the release is complete, begin only the next approved milestone.

The highest-priority future milestone is:

**Mod Profiles and Change Migration**