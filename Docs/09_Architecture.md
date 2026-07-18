# Architecture

**Version:** 0.9\
**Status:** Active\
**Last Updated:** 2026-07-18\
**Applies To:** Entire Project

------------------------------------------------------------------------

# Overview

Wartales Editor follows the Model-View-ViewModel (MVVM) architectural
pattern.

The architecture is built around reusable infrastructure. New features
are expected to compose existing systems rather than introduce parallel
implementations.

Core principles:

-   Separation of presentation and business logic.
-   Preserve the loaded project structure.
-   Modify the loaded JSON document directly whenever practical.
-   One implementation per major responsibility.
-   Transactional operations for structural content creation.
-   Validation before persistence.
-   Rollback on failed operation validation.

------------------------------------------------------------------------

# Core Architectural Subsystems

The editor now consists of seven primary reusable subsystems:

-   Editing
-   Snapshots
-   Profiles
-   Validation
-   Project Mutation
-   Content Creation
-   Project Operations

All operate on the same `ProjectModel` and ultimately modify the same
`RootDocument`.

------------------------------------------------------------------------

# Operation Architecture

Content creation is now executed exclusively through the Project
Operation pipeline.

``` text
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
```

The UI no longer calls content creation services directly.

------------------------------------------------------------------------

# Transaction Architecture

Every structural operation executes transactionally.

``` text
Capture
    │
    ▼
Execute Operation
    │
    ▼
Operation Validation
    │
 ┌──┴──────────┐
 │             │
 ▼             ▼
Commit     Rollback
```

Rollback is mutation-based rather than rebuilding `ProjectModel`.

This preserves:

-   modification tracking
-   undo/redo integration
-   UI state
-   selection state
-   future workflow composition

------------------------------------------------------------------------

# Validation Architecture

Validation consists of two distinct layers.

## Generic Validation

Generic validation verifies project-wide correctness without knowledge
of specific Wartales features.

Examples include:

-   token compatibility
-   serialization
-   structural integrity

## Operation Validation

Each content creation operation owns its own validator.

Responsibilities include:

-   required objects exist
-   required schema exists
-   operation completed successfully
-   no partial operation remains

Operation validation complements generic validation rather than
replacing it.

------------------------------------------------------------------------

# Structural Property Rules

The architecture distinguishes between:

-   modified existing properties
-   newly created structural properties

Existing properties continue to enforce compatible JSON token types.

Structurally created properties are validated through operation
validation and generic schema validation instead of original-value token
comparison.

------------------------------------------------------------------------

# Verified Architecture

The following has been verified end-to-end:

-   Transaction rollback
-   Successful operation commit
-   Save validation
-   Save serialization
-   In-game loading
-   In-game creation and use of the Anvil
-   In-game unlocking of the Blacksmith profession
-   Successful creation of new gameplay content

------------------------------------------------------------------------

# Current Architecture Status

The editor platform is considered stable through Version 0.8.x.

Future milestones are expected to extend the existing operation
framework.

Planned operations include:

-   Upgrade All Equipment
-   Additional faction equipment recipes
-   Future camp content
-   NPC creation
-   Profession creation
-   Batch operations

The current architecture should be extended rather than redesigned
unless a genuine architectural defect is discovered.