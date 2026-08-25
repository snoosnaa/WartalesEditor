# Architecture

**Version:** 1.0  
**Status:** Active  
**Last Updated:** 2026-07-20  
**Applies To:** Entire Project

---

# Overview

Wartales Editor follows the Model-View-ViewModel (MVVM) architectural pattern.

The architecture is built around reusable infrastructure. New features are expected to compose existing systems rather than introduce parallel implementations.

Core principles:

- Separation of presentation and business logic.
- Preserve the loaded project structure.
- Modify the loaded JSON document directly whenever practical.
- One implementation per major responsibility.
- Transactional operations for structural content creation.
- Validation before persistence.
- Rollback on failed operation validation.

---

# External Game Data Import

QuickBMS integration is an auxiliary transport subsystem. It does not mutate
`ProjectModel` and is not part of the Project Mutation, gameplay operation,
transaction, profile, snapshot, or Gameplay Operation State architectures.

```text
MainViewModel
        │
        ▼
QuickBmsImportService
        ├── WartalesInstallationService
        ├── QuickBmsToolchainService
        ├── ExtractionWorkspaceService
        ├── IExternalProcessRunner
        └── FileFingerprintService
        │
        ▼
Temporary staging validation
        │
        ▼
<Wartales installation>\Extracted\data.cdb
        │
        ▼
JsonDataService.LoadProject (durable file)
        │
        ▼
Normal MainViewModel project promotion
```

Milestone 1 treats live `res.pak` as read-only input. The orchestrator passes
only the absolute script, package, and fresh staging directory to
`quickbms.exe`; no write/reimport flag or batch-file path exists in production.
It requires the script-confirmed `PAK\0` signature, records SHA-256 identities,
verifies the source package after extraction, requires exactly one non-empty
`data.cdb`, and validates it with the production JSON/project-model loader.
Only a validated staging CDB may be copied through the deterministic
`Extracted\data.cdb.importing` path, fingerprint-verified, moved to
`<Wartales installation>\Extracted\data.cdb`, and loaded again from that durable
identity. A failed attempt returns no project, so `MainViewModel` cannot replace
the current project. Per-attempt staging is never reused.

An existing durable CDB requires explicit player confirmation before QuickBMS
runs. The service independently refuses an unapproved replacement, including a
file that appears after the UI check. Promotion failure leaves the active
project unpublished; importing artifacts are removed when safe. The durable
project path enables existing adjacent-file workflows such as Gameplay
Operation State persistence without changing their semantics.

`ExternalProcessRunner` launches QuickBMS suspended through the Windows native
process API, assigns it to an editor-owned Job Object, and only then resumes its
primary thread. Descendants inherit the same job, closing the pre-assignment
spawn race. Normal completion, timeout, and cancellation poll the job's active
process count with a bounded delay; completion is accepted only at zero.
Timeout/cancellation terminates the Job Object before the bounded zero-count
wait. Failure to prove zero contained processes is a separate fatal state and
leaves staging untouched without post-hashing or project promotion. Staging
creation and use validate existing path components and the GUID session against
reparse-point redirection. Cleanup additionally refuses any tree containing a
reparse entry. These repeated checks materially constrain junction replacement;
they do not claim impossible race-proof filesystem security.

Extracted-CDB discovery controls directory descent instead of using unrestricted
recursive enumeration. Reparse directories are skipped, reparse files are
rejected, and the final regular file is independently checked against the exact
session boundary before hashing/loading.

Shared Open/Import promotion prepares reference and localization state without
changing the live services. After preparation succeeds, the prepared services,
file identity, and candidate project are published coherently; publication
failure restores the captured prior service/project state.

Default path composition is isolated in `QuickBmsImportOptions`: it derives the
current user's Desktop QuickBMS folder and the standard Program Files (x86)
Steam Wartales location. Services accept explicit options and a process-runner
abstraction for deterministic testing and later settings UI without embedding
a user-specific absolute path in core logic.

Backup, reimport, candidate package construction, live replacement, Restore
Original Game File, Update Survival, Golden CDB, and external-tool packaging are
outside this architecture milestone.

---

# Core Architectural Subsystems

The editor now consists of eight primary reusable subsystems:

- Editing
- Snapshots
- Profiles
- Validation
- Project Mutation
- Content Creation
- Project Operations
- Transaction Framework

All subsystems operate on the same `ProjectModel` and ultimately modify the same `RootDocument`.

The goal is to extend these subsystems rather than replace them.

---

# Operation Architecture

Content creation is executed exclusively through the Project Operation pipeline.

```text
MainViewModel
        │
        ▼
ProjectOperationService
        │
        ▼
IProjectOperation
        │
        ▼
ContentCreationService
        │
        ▼
ProjectMutationService
        │
        ▼
ProjectMutationResult
        │
        ▼
Operation Validation
        │
   ┌────┴────┐
   ▼         ▼
 Commit   Rollback
```

The UI no longer calls content creation services directly.

---

# Transaction Architecture

Every structural operation executes transactionally.

```text
Capture
    │
    ▼
Execute Operation
    │
    ▼
Operation Validation
    │
┌───┴────────┐
│            │
▼            ▼
Commit   Rollback
```

Rollback is mutation-based rather than rebuilding `ProjectModel`.

This preserves:

- modification tracking
- undo / redo integration
- UI state
- selection state
- future workflow composition

---

# Validation Architecture

Validation consists of two distinct layers.

## Generic Validation

Generic validation verifies project-wide correctness without knowledge of specific Wartales features.

Examples include:

- token compatibility
- serialization
- structural integrity

## Operation Validation

Each content creation operation owns its own validator.

Responsibilities include:

- required objects exist
- required schema exists
- operation completed successfully
- no partial operation remains

Operation validation complements generic validation rather than replacing it.

Validators verify project state.

Validators do not mutate project state.

---

# Structural Property Rules

The architecture distinguishes between:

- modified existing properties
- newly created structural properties
- removed known properties

Existing properties continue to enforce compatible JSON token types.

Structurally created properties are validated through operation validation and generic schema validation instead of original-value token comparison.

Known scalar or array-valued properties may be removed only through
`ProjectMutationService.RemovePropertyByPath`. The path must resolve to exactly
one existing `PropertyModel`, and that model must reference the matching source
`JProperty`. Object-valued properties are rejected explicitly before mutation;
array elements are not addressable by this API. A removal retains the exact
model, source property, parent object, model/source indices, effective path,
and prior `IsModified` state.

Rollback and Undo reattach the original instances at their original positions;
Redo detaches those same instances again. Empty parent objects are preserved.
This capability does not authorize object removal, array-element removal, entry
removal, recursive pruning, or generalized JSON deletion.

---

# Profile Update Integrity

Updating a managed profile reconciles the selected profile's prior snapshot
with the current project by category, stable entry ID, and
`EffectivePropertyPath`. `PropertyModel.IsModified` remains authoritative only
for differences from the live project's current baseline; it is not treated as
a complete replacement for prior profile content after save/baseline
acceptance.

Profile update starts with a fresh capture of the current editing delta, then
reconciles every prior profile record against the current intended live value.
Unchanged prior records are retained, changed prior records keep their stored
historical original and receive the new intended value, and records restored to
their own stored original are removed. New dirty targets come from normal
snapshot capture. Historical structural presence is recorded independently of
the original JSON token, so a present `null` value is never treated as proof of
absence. Legacy null records without that evidence fail safely when a missing
target makes the distinction necessary. Gameplay Operation State compatibility
is observationally refreshed against the current project before state and
additive requests are recaptured through their authoritative services.

An updated candidate is serialized to an isolated sibling file and reloaded
with the production profile loader. Workflow validation does not invoke profile
construction or the high-level reconciliation service. It independently checks
prior-record retention and reversion, current-delta coverage, preserved
historical originals and structural presence, canonical uniqueness, refreshed
Gameplay Operation State, additive requests, and identity metadata. The
managed profile is replaced only after those checks and summary calculation
succeed. Update Profile does not require a golden or pristine CDB reference.

Player-facing change accounting uses distinct effective leaf identity. Updated
and created live leaves count once; supported removal mutations count in apply
feedback; state metadata does not add a second count for represented leaves;
operation-only outcomes may use a synthetic row; and additive operation output
is derived from its authoritative deterministic operation data with overlap
removed. Profiles do not represent arbitrary deletion of historically existing
properties, including properties whose historical value was JSON `null`.
Current authorized removal restores a feature-created, explicitly
absent-baseline scalar leaf to absence through Gameplay Operation State.

---

# Architectural Invariants

The following rules are considered architectural invariants and should not change without a deliberate architectural decision.

- PropertyModel.IsModified remains the single source of truth.
- Rollback is mutation-based.
- Project reconstruction is prohibited.
- ProjectMutationService owns project mutation.
- Feature services orchestrate infrastructure rather than duplicating it.
- Validation verifies results but does not mutate project state.
- Extend existing infrastructure before introducing parallel implementations.
- Preserve verified implementations unless explicitly instructed otherwise.

---

# Gameplay Previous-Value Restoration

Gameplay tools expose one restoration contract: **Restore Previous Values**.
The authority is the compatible pre-tool baseline captured in
`GameplayOperationStateModel.BaselineArray` immediately before the tool first
changes its managed targets. Later settings preserve that baseline. The
`.wtstate` sidecar persists it across save/reload and compatible profile
snapshots transport it between projects.

Restore availability is determined centrally by `GameplayOperationStateService`.
If compatible historical state is absent, restoration remains unavailable;
current live values are not adopted by a restore request. Feature services
execute restoration through their normal `ProjectMutationService` and
transaction paths. Overworld Movement Speed and Rain Frequency use this same
captured authority; their fixed Vanilla values remain ordinary selectable
presets only.

Modeless gameplay dialogs must re-check this authority when Restore executes;
button enablement and dialog-open ViewModel data are presentation only. Random
Trait Exclusions resolves its restore selection from the current compatible
state through `RandomTraitExclusionsService`, then issues its normal Apply
request only after successful resolution. This keeps profile-carried state
replacement authoritative and makes stale-dialog rejection mutation- and
history-free.

Restore Previous Values is an immediate gameplay action. All shared preset,
Party Economy, Random Trait Exclusions, Movement, and Rain dialogs dispatch the
same validated operation path used by ordinary Apply during the Restore click;
ordinary Apply remains available for later manual configuration. RTE effective
accounting uses canonical per-trait `done` identities and exact current-versus-
captured-baseline comparison. Gameplay Operation State presence alone is not an
effective change, while changed leaves remain deduplicated from any synthetic
summary representation.

This contract is distinct from Detailed Editor Reset Property, whose authority
is the current `PropertyModel` baseline. `PropertyModel.IsModified`, save-time
baseline acceptance, profile reconciliation, mutation ownership, and rollback
semantics are unchanged. The editor does not claim universal game-default
restoration and has no Golden CDB dependency.

---

# Verified Architecture

The following has been verified end-to-end:

- Transaction rollback
- Successful operation commit
- Save validation
- Save serialization
- In-game loading
- In-game creation and use of the Anvil
- In-game unlocking of the Blacksmith profession
- Successful creation of new gameplay content
- Upgrade All Equipment
- Nested property mutation infrastructure
- Known-property removal with exact rollback and deterministic Undo / Redo
- Atomic operation history
- Transaction framework integration

---

# Current Architecture Status

The editor platform is considered stable through Version 0.8.x.

Future milestones are expected to extend the existing infrastructure rather than redesign it.

Planned future operations include:

- Additional camp content
- Additional equipment content
- Additional faction equipment recipes
- NPC creation
- Profession creation
- Batch operations
- Future content creation tools

The current architecture should be extended rather than redesigned unless a genuine architectural defect is discovered.
