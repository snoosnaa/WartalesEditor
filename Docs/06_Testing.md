# Testing Guide

**Document Version:** 1.1  
**Last Updated:** 2026-07-12

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
