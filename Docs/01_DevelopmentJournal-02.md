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

Completed the Operation Framework, Transaction Framework, and Verified Content Creation milestone.

This milestone transformed Wartales Editor from a safe editing platform into a reusable transactional content creation platform capable of safely introducing new gameplay content while preserving project integrity.

During final integration, additional work was required to recover object-valued mutation support following merge conflicts introduced while integrating the nested property architecture.

Rather than introducing feature-specific workarounds, the existing Project Mutation infrastructure was extended to support reusable object mutation, preserving the architectural goal of a single mutation pipeline for all future content creation features.

The milestone concluded with successful recovery of the content creation pipeline, validator updates, extended editor testing, and several hours of successful in-game verification.

---

## Completed

### Project Operation Architecture

Completed:

- ProjectOperationService
- IProjectOperation
- ProjectOperationResult
- Unified operation execution pipeline

Operations now execute exclusively through reusable project operations.

---

### Transaction Framework

Completed:

- Transaction orchestration
- Mutation journaling
- Rollback for:
  - created entries
  - created properties
  - modified properties
  - object-valued mutations
- Mutation-based rollback
- Elimination of project reconstruction

Rollback now restores project state entirely through mutation records while preserving modification tracking and editor state.

---

### Project Mutation Layer

Expanded the mutation framework to support nested object mutations.

Implemented:

- Object-valued mutation support
- Nested JObject synchronization
- Reusable object mutation API
- Object mutation rollback
- Nested PropertyModel synchronization
- Preservation of existing JSON object instances
- Preservation of unknown JSON members

This infrastructure is now reusable by future content creation operations.

---

### Validation

Completed:

- Operation-specific validators
- Separation of generic validation from operation validation
- Validation updates for nested property architecture
- Object container validation
- Structural validation refinement

Validation continues to verify project state without mutating project data.

---

### Content Creation

Completed:

- Recovery of ContentCreationService
- Recovery of Add Camp Facilities
- Integration with object mutation infrastructure
- Removal of obsolete object mutation helpers
- Reuse of ProjectMutationService for object containers

The completed implementation now reuses the same mutation pipeline for both scalar and object-valued mutations.

---

## Runtime Testing

Editor verification successfully confirmed:

- Successful project builds
- Object mutation infrastructure
- Nested object mutation
- Transaction rollback
- Validation
- Atomic Undo / Redo
- Save validation
- Save / Reload
- Idempotence
- Upgrade All Equipment regression testing
- Add Camp Facilities regression testing

Extended in-game verification confirmed:

- Successful loading of modified data.
- Stable gameplay over multiple hours.
- Weather modifications operating as expected.
- Add Camp Facilities functioning correctly.
- Successful Anvil construction.
- Successful Apothecary Table construction.
- Blacksmith functionality.
- Alchemy functionality.
- Upgrade All Equipment functioning correctly.
- Save / Reload compatibility.
- No regressions introduced by the recovered mutation architecture.

## Major Decisions

Several architectural decisions made during this milestone will guide future development.

- Continue extending existing infrastructure rather than introducing parallel implementations.
- Extend ProjectMutationService to support reusable object-valued mutation rather than implementing feature-specific object editing.
- Preserve EntryModel.SourceEntry as the authoritative source for existing JSON containers.
- Preserve existing JObject instances whenever object-valued properties are updated.
- Keep PropertyModel.IsModified as the single source of truth for modification state.
- Preserve mutation-based rollback and prohibit project reconstruction.
- Keep operation validation read-only.
- Validate object containers through the underlying JSON document while validating scalar values through PropertyModels.
- Preserve public APIs wherever practical while extending infrastructure.
- Complete extended in-game verification before considering the milestone complete.
- Introduce a structured ChatGPT and Codex development workflow to separate architectural planning from implementation while maintaining a single architectural authority.

The recovery work reinforced the importance of infrastructure-first development.

Rather than repairing individual features independently, the project now possesses reusable object mutation capabilities that future content creation operations can leverage without introducing duplicate implementations.

---

## Milestone Achieved

**Version 0.8.1 — Operation Framework & Verified Content Creation**

Wartales Editor now provides a mature transactional content creation platform capable of safely introducing and validating new gameplay content.

The editor now supports:

- Reusable Project Operations
- Transaction Framework
- Mutation-based rollback
- Nested property architecture
- Object-valued mutation
- Operation-specific validation
- Reusable Content Creation infrastructure
- Safe gameplay modification
- Verified in-game content creation

This milestone concludes the transition from building foundational infrastructure to leveraging that infrastructure for future gameplay features.

---

## Development Workflow

This milestone also established the long-term development workflow for the project.

### ChatGPT

Primary responsibilities:

- Software architecture
- Milestone planning
- Documentation
- Architectural review
- Runtime testing plans
- Long-term roadmap management

### Codex

Primary responsibilities:

- Project implementation
- Complete file generation
- Compilation
- Architectural preservation
- Infrastructure implementation

Every milestone now follows the same verification pipeline:

1. Architectural planning.
2. Codex implementation.
3. Codex compilation.
4. Visual Studio build verification.
5. Runtime testing.
6. Documentation updates.
7. Git commit.
8. Git push.

No milestone is considered complete until successful runtime verification has been performed.

---

## Next Focus

With the transactional content creation platform complete and verified, development now pauses implementation work in favor of a comprehensive roadmap review.

The next milestone will:

- Review every planned feature.
- Remove obsolete roadmap items.
- Group remaining work into logical implementation milestones.
- Identify opportunities to reuse the completed infrastructure.
- Prioritize development leading toward Version 1.0.

Future feature groups are expected to include:

- Additional Content Creation
- Gameplay Tweaks
- Editor Improvements
- Long-Term Platform Features

This roadmap review marks the transition from infrastructure construction to disciplined feature expansion built upon the now-stable architecture.

---

# Session 0.9.1 — Additive Profile Restoration Repair

## Summary

Investigated and repaired the gap between property-target snapshots and
additive content operations. Mod Profile Version 2 now stores explicit
requests for Add Camp Facilities and Upgrade All Equipment. Profile apply
resolves only these approved identities, executes them through the
existing Project Operation pipeline, then applies the filtered ordinary
snapshot and restores gameplay-operation state.

Operation-owned filtering is deliberately narrow. Camp filtering is
limited to Anvil and ApothecaryTable builder-owned values. Equipment
filtering is limited to approved catalog entries whose flag change is
exactly the operation's bitwise-OR output. Unrelated manual changes remain
in the property snapshot.

The combined mutation result uses the existing rollback and operation
history infrastructure. One profile application therefore produces one
Undo/Redo action for additive, ordinary-property, and gameplay-state
changes. A later additive failure rolls back earlier replayed operations
and stops snapshot application.

Version 1 profiles remain loadable with an empty request collection.
Direct Snapshot import was intentionally left unchanged.

The World Convenience menu wrapper was removed. Overworld Movement Speed
is now a direct Gameplay Tools item.

## Verification

- Build succeeded with zero warnings and zero errors.
- Disposable automated verification covered Version 1 compatibility,
  Version 2 round-trip serialization, unknown and duplicate requests,
  all operation-detection combinations, partial-state rejection, ordered
  replay, complete equipment-catalog mutation, craft creation,
  operation-owned filtering, manual-property preservation, idempotence,
  staged rollback, and combined Undo/Redo.
- Visual Studio UI, save/reload, and in-game verification remain pending.

## Profile Tracking and Result Refinement

Visual Studio verification exposed a UI tracking discrepancy after
profile replay. Additive operations created the expected JSON,
PropertyModels, IsModified state, and rollback records, but
MainViewModel retained the clean project's original trackedProperties
set. That set omitted 488 newly created equipment properties and 24
newly created camp properties, producing a displayed count of 61 instead
of 573.

Profile apply now uses the same post-operation tracking refresh as direct
operations. Model-level verification reproduced the old 61 count from
the stale set and confirmed 573 after rescan, with matching Change
Summary, direct/profile JSON and modified paths, one-action Undo/Redo,
idempotent reapply, and additive failure rollback.

The result popup now presents one Changes outcome and no longer exposes
gameplay-tool versus property-change categories.
