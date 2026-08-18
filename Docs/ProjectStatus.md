Project Status

Application Version: 0.10.0 (Development)

Status: Active Development

Last Updated: 2026-08-18

Purpose

This document describes the current state of the Wartales Editorproject.

It is intentionally concise.

It answers:

Where the project is today.

What has been completed.

What is currently being developed.

What is planned next.

Detailed architecture belongs in Architecture.md.

Development workflow belongs in CodexDevelopmentGuide.md.

Product philosophy belongs in PlayerFirstDesign.md.

Historical information belongs in DevelopmentJournal.md andChangelog.md.

Current State

Wartales Editor has transitioned from infrastructure development toplayer-focused gameplay feature development.

The application:

Builds successfully.

Passes validation.

Passes save/reload testing.

Supports persistent Gameplay Operation State.

Passes extended runtime testing.

Has completed multiple in-game verification passes.

Is maintained with Git and GitHub.

The editing platform and supporting architecture are considered stable.

Future development is focused primarily on expanding gameplay tools andimproving the player experience.

Current Milestone

Restore Previous Values correction

Current phase:

Complete, repository-verified, Engineering reviewed, and Project Owner
accepted. The Renewed Focused Engineering Review returned **PASS WITH
NON-BLOCKING NOTES**; the note is limited to automated coverage exercising the
production ViewModel/service/operation paths instead of synthesized WPF button
clicks. Project Owner interactive testing supplied the runtime evidence and
concluded with an explicit **PASS** after the final consistency corrections.
Final commit and push are the remaining lifecycle checkpoint.

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

Final verification and commit/push checkpoint for the accepted Final Feature
Batch

Completed bounded reset-authority investigation and Restore Previous Values
correction

Bounded QuickBMS/package-replacement experiments, followed by integrated
Import / Install / Restore

Update Survival after the integrated package workflow

Roadmap priorities may continue evolving as additional game systems areinvestigated.

High-Priority Technical Work

Update Survival

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

Complete the final verification and commit/push checkpoint for the accepted and
reconciled Final Feature Batch. The next authorized engineering activity after
that checkpoint was the bounded reset-authority investigation, followed by the
Restore Previous Values correction. Integrated Import / Install / Restore and
Update Survival have not started.

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
