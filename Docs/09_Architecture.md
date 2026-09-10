# Architecture

**Version:** 1.0  
**Status:** Active  
**Last Updated:** 2026-09-10
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

# Portable Package and Launcher Boundary

The accepted portable-package architecture separates the player-facing entry
point from the real WPF application payload:

```text
WartalesEditor.exe          dedicated launcher
App\WartalesEditor.exe     real WPF application
App\...                     intact self-contained application payload
README.pdf
USER-GUIDE.pdf
LICENSE
THIRD-PARTY-NOTICES.txt
CHANGELOG.md
```

The main application remains a self-contained, multi-file, untrimmed
`net10.0-windows` `win-x64` WPF `WinExe` with ReadyToRun disabled. Its SDK
publish payload is preserved under `App\` except for approved public-package
exclusions such as PDBs. Runtime files are not manually relocated.

The separate launcher is a self-contained, trimmed, single-file
`net10.0-windows` `win-x64` `WinExe` with ReadyToRun disabled and no WPF or
WinForms dependency. From `AppContext.BaseDirectory`, it launches only the
absolute `App\WartalesEditor.exe` path with `UseShellExecute = false`, sets the
child working directory to `App\`, forwards each argument through
`ArgumentList`, waits, and returns the child's exit code. It does not search,
elevate, update, repair, migrate, download, or clean files. Startup failure is
reported through the native Windows message box.

Quick Help resolves `USER-GUIDE.pdf` beside the real executable first. Only
when that executable directory is specifically named `App` may it resolve the
guide from the immediate parent package root. Current working directory and
unbounded parent searching are never authorities.

Package construction publishes the main application and launcher separately,
stages the intact application under `App\`, and keeps the five public/legal
documents at root. Before any bounded staging recreation, the exact repository
`output\` authority and descendant path are validated against traversal,
file/directory confusion, reparse-point redirection, and destruction of package
inputs. Validation then proves exact relative-path-set and SHA-256 equivalence
for the staged application payload, proves root-launcher provenance by SHA-256,
and enforces the layout and exclusion rules. Package creation does not imply a
tag, release, or publication.

This boundary changes distribution and startup only. Project mutation,
transactions, validation, profiles, snapshots, gameplay state, QuickBMS, and
all editor behavior remain unchanged.

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

QuickBMS toolchain location is machine-local application configuration.
`QuickBmsLocationService` stores one selected folder outside project, profile,
snapshot, Golden, and Gameplay Operation State authority. Resolution checks a
valid saved folder first, the historical `<Desktop>\quickbms` convention second,
and otherwise returns a safe guided failure. A valid folder must contain readable,
non-empty regular files named `quickbms.exe` and
`Shiro_Games_PAK_script.bms`. `MainViewModel` applies the same resolution to
Import From Wartales, Export Back to Wartales, and detached Golden CDB
acquisition. Services continue accepting explicit options and process-runner
abstractions for deterministic testing without embedding user-specific paths in
portable data.

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
provenance when state crosses a source-generation, rebase, replacement,
historical-reactivation, or portable-transfer boundary. A state captured by an
active operation in the current ordinary-open project chain may instead use its
exact `LocalRestoreContentIdentity` binding, provided current-content binding
and all existing target, shape, settings, and expected-current-value checks
remain valid. Save advances that bounded local binding with the successfully
persisted revision; it never turns current edited content into a new source
generation.

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

# Profile Operation Intent and Update Survival

Formats 4 and 5 separate two authorities that must not be conflated. The
profile root carries `FormatVersion`; each item in `OperationRequests` carries
the canonical `OperationId` and operation-specific `Settings` that represent
portable, source-independent player intent. The requests contain no
restore baseline, source identity, target fingerprint, or Restore Previous
Values authority. Gameplay Operation State is source-bound runtime authority;
as applicable it owns the captured baseline, applied setting, expected-current
fingerprint, target identity and shape, source-generation identity, and local
restore authority.

New profiles use format 5. Format 5 retains the format-4 gameplay and Apply
contract and adds an optional `ImpactManifest` as historical reporting
authority. Recognized stateful gameplay outcomes are captured
as canonical operation requests, including after Save has accepted raw
`PropertyModel.IsModified` values. Operation-owned raw snapshot leaves are
excluded so they cannot compete with semantic replay; unrelated ordinary edits
remain ordinary profile content. Current-source state may be retained for
strictly validated exact-source baseline fidelity, but it is never the portable
authority. No intent is inferred from unsupported raw values alone.

Formats 1–4 remain readable and applicable. Recognized legacy gameplay state is
projected into canonical intent only when it can be validated safely. Explicit
gameplay Create/Update uses the current format; metadata-only editing does not
silently migrate an older profile, and an old restore baseline never becomes
portable authority.

On a structurally compatible new source generation, Profile Apply replays
portable intent through the existing authoritative feature services. Output is
calculated from the new source, a fresh baseline is captured there, and fresh
Gameplay Operation State is bound to that source. Old source-bound state is not
rebound, operation-owned raw leaves do not overwrite replay output, incompatible
semantic intent fails safely, and the complete Profile Apply remains one atomic
Undo/Redo action. On the same source, retained state may seed exact baseline
fidelity only after the normal source, shape, fingerprint, and state validation
passes; stale or malformed state is never trusted.

### Starting Resources intent compatibility

Starting Resources currently captures seven additive values: Krowns, Bread,
Apples, Iron Ore, Wood, Cloth, and Hemp. Historical format-4 requests containing
the original six fields remain valid without a profile-format change. Missing
Hemp normalizes to zero requested extra Hemp; it does not create Hemp when the
destination baseline lacks it or remove/replace Hemp already owned by that
baseline. Changed-source replay derives output from the destination and creates
fresh destination-bound Gameplay Operation State. Exact-source comparison also
normalizes only the missing Hemp field while continuing to reject mismatches in
the original six values.

Hemp uses the existing origin-inventory array mutation. A requested positive
amount creates the standard item object only when absent, while unknown items,
unknown fields, and ordering are preserved. Restore Previous Values reapplies
the captured baseline array, so a Hemp object introduced by the operation is
removed when structural absence was the original state.

### Accepted preset catalog additions

The approved preset sets include Lectern Knowledge Gain at 2×, 3×, 4×, and 5×;
Cooking Pot Food Reduction at 3/6/9, 4/8/12, 5/10/16, and 6/12/18; and Tent
Valour at 1/2/3, 2/3/4, and 3/4/5. These are ordinary additions within the
existing preset and Party Economy architectures; identifiers, state ownership,
validation, transactions, and Restore behavior are unchanged.

### Already-configured intent behavior

When valid intent already matches raw values but compatible state is absent,
Apply creates no synthetic property mutation. It may establish fresh state and
classifies the operation as already configured, making Restore Previous Values
available when the new state provides authority. Matching values plus matching
valid state permit a true no-op while retaining the same semantic result.

### Random Trait Exclusions discovery and replay

Random Trait Exclusions has two data-driven candidate paths. The legacy path
accepts Positive/Negative personality traits in the semantic `Starting` and
`Recruitment` groups. The weighted path accepts a Positive/Negative personality
trait in any structurally resolved group when `recruitWeight` is numeric,
finite, and greater than zero. The paths are unioned by stable canonical trait
ID and ordered deterministically. `recruitWeight` is read-only discovery
metadata; it is not mutated or owned by the operation.

Separators continue to identify containing semantic groups, but fixed physical
`Starting → Hidden → Recruitment → Acquired` ordering is not eligibility
authority. Starting and Recruitment spans are resolved structurally, weighted
candidates may live outside them, and each candidate's containing semantic
group remains part of replay compatibility identity. Every candidate then uses
the same `done` mutation, validation, Gameplay Operation State, rollback,
Restore Previous Values, and atomic Undo/Redo pipeline.

Replay carries an explicit source context. Exact-source and public direct replay
remain strict. Changed-source profile replay may omit a requested historical ID
only when that ID has zero occurrences in the complete destination trait sheet;
the unavailable ID is reported and retained in stored Profile Operation Intent.
Skipped IDs create no destination baseline, state, mutation, or Profile Impact
Manifest leaf.

Before candidate acceptance, replay indexes canonical IDs across the complete
destination trait sheet. More than one occurrence is an ambiguity failure.
Exactly one occurrence must still qualify as a supported candidate and match
saved Positive/Negative polarity and semantic group. Consequently,
present-but-ineligible, polarity drift, group drift, and mixed candidate/
noncandidate duplicates remain strict preflight failures rather than absence.
All-unavailable changed-source replay creates no state or empty history action;
other compatible profile operations may continue within the normal atomic
Profile Apply.

Unavailable replay is a transient operation/presentation result, not persisted
profile authority. `ProfileOperationApplyStatus` preserves its established
numeric values (`Applied` 0, `AlreadyConfigured` 1, `Failed` 2, `Unsupported`
3) and appends `Unavailable` as 4.

## Profile Reconciliation (Formats 4–5)

Profile Create captures canonical intent from compatible authoritative Gameplay
Operation State, excludes owned raw leaves, retains unrelated ordinary edits,
and writes the current format. Update Existing Profile reconciles by stable
`OperationId`:
unchanged intent is preserved, changed authoritative intent replaces it, new
intent is added, and authoritatively restored intent is removed. Historical
intent remains when there is no new authoritative semantic state.

If historical intent exists, current authoritative state is absent, and a
direct edit overlaps an operation-owned raw property, Update fails as ambiguous
before replacement. It never silently discards the raw edit, replaces the
historical intent, or persists conflicting dual authority. Candidate creation,
independent validation, and managed-file replacement remain atomic.

## Effective Profile Change Counting

Profile Manager's primary **Profile Changes** value is historical clean-baseline
impact for one profile gameplay revision. It is the number of distinct
canonical game-data leaves whose profile-produced final value differs from the
exact pristine Wartales source generation against which the authority was
established. Current target state does not redefine it: Apply, Undo/Redo,
Save/reopen, loading a different CDB, a Wartales update, or a changed/disappeared
Golden CDB cannot make an established count collapse toward zero.

Format 5 may carry an optional `ImpactManifest`. The manifest records its own
format and impact-semantics versions, verified source-generation identity,
profile gameplay-content identity, total count, canonical affected-leaf
evidence, evidence fingerprint, and establishment metadata. A leaf is identified
by sheet, stable entry ID, and effective property path, with a `Created`,
`Updated`, or `Removed` mutation kind. The count includes distinct effective
`PropertyModel` leaves only. Operation requests, Gameplay Operation State,
profile metadata, fingerprints, container objects, created entries as separate
units, and state-only operation metadata do not count. Semantic operations such
as Paths, Request Board Rewards, Starting Resources, Party Economy, Random Trait
Exclusions, Add Camp Facilities, and Upgrade All Equipment contribute their
actual affected leaves; one operation is not automatically one change.

The manifest is reporting/presentation authority only. It is never Apply,
mutation, compatibility, Restore Previous Values, or Gameplay Operation State
authority. A valid zero-leaf manifest displays `0 changes`; missing, invalid, or
unestablished authority displays `Unavailable`. Invalid optional authority is
isolated from otherwise valid core profile content. All case-insensitive root
aliases are removed before core deserialization; duplicate aliases invalidate
optional authority, normal consumers receive only a validated manifest or
`null`, and serialization emits at most one canonical `ImpactManifest`.

Establishment requires exact pristine bytes whose SHA-256 equals the verified
source-generation identity. Current persisted pristine source bytes qualify only
after independent verification. The optional Golden CDB qualifies only on the
same exact-hash rule and remains read-only; BuildID, filename, structural
similarity, `OriginalJson`, and in-memory state are not substitutes. No automatic
QuickBMS extraction occurs for count establishment. SHA-256 provides
deterministic identity, binding, and corruption/tamper detection—not authorship,
signing, or trusted authenticity.

A valid manifest belongs to one gameplay-content revision, source generation,
and impact-semantics version. Metadata-only changes preserve it. Gameplay/source
changes rebuild it when an exact baseline and complete evaluation are available;
otherwise a valid Create/Update may continue with stale authority removed and
Profile Changes shown as `Unavailable`. Applying a source-A profile to source B
does not rewrite source-A history; Update Survival independently decides whether
portable intent can apply to B.

Ordinary unavailable baseline, compatibility, provider, I/O, and environment
conditions degrade to no manifest. Evaluator-owned impossible invariants are
distinct engineering failures. Rollback-integrity and fatal CLR failures retain
their established propagation behavior. The target-context observational count
API remains available for accounting where needed, but it is not Profile
Manager's primary historical authority; its cleanup must leave project data,
Gameplay Operation State, Undo/Redo, open dialogs, and pending input unchanged.

## Profile Apply Presentation Synchronization

After a successful Apply, currently open gameplay dialogs refresh from the
authoritative final project. Closed dialogs are not recreated, and a failed or
rolled-back Apply does not refresh dialogs. Current settings, Restore Previous
Values availability, preview, and validation refresh while compatible pending
unapplied input is preserved. Random Trait Exclusions rediscovers membership by
stable trait ID, personality, and semantic group: new candidates receive fresh
defaults, removed candidates disappear, and incompatible reused identities do
not retain stale selections. Array position is never semantic authority.

Semantic completion reporting is grouped by canonical operation identity, not
raw property path or display wording. Different operations with identical text
remain distinct, while one operation produces one player-facing result. Normal
completion text does not expose JSON, fingerprint, replay, snapshot, source-
identity, or mutation-journal terminology.

## Paths Gameplay Tools

Paths Gameplay Tools extend the existing operation architecture without a
parallel mutation path.

**Path Level Requirements** owns only
`constant/PathXpBase/value` and `constant/PathXpNext/value`.
`constant/PathMaxLevel/value` is compatibility context, not mutation authority.
The runtime requirement formula is `PathXpBase + ((level - 1) × PathXpNext)`.
Scaling uses a captured noncompounding baseline, checked arithmetic, nearest
rounding with midpoints away from zero, and a minimum stored value of 1. The
accepted percentages are 100, 80, 60, 40, and 20.

**Path XP Rewards** is represented by four independent operations keyed by the
canonical `PathMight`, `PathTrade`, `PathCrime`, and `PathMystery` identities.
Each owns only `counter/<canonical entry ID>/pathXP` for current non-Outdated
counters whose canonical `path` matches that operation. `MerchAttack`, every
`reward.pathXp` structure, special consequence rewards, `thresholdXp`,
`SpecialisedApprenticeships`, other Paths, and global Path requirements are
outside this ownership boundary. The accepted multipliers are 1 through 5.

The stable format-4 operation IDs are `path-level-requirements`,
`path-xp-rewards-might`, `path-xp-rewards-trade`, `path-xp-rewards-crime`, and
`path-xp-rewards-mystery`. Portable intent stores only the selected percentage
or multiplier. It never stores target lists, source identity, baselines,
fingerprints, restore authority, or localized display names.

Gameplay Operation State remains source-bound. Requirement state captures the
local Base/Next baseline and max-level compatibility context. Each reward state
captures the discovered ordinary membership and original scalar values for one
Path. Same-source membership drift fails safely. Changed-source Update Survival
replay rediscovers current destination authority, uses a fresh destination
baseline, and creates fresh destination-bound state; added valid targets are
included and removed historical targets do not remain portable authority.

Localization is presentation-only. Canonical Path IDs remain semantic
authority, while `LocalizationService` supplies player-facing Path names from
loaded Language Data. Direct localized `name`, `text`, or `title` values take
precedence over structural fallback text, preventing nested progression-rank
structures from replacing the main Path name. Profile, candidate-validation,
and gameplay-state presentation use the same loaded localization authority.

Run Speed storage remains unchanged. `Vanilla` maps to the player-facing
Vanilla preset; persisted `Faster` with values 8/14 displays as **Fast**;
persisted `Fast` with values 9/17 displays as **Faster**; and `VeryFast`
displays as **Very Fast**.

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

Restore authority has two deliberately separate forms. Bounded local-chain
authority binds a newly captured active state to the exact current project
content and may survive Save/reopen even when pristine source provenance is
unknown. It does not establish source authenticity and is scrubbed when state
becomes untrusted history or crosses authoritative source replacement/rebase.
Cross-generation, historical-reactivation, and portable authority continues to
require a verified source-generation identity. Matching target values alone
never promotes arbitrary unknown history to active state.

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
