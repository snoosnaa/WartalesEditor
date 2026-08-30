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

## CDB Identity

Golden CDB adds a third, deliberately separate identity:
`GoldenCdbIdentity` is SHA-256 over the exact bytes at
`<Documents>\Wartales Editor\Golden CDB\data.cdb`. It represents the one
reference explicitly designated by the user. It neither proves pristine/vanilla
status nor participates in source provenance, current-content binding, gameplay
state, profiles, snapshots, Restore Previous Values, or Update Compatibility.

Golden references are loaded through the shared exact-byte parsing core without
reading adjacent `.wtstate`. Atomic Set/Replace uses only transient sibling
candidate and rollback files and retains no metadata or archive. The parsed
Golden and comparison index are lazy, hash-keyed caches; every Golden operation
rehashes the canonical file so an external change invalidates stale state.

Comparison uses stable modeled identities only: unique sheet name, explicit
unique entry ID, and unique effective property path. Proven changes are counted
separately from aggregated ambiguous, ID-less, or unsupported coverage. The
comparison index retains unresolved identities so one-sided ambiguity or
unsupported identity suppresses false Missing/New results and descendant
comparison. Arrays
are compared as one property by shape and then deep value. This entire path is
observational and owns no mutation authority.

Set Current uses a fresh sidecar-free parse of the durable source and deep
compares its `RootDocument` with the live project. That content authority detects
structural removals and additions as well as scalar changes without redefining
`PropertyModel.IsModified`; sidecar-only gameplay state remains outside the CDB
comparison. Golden publication cleanup failures retain a coherent canonical
reference but surface an Available-with-cleanup-warning state until recognized
transaction residue is removed.

Golden's SHA-256 identity remains internal authority for exact-byte verification,
cache invalidation, replacement, and comparison; the ordinary management window
does not expose the hash. The import-current-game convenience action does not add
an extraction subsystem: it awaits the existing `MainViewModel` Import From
Wartales orchestration and passes only its successfully promoted durable project
to `GoldenCdbService`. QuickBMS owns acquisition and source provenance; Golden
owns designation and exact-byte storage.

`SourceCdbGenerationIdentity` is the SHA-256 identity of pristine Wartales CDB
bytes established by validated QuickBMS import. `CurrentCdbContentIdentity` is
the SHA-256 identity of the exact persisted project revision. Source identity
is generation authority; current identity is adjacent-manifest binding. Save
changes only the latter.

The Version 2 `.wtstate` file stores both identities, active gameplay state, and
bounded history. A matching current-content binding is necessary but not
sufficient for verified source provenance: the manifest must also contain a
valid source identity. Version 1, mismatched, null-source, malformed, and
unreadable state has unknown provenance. Unknown history has no actionable
source identity and cannot later reactivate; verified history may reactivate
only after exact-source return and full validation.

An active record must agree with the verified manifest source before it can be
treated as verified. Missing, invalid, or contradictory record provenance is
scrubbed when the record is retained as unknown history. This does not affect
legitimate historical records whose source was established by an earlier
verified import transition.

Portable gameplay state additionally requires a current provenance-aware
profile/snapshot, a valid root source identity, the same valid identity on the
embedded record, and a verified matching target project. Ordinary compatible
property changes remain backward compatible when this gameplay-state gate is
not met.

Compatibility probes and Update Compatibility reports are observational. Raw
unknown JSON remains preserved by the authoritative `RootDocument` even when a
sheet or tool target cannot be modeled.

Background provenance and state-trust checks run automatically, but project
publication does not open the full compatibility report. **Check Compatibility**
explicitly rebuilds the report from the active in-memory project. Internal
assessment retains compatible results for diagnostics; the default window
filters them out, reports only issues and warnings, and supplies an all-clear
state when no attention is required. Repeated checks replace prior results, and
a project switch closes the modeless window and discards the old report.

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

- `LocalizationService` owns localization lookup and parsing.
- `LanguageDataService` owns one-time setup and durable application-level state.
- The user may select any valid Wartales export localization XML file.
- Embedded `lang` metadata is authoritative; the source filename is not.
- One canonical copy is stored at
  `<Documents>\Wartales Editor\Language Data\export.xml`.
- The canonical copy loads automatically on future application launches.
- Missing or invalid data falls back nonfatally to internal IDs.
- The Detailed Editor exposes setup when needed; **Tools → Language Data...**
  supports replacement.
- Setup and replacement reuse the existing validated Wartales installation
  context to open the source picker in the game root and preselect a valid
  `export_*.xml` candidate. Detection or discovery failure retains manual
  selection.
- Available state uses the existing green success treatment; unavailable and
  invalid states remain non-success states.
- Replacement recovery restores active state only after the prior canonical
  file is validated and fingerprint-proven. Unrecoverable recovery clears
  localization, while cleanup-only failure retains the coherent new setup and
  reports a warning.
- Localization-aware searching

`texts_*.xml` is not used. Full application localization and simultaneous
multiple-language storage are not implemented.

The editor always treats internal IDs as authoritative.

Localization exists only to improve discoverability.

---

# Design Decisions

## Internal IDs remain authoritative

Display localized Wartales names whenever possible.

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
