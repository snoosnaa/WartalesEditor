# Development Journal

**Version:** 0.1
**Status:** Active
**Last Updated:** 2026-07-10
**Applies To:** Entire Project

---

# Table of Contents

- Session 001

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

Load EntryModel objects and display entries for the selected sheet.