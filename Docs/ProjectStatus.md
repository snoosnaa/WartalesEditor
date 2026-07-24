Project Status

Application Version: 0.9.1 (Development)

Status: Active Development

Last Updated: 2026-07-24

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

The Resource Respawn Speed investigation is complete. It confirmed
shared Slow, Normal, and Fast gather-refill constants, but also identified
shared exposure to excluded gathering systems. Implementation is deferred
pending future runtime validation. No Resource Respawn gameplay feature
was added.

Remaining World Convenience scope:

Vendor refresh speed

Camera zoom improvements

Recently Completed

Version 0.9.0 --- Personal Gameplay Expansion

Completed and verified:

Starting Resources

Volunteer Trait Wage Reduction

Maximum Valour

Valour Restored After Rest

Saddlebag Carrying Capacity

Pony Starting Capacity

Overworld Movement Speed

Rain Frequency

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

Multiple in-game verification passes

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

Investigation Complete

Resource Respawn Speed --- deferred pending future runtime validation

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

Profile polish

UI polish investigation and implementation

Gameplay roadmap audit

Bundled gameplay feature development

Roadmap priorities may continue evolving as additional game systems areinvestigated.

High-Priority Technical Work

Update Survival

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

Every new gameplay feature should be regression-tested against thisprofile before being considered complete.

Required Codex Reading

Before implementing any milestone, Codex must read:

ProjectStatus.md

Architecture.md

CodexDevelopmentGuide.md

When available, PlayerFirstDesign.md should be added to this list.

Next Task

Complete documentation, commit and push Version 0.9.1, then investigate
and implement Profile polish. UI polish, a gameplay roadmap audit, and
bundled gameplay feature development follow.

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
