# Development Journal

**Version:** 0.6
**Status:** Active
**Last Updated:** 2026-07-12
**Applies To:** Entire Project

---

# Table of Contents

- Session 001
- Session 002
- Session 003
- Session 004
- Session 005
- Session 006
- Session 007

---

# Session 001

## Summary

Initial project setup and application foundation.

## Completed

- Created the WPF project.
- Renamed the application to Wartales Editor.
- Established the MVVM architecture.
- Added the Helpers, Models, Services, and ViewModels folders.
- Installed Newtonsoft.Json.
- Implemented the Open command.
- Added file selection using OpenFileDialog.
- Added support for remembering the previously selected file.
- Parsed the original Wartales `data.cdb`.
- Created ProjectModel, SheetModel, and EntryModel.
- Displayed loaded sheets in the user interface.
- Adopted the three-pane editor layout.
- Established the documentation structure.

## Major Decisions

- Use the original `data.cdb` as the primary reference.
- Build the application incrementally.
- Preserve the original file formatting whenever possible.
- Focus on gameplay concepts instead of JSON structure.

## Current Status

The application successfully opens and parses the original `data.cdb` and displays the list of Categories.

## Milestone Achieved

Project foundation completed.

## Next Focus

Display entries and build the three-pane editor.

---

# Session 002

## Summary

Completed the transition from a category browser into a functional three-pane data browser.

## Completed

- Added `SelectedEntry` to the ViewModel.
- Completed the MVVM selection pipeline.
- Displayed Settings for the selected Category.
- Added the third Properties pane.
- Displayed editable PropertyModel objects.
- Replaced `KeyValuePair<string, object?>` with `PropertyModel`.
- Updated `JsonDataService`.
- Updated UI bindings.
- Successfully built and tested after every logical change.

## Major Decisions

- Display internal IDs whenever practical.
- Preserve every Category, including empty Categories.
- Use dedicated model classes instead of generic collections.
- Continue building in small, verifiable increments.

## Current Status

The application provides a complete three-pane browsing experience.

Users can:

- Open a CDB.
- Browse Categories.
- Browse Settings.
- View Properties.

## Milestone Achieved

Data Browser completed.

## Next Focus

Implement property editing.

---

# Session 003

## Summary

Completed the first fully functional editing workflow from loading the game's CDB through modifying gameplay values and successfully verifying those changes inside Wartales.

## Completed

### User Interface

- Added pane headers.
- Renamed Sheets → Categories.
- Renamed Entries → Settings.
- Added Show Empty Categories.
- Improved navigation.

### Editing

- Replaced read-only properties with editable controls.
- Connected PropertyModel directly to SourceProperty.
- Updated RootDocument automatically.
- Added Save support.
- Reloaded saved files successfully.

### Verification

Successfully verified live gameplay edits inside Wartales.

Verified workflow:

Open

↓

Browse

↓

Edit

↓

Save

↓

Package

↓

Launch

↓

Verify

## Major Decisions

- Shift focus toward improving the gameplay modding workflow.
- Prioritize practical features over technical complexity.
- Continue using internal IDs until localization support is available.

## Current Status

The editor became a fully functional gameplay editor capable of modifying live game data.

## Milestone Achieved

Version 0.1.0 — First Functional Editor

## Next Focus

Improve search, navigation and localization.

---

# Session 004

## Summary

Completed Find Anything v1 while laying the foundation for intelligent editing.

## Completed

### Search

- Introduced `SearchService`.
- Introduced `SearchResultModel`.
- Added the Find Anything panel.
- Added global searching across all Categories.
- Added searching by:
  - Internal IDs
  - English localized names
  - Property names
  - Property values
- Added automatic navigation to matching Categories and Settings.
- Added automatic property selection.
- Added search result counts.

### Localization

- Introduced `LocalizationService`.
- Added support for importing `export_en.xml`.
- Added localization-aware searching.
- Combined localized names with internal IDs throughout the editor.

### Smart Editing Foundation

- Introduced type-aware property editors.
- Added validation.
- Added dropdown framework.
- Added category-aware reference discovery.
- Introduced `ReferenceValueModel`.
- Added smart property editors based on data type.
- Established the architecture for future intelligent editors.

## Major Decisions

- Treat search primarily as navigation.
- Keep display names separate from internal identifiers.
- Build language-independent localization support.
- Build an extensible editor framework rather than individual editors.

## Current Status

The editor now supports intelligent navigation together with context-aware property editing.

## Milestone Achieved

Find Anything v1 and Smart Property Editors completed.

## Next Focus

Implement safe editing infrastructure.

---

# Session 005

## Summary

Completed the editing safety infrastructure that transforms the editor from a simple property editor into a reliable editing application.

## Completed

### Safe Editing

- Added original value capture for every editable property.
- Added property modification tracking.
- Added project modification tracking.
- Added visual modified indicators.
- Added modified property counting.
- Added dynamic window title modification indicator.
- Added Reset Property.
- Added modification status reporting.
- Established the foundation for future Change Summary support.

### Undo / Redo

- Implemented unlimited session undo history.
- Implemented unlimited session redo history.
- Added Undo and Redo commands.
- Added toolbar controls.
- Added Ctrl+Z support.
- Added Ctrl+Y support.
- Added automatic history clearing when opening a new project.

### History Architecture

- Introduced `EditHistoryService`.
- Introduced `PropertyEditAction`.
- Introduced property value change events carrying previous and new values.
- Decoupled history recording from property editing.
- Built a reusable history architecture for future batch editing, import/merge, and change summaries.

### Verification

Successfully verified:

- Property modification tracking.
- Project dirty-state tracking.
- Reset Property.
- Multiple modified properties.
- Save state reset.
- Unlimited Undo.
- Unlimited Redo.
- Toolbar commands.
- Keyboard shortcuts.
- History reset when opening a new project.

### Known Minor Issue

- Undo performed from the toolbar may reposition the text caret within certain WPF text editors. This does not affect data integrity and is considered a low-priority UI refinement.

## Major Decisions

- Treat editing history as a reusable application service.
- Keep undo/redo independent from property editing.
- Build infrastructure before implementing advanced editing features.
- Preserve clean MVVM separation throughout the editing pipeline.

## Current Status

The editor now provides a safe editing environment with modification tracking, reset capability, and unlimited session undo/redo.

## Milestone Achieved

Version 0.4.0 — Safe Editing Infrastructure

## Next Focus

Implement Change Summary – Pass 1 using the new modification and history infrastructure.

---

# Session 006

## Summary

Completed Change Summary – Pass 1, providing users with a live review of every unsaved modification while preserving the existing editing architecture.

This milestone focused on presenting the current modification state rather than recording editing history.

## Completed

### Change Summary

- Added `ChangeSummaryItemModel`.
- Added `ChangeSummaryViewModel`.
- Added the Change Summary window.
- Displayed:
  - Category
  - Setting
  - Property
  - Original Value
  - Current Value
- Added automatic refresh after:
  - Property edits
  - Undo
  - Redo
  - Reset Property
  - Save
  - Opening another project
- Added modeless window behavior.
- Added navigation back to modified properties.
- Added double-click navigation.
- Added localized setting names.

### Architecture

- Reused the existing PropertyModel modification state as the single source of truth.
- Avoided introducing a second change-tracking system.
- Switched from duplicated observable collections to snapshot generation with the ChangeSummaryViewModel owning the presentation collection.
- Preserved clean MVVM separation between editing logic and presentation.

### Runtime Testing

Verified:

- Live summary updates.
- Undo integration.
- Redo integration.
- Reset Property integration.
- Save baseline reset.
- Navigation to modified properties.
- Double-click navigation.
- Localized display names.
- Modeless window behavior.
- Window reopening.
- Category grouping.

### UX Improvements

Runtime testing resulted in several usability refinements:

- Replaced internal IDs with localized display names where available.
- Simplified grouping from Category + Setting to Category only.
- Improved window activation after navigation.
- Corrected Change Summary window interaction.
- Corrected Close button behavior.
- Improved overall presentation and readability.

## Major Decisions

- Treat Change Summary as a view of the current modification state, not an editing history.
- Keep modification tracking centralized within PropertyModel.
- Separate presentation concerns into ChangeSummaryViewModel.
- Validate new functionality through live runtime testing before completing the milestone.

## Current Status

The editor now provides a complete workflow for editing, reviewing pending changes, navigating directly to modified properties, and saving with confidence.

## Milestone Achieved

Version 0.5.0 — Change Summary

## Next Focus

Implement Mod Profiles and Change Migration to preserve user modifications across future Wartales updates.

---

# Session 007

## Summary

Completed the architectural foundation for the Snapshot workflow while preparing the editor for Mod Profiles, Change Migration, and future workflow-driven features.

This session focused on strengthening the application's architecture before exposing snapshot functionality through the user interface.

## Completed

### Snapshot Workflow

- Introduced `ModificationSnapshotWorkflowService`.
- Implemented snapshot workflow orchestration.
- Added export workflow.
- Added preview workflow.
- Added import workflow.
- Added snapshot import and export result models.
- Completed the infrastructure for end-to-end snapshot processing.

### User Interface Foundation

- Added a standard application menu.
- Introduced File, Edit, View, Tools, and Help menus.
- Added the Snapshot menu structure.
- Added placeholders for Validation and Developer Tools.

### Dialog Architecture

- Introduced `IFileDialogService`.
- Introduced `IMessageDialogService`.
- Implemented WPF dialog services.
- Removed direct file dialog ownership from `MainViewModel`.

### Dependency Management

- Converted `MainViewModel` to constructor injection.
- Moved service creation responsibility to `MainWindow`.
- Reduced ViewModel coupling to concrete implementations.
- Prepared the application for future dependency injection if desired.

### Runtime Testing

Successfully verified:

- Constructor injection.
- File dialog abstraction.
- Menu integration.
- Open and Save workflows.
- Ctrl+O.
- Ctrl+S.
- Undo.
- Redo.
- Reset Property.
- Change Summary.
- Navigate button.
- Double-click navigation.
- Successful builds after each architectural stage.

## Major Decisions

- Build the Snapshot engine before implementing Mod Profiles.
- Treat snapshot workflows as reusable application services.
- Separate workflow orchestration from presentation.
- Separate dialog services from application logic.
- Continue investing in long-term maintainability before expanding user-facing functionality.

## Current Status

The editor now has a reusable workflow layer capable of supporting snapshot export, preview, import, future Mod Profiles, validation workflows, and additional editing tools.

## Milestone Achieved

Version 0.5.1 — Snapshot Workflow Foundation

## Next Focus

Implement Snapshot UI – Pass 1 by connecting Export Snapshot, Preview Snapshot, and Import Snapshot to the completed workflow infrastructure.