# Current Task

## Current Milestone

Public Release Preparation

## Current Status

**Public Release Preparation is IN PROGRESS.** Phase 1 and Phase 2 are complete.
The TyTech Green accent investigation and implementation completed, and
Engineering Review passed after the required corrections. Project Owner visual
acceptance then failed, the Project Owner rejected the color update, and the
implementation was fully rolled back to the pre-experiment UI. No further color
work is planned before Version 1.0.0. The rejected experiment lifecycle is
closed, with production and tests restored to their accepted baseline. The
separately approved Wartales Modding Community credit remains in both
authoritative public Markdown documents as a goodwill acknowledgement, not legal
attribution. The color experiment no longer blocks release preparation. Both
public PDFs were regenerated and visually validated. RC1 was rejected before
ZIP/checksum creation because its application DLL retained the local build-
machine PDB path in CodeView metadata. The accepted Release-only `PathMap`
correction maps project paths to `/_/`; its review, commit, and push lifecycle
is **CLOSED**.

RC2 was generated from commit
`4bd349834585075bf5a02bdce841e4b31d47a751`. Repository regression, publish,
path-privacy, package, Defender, extracted-package smoke, and clean-machine
validation all passed. The immutable ZIP SHA-256 is
`4607F2E1F33F17CCC4FA77ADACF07904B07D7622A72E0EE6AE3D3F976622F26E`.
Final publication-readiness review nevertheless found that the packaged
`README.pdf` still contains the unresolved “Validated Wartales build: to be
recorded” placeholder, while the final release notes do not record the exact
validated Wartales, QuickBMS, and Shiro script versions promised by the public
documentation. RC2 therefore remains validated evidence but is not publishable.
No tag or GitHub Release exists. Nexus remains unauthorized and pending a
separate Project Owner decision.

The shared non-default Wartales installation-location correction is now
implemented and locally verified. Import, Export Back to Wartales, Golden CDB
convenience import, and Language Data automatic discovery use one resolver with
saved player choice, local Steam-library discovery, legacy-path compatibility,
and manual folder fallback. The initial Engineering Review findings have been
corrected and locally verified. The first renewed review found one duplicate
Golden installation-resolution error presentation; operation-level error
ownership and deterministic regression coverage were corrected, and the renewed
Focused Engineering Review passed. Project Owner runtime acceptance also passed
on the secondary-drive Steam installation: automatic Wartales discovery, Import,
the QuickBMS prerequisite flow, User Guide access, and Language Data all worked.
The final QuickBMS-missing message now points players to the User Guide for
detailed setup instructions. Wartales Installation Resolution is closed:
implemented, reviewed, Project Owner accepted, committed, and pushed. RC2 is
superseded for publication, and no fresh candidate has been generated.

The bounded pre-release Save feedback and Run Speed presentation corrections
are implemented and repository-verified. The normal Save command now presents
`Saving…` through the existing status area, allows one render-priority dispatcher
turn before the unchanged synchronous persistence pipeline begins, and rejects
reentrant Save execution until the operation finishes. Internal saves used by
unsaved-change and Export workflows retain their established synchronous
contract. Run Speed retains the persisted `Vanilla`, `Faster`, `Fast`, and
`VeryFast` identifiers and exact 6/11, 8/14, 9/17, and 12/22 values while the
player-facing progression is now Vanilla, Fast, Faster, Very Fast with distinct
descriptions. Focused Engineering Review passed and Project Owner interactive
acceptance passed for both corrections. Save Feedback and Run Speed Presentation
are closed and ready for their commit checkpoint.

Quick Help V1 completed its full engineering lifecycle, Focused Engineering
Review (**PASS**), Project Owner Interactive Acceptance (**PASS**), footer
correction, brief visual re-test (**PASS**), reconciliation, and final
commit/push checkpoint. Quick Help V1 is **CLOSED**. See
`PublicReleasePreparation.md` for the release model and remaining work.

## Most Recently Closed Batch

Quick Help V1 adds one always-visible main-screen action and an owned, modeless,
single-instance reference window with Import, Gameplay Tools, Profiles, Restore
Previous Values, and Export tabs. One footer reminder points to the packaged
`USER-GUIDE.pdf`; Open User Guide resolves that local file beside the executable
through Windows shell association. The feature has no project dependency,
persistence, first-run behavior, or network behavior. The final focused suite
passes 41 checks, and the existing Export/MainWindow suite passes 202/202.

## Previous Closed Batch

Request Board Rewards V1 and the shared Restore Previous Values authority
correction are complete, Engineering reviewed with non-blocking hardening notes,
Project Owner accepted, finally verified, committed, pushed, and closed.

Request Board Rewards exposes 100%, 150%, 200%, and 300% captured-baseline
presets for the shared Min/Max base Krown reward arrays. Its real-game acceptance
launched Wartales, started a new game, showed changed Request Board rewards,
completed a battle and save, and succeeded after full exit/reload. Profiles store
percentage intent and derive output from the destination project's own baseline.

The systemic Restore defect came from shared provenance authority, not Request
Board Rewards. `LocalRestoreContentIdentity` now provides bounded same-project
authority for ordinary-open Apply, Save, and Save/reopen workflows. It does not
certify pristine source data and is not portable. Rebase, imported history,
Update Survival, and cross-generation restoration still require verified source
authority. Focused verification passes Request Board 101, Golden 199, Export
202, Update Survival 180, QuickBMS Import, Language Data, focused Restore, and
all 25 Class A groups with zero build warnings or errors.

## Prior Closed Milestone

QuickBMS Export Back to Wartales Version 1

## Previous Active Status

**The complete QuickBMS Export and Golden acquisition milestone batch is closed.**
QuickBMS Export Back to Wartales Version 1 passed Engineering Review and Project
Owner live-package acceptance. The bounded MainWindow maximize cleanup passed
focused review and Project Owner visual acceptance. Golden local status, detached
acquisition-only behavior, exact same-path active-project preservation, and stale
temporary-session reconciliation passed Final Narrow Engineering Review and the
Project Owner brief re-test. The accepted direct Export workflow is Edit/Load → normal
Save when required → one exact persisted-byte snapshot for identity and staging
→ warning/confirmation → contained filtered QuickBMS
reimport → read-only re-extraction → exact byte verification → safe cleanup.
Known transport results cannot be rewritten by result-presentation or completed-
progress observer failures, including when cleanup also fails. Partial workspace-
creation cleanup failures preserve their retained path and warning independently.
Deterministic coverage now exercises owner-resolution failure, the actual
MainWindow close/retry chain, and complete application-state neutrality for
verified success and verification failure. Project Owner live-package acceptance
passed. The cleanup removes persistent work-area `MaxWidth`/`MaxHeight`
constraints that undersized the native maximized window; Export behavior is
unchanged.

Version 1 intentionally has no automatic backup/restore, manifest, lineage,
provenance or generation gate, Golden coupling, or project-state mutation. The
recovery model is an optional player-made `res.pak` copy plus Steam Verify
Integrity of Game Files or reinstall when necessary.

Previous milestone:

Golden CDB Version 1

Previous milestone status:

**CLOSED.** The Final Renewed Focused Engineering Review returned **PASS WITH
NON-BLOCKING NOTES**. Project Owner functionality testing, the two requested
acceptance corrections, and the brief Project Owner re-test passed. Engineering
and Project Owner acceptance, final behavioral coverage, and documentation
reconciliation are complete.

Golden is the one exact CDB file the user explicitly designates as their
reference. It is stored at
`<Documents>\Wartales Editor\Golden CDB\data.cdb`, identified by exact-byte
SHA-256, and managed through **Tools → Golden CDB...**. The editor validates
structural usability but does not certify vanilla, pristine, Steam-verified, or
current-game status. Set/Replace is atomic with transient same-directory recovery
only; Remove creates no archive; no metadata companion exists.

Golden reference loading uses the shared exact-byte parser without loading an
adjacent `.wtstate`, publishing source provenance, or installing gameplay state.
Load Golden uses normal unsaved-change and project-publication behavior. The
reserved canonical path is protected for both a loaded Golden project and an
explicit destination selection, with Save Anyway, Choose Another Location, and
Cancel outcomes. An intentional overwrite invalidates and reconciles caches from
the actual canonical bytes, including a CDB-committed/state-persistence-failed
save.

Compare Current to Golden is explicit, live, observational, and difference-only.
Stable matching uses unique sheet names, explicit unique entry IDs, and unique
effective property paths. ID-less, ambiguous, and unsupported structures are
aggregated as coverage limitations rather than counted as proven differences;
unresolved identities suppress false Missing/New and descendant comparisons.
Golden owns no project mutation, Undo, profile, snapshot, gameplay-state,
Update Survival, Restore Previous Values, or Check Compatibility authority.

Set Current now deep-compares live CDB content with a fresh sidecar-free persisted
parse, detecting structural removals even when gameplay state is also modified
while allowing genuine state-only changes. Cleanup failures surface a warning and
recognized stale transaction artifacts are cleared before the next publication.
Load publication rollback has deterministic coverage, and all Golden destination
intent is resolved before ordinary save validation.

The current-game import convenience action reuses the authoritative QuickBMS
toolchain, process-containment, staging, discovery, fingerprint, and validation
mechanics through a detached temporary `GoldenImport` workspace. It designates
that validated file through `GoldenCdbService.SetFromFile`, then cleans the
workspace. It never publishes to normal `Extracted\data.cdb`, reads or writes its
`.wtstate`, or publishes an active project. Normal **Import From Wartales** still
owns durable Extracted publication and active-project replacement; **Load Golden
CDB** remains the explicit Golden action that opens it. No write-back or deploy
behavior exists.

Final verification: all Release builds complete with zero warnings and zero
errors; Golden passes 199 checks, Export passes 202 checks, Update Survival passes
180 checks, focused QuickBMS Import and Language Data pass, and all 25 Class A
groups pass. Marked stale detached sessions are reconciled before a new Golden
session; unrecognized, unsafe, reparse-containing, or still-locked content blocks
refresh without creating another GUID. The
final regressions invoke the original Import From Wartales command, accepted
existing-Golden replacement through the live Golden window event, and three
close/reopen/import cycles with no stale callback.

The earlier deferred classification of Golden-window-local operation feedback was
superseded by Project Owner acceptance direction. The owned Golden ViewModel now
retains one ephemeral local progress/result message until the next Golden action;
success, cancellation, split import/designation outcomes, failures, comparison,
and cleanup warnings appear in the Golden window. Detached cleanup warnings are
preserved after success, replacement decline, and designation failure. Closing and reopening creates a
fresh ViewModel with no stale result. Technical identity/hash remains hidden.
The final detached-temp cleanup correction passed Final Narrow Engineering Review
with no findings, and the Project Owner brief re-test passed. Golden CDB Version 1
is fully closed.

Earlier milestone:

Update Survival

Earlier milestone status:

Complete, reviewed, accepted, and reconciled. The final Renewed Focused
Engineering Review returned **PASS WITH NON-BLOCKING NOTES**. Project Owner
Interactive Acceptance and renewed acceptance after the compatibility UX
corrections both returned **PASS**.

The implementation uses two exact-byte identities. The pristine QuickBMS
candidate establishes `SourceCdbGenerationIdentity`; ordinary Save never
changes it. `CurrentCdbContentIdentity` binds the adjacent Version 2 `.wtstate`
to the exact persisted revision and advances only after successful Save.
Ordinary Open does not infer pristine provenance from current file bytes.

Version 2 state separates exact current-content binding from verified source
provenance. Unknown, legacy, mismatched, malformed, and unreadable prior state
cannot supply Restore authority; unknown history has actionable identity
scrubbed, while verified history may reactivate only after exact-source return
and full validation. Profile Version 3 and snapshot Version 2 require trusted,
agreeing root, record, and target source provenance before portable gameplay
state activates, without changing ordinary three-way apply behavior. QuickBMS
distinguishes verified, unknown-source, malformed, unreadable, and absent prior
state. Background compatibility evidence remains available without opening a
window. **Tools → Check Compatibility** explicitly reassesses the current
in-memory project, replaces the prior report, and opens or focuses one modeless
issue-focused window.

Every shipped gameplay operation now executes with an explicit live mutation
journal. Successful project and gameplay-state mutations are recorded before
later work can fail; execution exceptions and validator failures roll back the
same aggregate. Rollback is attempted at most once; rollback failure is reported
as fatal. Add Camp Facilities performs complete target/object/craft preflight,
including recipe-creation source and `lines` validation. Upgrade All Equipment
resolves exactly one entry per approved catalog ID. Source-inconsistent active
records are scrubbed into unknown history and cannot later reactivate.
Unknown raw JSON remains authoritative in `RootDocument`.

The full report does not auto-open during ordinary Open, QuickBMS publication,
changed-generation publication, or unknown-provenance publication. Compatible
rows are hidden by default and a concise all-clear is shown when appropriate.
The window follows the working owned, taskbar-visible modeless utility pattern.
Project Owner testing confirmed normal minimize/restore, close/reopen/re-run,
and Restore Previous Values across multiple gameplay features, including after
closing and reopening a feature window in the same project session.

Verification: application and both test projects build with zero warnings and
zero errors; 180 focused Update Survival checks, all 25 Class A groups, and the
QuickBMS and Language Data suites pass. Final Project Owner acceptance is
**PASS**. Golden CDB subsequently began as a separate milestone.

Previous milestone:

Generic Wartales Language Data Setup

Previous milestone status:

Complete, reviewed, accepted, and reconciled. The Renewed Focused Engineering
Review returned **PASS WITH NON-BLOCKING NOTES**. Project Owner Interactive
Acceptance and renewed acceptance after the final source-selection and success-
styling corrections both returned **PASS**. The owner's final result was,
“Pass. Both work well.”

`LanguageDataService` owns a single application-level canonical file at
`<Documents>\Wartales Editor\Language Data\export.xml`. Any valid Wartales export
localization XML may be selected; embedded non-empty `lang` metadata is
authoritative. Setup and replacement validate both the selected source and the
stored temporary candidate before atomic promotion. A late publication failure
either restores and revalidates the exact prior canonical bytes before restoring
active state, or clears localization and reports an explicit recovery failure.
Cleanup failures are reported without rolling back an otherwise coherent new
setup. The original source file is not required after setup.

Startup loads valid canonical data automatically. Missing or invalid canonical
data clears active localization and falls back nonfatally to internal IDs. The
Detailed Editor presents a compact setup action only when needed, and
**Tools → Language Data...** provides status and replacement. Project Open and
Import From Wartales no longer prepare localization during project promotion.
Both setup entry points reuse the existing validated Wartales installation
context, preselect a valid language-agnostic `export_*.xml` candidate when one
is available, and retain the manual picker fallback. Ready state uses the
existing green success treatment; missing and invalid states do not.

`texts_*.xml`, full application localization, QuickBMS follow-on work, Update
Survival, and all project/gameplay mutation state are outside this feature.

Previous milestone status:

QuickBMS Milestone 1 is complete and accepted. The Renewed Focused Engineering
Review returned **PASS WITH NON-BLOCKING NOTES — Ready for Project Owner
Interactive Acceptance**, and Project Owner Interactive Acceptance returned
**PASS**.

The durable extracted-CDB correction is implemented and repository-verified.
**Import From
Wartales** validates the installed game and external QuickBMS toolchain, creates
a unique editor-owned temporary workspace, invokes `quickbms.exe` with the
Shiro Games PAK script in read-only extraction form, discovers exactly one
`data.cdb`, validates it through `JsonDataService.LoadProject`, promotes it
through `Extracted\data.cdb.importing` to
`<Wartales installation>\Extracted\data.cdb`, and opens the durable file before
replacing the editor's current project.

If that durable file already exists, the player must confirm replacement before
QuickBMS runs. Cancel preserves the file and current project. The service also
refuses any unapproved replacement at promotion time. Promotion failure returns
no candidate project, and successful imported projects have a stable file path
for existing `.wtstate` persistence.

The final process-lifetime correction launches QuickBMS suspended, assigns it
to an editor-owned Windows Job Object, and resumes it only after containment is
established. Descendants inherit the job. Normal completion,
timeout/cancellation, and bounded termination now require the job's active
process count to reach zero before post-hashing or cleanup. An unproven
termination produces a distinct fatal result and deliberately retains staging.
Staging root/session components are rejected when they contain
reparse points, cleanup refuses any reparse-bearing tree, and CDB discovery
uses controlled traversal that never descends through junctions. Candidate
files are independently checked for containment, regular-file identity, and
reparse status.

Shared Open/Import promotion now prepares project-derived reference data without
mutating live state. Application language data is initialized independently at
startup. Publication occurs only after reference preparation succeeds, with
prior state retained for rollback if publication itself fails.
Production regressions cover Job Object containment of a real
parent/child/grandchild tree during timeout/cancellation and when the root exits
first, plus staging junctions, cleanup replacement, discovery escape, empty
output, language-data independence, and successful promotion. The final
Renewed Focused Engineering Review passed with non-blocking notes.

The production path calculates SHA-256 identity for `res.pak`, QuickBMS, the
script, and extracted CDB. It verifies the source package again after process
completion, rejects start/timeout/exit failures and missing, ambiguous, empty,
or invalid CDB output, and cleans per-attempt staging. The game package must
have the `PAK\0` signature required by the supplied script. The supplied fresh
installation was exercised directly: QuickBMS exited 0, the 6,691,681-byte
`data.cdb` loaded as 395 sheets, and the 791,334,661-byte `res.pak` retained
SHA-256 `665BAF4E4240D8822178D634D8A8CD830B961781D77B1687B9CF24052D95CAC9`
before and after extraction. The post-correction real run promoted that CDB to
`Extracted\data.cdb`, assigned the durable path to the project, proved
a stateful Starting Resources operation and `.wtstate` creation, cleaned
staging, and left no importing artifact.

Project Owner interactive testing confirmed that Import From Wartales succeeds,
the durable project exists at
`C:\Program Files (x86)\Steam\steamapps\common\Wartales\Extracted\data.cdb`,
the adjacent `data.cdb.wtstate` file is created, and Starting Resources no
longer reports that the project lacks a file path.

The integration is transport infrastructure only. Gameplay mutation,
transactions, Undo/Redo, profiles, snapshots, Restore Previous Values,
Gameplay Operation State, and `.wtstate` semantics are unchanged. Reimport,
package backup/replacement, Restore Original Game File, Update Survival, Golden
CDB, and redistribution of QuickBMS or the Shiro script are not implemented.

Documentation reconciliation, final verification, the single milestone commit,
and normal push to `origin/main` complete this accepted milestone.

Previous milestone:

Restore Previous Values correction

Previous milestone status:

The Project Owner standardized every gameplay reset control as **Restore
Previous Values**. The implementation now uses compatible Gameplay Operation
State as the single authority for the values captured before a gameplay tool
first changed its targets. Restore remains unavailable when that history is
missing, and an explicit restore request never adopts current configured values
as fabricated history.

The 17 shared preset tools, Party Economy, Random Trait Exclusions, Overworld
Movement Speed, and Rain Frequency use the accepted contract. Movement and Rain
retain Vanilla as ordinary selectable presets but no longer use those fixed
values as reset authority. `.wtstate`, compatible profile state, mutation-based
rollback, validation, and one-action Undo/Redo remain authoritative. Detailed
Editor Reset Property and `PropertyModel.IsModified` are unchanged. No Golden
CDB support was added.

The focused Random Trait Exclusions correction rejects Restore at the
execution boundary when compatible history has disappeared, without issuing an
Apply request. Restore selections are resolved from current compatible Gameplay
Operation State rather than immutable dialog-open candidate baselines, including
after profile/state replacement. Candidate presentation metadata is refreshed
after project operations.

The final consistency correction establishes one interaction contract across
all 23 gameplay entries: Restore Previous Values immediately applies the
captured pre-tool values through the normal validated operation path. Apply
remains available for ordinary manual configuration. Party Economy no longer
requires a second Apply click. Exact RTE baseline equality contributes zero
effective changes and produces no synthetic Review Changes row; additional
exclusions retain per-trait accounting. Renewed Focused Engineering Review and
final Project Owner acceptance are complete. The review returned **PASS WITH
NON-BLOCKING NOTES**: automated coverage exercises production ViewModel,
service, operation, mutation, validation, and history paths rather than
synthesizing WPF button clicks. Project Owner interactive testing supplies the
runtime evidence and concluded with an explicit **PASS** after the final
corrections. Sequential application and test builds complete with zero warnings
and zero errors, the focused suite passes, and all 25 Class A compatibility
groups pass. Its final commit and push checkpoint is complete.

Previous milestone context:

Update Profile engineering implementation and its Renewed Focused Engineering
Review are complete. The review returned **PASS WITH NON-BLOCKING NOTES — Ready
for Project Owner Interactive Acceptance**. Sequential application and test
builds completed with zero warnings and zero errors, all 22 Class A
compatibility groups passed, `git diff --check` passed, and no profile-update
temporary artifacts remained.

Random Trait Exclusions is implemented under Party with dynamically discovered,
searchable Positive and Negative checklists. Checked traits remain eligible;
unchecked traits receive `done=false`. Exact true, false, and absent baselines
are preserved, and Restore Previous Values uses the approved known-property
removal primitive only for an originally absent scalar `done` leaf.

The feature uses one atomic gameplay operation, mutation-based rollback, one
Undo/Redo history action, feature validation, Gameplay Operation State,
snapshots, profiles, and reconciled Update Profile capture. Review Changes
retains an understandable operation outcome when an exact removal has no
attached PropertyModel row.

Update Profile now reconciles the selected profile's prior effective-path
records with the current intended project instead of replacing them from dirty
leaves alone. This preserves profile content after save/baseline acceptance,
removes records intentionally restored to their profile-stored original, and
retains state-backed ordinary targets and additive requests. Candidate profiles
are staged, reloaded, and checked by an independent invariant validator that
does not call profile construction or high-level reconciliation. Historical
structural presence is explicit and distinct from JSON `null`, while Gameplay
Operation State compatibility is refreshed against current live targets before
capture. Reopening a saved CDB uses the existing profile as authoritative
history and does not require a separately maintained pristine CDB. Failed
validation preserves the previous managed profile.

Change accounting now uses effective leaf identity across current-project,
profile, and mutation-result counts. Supported removal mutations are included in
apply feedback, Random Trait Exclusions state-only outcomes remain synthetic
when necessary, additive output remains deterministic and overlap-aware, and
Review Changes resolves duplicate nested names by `EffectivePropertyPath`.

The correction pass also centralizes legacy pathless resolution, surfaces
ambiguity instead of dropping Review Changes rows, filters additive output by
effective path, and closes the public no-validation replacement route. Generic
deletion of clean-baseline properties is outside the authorized profile scope;
the supported removal case remains exact restoration of a feature-created
absent-baseline scalar property.

The earlier reset-authority investigation was completed separately and led to
the current Restore Previous Values contract.

The focused correction pass now preflights every candidate before mutation,
requires explicit stable source IDs, derives Review Changes from the persisted
Random Trait Exclusions state specifically, and validates exact requested/result
allowed-ID equality. Permanent smoke coverage also includes disconnected-target
atomicity, identity failures, unrelated gameplay-state changes, validator
mismatch rollback, and one complete managed Update Profile replay.

The real-data dialog-open failure is corrected. Trait group membership now comes
from the `trait` sheet's ordered `Starting`, `Hidden`, `Recruitment`, and
`Acquired` separator anchors; the optional numeric per-entry `gen` field is no
longer mistaken for group identity. Operation-state fingerprints retain the
resolved Starting/Recruitment group. Realistic separator-shaped coverage now
constructs the complete dialog ViewModel, proves opening is mutation-free,
checks malformed candidate and noncandidate boundaries, and rejects invalid
separator metadata. The current clean installed CDB resolves 37 candidates
(22 Positive and 15 Negative), with Stoic and Gourmand initially disabled.
This is a feature-specific correction and introduces no architecture changes.

Direct Snapshot Preview remains read-only and can conservatively report an
absent-baseline leaf as missing. Profile application materializes exclusion
state before matching and is unaffected.

Project Owner acceptance is now recorded for the corrected Update Profile,
Review Changes/effective-accounting, and full-configuration runtime workflows.
The owner applied a known non-damaged profile, saved and reloaded its CDB, made
additional edits, and updated the same profile successfully. Its effective
change count increased from 633 to 636 and validation reported no issues.
Review Changes displayed the correct result; the prior six-change discrepancy
for the nested Campfire `tool.height` and `tool.width` paths was not present.

The owner subsequently applied the full intended 645-effective-change
configuration, saved a new profile from that configured state, launched
Wartales, started a game, and played for more than one hour without obvious
instability attributable to the editor configuration. Random Trait Exclusions
behaved as expected during that session: no recruit received a trait that had
been disabled. This is positive runtime evidence, not statistical proof that an
excluded trait can never occur.

The remaining lifecycle evidence is now reconciled. Random Trait Exclusions
passed renewed focused Engineering Review with **PASS WITH NON-BLOCKING NOTES —
Ready for Project Owner Interactive Acceptance**. The owner then confirmed that
the dialog opened, the feature applied, exactly five traits were unchecked, and
exactly five changes were reported. Its later runtime evidence remains positive:
no disabled recruit trait was observed during the over-one-hour session.

The Project Owner has also explicitly confirmed that Lectern Knowledge Gain and
Positive Random Traits were tested, are working, and are accepted. These remain
separate features with their existing documented semantics. No additional test
details are inferred from those acceptance statements.

All Final Feature Batch components have now completed their required
implementation, verification, Engineering Review, Project Owner acceptance,
and documentation reconciliation. The batch is accepted, reconciled, and ready
for its final commit checkpoint.

The previously damaged approximately 554-change `All Mods.wtprofile` is not
considered repaired. The owner created a new profile from the successfully
configured 645-change state.

## Next Required Step

Commit and push the accepted Save feedback and Run Speed presentation
corrections, then determine the exact validated Wartales BuildID, QuickBMS
version/hash, and Shiro script revision/hash. Public-document updates, PDF
regeneration, and a fresh release candidate require separate authorization. RC2
remains superseded for publication; no `v1.0.0` tag, GitHub Release, or Nexus
publication is authorized.
