# Development Journal

**Version:** 0.4
**Status:** Active
**Last Updated:** 2026-07-11
**Applies To:** Entire Project

---

# Table of Contents

- Session 001
- Session 002
- Session 003
- Session 004

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

## Milestone Achieved

Project foundation completed.

## Next Focus

Display entries and build the three-pane editor.

---

# Session 002

## Summary

Completed the transition from a sheet browser into a functional three-pane data browser.

## Completed

- Added `SelectedEntry` to the ViewModel.
- Completed the MVVM selection pipeline.
- Displayed entries for the selected Category.
- Added the third Properties pane.
- Displayed properties for the selected Setting.
- Introduced `PropertyModel`.
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

The application now provides a complete three-pane browsing experience.

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

Completed **Find Anything v1**, transforming search from a simple filter into a global navigation system.

## Completed

### Search

- Introduced `SearchService`.
- Introduced `SearchResultModel`.
- Added a dedicated Find Anything results panel.
- Added global searching across all Categories.
- Added searching by:
  - Internal IDs
  - English localized names
  - Property names
  - Property values
- Added automatic navigation to matching Categories and Settings.
- Added automatic property selection for matched properties.
- Added result count.
- Added automatic hiding of the results panel when no search is active.

### Localization

- Introduced `LocalizationService`.
- Added support for importing `export_en.xml`.
- Added localization-aware searching.
- Combined English display names with internal IDs throughout the search results.
- Established the architecture for supporting additional game languages in the future.

### User Interface

- Simplified the search results layout.
- Replaced separate Setting and Localized Name columns with a single Name column.
- Renamed **Search Results** to **Find Anything**.
- Improved navigation consistency throughout the editor.

## Major Decisions

- Treat search primarily as a navigation tool rather than a filtering tool.
- Display English names whenever available while always preserving the internal ID.
- Keep internal navigation data separate from what is displayed to the user.
- Build localization support in a language-independent way while initially supporting English.

## Current Status

Users can now locate virtually any editable gameplay element using the information they already know, regardless of where it exists within the game's data.

## Milestone Achieved

Find Anything v1 completed.

## Next Focus

Begin **Edit Anything** by introducing:

- Type-aware property editors.
- Validation.
- Change tracking.
- Improved editing safety.