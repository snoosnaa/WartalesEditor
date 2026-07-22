# Wartales Editor

## Project Snapshot

**Application Version:** 0.8.1 (Development)  
**Documentation Version:** 1.1  
**Last Updated:** 2026-07-21

---

# Project Vision

Wartales Editor is a professional Windows desktop editor for Wartales focused on safe, intelligent, and extensible game data editing.

The project emphasizes long-term maintainability, reusable architecture, transactional safety, validation, and future content creation.

The long-term goal is to provide a complete content creation platform that allows users to safely modify existing game content while eventually supporting creation of entirely new gameplay content without compromising project integrity.

---

# Project Status

**Status:** Stable Development Build ✅

**Current Development Version:** 0.8.1

**Current Milestone:**

Operation Framework, Transaction Framework, and Verified Content Creation

**Status:** Complete ✅

The editor now provides:

- Intelligent navigation
- Localization-aware search
- Type-aware property editing
- Safe modification tracking
- Unlimited Undo / Redo
- Live Change Summary
- Snapshot workflow
- Mod Profiles
- Validation framework
- Validation results window
- Save validation
- Project Mutation layer
- Content Creation infrastructure
- Project Operation framework
- Transaction framework
- Mutation-based rollback
- Operation-specific validation
- Nested property discovery
- Nested property identity
- Nested property mutation infrastructure
- Object-valued mutation infrastructure

---

# Major Verified Capabilities

The following capabilities have been verified end-to-end.

## Editing Platform

- Safe editing workflow
- Unlimited Undo / Redo
- Intelligent property editing
- Snapshot export/import
- Mod Profiles
- Validation before save
- Save / Reload
- Serialization integrity

## Transaction Framework

Verified:

- Transaction rollback after forced validation failure
- Successful transaction commit
- Mutation-based rollback
- Atomic operation history
- Project mutation aggregation
- Operation validation
- Nested object mutation
- Object-valued mutation
- Rollback after nested object creation
- Validation after nested object creation

## Content Creation

The following content creation features have been fully verified.

### Add Camp Facilities

Verified:

- Recipe creation
- Workshop integration
- Anvil creation
- Apothecary Table creation
- Camp placement
- Blacksmith functionality
- Alchemy functionality
- Save / Reload
- Validation
- Atomic Undo / Redo
- Idempotence
- Extended in-game stability testing

### Upgrade All Equipment

Verified:

- Approved equipment identification
- Existing flag preservation
- Bitmask updates
- Missing flag creation
- Validation
- Save / Reload
- Atomic Undo / Redo
- In-game upgrade functionality
- Regression testing after object mutation recovery

### Gameplay Modifications

Verified:

- Weather frequency modifications
- Rain disabled through gameplay configuration
- Extended in-game weather testing
- Stable operation during long-duration gameplay

---

# Current Architecture

The editor is organized around reusable architectural subsystems.

- Editing
- Snapshots
- Profiles
- Validation
- Project Mutation
- Content Creation
- Project Operations
- Transaction Framework

All subsystems operate on the same ProjectModel and ultimately modify the same RootDocument.

Current operation pipeline:

MainViewModel

↓

ProjectOperationService

↓

IProjectOperation

↓

ContentCreationService

↓

ProjectMutationService

↓

ProjectMutationResult

↓

Operation Validation

↓

Commit / Rollback

Rollback is mutation-based rather than rebuilding ProjectModel.

This preserves:

- modification tracking
- undo / redo history
- editor state
- selection state
- reusable operation composition

PropertyModel.IsModified remains the single source of truth for modification tracking.

---

# Completed Milestones

The following milestones have been completed and verified.

- ✅ Project Foundation
- ✅ Data Browser
- ✅ First Functional Editor
- ✅ Find Anything
- ✅ Smart Property Editors
- ✅ Safe Editing Infrastructure
- ✅ Change Summary
- ✅ Snapshot Workflow
- ✅ Snapshot User Interface
- ✅ Complete Profile Manager
- ✅ Validation Framework
- ✅ Project Mutation Layer
- ✅ Content Creation Infrastructure
- ✅ Project Operation Framework
- ✅ Transaction Framework
- ✅ Nested Property Discovery
- ✅ Nested Property Identity
- ✅ Nested Property Mutation Infrastructure
- ✅ Object Mutation Infrastructure
- ✅ Add Camp Facilities (Fully Verified In-Game)
- ✅ Upgrade All Equipment (Fully Verified In-Game)
- ✅ Weather Gameplay Modification (Verified In-Game)

---

# Current Roadmap

The editor has transitioned from building foundational infrastructure to expanding reusable content creation capabilities.

The next phase will focus on organizing future work into logical implementation groups before additional development begins.

## Content Creation

Planned future features include:

- Additional camp facilities
- Additional camp decorations
- Additional faction equipment recipes
- NPC creation
- Profession creation
- Additional gameplay objects
- Future content creation tools

## Gameplay Tweaks

Future gameplay modifications will be grouped into a dedicated milestone.

Current roadmap items include:

- Starting resources
- Starting knowledge unlocks
- Path challenge balancing
- Economy adjustments
- Additional weather options
- Difficulty adjustments

## Editor Improvements

Future editor improvements include:

- Validation expansion
- Additional property editors
- Batch editing tools
- Batch operations
- Performance improvements
- Search improvements

## Long-Term Features

Future long-term goals include:

- Merge preview
- Additional transaction testing
- Community sharing after Version 1.0
- In-game profile credits
- Byte-for-byte CDB formatting preservation

---

# Development Workflow

Development now follows a dual-AI workflow.

## ChatGPT

Responsibilities:

- Software architecture
- Milestone planning
- Documentation
- Code review
- Runtime verification planning
- Long-term design decisions

## Codex

Responsibilities:

- Project implementation
- File modification
- Project compilation
- Architectural preservation
- Complete file replacement generation

Every milestone follows this workflow:

1. Architectural planning
2. Codex implementation
3. Codex compilation
4. Visual Studio build verification
5. Runtime testing
6. Documentation updates
7. Git commit
8. Git push

No milestone is considered complete until runtime testing has been successfully completed.

---

# Current Task

1. Complete remaining documentation updates.
2. Perform final documentation review.
3. Final build verification.
4. Commit documentation.
5. Push to GitHub.
6. Conduct a comprehensive roadmap review.
7. Organize remaining work into future implementation milestones.

---

# Project Health

The project is currently considered architecturally stable.

The editor now possesses a mature reusable infrastructure including:

- Transactional content creation
- Mutation-based rollback
- Nested property editing
- Object-valued mutation
- Operation validation
- Reusable project operations

Future development is expected to focus primarily on extending the existing architecture rather than redesigning it.

The project has successfully transitioned from infrastructure construction into feature expansion while maintaining a clean, reusable architecture suitable for long-term development.