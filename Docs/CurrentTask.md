# Current Task

## Current Milestone

**Version 0.8.0 Complete — Release Preparation**

---

# Current Status

The project builds successfully.

Version **0.8.0 — Validation Framework** has been fully implemented, runtime tested, verified, and polished.

Current build status:

✅ Build succeeds.

Git commit has **not** yet been created.

The project documentation is currently being updated before creating the Version 0.8.0 release commit.

---

# Recently Completed

## Validation Framework

Implemented:

- Validation pipeline
- Validation workflow orchestration
- Validation rule architecture
- Validation service layer
- Validation issue model
- Validation result model
- Validation severity model
- Validation category model
- Extensible validator architecture

---

## Validation Rules

Implemented:

- Read-only property validation
- Property definition validation
- Numeric range validation
- Reference value validation
- Safe gameplay validation

Validation is intentionally limited to rules that can be verified with confidence.

---

## Validation User Interface

Completed:

- Validate Project command
- Validation Results window
- Validation summary header
- Severity counters
- Severity filtering
- Empty validation success view
- Validation navigation
- Copy Results
- Re-run Validation

The Validation Results window now operates as a reusable, single-instance modeless utility window.

---

## Architecture Improvements

Completed:

- Validation integrated through a reusable workflow
- Save validation now reuses the validation pipeline
- Validation Results lifecycle management
- Shared navigation workflow
- Shared modeless utility window architecture
- Independent utility window focus behavior

No duplicate validation implementations were introduced.

---

## Runtime Verification

Successfully verified:

- Manual validation
- Save validation
- Validation navigation
- Validation filtering
- Validation refresh
- Validation clipboard export
- Single-instance window behavior
- Independent modeless window focus
- Undo compatibility
- Redo compatibility
- Profile compatibility
- Snapshot compatibility
- Change Summary compatibility
- Successful builds after every implementation stage

---

# Current Objective

Complete the remaining release documentation.

Remaining documents:

- DevelopmentJournal.md
- Architecture.md
- ProjectSnapshot.md
- LessonsLearned.md
- DeveloperGuide.md

After documentation is complete:

- Create the Version 0.8.0 Git commit.
- Push to GitHub.

No additional code changes are planned before this release commit.

---

# Next Planned Milestones

Priority order:

## 1. Version 0.9.0 — Content Creation Tools (Pass 1)

Primary objective:

Begin building powerful gameplay editing tools using the completed editor infrastructure.

Initial tools:

- Add Camp Anvil
- Make All Equipment Upgradeable

All tools must automatically integrate with:

- Property modification tracking
- Undo / Redo
- Change Summary
- Snapshots
- Profiles
- Validation

No separate editing systems will be introduced.

---

## 2. UI Modernization

Primary objectives:

- Improved utility window sizing
- Consistent utility window placement
- Open utility windows on the same monitor as the main editor
- Independent taskbar buttons for utility windows
- Keyboard shortcuts for utility windows
- Larger action buttons
- Visual polish

---

## 3. Validation Expansion

Future enhancements:

- Additional validation rules
- Cross-sheet validation
- Duplicate detection
- Advanced gameplay validation
- Exportable validation reports

The existing validation framework will be expanded rather than replaced.

---

## 4. Future Features

Post Version 1.0:

- Community profile sharing
- Merge Preview
- Batch Editing
- In-game profile credits popup
- Byte-for-byte CDB formatting preservation improvements

Steam Workshop support is intentionally excluded because Wartales does not support Steam Workshop.

---

# Architecture Notes

The editor architecture remains stable.

Property editing flow:

```text
PropertyModel
        │
ModifiedChanged
        │
        ▼
MainViewModel
        │
        ▼
Project.IsModified
```

History flow:

```text
PropertyModel
        │
ValueChanged
        │
        ▼
EditHistoryService
        │
        ▼
Undo / Redo
```

Profile workflow:

```text
Profile Manager UI
        │
        ▼
ProfileManagerViewModel
        │
        ▼
ModProfileWorkflowService
        │
        ▼
ModificationSnapshotWorkflowService
        │
        ▼
Match
Preview
Apply
```

Validation workflow:

```text
Validate Project
        │
        ▼
ValidationWorkflowService
        │
        ▼
Validation Pipeline
        │
        ▼
Validation Rules
        │
        ▼
Validation Results
        │
        ▼
Validation Results Window
```

The following remain single implementations throughout the application:

- Property modification tracking
- Snapshot matching
- Snapshot preview
- Snapshot application
- Validation pipeline

`PropertyModel.IsModified` remains the single source of truth for modification state.

---

# Known Minor Issues

No known functional issues.

Future UI modernization will address:

- Consistent utility window sizing
- Consistent utility window placement
- Utility window taskbar behavior
- Utility window keyboard shortcuts

---

# Build Status

✅ Last build successful

---

# Next Session

Finish release documentation.

Create the Version 0.8.0 Git commit.

Push to GitHub.

Begin implementation of **Version 0.9.0 — Content Creation Tools (Pass 1)**.