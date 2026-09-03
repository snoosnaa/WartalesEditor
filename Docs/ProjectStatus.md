Project Status

Application Version: 1.0.0 (Public Release Preparation)

Status: Release Preparation In Progress

Last Updated: 2026-09-03

Purpose

This document describes the current state of the Wartales Editor project.

It is intentionally concise.

It answers:

Where the project is today.

What has been completed.

What is currently being developed.

What is planned next.

Detailed architecture belongs in Architecture.md.

Development workflow belongs in CodexDevelopmentGuide.md.

Product philosophy belongs in PlayerFirstDesign.md.

Historical information belongs in DevelopmentJournal.md and Changelog.md.

Current State

Wartales Editor has completed its accepted feature and engineering milestones
and is now preparing Version 1.0.0 for public release.

The application:

Builds successfully.

Passes validation.

Passes save/reload testing.

Supports persistent Gameplay Operation State.

Passes extended runtime testing.

Has completed multiple in-game verification passes.

Is maintained with Git and GitHub.

The editing platform and supporting architecture are considered stable.

The current work is limited to release preparation; future product development
remains outside this phase.

Current Milestone

Public Release Preparation

Current phase:

**Public Release Preparation is IN PROGRESS.** Phase 1 is COMPLETE: legal/public
metadata, repository hygiene, public onboarding, version reconciliation, public
release-note groundwork, and Git-history privacy sanitization are complete
locally and remotely.

**History sanitization:** COMPLETE locally and remotely.

The TyTech Green accent investigation and implementation completed, and
Engineering Review passed after the required corrections. Project Owner visual
acceptance then failed, the Project Owner rejected the color update, and the
implementation was fully rolled back to the pre-experiment UI. No further color
work is planned before Version 1.0.0. Production and tests are restored to their
accepted baseline, and the rejected experiment lifecycle is closed. The
separately approved Wartales Modding Community acknowledgement remains in the
public README and User Manual Markdown as a goodwill acknowledgement without a
legal-attribution role. The color experiment no longer blocks release
preparation. Both public PDFs were subsequently regenerated and visually
validated. The first Release Candidate attempt was stopped before ZIP/checksum
creation because the application DLL exposed the local build-machine PDB path
through its CodeView record. A Release-only `PathMap` correction now maps
project paths to `/_/`; its Focused Engineering Review returned **PASS** with no
findings. After the correction's commit/push checkpoint, the next activity is a
fresh replacement Release Candidate generation pass. The rejected RC1 staging
is evidence only and is not release input.

Phase 2 is COMPLETE and closed: the exact publish process, package manifest,
complete public User Manual, checksum and malware-scan procedures, clean-machine
plan, issue-report guidance, and later publication sequence are defined and
repository-verified. The User Manual has a Project Owner content-review pass and
PDF visual pass. The README has a Project Owner content-review pass and PDF
visual pass. The binary-package plan uses `README.pdf` and `USER-GUIDE.pdf`
while the repository Markdown remains authoritative.

**Quick Help V1 is CLOSED.** The always-visible main-window action, owned
modeless five-tab utility window, shared footer reminder, packaged User Guide
launcher, single-instance lifecycle, and focused WPF/STA coverage completed
Focused Engineering Review (**PASS**) and Project Owner Interactive Acceptance
(**PASS**), including the footer correction and brief visual re-test. The final
focused suite passes 41 checks and the Export/MainWindow suite passes 202/202.
The replacement release candidate and checksum, scan and clean-machine
validation, exact supported Wartales/toolchain recording, GitHub Issues
configuration, final release review, `v1.0.0` tag, GitHub Release, and any
separately authorized Nexus publication remain pending.

Request Board Rewards V1 completed investigation, approved design,
implementation, focused Engineering Review (**PASS WITH NON-BLOCKING NOTES**),
real-game runtime validation, and Project Owner acceptance. Wartales launched
with the modified CDB, a new game showed changed Request Board base Krown
rewards, a battle and save completed, and the save loaded successfully after a
full exit. The 101-check focused suite remains green.

The shared Restore Previous Values correction is also closed. The systemic
ordinary-open failure was caused by shared provenance authority rather than
Request Board Rewards. Bounded `LocalRestoreContentIdentity` authority now
supports immediate and Save/reopen restoration without claiming pristine
source authenticity. Verified source authority remains mandatory across
rebase, source-generation change, imported history, portable transfer, and
Update Survival boundaries. Focused Engineering Review returned **PASS WITH
NON-BLOCKING NOTES**, and Project Owner interactive acceptance passed.

Final sequential verification passes Golden 199/199, Export 202/202, Update
Survival 180/180, focused QuickBMS Import, Language Data, focused Restore, and
all 25 Class A groups with zero warnings or errors.

Previous active status:

**QuickBMS Export Back to Wartales Version 1 is complete, reviewed, Project Owner
accepted, and closed. Public Release Preparation followed and is now in progress.**
File → Export Back to Wartales now reuses normal Save, captures the persisted
CDB once, binds identity validation and staging to that exact byte snapshot,
stages only `Modded\data.cdb` in a marked temporary session, and uses
the contained QuickBMS runner for filtered direct reimport. Success requires an
exact read-only re-extraction and byte-for-byte verification. Known transport
outcomes survive presentation and completed-progress observer failures, and
cleanup warnings remain independent of success, failure, pre-write cancellation,
and partial workspace creation. Permanent tests cover owner-resolution failure,
the actual MainWindow close/retry lifecycle, and deep state neutrality across
project JSON and identities, gameplay state, Undo/Redo, profiles, snapshots,
Golden, compatibility reporting, and `.wtstate`. No automatic
backup, restore, provenance/generation gate, Golden integration, or project
mutation was added. Project Owner live-package acceptance passed: the editor
verified the live write, Wartales reported modified files, and the accepted Rusty
Shiv sale-price change appeared in a new game. No additional live Export was
performed during final correction, review, or reconciliation verification.

Previous milestone:

Golden CDB Version 1

Previous phase:

**CLOSED.** Implementation, both Project Owner-directed corrections, the brief
Project Owner re-test, final behavioral-regression correction, documentation
reconciliation, Engineering acceptance, and Project Owner acceptance are
complete. The Final Renewed Focused Engineering Review returned **PASS WITH
NON-BLOCKING NOTES** with no production defect, unsafe test seam, artifact
residue, or remaining blocker.

Golden CDB is one optional, user-authoritative, editor-owned reference stored at
`<Documents>\Wartales Editor\Golden CDB\data.cdb`. Wartales Editor validates
structural usability and records the SHA-256 identity of the exact stored bytes;
it does not certify that the user-selected file is vanilla, pristine, current,
or Steam-verified. No companion metadata, archive history, or numbered copy is
created.

**Tools → Golden CDB...** opens one modeless management window for Set/Replace,
standalone selection, current installed Wartales import/designation, Load,
Compare, and Remove. The import action reuses the existing read-only QuickBMS
extraction mechanics in a detached temporary `GoldenImport` workspace, designates
those exact validated bytes, and cleans the workspace. It does not publish or
touch normal `Extracted\data.cdb` or its `.wtstate`. Normal Import From Wartales
still acquires, durably publishes, and opens the Extracted project, while Load Golden CDB
remains the explicit Golden action that replaces the current project. It adds no
extraction or write-back path. The
exact-byte SHA-256 identity remains internal and is no longer shown in the normal
window. Reference loading is explicitly
sidecar-free and never installs `.wtstate` gameplay state or source provenance.
Saving to the reserved canonical path requires explicit Save Anyway, Choose
Another Location, or Cancel intent and reconciles the Golden identity from the
actual canonical bytes even if later state persistence fails.

Comparison is observational and difference-only. It matches unique sheet names,
explicit unique entry IDs, and unique effective property paths; unsupported or
ambiguous stable identity is reported separately as incomplete coverage and
suppresses false Missing/New or descendant results. Set Current compares live CDB
content with a fresh sidecar-free persisted parse, so structural changes cannot be
hidden by simultaneous gameplay-state changes. Cleanup residue is reported as an
explicit warning, and save intent/final destination is resolved before unchanged
ordinary validation. Golden
does not authorize mutation, Restore Previous Values, profile behavior, Update
Survival provenance, or Check Compatibility.

Permanent isolated verification includes 199 Golden CDB checks, 180 Update
Survival checks, focused QuickBMS and Language Data suites, and all 25 Class A
compatibility groups. All four projects build with zero warnings and zero errors.
The final Golden additions behaviorally prove that the original Import From
Wartales command has no Golden side effect, accepted convenience import replaces
an existing Golden with exact durable bytes, and three close/reopen/import window
cycles retain exactly one event path.

Project Owner acceptance for Golden functionality is complete. The later required
local-status correction passed focused review and Project Owner confirmation.
Golden operations show ephemeral progress and result text in
the owned Golden window while retaining compatible main-status messages. Import
success, replacement decline, designation failure, QuickBMS failure, Load,
Set/Replace/Select, Remove, Compare, and cleanup warnings have distinct truthful
local outcomes. Closing and reopening starts with no stale message, and the
technical identity/hash remains hidden. The earlier deferred classification is
superseded. Export Project Owner acceptance and the reviewed, visually accepted
MainWindow maximize cleanup are complete.

Project Owner subsequently corrected the Golden convenience import semantics.
That action is now detached acquisition-only with respect to the active editor
and the normal Extracted working file: it does
not prompt to abandon unsaved work, call active-project publication, rebuild
references, clear history, install imported provenance/state, or change the
current file. Exact same-path tests prove an active `Extracted\data.cdb`, its
`.wtstate`, dirty in-memory CDB/gameplay state, and all related editor state remain
unchanged. Accepted/declined replacement and acquisition/designation failures all
preserve the current project, and detached cleanup warnings survive decline and
designation failure. A tiny temporary marker identifies editor-owned detached
sessions; the next Golden acquisition safely reconciles marked stale sessions
before creating a new GUID, while unrecognized or undeletable content blocks the
refresh instead of accumulating folders. Final Narrow Engineering Review returned
**PASS** with no findings, and the Project Owner brief re-test passed. The detached
Golden acquisition correction and Golden CDB Version 1 are closed.

Completed sequence:

QuickBMS safe Import → Update Survival → Golden CDB → QuickBMS Export Back to
Wartales. Public Release Preparation followed and is now in progress.

Earlier milestone:

Update Survival

Earlier phase:

Complete, reviewed, accepted, and reconciled. Final Renewed Focused Engineering
Review: **PASS WITH NON-BLOCKING NOTES**. Final Project Owner Interactive
Acceptance: **PASS**.

Update Survival now distinguishes the immutable pristine-source identity from
the current persisted CDB revision. QuickBMS import establishes authoritative
`SourceCdbGenerationIdentity` provenance, normal Save preserves it while
advancing `CurrentCdbContentIdentity`, and ordinary Open trusts source
provenance only when the adjacent Version 2 `.wtstate` current-content binding
matches the exact bytes parsed and the manifest also contains a valid source
identity. A valid byte binding with an unknown source remains unknown source
provenance. Missing, legacy, malformed, unreadable, or mismatched state never
fabricates Restore Previous Values authority.

Version 2 `.wtstate` retains one active record and at most one historical record
per gameplay operation. Verified cross-generation history retains source
authority for exact-source revalidation; unknown or untrusted history has its
actionable provenance scrubbed and cannot reactivate. Profiles Version 3 and
snapshots Version 2 carry source identity diagnostically and on portable
gameplay state. Portable state activates only when current-format root, record,
and verified target provenance agree; ordinary profile three-way comparison
remains independent. A modeless Update Compatibility
report observationally assesses shipped tools without mutating project data.
Every shipped operation now uses an explicit execution mutation journal so
execution exceptions and validator failures share mutation-based rollback, and
a failed rollback is surfaced after its single attempted inverse application.
Add Camp Facilities preflights its full object/craft scope, while Upgrade All
Equipment requires exactly one target for every approved catalog ID.

Active records must also agree with the verified manifest source. Contradictory
record identities are retained only as scrubbed unknown history. Add Camp
preflight includes the backing craft source and `lines` array required for new
recipe insertion. Compatibility classifications are covered through real Add
Camp and Upgrade assessment fixtures.

Repository verification currently includes 180 focused Update Survival checks,
all 25 Class A compatibility groups, the QuickBMS process/promotion suite, and
zero-warning builds. The current real canonical CDB was inspected read-only at
6,691,681 bytes, SHA-256
`29BE149FD1AD68D849FA498F671A8E71868117EB3AA15B25643D86C647E76576`,
with 395 sheets. Golden CDB, Steam APIs, heuristic target migration, and a
mandatory migration wizard were excluded from Update Survival; Golden CDB is
now implemented as the separate current milestone.

The final Renewed Focused Engineering Review passed with non-blocking notes.
Project Owner testing established and accepted the intended workflow: import a
fresh CDB, apply the desired profile, and explicitly choose **Tools → Check
Compatibility**. Background provenance safety remains automatic, but the full
window no longer opens during project publication. Explicit checks use current
in-memory data, replace prior results, hide compatible rows, and show an
all-clear result when no issues exist. The window now matches the established
owned modeless/taskbar behavior. Renewed testing confirmed no automatic popup,
explicit checking, issue-only/all-clear presentation, minimize/restore,
close/reopen/re-run, and Restore Previous Values across multiple gameplay
features. General gameplay tools, modifications, and profiles remained stable.

Previous milestone:

Generic Wartales Language Data Setup

Previous phase:

Complete, reviewed, accepted, and reconciled. The Renewed Focused Engineering
Review returned **PASS WITH NON-BLOCKING NOTES**. Project Owner Interactive
Acceptance and the renewed acceptance after the final UX corrections both
returned **PASS**; the owner's final result was, “Pass. Both work well.”

The user can select any valid Wartales export localization XML file. Embedded
`lang` metadata is authoritative, and one canonical copy is retained at
`<Documents>\Wartales Editor\Language Data\export.xml`. It loads automatically
at application startup. Missing or invalid data clears active localization and
falls back to internal IDs without blocking the application, Detailed Editor,
normal Open, or Import From Wartales. The Detailed Editor supplies a compact
setup action, while **Tools → Language Data...** shows status and supports safe
replacement. Setup and replacement reuse the validated Wartales installation
context to preselect a valid `export_*.xml` source or open its folder; detection
and candidate failures retain the manual picker fallback. The source remains
non-authoritative after copying. Available state uses the application's existing
green success treatment, while missing and invalid states remain distinct.
Runtime replacement refreshes current Detailed Editor/search and
Change Summary presentation; already-open Random Trait Exclusions dialogs use
their existing labels until reopened. Late publication recovery validates and
fingerprint-proves prior canonical restoration before republishing its state;
unrecoverable recovery clears localization and reports invalid state. Cleanup
failure preserves a coherent new setup and is surfaced as a warning.

Language data is application-level read-only presentation state. It is not
project, mutation, gameplay, profile, snapshot, transaction, or QuickBMS state.
`texts_*.xml` and full application localization are excluded.

Accepted non-blocking notes remain: Random Trait Exclusions refreshes localized
labels after close/reopen; fingerprint-mismatch behavior is implemented without
a dedicated mutation-based mismatch fixture; persistent external locks may
prevent rollback cleanup but later attempts fail safely; and the pre-existing
QuickBMS symlink regression may skip when Windows symlink privilege is absent.

Previous milestone:

QuickBMS Integration Milestone 1 — Safe CDB Import

Previous phase:

Complete and accepted. Renewed Focused Engineering Review passed with
non-blocking notes, and Project Owner Interactive Acceptance passed.

The initial and renewed Focused Engineering Reviews required four bounded
corrections and one final process-lifetime hardening pass. Those corrections are
now implemented and repository-verified. QuickBMS starts suspended, enters an
editor-owned Windows Job Object before resuming, and can complete only when the
job reports zero active processes. Timeout/cancellation terminates the job and
uses the same bounded zero-count proof; failure remains fatal without
post-hashing, cleanup, or promotion. Staging creation/use/cleanup rejects junction and
reparse redirection; CDB discovery uses controlled contained traversal; and
shared project promotion prepares project-derived reference state before
publication. Application language data is now owned independently. The first Project Owner interactive acceptance then identified
that the opened project had no durable file identity. That bounded defect is
corrected. Renewed Engineering Review confirmed the correction and all prior
safety blockers remain closed.

Import From Wartales now validates the default Steam installation and the
external QuickBMS/Shiro toolchain, extracts into fresh temporary staging, and
validates exactly one extracted `data.cdb` through the production project
loader before atomically promoting it to
`<Wartales installation>\Extracted\data.cdb` and loading that durable file.
Existing Extracted data requires confirmation before replacement. SHA-256 fingerprints cover the source package,
tool, script, and extracted CDB. A real extraction from the supplied fresh
installation produced a 395-sheet project while `res.pak` remained byte-
identical in size and SHA-256. The post-correction real extraction promoted the
same 6,691,681-byte CDB to the durable path, applied a stateful Starting
Resources operation, confirmed `.wtstate` creation, and
left one CDB with no importing or state-test artifact. The UI reuses existing unsaved-change,
player-facing dialog, reference-data, localization, and project-tracking paths.
The durable identity allows `.wtstate` persistence after import without any
Gameplay Operation State change.

Project Owner interactive testing confirmed successful import, the durable
`Wartales\Extracted\data.cdb`, its adjacent `data.cdb.wtstate`, and successful
Starting Resources use without the former missing-file-path error.

The integration is separate from project mutation and gameplay architecture.
No write/reimport flags are reachable. Backup, candidate package creation,
live replacement, Restore Original Game File, Update Survival, Golden CDB, and
tool redistribution remain deferred. QuickBMS Milestone 1 is closed.

Previous milestone:

Restore Previous Values correction

Previous phase:

Complete, repository-verified, Engineering reviewed, and Project Owner
accepted. The Renewed Focused Engineering Review returned **PASS WITH
NON-BLOCKING NOTES**; the note is limited to automated coverage exercising the
production ViewModel/service/operation paths instead of synthesized WPF button
clicks. Project Owner interactive testing supplied the runtime evidence and
concluded with an explicit **PASS** after the final consistency corrections.
Its final commit and push checkpoint is complete.

All gameplay reset controls now use one player-facing contract: Restore
Previous Values. Compatible Gameplay Operation State retains the target values
captured before the tool first changed them, `.wtstate` preserves that history
across save/reload, and Profiles transport compatible state. Restore is
unavailable when that history is absent. Current configured values are not
silently adopted by an explicit restore request.

The 17 shared preset tools, Volunteer Wages, Valour Points, Carrying Capacity,
Random Trait Exclusions, Overworld Movement Speed, and Rain Frequency share
this authority. Movement no longer uses fixed 6/11 as reset authority and Rain
no longer uses fixed regional Vanilla values as reset authority; those Vanilla
choices remain ordinary presets. Detailed Editor Reset Property and
`PropertyModel.IsModified` remain unchanged. No Golden CDB capability was
introduced.

Repository verification covers missing-history safety, deliberately
non-catalog baselines, multiple preset changes, exact trait-property presence,
Movement and Rain restoration, sidecar reload, profile transport, and atomic
Undo/Redo. Random Trait Exclusions additionally resolves its historical
selection from current compatible Gameplay Operation State when Restore is
clicked. A stale modeless dialog therefore cannot apply cached selections after
Undo, and profile/state replacement supersedes dialog-open baseline data. RTE
effective accounting compares current exact trait presence/values with that
captured baseline instead of treating operation-state existence as a change.
Sequential main and test builds complete with zero warnings and zero errors,
the focused Restore Previous Values suite passes, and all 25 Class A
compatibility groups pass.

Previous milestone:

Final Feature Batch

Final phase:

The Final Feature Batch is implemented, verified, Engineering reviewed, Project
Owner accepted, and documentation reconciled. Update Profile and corrected
effective accounting passed Renewed Focused Engineering Review and Project
Owner acceptance. Lectern Knowledge Gain and Positive Random Traits were each
tested, confirmed working, and accepted by the Project Owner. Random Trait
Exclusions passed renewed focused Engineering Review and Project Owner
interactive acceptance, with additional positive runtime evidence.

Lectern Knowledge Gain is available under Progression with 1×, 2×, 3×, and
5× captured-baseline presets for `GainOnLecternRest`. Positive Random Traits is
available under Party and atomically controls the three current random-trait
probability bands; Positive Only uses `0 / 1 / 0` and affects only future
eligible procedural generation. Profile Manager can explicitly update the
selected managed profile by reconciling its prior effective-path records with
the current intended project. Baseline-accepted records remain represented,
intentional restoration to profile-stored originals removes obsolete records, and identity
metadata plus original creation time are preserved. A staged candidate must
reload and pass independent retained-history, current-delta, uniqueness,
metadata, Gameplay Operation State, and additive-request invariants before
atomic same-path replacement.

Random Trait Exclusions is now available under Party. It dynamically discovers
compatible Starting/Recruitment traits, groups them as Positive and Negative,
and controls future random eligibility through `done=false` exclusions. Exact
true, false, and absent baselines are retained; absent restoration uses the
approved property-removal primitive. Operation state, atomic Undo/Redo,
profiles, Update Profile reconciliation, update compatibility, and removal-only
Review Changes truth are covered by repository-backed verification. The focused
correction pass adds full connected-target preflight, explicit stable source-ID
requirements, operation-specific persisted-state attribution, exact requested/
result validation, and integrated same-file Update Profile replay. A subsequent
focused correction fixes the real-data dialog-open failure by deriving trait
groups from ordered sheet-level separator anchors rather than the unrelated
optional numeric `gen` field. State fingerprints now retain actual separator
group identity, and realistic coverage includes full mutation-free ViewModel
construction plus malformed separator/candidate boundaries. No architecture
changed. Project Owner runtime evidence for Random Trait Exclusions is positive:
during an over-one-hour game session, no recruit received a trait that had been
disabled. This observation supports the configured behavior but is not
statistical proof that an excluded trait can never occur. Its renewed focused
Engineering Review passed with non-blocking notes, and Project Owner interactive
acceptance confirmed that the dialog opened, the feature applied, exactly five
traits were unchecked, and exactly five changes were reported.

Repository-backed verification covers gameplay baseline restoration, scaling,
validation, idempotence, rollback, Undo/Redo, operation-state and profile/
snapshot persistence, plus managed profile path enforcement, baseline-accepted
profile reconciliation, metadata preservation, current-format rewriting,
profile-relative reconciliation across save/reload, failed-validation
preservation, shared modern/legacy path resolution, and unified live-leaf plus
supported-removal accounting. Independent candidate validation no longer calls
the high-level reconciliation path. Historical structural presence is distinct
from JSON `null`, and gameplay-state compatibility is refreshed observationally
before Update Profile capture. Update Profile uses the selected profile plus the
current editing delta and does not require a pristine CDB. Arbitrary deletion of
a previously existing property is not a profile capability. The build passes
with zero warnings and zero errors. The Renewed Focused Engineering Review
passed with non-blocking notes. Project Owner testing then applied a known
non-damaged profile, saved and reloaded the CDB, made further changes, and
updated the same profile successfully. Its effective count increased from 633
to 636, and validation reported no issues.

Review Changes displayed the correct result during that workflow. The prior
six-change discrepancy for the distinct Firecamp, FirecampT2, and FirecampT3
`tool.height`/`tool.width` paths was no longer present. The owner subsequently
applied the full intended 645-effective-change configuration, saved a new
profile, launched Wartales, started a game, and played for more than one hour
without obvious instability attributable to the editor configuration.

The Project Owner subsequently confirmed that Lectern Knowledge Gain and
Positive Random Traits were tested, are working, and are accepted. The
previously damaged approximately 554-change `All Mods.wtprofile` is not
considered repaired; the 645-change configured state was saved as a new
profile.

Previous milestone:

Class A Gameplay Expansion

Current phase:

Complete. Engineering compatibility corrections, Resource Replenishment, and
the current UX consistency pass are implemented, reviewed, and final-
reconciliation verified.
Fifteen preset-based Gameplay Tools were added, and the existing Valour Points
and Carrying Capacity tools were expanded with Tent and Hitching Post effects.
The dashboard now includes the Professions category and the approved Party,
World, and Camp & Equipment additions.

Preset Vanilla restoration now uses the exact captured operation baseline.
Mining and merchant refill rates scale from that baseline, and Battle Camera
preserves a differing captured minimum. Legacy two-target Valour and Carrying
states resolve current Tent and Hitching Post values without guessed defaults,
and expand their baselines only after an explicit safe Apply.

Repository-backed focused verification covers differing-baseline restoration,
Mining proportional scaling, merchant-rate scaling, Battle Camera baseline
drift, supported and custom legacy Party Economy upgrades, snapshot path
compatibility, every preset catalog entry, and representative malformed-target
failures. Resource Replenishment additionally covers 2×/3×/5× baseline-relative
scaling, exact Vanilla restoration, no compounding, atomic failures, Undo/Redo,
state persistence, snapshot serialization, profile replay, and preservation of
`GatherRefillFactorExtreme` and unrelated values.

Gameplay feature windows now share explicit owner restoration after closing,
and Apply actions show success or already-applied feedback inside the active
dialog. Starting Resources, Movement Speed, and Battle Camera Zoom include the
approved non-blocking visual notes. The shared restore action executes the
shared operation pipeline and restores the exact captured baseline rather than
only changing the selected preset. Invalid Starting Resources and Party Economy
input clears stale success feedback.

A fresh Wartales installation and freshly extracted game data were used for a
full current-mod-set gameplay smoke. The game launched, started a new campaign,
reached playable gameplay, saved, exited completely, relaunched, and loaded the
save. An earlier new-game-load freeze is non-reproducible after clean reinstall
and fresh extraction; its original cause was not identified.

During one camp-placeable-item creation session, the game displayed
`stacked content: 2`. Gameplay and item creation continued. A narrow literal
search did not locate its origin, so this remains a non-blocking diagnostic
observation and no speculative correction was made.

The shared window lifecycle and in-dialog Apply feedback passed focused
Engineering Review and received positive Project Owner visual feedback.
Resource Replenishment is not claimed as exhaustively timed across every
resource category. Campfire implementation/reference equivalence is
established; direct Tier 2 and Tier 3 assignment-count verification remains
pending and non-blocking, while Tier 1 intentionally remains at capacity 4.

Previous milestone:

Version 0.9.1 --- World Convenience

Current phase:

Complete and verified

Overworld Movement Speed and Rain Frequency are implemented and verified.
Rain Frequency's Vanilla, Less Rain, Rare Rain, and No Rain presets
update only `props.meteo.rainDaysPerMonth` on the twelve approved region
entries while preserving each region's baseline.

Additive Mod Profile restoration has been repaired and build verified.
New Version 2 profiles store explicit, validated requests for Add Camp
Facilities and Upgrade All Equipment, replay those operations before
ordinary snapshot properties, and record the combined result as one
mutation-based Undo/Redo action.

Version 1 profiles remain loadable and retain their original
property-target behavior. Direct Snapshot import remains property-target
based and does not replay gameplay operations.

The Gameplay Tools menu is now flat; Overworld Movement Speed appears
directly after the existing separator.

Automated model-level verification confirms request detection, operation
ownership filtering, Version 1 and Version 2 serialization, deterministic
replay, idempotence, staged rollback, ordinary property application, and
combined Undo/Redo.

Profile Manager now presents one effective Changes count that combines
ordinary profile changes with the deterministic project impact of
additive gameplay operations. Internal snapshot and operation-request
counts are no longer exposed in the player-facing summary.

Profile application now refreshes the editor's tracked PropertyModel set
after additive replay. Newly created nested properties therefore remain
visible through PropertyModel.IsModified, the main modification count,
and Change Summary. Profile Manager, apply results, the main counter, and
Change Summary now use the same modified-property outcome semantics.

The profile-apply result presents one player-facing Changes summary.
Snapshot-property and gameplay-operation categories remain internal.

Implemented presets:

Vanilla

Faster

Fast

Very Fast

Verification includes build verification, runtime testing, validation,
Save / Reload, Undo / Redo, Change Summary, Profiles, Snapshots, and
multiple in-game verification passes.

The Resource Respawn Speed investigation identified the shared Slow, Normal,
and Fast gather-refill constants. The resulting Resource Replenishment feature
is now implemented in the current milestone. Vendor Refresh and Battle Camera
Zoom are also implemented; neither remains pending World Convenience work.

Recently Completed

Version 0.9.0 --- Personal Gameplay Expansion

Completed with app-level verification:

Starting Resources

Volunteer Trait Wage Reduction

Maximum Valour

Valour Restored After Rest

Saddlebag Carrying Capacity

Pony Starting Capacity

Verification included:

Successful builds

Runtime testing

Save/Reload

Validation

Change Summary

Undo/Redo

Profile integration

Snapshot integration

Gameplay Operation State persistence

Volunteer correctly supports a 100% wage reduction.

Valour modifications function correctly in-game.

Saddlebag and pony carrying-capacity modifications function correctlyin-game.

Gameplay Features

Completed

The following gameplay tools have been implemented and verified:

Add Camp Facilities

Upgrade All Equipment

Character XP Controls

Profession XP Controls

Starting Resources

Volunteer Trait Wage Reduction

Maximum Valour

Valour Restored After Rest

Saddlebag Carrying Capacity

Pony Starting Capacity

Resource Replenishment

Class A preset tools listed in the current milestone

Completed Architecture

The following major systems are complete:

Editing Platform

Snapshot Workflow

Mod Profiles

Validation Framework

Project Mutation Layer

Content Creation Infrastructure

Project Operation Framework

Transaction Framework

Nested Property Infrastructure

Object-Valued Mutation Infrastructure

Array Mutation Infrastructure

Gameplay Operation State

Gameplay Operation State Persistence

The architecture is considered stable.

Future infrastructure should only be introduced when it directlysupports approved gameplay features.

Current Roadmap

Immediate Priorities

Completed final verification and commit/push checkpoint for the accepted Final
Feature Batch

Completed bounded reset-authority investigation and Restore Previous Values
correction

Completed QuickBMS safe Import, Update Survival, Golden CDB Version 1, and direct
QuickBMS Export Back to Wartales Version 1 milestones

Completed, reviewed, and Project Owner accepted Update Survival

Roadmap priorities may continue evolving as additional game systems areinvestigated.

High-Priority Technical Work

Optional future migration UX beyond the accepted conservative compatibility model

Update Existing Profile

Profile migration improvements

Merge Preview

Conflict resolution

Compare Projects

Preserve original CDB formatting

Preserve unknown game data

Automatic backups

These remain important but should not interrupt gameplay-featuredevelopment unless required.

Post-Version 1.0

Gameplay

Starting Influence

Community profile sharing

Community content sharing

Creator metadata

In-game profile credits

NPC creation

Encounter creation

Profession creation

Trait creation

Status-effect creation

Steam Workshop integration is intentionally excluded because Wartalesdoes not support Steam Workshop.

Current Regression Profile

The regression-testing Mod Profile contains all completed gameplay
features and should continue expanding. Clean-project restoration of Add
Camp Facilities and Equipment Upgrades requires a newly created Version 2
profile; legacy Version 1 profiles preserve their original snapshot-only
behavior.

Current coverage includes:

Camp Facilities

Equipment Upgrades

Character XP

Profession XP

Starting Resources

Volunteer Trait Wage Reduction

Maximum Valour

Valour Restored After Rest

Saddlebag Carrying Capacity

Pony Starting Capacity

Overworld Movement Speed

Rain Frequency

Class A preset tools

Resource Replenishment

Every new gameplay feature should be regression-tested against thisprofile before being considered complete.

Required Codex Reading

Before implementing any milestone, Codex must read:

ProjectStatus.md

Architecture.md

CodexDevelopmentGuide.md

When available, PlayerFirstDesign.md should be added to this list.

Next Task

Generate and validate the **Wartales Editor 1.0.0 Release Candidate** through a
separately authorized release-execution lifecycle. Regenerate and visually
verify the updated User Manual PDF before final package staging. Packaging,
checksum, malware scan, clean-machine validation, exact supported-version
recording, tagging, and publication remain separately authorized and pending.

Document Maintenance

ProjectStatus.md should always describe the project's current state.

Update this document whenever a milestone is completed and verified.

Historical information belongs in:

DevelopmentJournal.md

Changelog.md

Architecture belongs in:

Architecture.md

Development workflow belongs in:

CodexDevelopmentGuide.md

Player experience and UI philosophy belong in:

PlayerFirstDesign.md
