# Testing Guide

**Document Version:** 1.2
**Last Updated:** 2026-09-10

---

# Purpose

This document defines the testing strategy for Wartales Editor.

Its goals are to:

- Verify every completed milestone.
- Prevent regressions.
- Standardize testing before commits.
- Provide a repeatable release checklist.

Testing is considered part of development.

No milestone is complete until it has been successfully:

- Built
- Runtime tested
- Documented
- Committed

## Root Launcher and Portable Package Acceptance — 2026-09-10

The accepted post-1.1 portable-package work passed renewed Engineering Review
and Project Owner Acceptance. The package smoke suite passed 26 checks and
proves exact-path launch of `App\WartalesEditor.exe`, child working directory,
individual argument forwarding including an empty string, child-exit-code
propagation, native failure reporting, Unicode package paths, and the expected
two-process wait lifecycle.

Package safety coverage proves that the exact repository `output\` authority
and descendant components reject junction/reparse redirection, traversal,
sibling-prefix escape, output-root selection, file masquerading as a directory,
and required inputs inside the destination before recursive recreation.
Package validation rejects missing/extra/ambiguous paths, unreadable content,
same-name SHA-256 corruption, and root-launcher substitution. It validates one
root launcher, the intact main payload under `App\`, required runtime metadata,
culture/native structure, five root documents, no wrapper directory, no root
runtime clutter, and no PDB leakage.

Quick Help passed 49 checks, including executable-adjacent resolution and the
bounded immediate-parent lookup only when the real executable directory is
named `App`. Final accepted evidence also includes Class A Compatibility PASS;
Golden CDB 203; Paths Gameplay 208; Profile Atomic Apply 121; Profile Impact
Manifest 131; Profile Intent Update 35; Profile Operation Intent 59; Profile
Operation Replay 209; Profile Presentation 75; QuickBMS Export 215/215;
Request Board Rewards 101; Update Survival 180; Debug and Release builds with
zero warnings and errors; PowerShell parser validation; fresh staging; package
validation; zero source/staged path or SHA-256 differences; launcher hash match;
and a successful staged-launcher smoke ending with exit code 0. The existing
QuickBMS symbolic-link privilege skip is unrelated.

Accepted staging evidence was six root files, one root `App` directory, 400
application files, 13 culture directories, and zero PDBs. These counts are test
evidence, not permanent format constants.

Later release-candidate gates remain separate: Defender, SmartScreen and
Downloads-zone observation, clean non-admin Windows, Program Files-like
location, fresh-folder update/extraction, manual PDF-viewer interaction, and
final immutable ZIP/checksum equivalence.

## Version 1.1.0 Release Candidate Evidence — 2026-09-08

The complete release regression passed before packaging:

- Debug and Release builds: 0 warnings, 0 errors.
- Class A Compatibility: PASS.
- Profile Operation Intent: 84 checks.
- Profile Operation Replay: 209 checks.
- Atomic Profile Apply: 83 checks.
- Profile Intent Update: 123 checks.
- Profile Presentation: 75 checks.
- Update Survival: 180 checks.
- QuickBMS Export/routing: 215 checks.
- Golden CDB: 203 checks.
- Quick Help/MainWindow: 44 checks.
- Request Board Rewards: 101 checks.
- Paths Gameplay: 206 checks.
- Focused QuickBMS Import, Language Data, and Restore Previous Values: PASS.
- The established Windows privilege-only reparse/symbolic-link cases retained
  their permitted skip; all applicable checks passed.

The fresh self-contained `win-x64` publish produced 401 files and one project
PDB. The PDB was retained privately and excluded from the public staging tree.
The final package contains 405 files, has no private path findings, and exactly
matches its clean extraction by relative path, length, and SHA-256. The extracted
application opened a main window, reported Product Version 1.1.0 / File Version
1.1.0.0, found the adjacent Quick Help User Guide, accepted a normal close, and
exited. Windows Defender engine 1.1.26080.3 with signature 1.459.111.0 reported
no detections for the extracted package or immutable ZIP.

Candidate ZIP SHA-256:
`401D5247667A65E93911D5836BD018B14716F960C25E73C6F33F2E1C7003CCCC`.

The Project Owner subsequently confirmed Steam App ID 1527950, Wartales BuildID
25172421, from installed Steam content updated September 7, 2026 at 5:39 PM.
This closed the provisional candidate's BuildID verification gate. Final package
reconciliation regenerated the public documentation, build, ZIP, and hash.

---

## Profile Impact Manifest Acceptance — 2026-09-10

Format 5 adds optional historical Profile Changes authority without changing
format-4 Apply semantics. The focused Profile Impact Manifest suite passes 131
checks. Direct coverage includes case-insensitive alias isolation, duplicate
alias rejection, deterministic serialization, unsupported and tampered
manifests, canonical leaf identity/order, blank property paths, exact baseline
resolution, provider failure containment, evaluator-integrity classification,
and complete-evaluation rejection for unmatched, conflicting, invalid,
unsupported, failed, or missing results.

Real operation evaluation covers Paths, Request Board Rewards, Starting
Resources, Party Economy, Random Trait Exclusions, Add Camp Facilities, and
Upgrade All Equipment. Isolated structural evidence establishes 32 Add Camp
leaves and 517 Upgrade All leaves, equal to their authoritative distinct
PropertyModel mutation counts without created-entry or operation-request
inflation. A representative 744-leaf profile established all 744 leaves in
approximately 117–134 ms across final review and delegated acceptance runs.

Historical stability coverage proves the persisted count survives Profile and
CDB save/reopen, fully applied and partial/divergent targets, Apply, Undo, and
Redo. Valid zero displays `0 changes`; missing or invalid authority displays
`Unavailable`. Formats 1–4 remain usable, invalid optional authority does not
block core Apply, and Profile Manager reads validated persisted authority rather
than replaying profiles against the open CDB.

Final accepted regression evidence is: Debug and Release builds with zero
warnings and errors; Profile Impact Manifest 131; Profile Operation Intent 86;
Profile Operation Replay 209; Atomic Profile Apply 99; Profile Intent Update
123; Profile Presentation 75; Update Survival 180; Golden CDB 203; Request Board
Rewards 101; Paths Gameplay 208; QuickBMS Export/routing 215/215; Quick Help 44;
and Class A compatibility passing. Restore Previous Values, history/Undo/Redo,
Starting Resources, Party Economy, Random Trait Exclusions, Add Camp, and
Upgrade All remain covered by those passing suites. `git diff --check` passed;
no live Export was run.

Engineering Review passed after parser, provider-boundary, evaluator-integrity,
and direct-evidence corrections. Delegated Project Owner Acceptance passed the
stable historical count, zero/Unavailable, legacy/invalid, Create/Update,
changed-source, metadata/duplicate/import, large-profile, Apply/Restore, and
Golden-optional scenarios.

---

## Random Trait Exclusions Expansion Acceptance — 2026-09-10

Discovery coverage preserves legacy Starting/Recruitment candidates and adds
Positive/Negative personality traits with finite positive numeric
`recruitWeight`. Tests cover the eight current weighted Hidden traits, integer
and floating positive weights, zero/negative/missing/malformed/non-finite
weights, legacy/weighted overlap, deterministic union ordering, reordered and
additional top-level groups, nested separators, and unresolved group identity.
The current eight are Ascetic, Resilient, Sociable, Brave, Humanist, Masochist,
Delicate, and Allergic. These names are test evidence; production discovery
remains data-driven.

Mutation coverage proves weighted candidates use the existing `done` property,
Gameplay Operation State, validation, rollback, exact absent-property Restore,
and Undo/Redo paths. Dialog refresh preserves compatible pending selection,
adds new candidates with authoritative defaults, removes vanished candidates,
and rejects stale polarity/group identity.

Replay coverage distinguishes exact and changed source. Exact-source and direct
replay reject missing IDs. Changed-source replay omits and reports genuinely
absent IDs, applies compatible remaining intent atomically, preserves stored
intent, localizes unavailable names with canonical fallback, and creates no
state or history for an all-unavailable operation. Present-but-ineligible,
polarity drift, group drift, duplicate candidates, and a mixed eligible/
noneligible duplicate canonical ID all fail preflight without trait, gameplay
state, unrelated snapshot, or Undo/Redo mutation. Full destination-sheet
cardinality makes the same strict duplicate result independent of candidate
eligibility.

The initial Engineering Review failed because a mixed eligible/noneligible
duplicate could bypass candidate-only identity checking. The focused correction
added full-sheet cardinality before candidate acceptance and restored existing
public enum numeric assignments. Renewed Engineering Review returned **PASS**,
and Project Owner Acceptance returned **PASS**.

Final evidence: Profile Atomic Apply 121; Profile Operation Replay 209; all 13
repository smoke/regression projects passing; at least 1,696 explicitly counted
checks plus Class A compatibility; QuickBMS 215/215; Debug and Release builds
with zero warnings and zero errors; and `git diff --check` passing. The existing
Windows symbolic-link privilege skip remained unchanged and unrelated. No live
Export was run.

---

# Testing Philosophy

Every feature follows the same development pipeline.

```text
Implement

↓

Build

↓

Runtime Test

↓

Regression Test

↓

Document

↓

Commit
```

Never assume a feature works simply because it compiles.

---

# Standard Build Test

After every implementation step:

- Build the solution.
- Resolve all compiler errors.
- Resolve new warnings whenever practical.
- Confirm the application launches.

Expected Result

✅ Build succeeds.

---

# Project Loading

## QuickBMS Export Back to Wartales Version 1 Regression Contract

Automated export tests must use isolated package paths and must never target the
installed Wartales `res.pak`. The focused suite covers parser ambiguity,
one-read source-snapshot authority, post-snapshot source replacement,
persisted/staged identity, marker ownership and stale cleanup, exact write and
verification arguments, package signature/write-access preflight, contained
runner use, termination proof, exact re-extracted bytes, failure outcomes, and
cleanup semantics. Its STA WPF harness also exercises production `MainViewModel`
save-first behavior, shared Import/Export exclusion, reentrancy, confirmation
ordering, progress/title close, the actual MainWindow deferred close/retry chain,
owner-resolution failure, presentation and completed-progress observer
exceptions, partial-creation cleanup warnings, and complete application-state
neutrality. Main, Golden, Update
Survival, Language Data, QuickBMS import, and Class A regression suites remain
required before review.

The focused Export suite currently contains 202 passing checks. Six focused
post-acceptance WPF checks exercise unconstrained native maximization, restored
and maximized client coverage, wider/narrower and taller/shorter resizing,
menu/toolbar/status-bar width coverage, and Gameplay Tools/Detailed Editor
workspace transitions. Complete
neutrality captures the ProjectModel/root references and JSON, identities,
provenance and modification flags, full gameplay-operation state, Undo/Redo
stacks, profile files, snapshot bytes, Golden state/file, compatibility report,
and exact `.wtstate` bytes after Save and before transport, for both verified
success and verification failure. Directory
reparse boundaries use real Windows junctions. File symbolic-link cases report
the established explicit skip when Windows symlink privilege is unavailable;
production regular-file/reparse rejection remains enabled.

Project Owner Interactive Acceptance performed the first authorized installed-
package write and passed end to end. All automated Engineering verification must
still reimport only into copied packages and must never perform another live write
without explicit authorization.

## Golden CDB Version 1 Regression Contract

`Tests/GoldenCdbSmoke` uses an isolated temporary canonical directory and never
touches the user's Documents Golden or a real Wartales CDB. Its 199 permanent
checks cover canonical path resolution and normalized comparison, exact-byte
identity/copying, sidecar-free parsing, current-project preconditions, source
independence, structural rejection, atomic initial Set and replacement,
pre/post-promotion faults, exact rollback recovery and failed-recovery state,
candidate/rollback cleanup warnings and stale-artifact recovery, valid/corrupt/
missing Remove, and external-change detection. Source independence includes a
fresh-service/cold-cache canonical reload after deleting the selected source.

Load tests cover the ordinary unsaved-project prompt, cancellation, detached
publication, current/source identity boundaries, exclusion of adjacent gameplay
state, normal edit-history clearing, invalid-Golden preservation, and injected
failure after reference-data application with complete prior-publication
restoration. Save tests
cover active-canonical and selected-destination warnings, Save Golden Anyway,
Choose Another Location, Cancel, rejected/confirmed destination overwrite,
source-provenance preservation, comparison-cache invalidation, canonical identity
refresh, CDB-committed/sidecar-failed reconciliation, and protection after Remove.
Event-order assertions prove Golden intent and final destination resolution occur
before ordinary validation, including choose-other then select Golden again and
the ordinary non-Golden path.

Comparison tests cover exact and modeled all-clear results, unsaved-edit shortcut
suppression, scalar/missing/new/type/array shape and value differences,
sheet/entry aggregation, duplicate and ID-less identity coverage, unsupported raw
structures, one-sided unresolved-identity suppression with zero false Missing/New
rows, difference-versus-coverage counts, concise omission of equal values,
cache reuse/invalidation, and preservation of project JSON, modification state,
gameplay state, and history. Structural UI checks cover the Tools command,
management actions, owned modeless placement, single tracked window lifecycle,
and save-warning wiring. Project Owner WPF acceptance passed for the corrected
Golden functionality and window.

Isolated fake-runner integration checks exercise the Golden window's shared
QuickBMS extraction mechanics through the production `QuickBmsImportService`.
They verify exact imported-byte designation, preserved source provenance, no
write-back flags, explicit replacement decline, unsaved-project cancellation,
QuickBMS failure, and truthful successful-import/failed-Golden separation.
Structural UI checks confirm the import action and existing actions remain,
ordinary identity/hash text is absent, and status/cleanup-warning text remains.
Behavioral WPF coverage now also exercises the real wrapping local status area,
visible blocked import progress, import/designation success, replacement decline,
designation and QuickBMS failures, Load success/cancellation/failure,
Set/Replace/Select/Remove, comparison summary, cleanup warnings, message
replacement, and fresh close/reopen state. The local status remains ephemeral and
does not read or scrape the main editor status bar.
Detached acquisition/publication separation coverage uses production MainViewModel,
QuickBMS, Golden service, and WPF paths. Normal Import first publishes the active
`Extracted\data.cdb`; Golden import then uses an isolated transient
`GoldenImport` workspace and proves the exact active CDB and `.wtstate` bytes do
not change. Golden import with no project opens none; dirty CDB and gameplay-state projects receive no abandon
prompt; project/file references, JSON, identities, provenance, modification
flags, Gameplay Operation State, Undo/Redo, compatibility, reference data,
localization data, profiles/snapshots sentinels, and active `.wtstate` bytes remain unchanged;
accepted and declined replacement plus acquisition/designation failures remain
neutral; cleanup failure after decline/designation remains visible; detached
workspaces clean after ordinary outcomes; the next production acquisition
reconciles a released marked stale session; a still-locked session blocks before
QuickBMS or another GUID; and an injected active-publication
failure hook is never reached. Normal
Import still protects and publishes the acquired project, while Load Golden still
publishes explicitly.
Final behavioral regressions invoke the original main Import From Wartales
command and prove zero Golden confirmation or byte/identity side effects; accept
replacement of an existing Golden through the actual live Golden button/event and
prove exact imported-byte identity; and run three STA WPF close/reopen/import
cycles, including post-close button events, with one import/designation per live
cycle and no stale callback or duplicate message.

Final accepted evidence: all required Release builds complete with zero warnings
and zero errors; Golden passes 199 checks; Export passes 202; Update Survival
passes 180; focused QuickBMS Import and Language Data pass; and all 25 Class A
groups pass. MainWindow maximize/layout and Golden lifecycle checks pass, the
Golden identity remains hidden, and temporary roots/process audits are clean.

## Update Survival Regression Contract

Permanent automated coverage distinguishes pristine source generation from the
current saved revision. It covers exact-byte hashing, manifest binding,
missing/legacy/malformed/unreadable state, null-source bound manifests, Save and
Save As, same-source and changed-source QuickBMS re-import, actionable-provenance
scrubbing, exact-source verified history reactivation, legacy/current portable
profile and snapshot trust gates, mutation-free compatibility probes, complete
Add Camp and exact-ID Upgrade preflight, expanded serialized unknown-data
preservation, and single-attempt operation exception/validator rollback.

Production-path fixtures include verified-manifest/active-record source
contradictions, populated content mismatches, later-source non-reactivation,
missing and wrong-type craft `lines`, real Add Camp and Upgrade compatibility
classification, report-level `AssessmentFailed`, and explicit empty-Undo checks
after rollback failure.

Current repository evidence is 180 focused Update Survival checks, all 25 Class A
compatibility groups, and the QuickBMS process/promotion suite. Builds are run
sequentially because the WPF projects share generated intermediate output.
The symlink regression may report its established environment-dependent skip
when Windows symlink privilege is unavailable.

The real canonical CDB may be inspected read-only. All mutation, Save, and
re-import tests use isolated deterministic synthetic files.

### Profile Operation Intent / Update Survival Acceptance

The five-phase correction is complete. Automated evidence passes Debug and
Release builds with zero warnings and zero errors; Phase 1 Profile Operation
Intent 83, Phase 2 Replay 199, Phase 3 Atomic Apply 83, Phase 4 Create/Update
123, Phase 5 Presentation 75, all 26 current Class A/Profile groups, Update
Survival 180, Request Board Rewards 101, Golden CDB 203, QuickBMS Export
205/205, and Quick Help 44. Focused QuickBMS Import, Language Data, and Restore
Previous Values suites pass, and `git diff --check` passes. The QuickBMS file-
symlink case retains only its permitted environment skip when Windows symbolic-
link privilege is unavailable. Final Integrated Regression repeated this
complete accepted matrix and passed before commit/push preparation.

Coverage includes format-4 capture and backward compatibility, changed-source
and exact-source replay, state-only and true no-op outcomes, atomic rollback and
one-action Undo/Redo, Create/Update reconciliation, direct-edit ambiguity,
observational target-context counting, semantic result identity, success-only
dialog refresh, pending-input preservation, complete generic preset discovery,
Random Trait membership reconciliation, post-commit refresh isolation, and a
real temporary disk save/reopen with fresh dialog ViewModels.

Project Owner Interactive Acceptance is separate from automated verification
and passed. Using current post-update Wartales data, the owner verified Import,
Check Compatibility, the existing All Mods profile, Character XP 40%,
Profession XP 50%, Run Speed displayed as Fast, Positive Random Traits as
Positive Only, Request Board and Restore behavior, coherent Undo/Redo, and
Save/reopen persistence. After another Wartales update, the owner repeated the
workflow and additionally verified Export Back to Wartales, successful game
launch, and loading into gameplay.

### Product Decision Audit Follow-Up Acceptance

The accepted follow-up adds Lectern 4×, Cooking Pot 5/10/16, Tent Valour
3/4/5, Starting Resources Hemp and grouped quick controls, and remembered
QuickBMS folder selection. Direct Starting Resources evidence covers canonical
Hemp creation, structural-absence Restore, unknown item/field preservation,
legacy state, authentic six-field profile replay on a changed source, existing
destination Hemp, exact-source compatibility, strict mismatch rejection, and
current seven-field projection. Direct QuickBMS evidence reconstructs a saved
synthetic location and proves that it reaches production Import, Export, and
detached Golden acquisition paths; no live package is used or written.

Final automated evidence passes Debug and Release builds with zero warnings and
errors; Class A Compatibility; Profile Operation Intent 84; Profile Operation
Replay 209; Atomic Profile Apply 83; Profile Intent Update 123; Profile
Presentation 75; Update Survival 180; QuickBMS Export/routing 215; Golden CDB
203; Quick Help/MainWindow 44; Request Board Rewards 101; Paths Gameplay 206;
focused QuickBMS Import; and `git diff --check`. The Windows file-reparse case
retains its explicit skip when symbolic-link privilege is unavailable.

Project Owner Interactive Acceptance passed separately for the three preset
additions, Starting Resources grouping and Hemp behavior, Food and Materials
controls, Rope exclusion, Clear Extras, QuickBMS folder selection and memory,
invalid-folder rejection, saved-location use, and fallback behavior. No live
Export is claimed by this acceptance record.

### Party Economy Custom Preservation Acceptance

Delegated Project Owner acceptance exercised the production Party Economy
ViewModel, operation, state, profile, and history paths. Valid unmatched Tent
and Hitching Post values are detected as Custom, remain exact while independent
Valour and capacity fields change, and are replaced only after explicit named-
preset selection. Pending selection can return to Custom without mutating the
project. Invalid ranges, tier ordering, and the unsupported nonzero Tier 1
Hitching Post trait remain rejected.

Evidence covers non-mutating dialog construction, independent edits, explicit
preset replacement, Restore Previous Values, atomic Undo/Redo, state
save/reload, profile round-trip, and combined Custom Tent plus Custom Hitching
Post operation ordering. State and profile assertions confirm exact numeric
settings persist without a Custom token or schema change. Debug and Release
builds completed with zero warnings and errors; Class A Compatibility, Profile
Operation Intent 84, Profile Operation Replay 209, Atomic Profile Apply 83,
Profile Intent Update 123, and `git diff --check` passed. Delegated Project
Owner acceptance passed.

### Paths Gameplay Tools Acceptance

Path Level Requirements and Path XP Rewards completed Engineering Review with
a **PASS**. The final automated review matrix included zero-warning Debug and
Release builds; Paths Gameplay 189 checks before the UI correction; Profile
Intent phases 83/199/83/123/75; all 26 Class A/Profile groups; Update Survival
180; Request Board Rewards 101; Golden CDB 203; QuickBMS Export 205/205; Quick
Help 44; and focused QuickBMS Import, Language Data, and Restore Previous Values
suites. The QuickBMS symbolic-link matrix retained its environment skip because
Windows symbolic-link privilege was unavailable. `git diff --check` passed.

The accepted UI correction increased Paths Gameplay coverage to **206 focused
checks**. It verifies direct localized Path-name precedence over duplicate
nested rank data, English/alternate/missing-localization behavior, four
independent sections, compact multiplier selectors, Restore and Apply wiring,
unchanged and changed reward-range-only previews, status separation, and the
absence of player-facing target counts, configured totals, and changed-property
counts. An STA WPF measure/arrange check verified all four sections and all nine
actions fit a normal 640×660 client area with zero scrollable height. Debug and
Release builds passed with zero warnings and errors, Quick Help passed 44,
Profile Presentation passed 75, and `git diff --check` passed.

Project Owner Interactive Acceptance is distinct from that automation and
passed both functional and UI review. The owner confirmed Path Level
Requirements behavior; independent Crime and Chaos reward configuration;
Restore Previous Values; exclusion of other Paths and special rewards;
localized main Path headings; compact selectors; the simplified reward-range
preview; and normal dialog fit without required scrolling.

### Shared Restore Previous Values Authority Regression

Permanent ordinary-open fixtures use a valid current-content identity with
unknown source provenance. Coverage proves immediate Apply/Restore, Save then
Restore, Save/reopen then Restore, exact sidecar binding, expected-current and
target-shape rejection, authoritative rebase rejection, legacy/imported unknown
history rejection, verified-source preservation, independent Undo/Redo, and
no-op Restore history suppression. Representative scalar, multi-target,
array-backed, Party Economy, and removal-backed operation families use the same
shared authority path. The Request Board suite includes STA WPF interaction
with the production dialog's actual Apply and Restore buttons.

Compatibility workflow coverage verifies that shared ordinary/QuickBMS project
publication retains background transition evidence without opening the report,
the command requires a loaded project, repeated checks replace results using
current in-memory content, compatible rows are filtered from normal
presentation, zero/one/multiple issue summaries are correct, project switching
clears stale state, assessment creates no gameplay state or Undo history, and
the window uses the established owned modeless `ShowInTaskbar="True"` pattern.

Final Project Owner runtime acceptance confirmed the full WPF behavior that
structural automation cannot prove: no automatic popup, explicit current-
project Check Compatibility, issue-only and all-clear presentation, normal
minimize/restore, close/reopen/re-run, and one-window behavior. The owner also
verified Restore Previous Values across multiple gameplay features and after
closing/reopening feature windows within the same project session.

Verify:

- Open original `data.cdb`
- Project loads successfully
- Categories populate
- Settings populate
- Properties populate
- No exceptions occur

Expected Result

✅ Original project loads successfully.

---

# Navigation

Verify:

- Category selection
- Setting selection
- Property selection
- Selection synchronization
- Empty Categories
- Show Empty Categories

Expected Result

✅ Navigation remains synchronized.

---

# Find Anything

Verify searching by:

- Internal ID
- English name
- Property name
- Property value

Verify:

- Search results appear
- Correct Category selected
- Correct Setting selected
- Correct Property selected

Expected Result

✅ Direct navigation works correctly.

---

# Localization

Verify:

- A valid Wartales export localization file can be selected regardless of its
  filename.
- A validated Wartales installation opens setup/replacement in the game root;
  valid language-agnostic `export_*.xml` candidates are preselected, while no
  candidate or failed detection safely retains manual selection.
- Embedded `lang` metadata controls the active language code.
- The canonical `<Documents>\Wartales Editor\Language Data\export.xml` loads
  automatically after restart.
- Localized names display and participate in searching.
- Internal IDs remain available
- Missing or invalid canonical data does not block startup or project loading.
- Replacing language data refreshes current Detailed Editor presentation. A
  late failure preserves the prior setup only when exact restoration is proven;
  otherwise localization is cleared and invalid state is reported.
- Forced post-promotion failures prove exact rollback restoration, missing and
  locked rollback rejection, cleared invalid state after unrecoverable recovery,
  explicit cleanup warnings, and safe removal of stale rollback ownership on a
  later transaction.
- Source-deletion coverage reloads through a fresh service; replacement coverage
  verifies old/new/internal-ID search, selected-context notification, and open
  Change Summary refresh.
- `texts_*.xml` is neither requested nor required.
- Available state structurally uses the shared green success brushes; missing
  and invalid states retain the non-success informational treatment.

Expected Result

✅ Generic language data, fallback, replacement, and searching remain synchronized.

Final evidence: main and test builds completed with zero warnings/errors; the
focused language suite, real 10,534-entry English export validation, QuickBMS
focused suite, and all 25 Class A groups passed. The Project Owner interactively
passed setup, replacement, restart/persistence, detected source selection, and
green success-state presentation. The renewed acceptance result was **PASS**.

---

# Property Editors

Verify each editor type.

## Text

- Edit value
- Save
- Reload

---

## Number

Verify:

- Integer values
- Decimal values
- Invalid input
- Validation messages

---

## Boolean

Verify:

- Toggle
- Save
- Reload

---

## Dropdown

Verify:

- Correct values discovered
- Correct value selected
- Save
- Reload

---

## Read Only

Verify:

- Property cannot be edited

---

## Complex Placeholder

Verify:

- Displays correctly
- Remains read-only

Expected Result

✅ Every property editor behaves correctly.

---

# Validation

Verify:

- Invalid numbers rejected
- Valid numbers accepted
- Integer validation
- Decimal validation

Expected Result

✅ Invalid values never overwrite valid data.

---

# Safe Editing

Verify:

- Property modification tracking
- Project modification tracking
- Modified indicators
- Window title indicator
- Modification counter
- Reset Property

Expected Result

✅ Modification state always reflects editor state.

---

# Undo / Redo

Verify:

- Single Undo
- Single Redo
- Multiple Undo
- Multiple Redo
- Toolbar commands
- Ctrl+Z
- Ctrl+Y
- History reset after opening another project

Known Minor Issue

- Programmatic Undo/Redo may reposition the text caret within certain WPF text editors.
- This does not affect data integrity.

Expected Result

✅ Editing history behaves correctly.

---

# Change Summary

Verify:

- Change Summary opens.
- Live modifications appear automatically.
- Original Value is correct.
- Current Value is correct.
- Localized setting names display correctly.
- Category grouping displays correctly.
- Navigate button selects the correct property.
- Double-click navigation selects the correct property.
- Main editor receives focus after navigation.
- Reset Property updates the summary.
- Undo updates the summary.
- Redo updates the summary.
- Save clears the summary.
- Opening another project refreshes the summary.
- Closing and reopening the summary preserves correct state.

Expected Result

✅ Change Summary always reflects the current modification state.

---

# Save

Verify:

- Save succeeds
- File reloads
- Changes persist
- Modification indicators reset
- New editing baseline established
- Change Summary clears

Expected Result

✅ Saved project matches editor state.

---

# Request Board Rewards V1 Regression Contract

`Tests/RequestBoardRewardsSmoke` uses synthetic in-memory project models and
does not access a real CDB, Golden file, user `.wtstate`, or Wartales package.
Its 101
focused checks cover unique Min/Max target resolution, dynamic discriminator
discovery, order independence, matching-set validation, 100/150/200/300 percent
behavior, integer rounding away from zero, non-compounding Apply, complete
baseline restoration, atomic Undo/Redo, unknown-member preservation, isolated
temporary state persistence, profile
intent capture and destination-baseline replay, Update Existing Profile,
effective accounting, serialization validation, malformed and ambiguous target
rejection, arithmetic overflow, validator rollback, unrelated mutation
rejection, Update Survival classification, Golden observability, excluded reward
modifier preservation, and World-dashboard wiring.

The isolated in-game validation gate passed after Engineering Review. Wartales
launched with the full modified CDB, a new game showed clearly changed Request
Board base Krown rewards, a battle and save completed, and the save loaded after
a full exit. This confirms the intended base-reward target without claiming an
exact final-payout formula or exhaustive coverage of Champion, Weekly Bounty,
troop-size, negotiation/Influence, path, trait, Fief, and scripted-reward
interactions.

---

# Gameplay Verification

Whenever gameplay data changes:

Verify:

```text
Open

↓

Edit

↓

Save

↓

Package

↓

Launch Wartales

↓

Verify Gameplay
```

Expected Result

✅ Gameplay behaves as intended.

---

# Regression Testing

Perform before every major commit.

Verify:

- Loading
- Navigation
- Search
- Localization
- Property editors
- Validation
- Modification tracking
- Undo
- Redo
- Change Summary
- Save

Expected Result

✅ No previously completed feature regresses.

---

# Release Checklist

Before creating a release:

- [ ] Project builds successfully.
- [ ] Runtime testing completed.
- [ ] Regression checklist completed.
- [ ] Documentation updated.
- [ ] CHANGELOG updated.
- [ ] Version numbers updated.
- [ ] Git commit created.
- [ ] Changes pushed to GitHub.

---

# Current Verification Status

The following milestones have been fully verified:

- ✅ Project Foundation
- ✅ Functional Editing
- ✅ Find Anything
- ✅ Smart Property Editors
- ✅ Safe Editing
- ✅ Unlimited Undo / Redo
- ✅ Change Summary

Testing currently consists of:

- Incremental build verification
- Manual runtime testing
- Regression testing
- Live gameplay verification

Automated testing may be introduced in a future version if practical.
# Version 0.10.0 player communication verification

Runtime verification must cover the minimum window width and 100%, 125%, and
150% DPI; all menus and toolbars; Profiles complete, partial, already-applied,
and failed outcomes; Review Changes; all Check Project outcome branches; every
gameplay dialog; confirmations; file filters; About; keyboard access; status
messages; and representative error paths. Verify that no standard workflow
exposes Snapshot or mutation terminology and that no new binding errors occur.

Search Scope Semantics Correction is tracked separately and is not part of the
Pass 5 wording verification.

# Version 1.1.0 Published-Artifact Verification

The final GitHub Release assets were downloaded into a fresh verification
directory after publication. The downloaded ZIP SHA-256 was
`401D5247667A65E93911D5836BD018B14716F960C25E73C6F33F2E1C7003CCCC`, matching
both the published checksum file and the locally accepted immutable artifact.
Fresh extraction produced 405 files, included `WartalesEditor.exe`, `README.pdf`,
and `USER-GUIDE.pdf`, contained no PDB files, and reported Product Version 1.1.0
and File Version 1.1.0.0.
