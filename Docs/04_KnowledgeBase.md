# Knowledge Base

**Version:** 0.2
**Status:** Active
**Last Updated:** 2026-07-10
**Applies To:** Entire Project

---

# Table of Contents

- Architecture
- Data Model
- Parsing
- User Interface
- Design Decisions
- Future Enhancements
- Wartales Notes

---

# Architecture

The editor follows the MVVM (Model-View-ViewModel) pattern.

Current data flow:

Project

↓

Sheets

↓

SelectedSheet

↓

Entries

↓

SelectedEntry

↓

Properties

Selection should always flow downward through this hierarchy.

---

# Data Model

The primary object hierarchy is:

- ProjectModel
- SheetModel
- EntryModel
- PropertyModel

Each model has a single responsibility.

PropertyModel replaces the original use of KeyValuePair and serves as the foundation for future editing features such as:

- Editable values
- Original values
- Change tracking
- Property descriptions
- Data types
- Validation

---

# Parsing

The application parses the extracted Wartales `data.cdb`.

Important principles:

- Preserve unknown data whenever possible.
- Never discard information simply because it is not yet understood.
- Preserve the original structure whenever practical.
- Favor lossless editing over aggressive normalization.

---

# User Interface

The application currently uses a three-pane layout.

Left

Sheets

Center

Entries

Right

Properties

This layout mirrors the hierarchy of the game data and minimizes navigation.

---

# Design Decisions

## Display IDs

Display internal IDs instead of localized names whenever possible.

Reasons:

- IDs remain consistent across languages.
- IDs are more useful for modding.
- IDs match community documentation and discussions.

---

## Empty Sheets

Display empty sheets instead of hiding them.

Reason:

The editor should faithfully represent the original CDB.

Future enhancement:

Group empty sheets into a collapsible section.

---

## Incremental Development

Build the application using small, verifiable changes.

Whenever practical:

- Make one logical change.
- Build.
- Test.
- Continue.

This reduces debugging time and keeps the project stable.

---

## Documentation

Documentation is considered part of the project.

Documentation should be updated at the completion of each milestone before creating a Git commit.

---

# Future Enhancements

Planned improvements include:

- Group empty sheets
- Entry counts
- Search
- Batch editing
- Change migration
- Undo / Redo
- Property descriptions
- Type-aware editors

---

# Wartales Notes

Current observations:

- The extracted CDB contains French localized strings.
- The game can still display English, indicating localization data is stored elsewhere.
- Internal IDs are preferred over localized names.

Future investigation:

Determine where Wartales stores English localization data and how it can be integrated into the editor.