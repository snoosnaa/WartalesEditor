# Development Journal – Part 2

**Version:** 0.9\
**Status:** Active\
**Last Updated:** 2026-07-18\
**Applies To:** Entire Project

------------------------------------------------------------------------

# Table of Contents

-   Session 009
-   Session 010
-   Session 011

------------------------------------------------------------------------

This document continues the project history from **DevelopmentJournal.md**, which contains Sessions 001–008.

---

# Session 009

## Summary

Completed the Mod Profile system, transforming the previously completed Snapshot workflow into a complete profile management platform for saving, organizing, and reapplying gameplay modifications.

Rather than introducing parallel editing systems, this milestone extended the existing Snapshot architecture through composition while preserving the established modification tracking pipeline.

The milestone concluded with a complete Profile Manager implementation including profile creation, library management, import/export, application, runtime verification, and final UI polish.

---

## Completed

### Profile Architecture

- Introduced `ModProfileModel`.
- Introduced `ModProfileMetadataModel`.
- Introduced `ModProfileFormat`.
- Established the `.wtprofile` file format.
- Defined the relationship between Mod Profiles and Modification Snapshots.
- Preserved the Snapshot workflow as the underlying implementation.

### Profile Backend

- Implemented `ModProfileService`.
- Implemented `ModProfileSerializationService`.
- Implemented `ModProfileWorkflowService`.
- Implemented `ModProfileLibraryService`.
- Implemented `ModProfileLibraryPathService`.
- Introduced `ModProfileSummaryModel`.
- Added profile creation from the current project state.
- Added profile persistence.
- Added profile loading.
- Added profile deletion.
- Added profile duplication.
- Added profile renaming.
- Added profile import.
- Added profile export.

### Profile Browser

- Added the Profile Manager window.
- Added `ProfileManagerViewModel`.
- Added profile library browsing.
- Added metadata display.
- Added refresh support.
- Added empty library handling.
- Added toolbar integration.
- Added Tools menu integration.
- Implemented a modeless Profile Manager window.
- Ensured only one Profile Manager window can exist at a time.

### Profile Management

- Added Create Profile.
- Added Rename Profile.
- Added Duplicate Profile.
- Added Apply Profile.
- Added Import Profile.
- Added Export Profile.
- Added Delete Profile.
- Added a reusable Profile Details dialog.
- Unified profile operations through a shared request model.
- Preserved the existing Snapshot workflow for profile application.

### User Interface

- Expanded the Profile Manager toolbar.
- Improved window sizing on smaller displays.
- Improved the Profile Details dialog layout.
- Improved profile selection behavior after profile operations.
- Renamed **Profile Contents** to **Profile Change Count**.
- Simplified profile statistics to display only:

  - Modified Properties

### Runtime Testing

Successfully verified:

- Profile creation.
- Profile renaming.
- Profile duplication.
- Profile export.
- Profile deletion.
- Profile import.
- Profile application.
- Undo integration.
- Redo integration.
- Change Summary integration.
- Modification tracking.
- Snapshot application.
- Runtime UI refinements.

---

## Architecture

Throughout this milestone the existing editing pipeline remained unchanged.

Profiles compose the existing Snapshot workflow rather than duplicating it.

The architecture remained:

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

Only one implementation exists for:

- Snapshot Matching
- Snapshot Preview
- Snapshot Application
- Property modification tracking

`PropertyModel.IsModified` remains the single source of truth throughout the application.

---

## Major Decisions

- Build Mod Profiles as composition over the Snapshot workflow.
- Preserve a single implementation of Snapshot Matching, Preview, and Apply.
- Keep `PropertyModel.IsModified` as the only modification state.
- Avoid introducing parallel editing systems.
- Preserve clean MVVM separation throughout the Profile Manager.
- Complete runtime verification before committing the milestone.
- Perform UI polish immediately after runtime testing rather than deferring usability improvements.

---

## Milestone Achieved

**Version 0.7.0 — Complete Profile Manager**

The editor now provides a complete profile management workflow capable of creating, organizing, importing, exporting, duplicating, renaming, deleting, and applying reusable modification profiles.

Profiles integrate seamlessly with:

- Snapshot workflow
- Modification tracking
- Undo / Redo
- Change Summary
- Existing editing workflow

without introducing duplicate implementations.

---

# Session 010

## Summary

Completed the Validation Framework, establishing validation as a reusable subsystem alongside Editing, Snapshots, and Profiles.

This milestone intentionally focused on building reusable infrastructure rather than implementing a large number of validation rules.

Validation now serves as a shared service that future editor capabilities—including Content Creation Tools, Merge Preview, and advanced editing features—can reuse without introducing duplicate validation logic.

---

## Completed

### Validation Architecture

Implemented:

- Validation service layer
- Validation workflow orchestration
- Validation pipeline
- Validation rule infrastructure
- Validation issue model
- Validation result model
- Validation severity model
- Validation category model
- Extensible validator architecture

### Validation Rules

Implemented:

- Read-only property validation
- Property definition validation
- Numeric range validation
- Reference value validation
- Safe gameplay validation

Validation rules intentionally validate only information that can be verified with confidence.

### Validation Workflow

Added:

- Manual project validation
- Save validation
- Shared validation pipeline
- Validation reuse across editor workflows

The Save workflow now composes the Validation Framework instead of performing separate validation logic.

### Validation Results

Implemented:

- Validation Results window
- Severity counters
- Severity filtering
- Empty success view
- Validation navigation
- Copy Results
- Re-run Validation

The Validation Results window follows the same reusable modeless architecture established by the Profile Manager and Change Summary.

### Window Architecture

Refined modeless utility window behavior by:

- Maintaining a single instance of each utility window.
- Refreshing existing windows rather than creating duplicates.
- Allowing independent focus between utility windows and the main editor.
- Standardizing utility window lifecycle management.

---

## Runtime Testing

Successfully verified:

- Manual validation.
- Save validation.
- Validation rule execution.
- Validation filtering.
- Validation navigation.
- Validation refresh.
- Clipboard export.
- Single-instance window behavior.
- Independent window focus.
- Undo compatibility.
- Redo compatibility.
- Snapshot compatibility.
- Profile compatibility.
- Change Summary compatibility.
- Successful builds after every implementation stage.

---

## Architecture

Validation extends the existing architecture without introducing duplicate implementations.

The validation workflow is:

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

The editor continues to maintain exactly one implementation of:

- Property modification tracking
- Snapshot Matching
- Snapshot Preview
- Snapshot Application
- Validation pipeline

---

## Major Decisions

- Build validation as reusable infrastructure before expanding rule coverage.
- Validate only information that can be verified with confidence.
- Reuse the same validation pipeline for manual validation and save validation.
- Maintain modeless utility windows with a consistent lifecycle.
- Preserve existing editor architecture rather than introducing parallel workflows.
- Keep validation independent from editing logic.

---

## UI Improvements Identified

Future UI modernization work was expanded to include:

- Consistent utility window sizing.
- Consistent utility window placement.
- Opening utility windows on the same monitor as the main editor.
- Independent taskbar buttons for utility windows.
- Keyboard shortcuts for utility windows.

These improvements were intentionally deferred to the UI Modernization milestone to keep the Validation milestone focused on reusable infrastructure.

---

## Milestone Achieved

**Version 0.8.0 — Validation Framework**

The editor now provides four major reusable subsystems:

- Editing
- Snapshots
- Profiles
- Validation

This establishes a stable platform for future feature development.

---

## Next Focus

Begin **Version 0.9.0 — Content Creation Tools (Pass 1).**

Development now shifts from building foundational infrastructure to building powerful editor capabilities using the existing architecture.

Every new tool will automatically benefit from:

- Property modification tracking
- Undo / Redo
- Change Summary
- Profiles
- Validation
- Snapshot migration

without requiring additional editing systems.

--------------------------------------------------

# Session 011

## Summary

Completed the Operation Validation & Transaction Framework milestone.

This milestone transformed Wartales Editor from a safe editing platform
into a transactional content creation platform capable of introducing
new gameplay content while preserving project integrity.

Rather than continuing to build individual content creation features
directly, development paused to establish reusable architecture that
every future operation can share.

## Completed

### Project Operation Architecture

-   Introduced ProjectOperationService.
-   Established IProjectOperation as the common operation contract.
-   Added ProjectOperationResult.
-   Unified operation execution.

### Transaction Framework

-   Implemented transaction orchestration.
-   Added mutation journaling.
-   Added rollback for:
    -   created entries
    -   created properties
    -   modified properties
-   Eliminated project reconstruction in favor of mutation-based
    rollback.

### Validation

-   Introduced operation-specific validators.
-   Separated generic validation from operation validation.
-   Refined structural property validation so newly created properties
    are validated appropriately without weakening generic token-type
    validation.

### Content Creation

-   Completed AddCampFacilitiesOperation.
-   Integrated it into the Project Operation pipeline.
-   Removed remaining parallel execution paths.

## Runtime Testing

Verified:

-   Forced validation failure correctly triggered rollback.
-   Rollback restored project cleanliness.
-   Save validation succeeded.
-   Modified CDB loaded successfully in Wartales.
-   Camp recipes unlocked through normal gameplay.
-   Anvil could be constructed and used.
-   Blacksmith profession unlocked correctly.
-   Apothecary functionality verified.
-   Successful builds after every implementation stage.

## Major Decisions

-   Build reusable operation infrastructure before additional content
    creation features.
-   Keep generic validation generic.
-   Validate operation-specific requirements within operation
    validators.
-   Roll back mutations rather than rebuilding ProjectModel.
-   Distinguish structural property creation from property modification.
-   Complete in-game verification before closing the milestone.

## Milestone Achieved

**Operation Validation & Transaction Framework**

The editor now supports safe, reusable, transactional content creation
with automatic rollback and verified in-game operation.

## Next Focus

Begin implementing **Upgrade All Equipment** using the completed
operation framework.

Future operations will reuse:

-   ProjectOperationService
-   ProjectMutationService
-   Transaction rollback
-   Operation validation
-   Generic validation
-   Existing editing infrastructure
