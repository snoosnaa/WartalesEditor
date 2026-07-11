# Knowledge Base

**Version:** 0.4
**Status:** Active
**Last Updated:** 2026-07-11
**Applies To:** Entire Project

---

# Table of Contents

- Architecture
- Data Model
- Parsing
- User Interface
- Editing Pipeline
- Find Anything
- Localization
- Design Decisions
- Future Enhancements
- Wartales Notes

---

# Architecture

The editor follows the MVVM (Model-View-ViewModel) pattern.

Current data flow:

```
Project
    ↓
Categories (Sheets)
    ↓
SelectedCategory
    ↓
Settings (Entries)
    ↓
SelectedSetting
    ↓
Properties
    ↓
RootDocument
```

Selection always flows downward through this hierarchy.

Property edits flow upward through the editing pipeline to update the original JSON document.

---

# Data Model

The primary object hierarchy is:

- ProjectModel
- SheetModel
- EntryModel
- PropertyModel
- SearchResultModel

Each model has a single responsibility.

## PropertyModel

Responsible for displaying and editing individual property values.

Each PropertyModel maintains a direct reference to its originating `JProperty`, allowing edits to immediately update the underlying JSON document.

Future responsibilities:

- Original values
- Change tracking
- Property descriptions
- Data types
- Validation
- Specialized editors

## SearchResultModel

Represents a single Find Anything result.

Contains:

- Category
- Setting
- Localized Name
- Display Name
- Matched Property

The model exists to separate navigation data from the user interface.

---

# Parsing

The application parses the extracted Wartales `data.cdb`.

Important principles:

- Preserve unknown data whenever possible.
- Never discard information simply because it is not yet understood.
- Preserve the original structure whenever practical.
- Favor lossless editing over aggressive normalization.

The original JSON document is retained as the project's `RootDocument`, allowing edited values to be written back without reconstructing the file.

---

# User Interface

The application currently uses a three-pane layout.

```
Categories

↓

Settings

↓

Properties
```

The editor intentionally presents gameplay terminology instead of internal implementation names.

| Internal Model | User Interface |
|----------------|----------------|
| Sheet | Category |
| Entry | Setting |
| Property | Property |

Additional usability features include:

- Pane headers
- Find Anything
- Search scope selection
- Hidden empty Categories by default
- Show Empty Categories option
- Status bar

---

# Editing Pipeline

Current editing flow:

```
TextBox

↓

PropertyModel

↓

SourceProperty (JProperty)

↓

RootDocument

↓

SaveProject()

↓

Modified data.cdb
```

This editing pipeline has been verified through successful in-game testing.

---

# Find Anything

Find Anything is the editor's primary navigation system.

Current capabilities:

- Search every Category.
- Search internal IDs.
- Search English display names.
- Search property names.
- Search property values.
- Search multiple fields simultaneously.
- Display combined localized names and internal IDs.
- Navigate directly to matching Categories and Settings.
- Automatically select matching properties.

Find Anything is intended to answer a single question:

> "Where is the thing I want to edit?"

---

# Localization

Localization is intentionally implemented independently from any specific language.

Current implementation:

- LocalizationService
- Import `export_en.xml`
- English display names
- Localization-aware searching

Future implementation:

- Import any `export_<language>.xml`
- Remember selected language
- Gracefully fall back to internal IDs
- Display multiple languages if desired

The editor always treats internal IDs as authoritative.

Localization exists only to improve discoverability.

---

# Design Decisions

## Internal IDs remain authoritative

Display English names whenever possible.

Always preserve internal IDs.

Reasons:

- IDs remain stable.
- IDs match community documentation.
- IDs survive localization changes.
- IDs are required for advanced modding.

---

## Search is Navigation

Search exists to help users locate gameplay data quickly.

Filtering lists is considered a secondary benefit.

The editor should always navigate directly to the selected result.

---

## Incremental Development

Every feature should:

- Build successfully.
- Be tested.
- Be documented.
- Be committed.

---

## Documentation First

Documentation is part of the project.

A feature is not considered complete until the documentation has been updated.

---

# Future Enhancements

## Editing

- Type-aware editors
- Validation
- Change tracking
- Property descriptions
- Specialized editors

## Workflow

- QuickBMS integration
- Backup on Save
- Recent Files
- Save & Exit

## Modding

- Mod Profiles
- Batch editing
- Change migration
- Localization improvements

---

# Wartales Notes

Current observations:

- The extracted `data.cdb` primarily contains internal identifiers.
- English display names are stored separately.
- Internal IDs remain the authoritative identifiers.

Confirmed example:

| Display Name | Internal ID |
|--------------|-------------|
| Rusty Shiv | DaggerStart |
| Barrel Lid | ShieldStart |

The editor now supports searching by either English display names or internal IDs while always preserving the underlying game identifiers.

Future investigation:

- Additional language support.
- Localization fallback behavior.
- Better integration between localization and gameplay data.