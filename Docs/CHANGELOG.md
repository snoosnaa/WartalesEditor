# Changelog

All notable changes to Wartales Editor are documented in this file.

The format is inspired by Keep a Changelog and adapted for this project.

---

# Class A Gameplay Expansion

**Status:** Complete; focused compatibility corrections, Resource
Replenishment, UX consistency, Engineering Review, Project Owner runtime smoke,
and final reconciliation verification passed

## Added

- Preset Gameplay Tools for Delicious Meals, Forging Assistance, Mining &
  Woodcutting, Fishing, Lockpicking, Nine Puzzle Assistance, Run Stamina
  Recovery, Battle Camera Zoom, Campfire Expansion, Cooking Pot Food Reduction,
  Workshop Materials, Vendor Refresh, Ruby & Sapphire Value, and Time Between
  Rests.
- A Professions dashboard category and the approved Party, World, and Camp &
  Equipment dashboard entries.
- Feature-specific validator dispatch for every new operation.
- Resource Replenishment presets that scale the captured Slow, Normal, and Fast
  refill categories by 1×, 2×, 3×, or 5× without changing the Extreme factor.

## Changed

- Valour Points now includes Vanilla and Increased Tent Valour tier presets.
- Carrying Capacity now includes Vanilla and Increased Hitching Post base and
  Draught Pony tier bonuses.
- Legacy two-target Valour and Carrying operation states remain valid and are
  upgraded to the expanded state shape on the next explicit safe Apply. Current
  Tent and Hitching Post values are resolved from the project rather than guessed.
- Vanilla preset restoration uses each operation's exact captured baseline.
  Mining and merchant rates scale proportionally from that baseline, while
  Battle Camera preserves the captured minimum distance.
- New snapshots record effective nested property paths. Older snapshots without
  paths retain their original leaf-name matching behavior.
- Gameplay Tool dialogs show in-dialog Applied successfully or Already applied
  feedback, and feature windows explicitly restore their owner after closing.
- Reset to Game Default now applies Vanilla through the shared operation
  pipeline, restoring the exact captured baseline with normal operation state,
  Review Changes, and single-action Undo/Redo behavior.
- Starting Resources and Party Economy clear stale success/no-op feedback when
  their current input becomes invalid.
- Starting Resources, Movement Speed, and Battle Camera Zoom show the approved
  non-blocking display/visual notes.

## Verified

- Zero-warning, zero-error build.
- Clean-CDB apply, validation, forced-failure rollback, exact Undo/Redo,
  idempotence, missing/wrong/duplicate target handling, save/reload state
  persistence, profile serialization, mixed profile replay, and effective
  change counting.
- Repository-backed focused coverage for differing-baseline Vanilla restoration,
  Mining and merchant proportional scaling, Battle Camera baseline drift,
  supported and custom legacy Valour/Carrying upgrades, snapshot full-path and
  legacy matching, all preset catalog entries, and representative malformed
  tier/discriminator/state cases.
- Reset coverage for a scalar preset, baseline-scaled preset, and multi-target
  Campfire preset, including recorded Vanilla state and exact Undo/Redo.
- Symmetric Campfire malformed-target coverage for missing and wrong-type
  `tool.toolCapacity` as well as `tool.capacity`.
- Resource Replenishment baseline capture, proportional outputs, exact
  restoration, no compounding, malformed baselines, atomic rollback, Undo/Redo,
  state persistence, snapshot serialization, profile replay, and preservation of
  unrelated values and `GatherRefillFactorExtreme`.
- A fresh-install, fresh-extraction full-mod gameplay smoke launched, started a
  new game, reached play, saved, exited, relaunched, and loaded the save. The
  earlier freeze is non-reproducible after clean reinstall and fresh extraction;
  its cause remains unknown.

## Non-Blocking Notes

- Campfire implementation/reference equivalence is established. Direct in-game
  Tier 2/Tier 3 assignment-count verification remains pending; Tier 1
  intentionally remains at capacity 4.
- Resource Replenishment is not claimed as exhaustively timed across every land,
  fishing, sea, and special renewable category.
- `stacked content: 2` remains a non-blocking observation from one camp-item
  creation session; its origin was not located by the narrow string search.

---

# Version 0.9.1 - World Convenience

**Status:** Complete and verified

## Added

- Rain Frequency as a direct Gameplay Tools item after Overworld
  Movement Speed.
- Vanilla, Less Rain, Rare Rain, and No Rain presets for ordinary
  regional rain.
- Exact preset detection, Custom and unavailable states, persisted
  gameplay-operation state, and a modeless player-facing dialog.

## Changed

- The twelve approved `region` entries can now update only
  `props.meteo.rainDaysPerMonth` as one atomic operation.
- Each preset is calculated from the entry's verified Vanilla baseline
  of 4 or 6; Rare Rain preserves 1.5 for baseline-6 regions.

## Verified

- Build verification, runtime testing, validation, Save / Reload,
  Undo / Redo, Change Summary, Profiles, Snapshots, and multiple in-game
  verification passes.

## Investigated

- Resource Respawn Speed confirmed shared Slow, Normal, and Fast gather
  refill constants.
- Implementation was deferred pending future runtime validation because
  the shared refill architecture may affect excluded gathering systems.
- No Resource Respawn gameplay feature was added.

---

# Version 0.9.1 - Additive Profile Restoration Repair

**Status:** Development; Visual Studio and in-game verification pending

## Added

- Version 2 Mod Profile operation requests with stable identifiers for
  Add Camp Facilities and Upgrade All Equipment.
- Safe request validation, duplicate rejection, deterministic operation
  resolution, and player-facing gameplay-tool result counts.
- Profile-level mutation aggregation for staged rollback and one-action
  Undo/Redo.

## Changed

- New profiles detect valid applied additive operations from project
  content and filter only deterministic operation-owned snapshot records.
- Profile Manager now displays one effective Changes count representing
  ordinary and additive project modifications without exposing their
  internal storage mechanisms.
- Profile application replays Add Camp Facilities and Upgrade All
  Equipment before applying ordinary snapshot properties.
- Version 1 profiles remain loadable without inferred operation requests.
- Direct Snapshot import remains property-target based.
- Overworld Movement Speed now appears directly in Gameplay Tools without
  a World Convenience submenu.

## Fixed

- Clean-project profiles can recreate camp tool structures, Workshop
  recipes, and missing equipment flags through the verified Project
  Operation pipeline.
- Profile apply now refreshes property tracking after structural replay,
  so newly created equipment and camp PropertyModels appear in the main
  modified count and Change Summary.
- Profile apply now reports one player-facing Changes result instead of
  exposing gameplay-tool and snapshot-property categories.
- Corrected prior documentation wording that did not distinguish profile
  capture coverage from verified clean-project additive restoration.

## Verified

- Zero-warning, zero-error build.
- Disposable model-level harness covering serialization, detection,
  filtering, ordered replay, idempotence, rollback, and Undo/Redo.

## Pending

- Full Profile Manager and result-dialog verification in Visual Studio.
- Save/Reload, Save As, reopening, and legacy-profile UI verification.
- In-game Add Camp Facilities, Upgrade All Equipment, and Overworld
  Movement Speed verification.

---

# Version 0.8.1 - Operation Framework & Verified Content Creation

**Released:** 2026-07-18

## Added

### Project Operation Architecture

-   ProjectOperationService
-   IProjectOperation abstraction
-   ProjectOperationResult
-   Operation execution pipeline
-   UI-facing operation orchestration

### Project Mutation Layer

-   ProjectMutationService enhancements
-   Mutation journaling
-   Rollback record models
-   ProjectMutationResult enhancements
-   Structural creation tracking

### Transaction Framework

-   ProjectOperationTransactionService
-   Automatic rollback on failed operation validation
-   Mutation-based rollback
-   Entry rollback
-   Property rollback
-   Updated-property rollback

### Operation Validation

-   Operation validator provider
-   Operation-specific validation architecture
-   AddCampFacilitiesOperationValidator
-   Separation of generic validation from operation validation

### Content Creation

-   AddCampFacilitiesOperation
-   Integration of ProjectOperationService into the application workflow
-   First reusable content creation operation

## Changed

-   Content creation now executes exclusively through the Project
    Operation pipeline.
-   Rollback no longer depends on rebuilding the project model.
-   Generic token-type validation now distinguishes structurally created
    properties from modified existing properties.
-   Validation architecture remains generic while operation-specific
    rules verify newly created content.
-   MainViewModel now executes operations through
    ProjectOperationService instead of directly invoking content
    creation services.

## Fixed

-   Corrected rollback behavior for created entries, created properties,
    and modified properties.
-   Corrected validation handling for structurally added properties.
-   Eliminated the remaining parallel execution path for Add Camp
    Facilities.
- Recovered ContentCreationService after merge corruption.
- Added reusable object-valued mutation support to ProjectMutationService.
- Updated Add Camp Facilities to use object mutation infrastructure.
- Updated Add Camp Facilities validation for nested property architecture.
- Corrected object mutation handling for props, tool, and icon containers.

## Verified

Successfully verified:

-   Transaction rollback after forced validation failure.
-   Successful operation commit.
-   Save validation after structural content creation.
-   Successful serialization.
-   Successful loading of modified data by Wartales.
-   In-game unlocking of camp recipes.
-   Successful construction and use of the Anvil.
-   Correct unlocking of the Blacksmith profession.
-   End-to-end operation pipeline from editor to gameplay.
-   Successful builds throughout implementation.
- Object-valued mutation infrastructure.
- Nested object mutation rollback.
- Nested object mutation validation.
- Add Camp Facilities idempotence.
- Atomic Undo / Redo after object mutations.
- Save / Reload after object mutations.
- Upgrade All Equipment regression testing.
- Extended in-game verification of Add Camp Facilities.
- Extended in-game verification of Upgrade All Equipment.
- Extended in-game verification of weather modifications.

## Notes

This milestone completed the transition from reconstruction-based structural editing to mutation-based transactional content creation.

During final integration, the object mutation layer was extended to support nested JSON objects while preserving rollback, validation, and atomic operation history.

Following recovery of the ContentCreationService and operation validator, all major content creation features were revalidated through extended in-game testing, confirming stable operation of Add Camp Facilities, Upgrade All Equipment, and gameplay weather modifications.

------------------------------------------------------------------------

# Version 0.8.0 - Validation Framework (Pass 1)

**Released:** 2026-07-17

## Added

### Validation Architecture

- Validation service layer
- Validation workflow orchestration
- Validation pipeline
- Validation rule infrastructure
- Validation issue model
- Validation result model
- Validation severity model
- Validation category model
- Extensible validation rule architecture

### Validation Rules

- Read-only property validation
- Property definition validation
- Numeric range validation
- Reference value validation
- Safe gameplay validation
- Validation based on the currently loaded project

### User Interface

- Validate Project command
- Validation Results window
- Validation summary header
- Severity counters
- Severity filtering
- Empty validation success view
- Validation navigation
- Copy Results
- Re-run Validation

## Changed

- Validation now executes through a reusable workflow architecture rather than being embedded in save operations.
- Save validation now reuses the same validation pipeline used by manual validation.
- Validation Results operates as a single-instance modeless utility window.
- Validation navigation integrates directly with the existing editor selection workflow.
- Validation windows now behave consistently with the Profile Manager and Change Summary architecture.

## Fixed

- Corrected validation window lifecycle management.
- Corrected validation window refresh behavior.
- Corrected duplicate validation window creation.
- Improved independent focus behavior for modeless utility windows.
- Corrected a WPF validation tooltip binding warning that could appear during application shutdown.
- Added a reusable converter for safely displaying the first property validation error.

## Verified

Successfully verified:

- Manual validation
- Save validation
- Validation rule execution
- Validation severity reporting
- Validation filtering
- Validation navigation
- Validation refresh
- Validation clipboard export
- Single-instance window behavior
- Independent modeless window focus
- Undo compatibility
- Redo compatibility
- Profile compatibility
- Snapshot compatibility
- Change Summary compatibility
- Successful builds throughout implementation

# Version 0.7.0 - Complete Profile Manager

**Released:** 2026-07-16

## Added

### Profile Management

- Create Profile
- Rename Profile
- Duplicate Profile
- Apply Profile
- Import Profile
- Export Profile
- Delete Profile
- Complete Profile Manager workflow

### Profile Architecture

- ModProfileModel
- ModProfileMetadataModel
- ModProfileFormat
- ModProfileService
- ModProfileSerializationService
- ModProfileWorkflowService
- ModProfileLibraryService
- ModProfileLibraryPathService
- ModProfileSummaryModel
- Reusable Profile Details dialog
- Unified profile request model

### User Interface

- Profile Manager window
- Profile Browser
- Profile metadata display
- Profile toolbar
- Profile Details dialog
- Profile creation workflow
- Profile rename workflow
- Profile duplication workflow

## Changed

- Mod Profiles now compose the existing Snapshot workflow instead of introducing a parallel implementation.
- Profile application reuses the existing Snapshot Match, Preview, and Apply pipeline.
- Profile creation captures the current modification state using the existing editing infrastructure.
- Profile statistics simplified to display **Modified Properties**.
- Profile Manager usability improved with additional UI polish.
- Improved startup sizing for smaller displays.
- Improved Profile Details dialog layout.
- Improved profile selection behavior after profile operations.

## Fixed

- Corrected Profile Manager selection synchronization after profile operations.
- Corrected Profile Manager selection visibility.
- Corrected Profile Details dialog sizing on smaller displays.
- Improved main window startup sizing across different monitor resolutions.

## Verified

Successfully verified:

- Create Profile
- Rename Profile
- Duplicate Profile
- Export Profile
- Delete Profile
- Import Profile
- Apply Profile
- Undo compatibility
- Redo compatibility
- Change Summary integration
- Modification tracking
- Snapshot application
- Successful builds throughout implementation

---

# Version 0.6.0 - Snapshot UI – Pass 1

**Released:** 2026-07-13

## Added

### Snapshot User Interface

- Export Snapshot
- Preview Snapshot
- Import Snapshot
- Complete end-to-end snapshot workflow
- Snapshot workflow success summaries
- Snapshot preview dialog
- Snapshot import dialog
- Snapshot export dialog

### Workflow Integration

- Snapshot UI connected to `ModificationSnapshotWorkflowService`
- Automatic modification tracking refresh after snapshot import
- Automatic Change Summary refresh after snapshot import
- Seamless integration with the existing editing workflow

## Changed

- Completed the first fully functional snapshot user workflow.
- Snapshot functionality now operates entirely through the reusable workflow infrastructure.
- Snapshot import behaves identically to manual editing, preserving existing application behavior.
- Existing editing architecture reused without introducing duplicate modification tracking.

## Verified

Successfully verified:

- Export Snapshot
- Preview Snapshot
- Import Snapshot
- Modification tracking refresh
- Change Summary refresh
- Undo compatibility
- Redo compatibility
- Save compatibility
- Live Wartales testing
- Successful builds throughout implementation

---

# Version 0.5.1 - Snapshot Workflow Foundation

**Released:** 2026-07-12

## Added

### Snapshot Architecture

- ModificationSnapshotWorkflowService
- Snapshot workflow orchestration
- Snapshot export workflow
- Snapshot preview workflow
- Snapshot import workflow
- Snapshot workflow result models
- Snapshot import result model
- Snapshot export result model

### Dialog Infrastructure

- IFileDialogService
- IMessageDialogService
- WpfFileDialogService
- WpfMessageDialogService

### User Interface

- Standard application menu bar
- File menu
- Edit menu
- View menu
- Tools menu
- Help menu
- Snapshot menu foundation
- Validation menu placeholder
- Developer Tools placeholder

### Architecture

- Constructor injection for MainViewModel services
- Separation of workflow orchestration from UI
- Separation of file dialogs from ViewModel logic
- Separation of message dialogs from ViewModel logic

## Changed

- MainViewModel no longer creates WPF file dialogs directly.
- MainViewModel now receives required services through constructor injection.
- MainWindow now composes application services during ViewModel construction.
- Open and Save operations now use the dialog abstraction layer.
- Editor architecture is prepared for Snapshot UI integration.

## Fixed

- Corrected Change Summary Navigate button command-state updates after introducing explicit command notifications.
- Preserved existing editor behavior after constructor injection refactor.
- Preserved Undo/Redo, Reset Property, Change Summary, and search functionality following dialog abstraction.

## Verified

Successfully verified:

- Constructor injection
- File dialog abstraction
- Message dialog abstraction
- Menu bar integration
- File menu commands
- Edit menu commands
- View menu commands
- Ctrl+O
- Ctrl+S
- Undo
- Redo
- Reset Property
- Change Summary
- Navigate button
- Double-click navigation
- Successful build after refactoring

---

# Version 0.5.0 - Change Summary

**Released:** 2026-07-12

*(No changes to this section.)*

---

# Version 0.4.0 - Safe Editing & Undo/Redo

**Released:** 2026-07-12

*(No changes to this section.)*

---

# Version 0.2.0 - Find Anything

**Released:** 2026-07-11

*(No changes to this section.)*

---

# Version 0.1.0 - First Functional Editor

**Released:** 2026-07-11

*(No changes to this section.)*

---

# Future Releases

Future releases will continue documenting:

- New features
- Architectural improvements
- User interface enhancements
- Bug fixes
- Performance improvements
- Documentation updates
# Version 0.10.0 — UI Polish

- Standardized player-facing terminology around Gameplay Tools, Detailed
  Editor, Profiles, Review Changes, and Check Project.
- Made routine profile maintenance and refresh-backed gameplay successes
  nonblocking while retaining blocking safety and compatibility results.
- Reworked standard errors to lead with player outcomes and retain technical
  details secondarily.
- Added an About experience backed by the authoritative assembly version.
- Kept Snapshot infrastructure internal and unchanged.
- Deferred Search Scope Semantics Correction to a separate pre-1.0 task.
