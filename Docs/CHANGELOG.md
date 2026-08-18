# Changelog

All notable changes to Wartales Editor are documented in this file.

The format is inspired by Keep a Changelog and adapted for this project.

---

# Restore Previous Values

**Status:** Implemented, repository-verified, Renewed Focused Engineering
Review passed with non-blocking notes, and Project Owner accepted; final commit
and push pending

## Changed

- Standardized all gameplay reset controls as **Restore Previous Values**.
- Compatible Gameplay Operation State now provides the single captured
  pre-tool authority for the 17 shared preset tools, Party Economy, Random
  Trait Exclusions, Overworld Movement Speed, and Rain Frequency.
- Restore controls remain unavailable when compatible historical state is
  absent, so current configured values are never fabricated as prior history.
- Overworld Movement restoration now returns to captured walk/run values rather
  than fixed 6/11. Vanilla remains an ordinary selectable movement preset.
- Rain restoration now returns to captured regional values rather than fixed
  Vanilla values. The existing regional presets remain selectable.
- Random Trait Exclusions continues to restore exact `true`, `false`, and
  absent `done` baselines.
- Random Trait Exclusions now resolves that baseline from current compatible
  Gameplay Operation State at click time. A stale modeless dialog cannot issue
  Apply after Undo removes its history, and compatible profile/state replacement
  supersedes cached dialog-open baseline data.
- Every gameplay Restore Previous Values button now applies immediately through
  its existing validated operation path. Party Economy fields update and apply
  in the same click; Apply remains available for later manual edits.
- Random Trait Exclusions effective accounting now compares exact current
  `done` presence/value with the captured baseline. Baseline-existing exclusions
  are not changes, and exact restoration removes the synthetic summary row.
- Detailed Editor Reset Property and `PropertyModel.IsModified` are unchanged.

## Verified

- Non-catalog starting values, multiple preset changes, missing-state safety,
  `.wtstate` reload, profile transport, exact trait restoration, Movement and
  Rain captured baselines, and atomic Undo/Redo have permanent compatibility
  coverage.
- Permanent Random Trait Exclusions lifecycle coverage includes open-dialog
  Undo rejection, authoritative state replacement, normal exact restoration,
  direct missing-history safety, and Restore Undo/Redo.
- Consistency coverage includes RTE `0 → 3 → 0 → 3 → 0` accounting across
  Apply, Restore, Undo, and Redo, plus immediate Party Economy restoration for
  Volunteer, Valour, and Carrying Capacity.
- Sequential main/test builds complete with zero warnings and zero errors, the
  focused Restore Previous Values suite passes, and all 25 Class A compatibility
  groups pass.
- Renewed Focused Engineering Review returned **PASS WITH NON-BLOCKING NOTES**.
  The sole note is that automated coverage exercises production
  ViewModel/service/operation paths instead of synthesizing WPF button clicks.
- Project Owner interactive evidence covered multiple gameplay features,
  immediate Positive Random Traits restoration, Party Economy consistency, and
  exact Random Trait Exclusions restoration/accounting. After the final
  corrections, the Project Owner explicitly returned **PASS**.

---

# Random Trait Exclusions

**Status:** Accepted; renewed focused Engineering Review and Project Owner
interactive acceptance passed, with positive runtime evidence

## Added

- A Party Gameplay Tool with searchable Positive and Negative trait checklists,
  Select All, Clear All, Restore Previous Values, Apply, and shared feedback.
- Dynamic candidate discovery for compatible Starting/Recruitment traits; no
  trait identifiers or candidate counts are hard-coded.
- Feature-specific operation state and validation for stable trait ownership,
  exact Boolean/absent baselines, fingerprints, and update compatibility.
- Candidate preflight now requires an explicit nonblank source `id`, exact
  source/model identity, and connected `done` models before mutation begins.

## Changed

- Unchecked traits receive `done=false`; checked traits preserve an eligible
  absent baseline or use `done=true` when explicitly enabling a pre-disabled
  trait. Existing units are unchanged.
- Restore Previous Values uses the approved property-removal primitive to recover
  an originally absent `done` leaf exactly.
- Snapshot/profile application restores deterministic exclusion state before
  ordinary property matching so absent-baseline leaves can be recreated safely
  without a new profile format.
- Review Changes shows a player-facing operation outcome when an exact
  absent-baseline restore has no attached modified property row.
- Review Changes now derives that fallback from the persisted/current Random
  Trait Exclusions state specifically, so unrelated gameplay-state changes do
  not create or count an exclusions outcome.
- Operation validation now requires exact set equality between the requested
  allowed traits, recorded allowed traits, and resolved owned candidates.

## Verified

- Dynamic grouping, true/false/absent baselines, mixed selections, Select All,
  Clear All, exact restore, idempotence, validation rollback, Undo/Redo,
  state persistence, snapshot/profile replay, update expansion, and independent
  Positive Random Traits operation state.
- Disconnected-target atomic preflight, stable source identity, cross-operation
  Review Changes attribution, requested/result mismatch rollback, and complete
  same-file Update Profile replay are covered by permanent smoke tests.
- Direct Snapshot Preview remains read-only and may conservatively report a
  missing absent-baseline leaf; profile application materializes exclusion state
  before matching and is unaffected.
- During an observed Project Owner session lasting more than one hour, no
  recruit received a trait disabled by Random Trait Exclusions. This is positive
  runtime behavioral evidence, not statistical proof that excluded traits can
  never occur.
- Renewed focused Engineering Review passed with non-blocking notes. Project
  Owner interactive acceptance confirmed that the dialog opened, the feature
  applied, exactly five traits were unchecked, and exactly five changes were
  reported.

---

# Property Removal Mutation Primitive

**Status:** Focused Engineering Review passed; approved for feature use

## Added

- `ProjectMutationService.RemovePropertyByPath` for strict removal of a known
  project-model property and its connected source `JProperty`.
- A dedicated removed-property rollback record preserving exact object identity,
  parent, source/model ordering, effective path, and prior modification state.

## Changed

- Project mutation results, transaction rollback/replay, and operation history
  now compose known-property removals with existing create and modify mutations.
- The removal API now explicitly rejects object-valued properties before any
  mutation or rollback record is created.

## Verified

- Nested removal, missing-target failure, exact rollback, repeated Undo / Redo,
  source/model ordering, created-property symmetry, empty-parent preservation,
  and forced validator rollback of modify/create/remove mutations.
- Public-factory object rejection, ambiguous/disconnected targets,
  same-property modify/remove, create/modify/remove, multiple-removal index
  drift, and exact first/last restoration.
- The capability remains limited to known properties. Random Trait Exclusions
  now uses it only for scalar `done` restoration; generalized deletion remains
  unsupported.

---

# Final Feature Batch

**Status:** Accepted and reconciled; implementation, automated verification,
Engineering Review, and Project Owner acceptance complete

## Added

- Lectern Knowledge Gain under Progression, using 1×, 2×, 3×, and 5×
  captured-baseline presets for Lectern rest Knowledge only.
- Positive Random Traits under Party. Positive Only selects the current
  two-positive branch with `0 / 1 / 0` for future eligible procedural units;
  existing units are unchanged.
- Update Profile in Profile Manager for explicitly selected managed profiles.

## Changed

- Updating a profile now reconciles prior records with the current intended
  project by stable effective path, preserving baseline-accepted content while
  replacing changed targets and removing profile-relative restorations.
- Updated candidates are staged and reloaded, then checked by an independent
  invariant validator that does not invoke profile construction or high-level
  reconciliation. It verifies retained history, current delta, reversions,
  canonical uniqueness, metadata, refreshed Gameplay Operation State, and
  additive requests before atomic same-path replacement. This preserves prior
  profile history without requiring a separate pristine CDB. Failed validation
  leaves the prior managed profile unchanged.
- Snapshot properties now record historical structural presence independently
  from their JSON value. Proven absent-to-created-to-absent restoration remains
  supported, historically present `null` deletion is rejected, and ambiguous
  legacy-null deletion fails safely.
- Update Profile observationally revalidates Gameplay Operation State against
  current live targets before capture, preventing stale cached compatibility
  from entering a profile.
- Main/project, Review Changes, profile, and apply-result accounting now share
  effective-leaf semantics for updated and created live properties. Supported
  removal mutations remain counted by apply feedback; arbitrary clean-baseline
  deletion is not a profile capability. Additive output remains deterministic
  and overlap-aware.
- Review Changes resolves nested properties by `EffectivePropertyPath`, so
  duplicate `height` and `width` leaves remain distinct.
- Legacy pathless properties use one shared resolver. Unique matches upgrade to
  canonical paths; ambiguity is surfaced and blocks profile replacement.
- Additive profile filtering now uses canonical effective paths, preserving
  unrelated same-leaf properties under different nested paths.
- Pathless legacy `flags` records are no longer guessed to overlap canonical
  Upgrade All Equipment `props.flags` output.

## Verified

- Preset scaling, exact Vanilla restoration, malformed-target rejection,
  idempotence, Undo/Redo, operation state, snapshots, and profile round trips.
- Managed profile selection/path safety, metadata preservation, baseline-
  accepted reconciliation, current-format rewrite, semantic candidate replay,
  failed-serialization/validation preservation, and no-change update safety.
- Mixed profile apply/save/close-reload/update/replay covers ordinary and nested changes,
  created and removed absent-baseline properties, Random Trait Exclusions,
  Campfire Expansion, Add Camp Facilities, Upgrade All Equipment, duplicate
  nested paths, additive overlap, and unified effective counts.
- Candidate validation independently rejects incomplete and duplicate profiles
  plus injected stale gameplay state after reopening the already modified source
  CDB. Historical absence, present-null deletion, present-null value reversion,
  and legacy-null ambiguity have dedicated coverage. Count regressions cover five added changes,
  same-target replacement, one deliberate reversion, and no-new-change updates;
  the no-validation replacement overload fails before touching managed bytes.
- The Renewed Focused Engineering Review passed with non-blocking notes. Its
  sequential main/test builds completed with zero warnings and zero errors, all
  22 Class A compatibility groups passed, `git diff --check` passed, and no
  profile-update temporary artifacts remained.
- Project Owner testing applied a known non-damaged profile, saved and reloaded
  its CDB, made further changes, and updated the same profile successfully. Its
  effective count increased from 633 to 636 and validation reported no issues.
- Review Changes displayed the correct result; the prior discrepancy involving
  the six distinct Firecamp, FirecampT2, and FirecampT3
  `tool.height`/`tool.width` paths was no longer present.
- The full intended 645-effective-change configuration was applied, saved as a
  new profile, and used to launch and play Wartales for more than one hour
  without obvious instability attributable to the editor configuration.
- Lectern Knowledge Gain and Positive Random Traits were each tested, confirmed
  working, and accepted by the Project Owner. No additional test details are
  inferred from those acceptance statements.
- Random Trait Exclusions passed renewed focused Engineering Review and Project
  Owner interactive acceptance. Its later runtime evidence remains positive.
- The previously damaged approximately 554-change `All Mods.wtprofile` is not
  considered repaired; the accepted 645-change configured state was saved as a
  new profile.

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
- The shared restore action applies the captured baseline through the operation
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
