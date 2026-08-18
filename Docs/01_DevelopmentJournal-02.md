# Development Journal – Part 2

**Version:** 0.9\
**Status:** Active\
**Last Updated:** 2026-08-18\
**Applies To:** Entire Project

------------------------------------------------------------------------

# 2026-08-18 — Final Feature Batch Project Owner Runtime Evidence

The final Update Profile correction passed Renewed Focused Engineering Review
with non-blocking notes. Project Owner testing then applied a known non-damaged
profile, saved and reloaded its CDB, made additional changes, and updated the
same managed profile successfully. Its effective count increased from 633 to
636, validation reported no issues, and Review Changes no longer showed the
prior six-change discrepancy for the distinct Campfire `tool.height` and
`tool.width` paths.

The Project Owner subsequently applied the full intended
645-effective-change configuration, saved a new profile, launched Wartales,
started a game, and played for more than one hour without obvious instability
attributable to the editor configuration. During that session, no recruit
received a trait disabled by Random Trait Exclusions. This is positive observed
runtime evidence, not statistical proof that excluded traits can never occur.

The remaining lifecycle evidence closes the Final Feature Batch. Random Trait
Exclusions passed renewed focused Engineering Review with non-blocking notes,
then passed Project Owner interactive acceptance: its dialog opened, the
feature applied, exactly five traits were unchecked, and exactly five changes
were reported. The Project Owner also explicitly confirmed that Lectern
Knowledge Gain and Positive Random Traits were tested, are working, and are
accepted. No additional test details are inferred from those statements.

The Final Feature Batch is accepted and documentation-reconciled for its final
commit checkpoint. The previously damaged approximately 554-change
`All Mods.wtprofile` is not considered repaired; the configured 645-change
state was saved as a new profile. Reset to Game Default authority remains the
next separate bounded investigation and has not started.

------------------------------------------------------------------------

# Session 012

## Summary

Implemented the final bounded gameplay/profile feature batch without adding
parallel infrastructure. Lectern Knowledge Gain and Positive Random Traits
extend the shared Gameplay Preset pipeline. Update Existing Profile composes
the authoritative profile capture, managed-library boundary validation, and
existing atomic serializer replacement.

## Completed

- Added captured-baseline Lectern Knowledge Gain presets for the single proven
  `GainOnLecternRest` target.
- Added the atomic three-target Positive Random Traits operation with Vanilla
  restoration and the canonical Positive Only `0 / 1 / 0` representation.
- Added explicit-selection Update Profile with confirmation, preserved identity
  metadata, complete recapture, same-path reselection, and in-dialog success.
- Extended compatibility smoke coverage for both gameplay tools and managed
  profile update safety.
- Verified the smoke harness and a zero-warning, zero-error application build.

## Acceptance Status

Engineering implementation is complete. Project Owner interactive editor and
in-game acceptance remain pending. QuickBMS/package replacement work has not
started.

------------------------------------------------------------------------

# Table of Contents

-   Session 009
-   Session 010
-   Session 011
-   Session 012

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

---

# Session 0.9.1 — World Convenience Pass 2

## Rain Frequency

Implemented Rain Frequency through the existing Gameplay Operation,
nested-property mutation, validation, transaction, state persistence,
history, snapshot, and profile architecture. The feature targets only
`props.meteo.rainDaysPerMonth` on the twelve approved region entries.

The four player-facing presets are Vanilla, Less Rain, Rare Rain, and No
Rain. Values are always derived from the verified per-region Vanilla
baseline of 4 or 6, so preset changes do not compound. Rare Rain retains
the required numeric 1.5 value for baseline-6 regions. Preset recognition
requires all twelve current values to match; mixed values remain Custom,
and missing or incompatible targets make the feature unavailable.

The dialog is modeless and appears directly after Overworld Movement
Speed in Gameplay Tools. It uses the existing ownership, duplicate
prevention, centering, focus restoration, and display-failure patterns.

Build verification completed with zero warnings and zero errors.
Disposable model-level verification covered all presets, Custom
detection, exact scope, unrelated-member preservation, atomic
failed-target behavior, safe no-op reapplication, and Undo/Redo.
Visual Studio and Wartales verification remain pending.

## Future Profile Usability

Profile application failures should identify the feature or edit that
could not be applied. Large failure sets should use grouped, expandable
details while the normal result remains concise. This item was recorded
only; it was not implemented in this pass.

---

# Session 0.9.1 — World Convenience Completion

## Summary

Completed and verified Version 0.9.1. The World Convenience milestone
delivered Overworld Movement Speed and Rain Frequency through the mature
Gameplay Operation architecture.

Overworld Movement Speed provides baseline-relative travel presets.
Rain Frequency provides Vanilla, Less Rain, Rare Rain, and No Rain while
preserving regional baseline differences and unrelated weather systems.

## Profile Restoration

Version 2 profiles now store explicit operation requests for Add Camp
Facilities and Upgrade All Equipment. Applying those profiles correctly
recreates additive content before applying ordinary snapshot changes.

Profile Manager presents one effective Changes count. Profile application
refreshes PropertyModel tracking so newly created nested properties
remain visible through modification counts and Change Summary. Gameplay
Tools remain flat rather than exposing internal feature groupings.

## Gameplay Operation State

Gameplay Operation State and its persistence workflow matured across
progression, economy, movement, and weather operations. Actual project
values remain authoritative while compatible state preserves clean
baselines, preset recognition, validation, and non-compounding
reapplication.

## Verification

Version 0.9.1 completed build verification, runtime testing, validation,
Save / Reload, Undo / Redo, Change Summary, Profiles, Snapshots, and
multiple in-game verification passes.

## Resource Respawn Investigation

Investigation confirmed global Slow, Normal, and Fast gather-refill
constants and explicit refill-category assignments in external resource
prefabs. The same architecture also appears on excluded gathering
systems, including fishing and special resources.

Resource Respawn Speed was therefore deferred pending future runtime
validation. No Resource Respawn gameplay feature was added.

## Transition

With the architecture stable and Version 0.9.1 complete, development
shifts toward Profile polish, UI polish, a gameplay roadmap audit, and
bundled gameplay feature development. Related features can now be
planned together while retaining focused implementation and verification
boundaries.
# 2026-08-12 — Version 0.10.0 Pass 5

Implemented the Player First communication pass. Visible terminology now
describes player outcomes, routine refresh-backed successes no longer interrupt
the workflow, compatibility and structural results remain blocking, and raw
technical errors are secondary details. Established project version 0.10.0 as
the assembly-backed About source. Build verification passed; Visual Studio
runtime verification remains pending. Search scope semantics were deliberately
left unchanged for a separate pre-1.0 correction.
# 2026-08-12 — Class A Gameplay Expansion

Implemented fourteen preset-based Gameplay Tools using the existing mutation,
operation-state, transaction, rollback, validation, profile, snapshot, and
Undo/Redo architecture. Expanded Valour Points with Tent tier bonuses and
Carrying Capacity with Hitching Post tier bonuses, including compatible legacy
state validation and upgrade-on-apply behavior.

Added the Professions dashboard category and the approved Party, World, and Camp
& Equipment entries. A clean-CDB smoke suite verified all 16 outcomes, forced
validator rollback, exact Undo/Redo, idempotence, malformed targets, save/reload,
and a 49-change mixed profile replay.

Profile replay exposed an existing ambiguity for nested properties that share a
leaf name. New snapshots now retain the effective property path, while legacy
snapshots continue to use their original name matching.

Engineering Review identified two compatibility defects in captured-baseline
restoration and legacy Party Economy expansion. The focused correction now makes
Vanilla restore the exact captured baseline, scales Mining and merchant rates
from that baseline, and preserves a differing Battle Camera minimum. Legacy
Valour and Carrying states now read current Tent and Hitching Post values instead
of supplying historical defaults; custom values require an explicit supported
preset selection before ownership expands.

A repository-backed compatibility smoke project now verifies differing-baseline
Vanilla restoration, proportional drift cases, safe legacy upgrades and exact
Undo/Redo, full-path and legacy snapshot matching, all preset catalog entries,
and representative malformed targets. Build verification completed with zero
warnings and zero errors. Renewed Engineering Review, interactive Visual Studio,
and in-game acceptance remain pending. Resource Replenishment was not implemented
and remains the next gameplay priority after its focused runtime smoke test.

# 2026-08-15 — Gameplay Corrections and Resource Replenishment

Implemented Resource Replenishment through the shared preset operation path.
The operation captures the current Slow, Normal, and Fast gather-refill values,
requires a finite positive strict ordering, and derives 2×, 3×, and 5× values
from that immutable baseline. Vanilla restores the exact baseline;
`GatherRefillFactorExtreme` remains untouched.

Focused compiled-schema inspection confirmed `item.tool.capacity` as the runtime
Campfire assignment-limit member, while current CDB data also retains the legacy
`toolCapacity` source representation. The preset keeps both synchronized with
the four dimension fields. The approved Tier 1 capacity remains 4; Tier 2 and
Tier 3 output 8 and 12 and still require in-game assignment verification.

Centralized the established modeless feature-window lifecycle so closing a
tracked Gameplay Tool or utility window explicitly restores and focuses its
owner without changing a non-minimized state. Added shared, non-modal in-dialog
Apply feedback for success and no-op results, plus the approved Starting
Resources, Movement Speed, and Battle Camera visual notes.

The compatibility smoke passed Resource baseline scaling, exact restoration,
no compounding, idempotence, malformed inputs, atomic failure, Undo/Redo,
operation-state save/reload, snapshot serialization, profile replay, and
unrelated-value preservation. The same suite verifies coordinated Campfire
dimensions and both capacity fields.

Project-owner evidence records a fresh installation and fresh extraction using
the complete current mod set: launch, new game, playable state, save, full exit,
relaunch, and save load all succeeded. The earlier new-game-load freeze is
non-reproducible after clean reinstall and fresh extraction; its cause was not
identified. One camp-item creation session displayed `stacked content: 2`
without blocking gameplay or creation. A narrow string search did not locate its
origin, so it remains a non-blocking observation with no speculative fix.

# 2026-08-16 — Gameplay Milestone Final Reconciliation

Corrected the shared preset Reset to Game Default handler. The button had only
selected Vanilla in the dialog; it now raises the same operation request as
Apply, restoring the exact captured baseline through the established
transaction, mutation, operation-state, Review Changes, feedback, and Undo/Redo
pipeline. Invalid Starting Resources and Party Economy binding states now clear
stale Applied successfully or Already applied feedback.

Expanded permanent repository-backed compatibility coverage with explicit
Vanilla-state checks for scalar, baseline-scaled, and multi-target presets, and
with symmetric missing/wrong-type `tool.toolCapacity` Campfire cases. Both
malformed cases fail without accepted mutation.

Focused Engineering Review completed with no blocking findings. Project Owner
testing on a completely fresh installation and fresh extraction applied the
complete current mod set and passed launch, new game, playable gameplay, save,
full exit, relaunch, and save reload. The earlier freeze was non-reproducible
after reinstall; its cause was not identified.

Campfire output/reference equivalence is established. Tier 1 intentionally
remains at capacity 4, while direct Tier 2 and Tier 3 assignment-count
verification remains pending and non-blocking. Resource Replenishment is not
claimed as exhaustively timed across every supported category.

The next authorized feature batch is Lectern Knowledge Gain, Update Existing
Profile, and Positive Random Traits. Integrated Import / Install / Restore has
been investigated but not implemented; bounded QuickBMS/package experiments and
Update Survival remain future work.

# 2026-08-17 — Property Removal Mutation Primitive

Extended the existing Project Mutation Layer with strict known-property removal
by effective path. The mutation removes both the resolved `PropertyModel` and
its exact connected source `JProperty`; it rejects missing, ambiguous, or
inconsistent targets rather than silently succeeding.

A dedicated rollback record retains the owning entry, exact model and source
instances, parent object, model/source positions, effective path, and prior
`IsModified` state. Transaction rollback and history replay restore the original
instances at their original positions or detach those same instances again.
The implementation composes with existing create and modify records in one
atomic operation and preserves empty parent objects.

Permanent compatibility coverage verifies nested removal, exact identity and
ordering, repeated Undo / Redo, created-property symmetry, missing-target
failure, and forced validation rollback after modify/create/remove. This is a
narrow architecture capability only: no object, array-element, entry, recursive,
or generalized JSON deletion was introduced. Random Trait Exclusions remains
pending focused Engineering Implementation Review of this primitive.

The first focused review found one blocking boundary gap: a caller could use
the public property-model factory to connect an object-valued property and then
request its removal. The correction now rejects `JTokenType.Object` before a
mutation result or detach operation is created. Permanent tests reproduce that
public-factory path and prove exact no-mutation failure.

The same hardening pass added same-property modify/remove and
create/modify/remove rollback, multiple-removal index-drift cycles, forced
validation rollback, exact first/last restoration, and ambiguous/disconnected
target rejection. Random Trait Exclusions remains blocked pending renewed
focused Engineering Implementation Review.

# 2026-08-17 — Random Trait Exclusions

Implemented the Party-facing Random Trait Exclusions tool using dynamic
Starting/Recruitment trait discovery and localized names where available. The
dialog provides searchable Positive and Negative checklists plus Select All,
Clear All, exact Restore Game Defaults, Apply, Close, shared feedback, and the
corrected modeless owner lifecycle. Existing units are not changed.

The atomic operation captures each owned trait's exact `done=true`,
`done=false`, or absent baseline. Unchecked traits receive `done=false`;
checked absent-baseline traits preserve absence, while checked pre-disabled
traits are explicitly enabled with `done=true`. Exact absent restoration uses
only the approved scalar known-property removal primitive. Gameplay Operation
State retains dirty and ownership truth while the leaf is detached, and Review
Changes presents a player-facing operation outcome when no property row remains.

Snapshot/profile replay now restores this deterministic operation state before
ordinary property matching, allowing a clean compatible project to recreate an
absent-baseline exclusion without changing profile serialization. Existing
Update Profile recapture includes the state through the normal complete-project
snapshot path.

Permanent smoke coverage passes dynamic grouping, pre-disabled and absent
baselines, mixed selections, Select All, Clear All, idempotence, exact restore,
compatibility failures, new-candidate expansion, forced rollback, Undo/Redo,
snapshot/profile replay, and independence from Positive Random Traits.
Interactive Visual Studio and in-game acceptance remain pending. Focused
Engineering Implementation Review is required before acceptance or commit.

# 2026-08-17 — Random Trait Exclusions Focused Corrections

The focused correction pass now resolves and preflights the complete candidate
set before mutation, rejects disconnected `done` models, and requires every
owned candidate to have an explicit nonblank source ID matching its model ID.
Operation validation compares normalized requested and recorded allowed-ID sets
and requires current resolved ownership exactly.

Gameplay Operation State now retains a nonserialized fingerprint of its last
persisted representation. Review Changes uses that authoritative per-operation
comparison for Random Trait Exclusions rather than the project-wide gameplay
dirty flag, preventing unrelated operations from creating or counting a false
fallback outcome. Load and successful save accept the current state fingerprint;
history replacements preserve it.

Permanent tests cover disconnected preflight atomicity, missing/blank/mismatched
source IDs, cross-operation summary attribution, exact validator mismatch
rejection and rollback, and a full same-path managed Update Profile replay with
an absent-baseline trait. Direct Snapshot Preview remains deliberately read-only
and can conservatively report a missing absent-baseline leaf; profile application
materializes exclusion state before matching. Renewed focused Engineering Review
and Project Owner interactive/in-game acceptance remain pending.
