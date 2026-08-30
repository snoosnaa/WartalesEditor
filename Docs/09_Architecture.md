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

# Application Language Data

Wartales export localization is application-level, read-only presentation
state. It is not project state and does not participate in project mutation,
transactions, Undo/Redo, profiles, snapshots, Gameplay Operation State, or
QuickBMS transport.

`LanguageDataService` owns validation, metadata, durable storage, startup load,
and replacement of the single canonical file:

```text
<Documents>\Wartales Editor\Language Data\export.xml
```

The user-selected filename is not authoritative. Validation requires a
well-formed unqualified `cdb` root, `project="Wartales"`, a non-empty embedded
`lang`, a direct sheet, and a non-empty dictionary prepared by the existing
`LocalizationService`. Version, revision, software version, and date remain
diagnostic. The selected source is copied to same-directory temporary storage,
reloaded and revalidated, then atomically promoted. One active canonical copy
is retained.

User-initiated setup and replacement reuse `WartalesInstallationService` and
the default QuickBMS installation path context. Only top-level `export_*.xml`
candidates that pass the same `LanguageDataService` content validation are
preselected. No candidate or installation-detection failure falls back to the
manual picker. The detected game file is source input only and never replaces
the canonical authority.

After promotion, publication has three explicit outcomes. Success retains the
new canonical file and matching active state. A failed publication restores the
prior rollback only after it validates and matches the fingerprint captured
from the prior canonical, then republishes localization prepared from the
restored file. If restoration cannot be proven, localization is cleared and
state becomes invalid rather than combining old in-memory state with new disk
data. Failure to remove obsolete rollback data leaves the coherent new setup
active but is surfaced as a distinct cleanup failure; the next transaction must
remove stale temporary ownership before it can proceed.

`LocalizationService` remains the sole localization-entry parser and preserves
its case-insensitive, global, later-key-wins lookup semantics. Startup failure
clears active localization and falls back to internal IDs without blocking the
application or project loading. Project promotion prepares only project-derived
reference data; it neither reads nor publishes language data.

The Detailed Editor exposes non-blocking setup when data is absent or invalid,
and **Tools → Language Data...** provides quiet replacement. `texts_*.xml` is
outside this subsystem. The application shell itself remains English.

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

Shared Open/Import promotion prepares project-derived reference data without
changing the live service. After preparation succeeds, the prepared references,
file identity, and candidate project are published coherently; publication
failure restores the captured prior reference/project state. Application-level
language data remains independent of promotion.

Default path composition is isolated in `QuickBmsImportOptions`: it derives the
current user's Desktop QuickBMS folder and the standard Program Files (x86)
Steam Wartales location. Services accept explicit options and a process-runner
abstraction for deterministic testing and later settings UI without embedding
a user-specific absolute path in core logic.

## Export Back to Wartales transport boundary

`MainViewModel` owns normal Save/validation and passes only the durable project
path plus `CurrentCdbContentIdentity` to `QuickBmsExportService`. The service
does not serialize or mutate `ProjectModel`. It reads the persisted source once,
proves identity from that exact in-memory snapshot, and copies those same bytes to a marked
`%TEMP%\WartalesEditor\QuickBmsExport\<guid>\Modded\data.cdb` session, validates
the accepted toolchain and live package, and invokes the shared contained runner
with `-w -r -r -f "{}data.cdb"`. Exit code alone is insufficient: a pure parser
must confirm exactly one `data.cdb` reimport.

After write, the package signature is rechecked and a separate contained
read-only `-o -f "{}data.cdb"` extraction populates the session's `Verify`
directory. Success requires exactly one regular extracted CDB whose length and
SHA-256 equal the staged bytes. Safe terminal outcomes remove the owned session;
unproven process termination preserves it for validated next-run reconciliation.
Import and Export share one mutually exclusive operation state. Preparation may
be cancelled; active write and verification cannot be cancelled by the user.
Cleanup status is independent of the primary transport result. Once write may
have started, result capture is separated from player-message presentation;
presentation or final UI cleanup failures cannot rewrite the known transport
outcome or make a pre-write-only unchanged-package claim.

This is intentionally direct live-package transport, not package management.
There is no editor-managed backup/restore, manifest, lineage, provenance or
generation gate, Golden dependency, profile/snapshot integration, or export
state persistence. Recovery is player-managed backup or Steam Verify/reinstall.
The existing read-only Import From Wartales path remains architecturally
separate.

---

# Core Architectural Subsystems

## Golden CDB Reference Boundary

`GoldenCdbService` owns one optional user-designated reference at
`<Documents>\Wartales Editor\Golden CDB\data.cdb`. `GoldenCdbIdentity` is the
canonical `sha256:` identity of the exact stored bytes. It is intentionally
independent of `SourceCdbGenerationIdentity` and
`CurrentCdbContentIdentity`: equality may be informative, but Golden never
establishes provenance, authorizes historical gameplay state, or changes Update
Survival classification.

`JsonDataService.LoadReferenceProject` uses the same exact-byte parsing core as
ordinary loading while excluding adjacent `.wtstate` publication. It hashes and
parses the same byte buffer, builds through `ProjectModelFactory`, requires at
least one modeled sheet, sets only current-content identity, and leaves source
identity and provenance unknown. Golden storage stages and revalidates exact
bytes in the canonical directory, atomically moves or replaces them, verifies
hash and length after promotion, and uses only transient candidate/rollback
siblings. No durable metadata, backup, archive, or source-path authority exists.
Recognized stale transaction siblings must be removed before another publication
begins. A canonical publication remains active when only post-publication cleanup
fails, but the service reports an Available state with an explicit cleanup warning
until the residue is removed; it never reports ordinary clean success.

`GoldenCdbComparisonService` is read-only. It caches only the Golden index under
the exact canonical hash and rebuilds the current index for every explicit
comparison so live unsaved edits participate. Matching is ordinal by unique
sheet name, explicit unique entry source ID, and unique
`EffectivePropertyPath`. Missing sheets/entries aggregate at their own scope;
ID-less, ambiguous, and unsupported records become separate coverage findings.
Indexes retain unresolved keys and scope-wide unsupported-identity state. An
unresolved identity on either side suppresses Missing/New classification and all
descendant comparison for that scope; only proven absence can produce a
difference. Coverage findings therefore never inflate `DifferenceCount`.
Arrays remain one property and distinguish shape from value by the established
gameplay-operation shape fingerprint. Comparison never calls the mutation,
transaction, profile, snapshot, gameplay-state, or compatibility systems.

The canonical path is reserved in the normal destination-based save workflow.
An intentional overwrite invalidates both Golden caches before Save and
reconciles them from the actual canonical file in a guaranteed completion path,
including when CDB publication succeeded but `.wtstate` persistence failed.
Loading Golden uses normal unsaved-change confirmation and
`PromoteLoadedProject`; it receives no special Restore Previous Values authority.
Detached load publication uses the shared failure-atomic path: reference data,
current project/file identity, and history remain or are restored together if
publication fails.

Set Current validates the live `RootDocument` against a fresh sidecar-free parse
of the persisted current file. This catches scalar and structural CDB changes,
including removed properties no longer represented by an attached
`PropertyModel`, while allowing gameplay-operation-state-only changes whose CDB
content is unchanged. Save destination and Golden-overwrite intent are resolved
before ordinary save validation; every final write remains subject to the
unchanged validation workflow.

The Golden window's **Import Current Wartales CDB as Golden** action composes two
existing authorities across an explicit acquisition/publication boundary.
`QuickBmsImportService` remains the one implementation for installation/toolchain
validation, process containment, temporary extraction, discovery, validation, and
fingerprints. Its internal detached acquisition result retains ownership of the
validated workspace until a caller finishes consuming it. Normal `ImportAsync`
adds durable `Extracted\data.cdb` promotion, gameplay-state reconciliation and
`.wtstate` persistence; the main-menu caller then performs unsaved protection and
`PromoteLoadedProject`, publishing references, current file/project, provenance,
history, and ordinary import presentation.

The Golden caller invokes only detached acquisition in the application-controlled
temporary `GoldenImport` root, without abandon-unsaved protection. It never calls
normal Extracted promotion or `PromoteLoadedProject`, and designates the exact
temporary validated CDB with `GoldenCdbService.SetFromFile` before cleanup. It
therefore cannot read, overwrite, fingerprint, timestamp, or persist state beside
an active normal `Extracted\data.cdb`. Golden replacement
retains its separate confirmation and atomic exact-byte publication. Acquisition
cancellation/failure never reaches Golden; a later Golden failure does not open
the acquired CDB or alter the active project. Cleanup is attempted after success,
decline, and designation failure; a failed cleanup is appended to the primary
local/dialog result. Detached sessions carry one temporary ownership marker that
is deleted last with the session. Before a new Golden acquisition, the service
removes only reparse-free GUID children with that exact marker. Unrecognized
content or a session that still cannot be deleted stops refresh before another
GUID is created, preventing silent accumulation. **Load Golden CDB** remains the
only Golden-window action that intentionally uses active-project publication.
Golden identity remains internal and is not displayed in the management window.

## Update Survival Identity and Compatibility

`ProjectModel` owns two production-read-only exact-byte identities.
`SourceCdbGenerationIdentity` identifies the pristine QuickBMS-extracted source
generation and remains stable through editing and Save. It is unknown for an
ordinary Open unless a Version 2 `.wtstate` manifest is bound to the exact
current file bytes. `CurrentCdbContentIdentity` identifies the persisted disk
revision and advances after successful Save. Neither identity participates in
`PropertyModel.IsModified`.

`JsonDataService` reads exact bytes once, hashes that buffer, and parses the
same buffer. `GameplayOperationStatePersistenceService` owns the adjacent
Version 2 manifest, active state, and bounded latest historical state per
operation type. Exact current-content binding and verified source provenance are
separate trust facts: source provenance is verified only when the manifest both
binds to the parsed bytes and contains a valid source identity. A bound
null/invalid source, legacy manifest, content mismatch, malformed manifest, or
unreadable manifest remains unknown provenance. Unknown/untrusted history has
actionable source authority scrubbed and cannot reactivate. Verified history may
reactivate only on exact verified source return plus full target/content
validation. Restore Previous Values additionally requires exact source
provenance plus all existing target, shape, settings, and current-value checks.
Save never turns current edited content into a new source generation.

An active record in a verified manifest is authoritative only when its record
source equals the verified manifest source. A missing, invalid, or contradictory
active-record identity is downgraded to unknown history with its actionable
identity cleared. Legitimate verified cross-generation history retains its
source identity and remains eligible for exact-source revalidation.

QuickBMS validated extraction is the authoritative source boundary. It captures
the prior bound manifest before replacement, promotes the validated candidate,
classifies source-to-source transition, revalidates compatible state, persists
the new manifest, and publishes an observational compatibility report. The
report and its gameplay probes own no mutations and never authorize renamed or
moved targets.

Provenance, content-binding, and gameplay-state trust classification remain
automatic during Open and QuickBMS import. The full report window is a separate
player-invoked workflow: **Tools → Check Compatibility** reassesses the active
in-memory `ProjectModel`, replaces its previous observational report, and opens
or focuses one modeless utility window. Compatible assessments remain available
internally, while normal presentation shows only problematic tool results and
project warnings, or a concise all-clear state. Project publication never opens
the full window automatically.

Profiles Version 3 and snapshots Version 2 carry source generation only as
portable diagnostics and gameplay-state provenance. Current-content identity
is not portable intent. Portable gameplay state becomes active only when the
container format is provenance-aware, root and record source identities are
valid and equal, and the verified target source matches them. Legacy or
inconsistent portable provenance skips gameplay-state transfer while compatible
ordinary properties retain three-way comparison.

`ProjectOperationExecutionContext` owns the live mutation aggregate for every
shipped gameplay operation. Mutations and gameplay-state replacements are
journaled as they succeed. Execution exceptions and validator failures roll
back through `ProjectOperationTransactionService`; a rollback attempt is never
repeated after it succeeds or fails, and rollback failure is fatal. Rollback
remains mutation-based and never reconstructs the project. Content-creation
preflight is observational: Add Camp Facilities resolves its complete unique
item/object/craft scope, including the craft sheet's connected source object and
`lines` array whenever recipe creation is required. Upgrade All Equipment
resolves exactly one entry for every approved catalog ID before the first
mutation.

---

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
