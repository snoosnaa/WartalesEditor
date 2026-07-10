# Development Journal

**Version:** 0.2
**Status:** Active
**Last Updated:** 2026-07-10
**Applies To:** Entire Project

---

# Table of Contents

- Session 001
- Session 002

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

The application successfully opens and parses the original `data.cdb` and displays the list of sheets.

## Next Milestone

Load `EntryModel` objects and display entries for the selected sheet.

---

# Session 002

## Summary

Completed the transition from a sheet browser into a functional three-pane data browser and established the architecture required for property editing.

## Completed

- Added `SelectedEntry` to the ViewModel.
- Completed the MVVM selection pipeline.
- Displayed entries for the selected sheet.
- Added the third property pane.
- Displayed properties for the selected entry.
- Introduced the new `PropertyModel` class.
- Replaced `KeyValuePair<string, object?>` with `PropertyModel`.
- Updated `JsonDataService` to populate `PropertyModel` objects.
- Updated the user interface to bind to `PropertyModel`.
- Successfully built and tested after each incremental change.

## Major Decisions

- Display internal IDs instead of localized names whenever possible.
- Preserve all sheets, including empty sheets, to accurately represent the source CDB.
- Defer quality-of-life improvements until the core editing workflow is complete.
- Use dedicated model classes instead of generic collections to support future expansion.
- Continue making small, verifiable changes with successful builds after each logical step.

## Current Status

The application now provides a complete three-pane browsing experience.

Users can:

- Open a CDB file.
- Browse sheets.
- Browse entries.
- View all properties for the selected entry.

The editor is now transitioning from a data browser into a data editor.

## Ideas Identified During Development

### Version 1.1

- Group empty sheets into a collapsible section.
- Display entry counts beside sheet names.
- Improve the status bar with additional project information.

### Version 1.2

- Generic batch editing operations.
- Property filtering.
- Preview changes before applying batch updates.

### Version 1.3

- Export change sets.
- Import change sets into newer CDB versions.
- Compare CDB files.
- Conflict detection during change migration.
- Preview imported changes before applying them.

## Next Milestone

Replace the read-only property viewer with editable property controls and begin implementing support for saving modified CDB files.