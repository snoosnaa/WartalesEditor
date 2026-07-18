# Wartales Editor

## Project Snapshot

**Application Version:** 0.8.0 (Development)\
**Documentation Version:** 1.0\
**Last Updated:** 2026-07-18

------------------------------------------------------------------------

# Project Vision

Wartales Editor is a professional Windows desktop editor for Wartales
focused on safe, intelligent, and extensible game data editing.

The project emphasizes long-term maintainability, validation, and
reusable architecture. The goal is to provide a complete content
creation platform that allows users to safely modify existing game
content and eventually create entirely new gameplay content while
preserving project integrity.

------------------------------------------------------------------------

# Project Status

**Status:** Builds Successfully ✅

**Current Development Version:** 0.8.0

**Current Milestone:** Operation Validation & Transaction Framework ---
Complete ✅

The editor now provides:

-   Intelligent navigation
-   Localization-aware search
-   Type-aware property editing
-   Safe modification tracking
-   Unlimited Undo / Redo
-   Live Change Summary
-   Snapshot workflow
-   Mod Profiles
-   Validation framework
-   Validation results window
-   Save validation
-   Project mutation layer
-   Content creation infrastructure
-   Project operation framework
-   Transactional rollback
-   Operation-specific validation

------------------------------------------------------------------------

# Major Verified Capabilities

The following capabilities have been verified end-to-end:

-   Safe editing workflow
-   Undo / Redo
-   Snapshot export/import
-   Mod Profiles
-   Validation before save
-   Transaction rollback on failed operation validation
-   Successful in-game creation of new camp facilities

The Add Camp Facilities operation has been fully verified in-game,
including successful save, load, recipe unlock, construction, placement,
and functional use.

------------------------------------------------------------------------

# Current Architecture

The editor is organized around reusable subsystems:

-   Editing
-   Snapshots
-   Profiles
-   Validation
-   Project Mutation
-   Content Creation
-   Project Operations

Operation pipeline:

ProjectOperationService → IProjectOperation → ContentCreationService →
ProjectMutationService → ProjectMutationResult → Operation Validation →
Commit / Rollback

Rollback is mutation-based rather than rebuilding the ProjectModel,
preserving editor state and enabling future reusable operations.

PropertyModel.IsModified remains the single source of truth for
modification tracking.

------------------------------------------------------------------------

# Completed Milestones

-   ✅ Project Foundation
-   ✅ Data Browser
-   ✅ First Functional Editor
-   ✅ Find Anything
-   ✅ Smart Property Editors
-   ✅ Safe Editing
-   ✅ Change Summary
-   ✅ Snapshot Workflow
-   ✅ Complete Profile Manager
-   ✅ Validation Framework
-   ✅ Project Mutation Layer
-   ✅ Content Creation Infrastructure
-   ✅ Project Operation Framework
-   ✅ Transaction Framework
-   ✅ Add Camp Facilities (Verified In-Game)

------------------------------------------------------------------------

# Current Roadmap

## Active Milestone

### Upgrade All Equipment

Primary goals:

-   Make all eligible equipment upgradeable.
-   Reuse the operation framework.
-   Reuse transaction rollback.
-   Reuse operation validation.

## Planned Content Creation

-   Additional faction equipment recipes
-   Future camp content
-   NPC creation
-   Profession creation
-   Batch operations

## Investigation

-   Weather frequency control (determine whether rain probability,
    duration, or cooldown can be safely modified through editable game
    data)

## Future

-   Validation expansion
-   Automated transaction tests
-   Merge preview
-   Community sharing after 1.0
-   In-game profile credits
-   Byte-for-byte CDB formatting preservation

------------------------------------------------------------------------

# Current Task

1.  Complete documentation updates.
2.  Final build verification.
3.  Commit and push milestone.
4.  Begin Upgrade All Equipment.