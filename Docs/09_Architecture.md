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

# Core Architectural Subsystems

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

Existing properties continue to enforce compatible JSON token types.

Structurally created properties are validated through operation validation and generic schema validation instead of original-value token comparison.

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