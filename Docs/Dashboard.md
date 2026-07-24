# Wartales Editor Dashboard

**Application Version:** 0.9.1  
**Document Version:** 1.9  
**Last Updated:** 2026-07-24

---

# Project Status

**Current Phase:** Player-Facing Gameplay Development

Wartales Editor has successfully transitioned from a safe editing platform into a transactional content creation platform.

The editor now provides reusable editing, validation, mutation, operation, transaction, and rollback infrastructure capable of safely introducing new gameplay content while preserving long-term maintainability.

The architecture has now been verified through both editor testing and extended in-game gameplay.

---

# Current Release

## Version 0.9.1 — World Convenience

**Status:** ✅ Complete

### Major Features

- Project Operation Framework
- Project Mutation Layer
- Content Creation Infrastructure
- Transaction Framework
- Mutation-based rollback
- Operation-specific validation
- Nested property discovery
- Nested property mutation infrastructure
- Object-valued mutation infrastructure
- First verified reusable content creation tools

### Verified In-Game

Successfully verified:

- Modified CDB loads correctly.
- Weather modifications operate as expected.
- Add Camp Facilities executes successfully.
- New recipes unlock through normal gameplay.
- Anvil can be built, placed, and used.
- Apothecary Table can be built, placed, and used.
- Blacksmith functionality verified.
- Alchemy functionality verified.
- Upgrade All Equipment verified.
- Save validation succeeds.
- Save / Reload succeeds.
- Atomic Undo / Redo verified.
- Idempotence verified.
- Transaction rollback verified through forced validation failure.
- Extended gameplay stability confirmed.

---

# Current Workflow

```text
Open Project
      ↓
Edit
      ↓
Project Operation
      ↓
Project Mutation
      ↓
Operation Validation
      ↓
Commit / Rollback
      ↓
Save Validation
      ↓
Save
      ↓
Package
      ↓
Playtest
```

This workflow has now been verified through multiple editor tests and extended in-game testing.

---

# Current Capabilities

## Editing Platform

- Intelligent editing
- Undo / Redo
- Change Summary
- Snapshot workflow
- Mod Profiles
- Validation
- Nested property editing

## Content Creation Platform

- Project Mutation Layer
- Object-valued mutation
- Content Creation Services
- Project Operations
- Transaction Framework
- Mutation-based rollback
- Operation validation
- Verified camp facility creation
- Verified equipment upgrade operation
- Verified gameplay modification support
- Persistent Gameplay Operation State
- Version 2 additive profile restoration

## Verified Gameplay Features

- Add Camp Facilities
- Upgrade All Equipment
- Character and Profession XP Controls
- Starting Resources
- Volunteer Wage Reduction
- Maximum Valour and Restored Valour
- Saddlebag and Pony Carrying Capacity
- Overworld Movement Speed
- Rain Frequency

---

# Roadmap

The next phase begins with Profile polish and UI polish, followed by a
gameplay roadmap audit and bundled gameplay feature development.

## Content Creation

Planned features include:

- Additional camp facilities
- Additional camp content
- Additional faction equipment recipes
- NPC creation
- Profession creation
- Additional gameplay objects

## Gameplay Tweaks

Planned features include:

- Starting knowledge unlocks
- Path challenge balancing
- Economy adjustments
- Additional weather options
- Difficulty adjustments
- Vendor Refresh Speed
- Camera Zoom Improvements
- Resource Respawn Speed, deferred pending future runtime validation

## Editor Improvements

Planned features include:

- Update Profile
- Better profile failure reporting
- Expandable failure details
- UI polish
- Validation expansion
- Batch operations
- Additional property editors
- Performance improvements
- Search improvements

## Long-Term Features

- Merge Preview
- Community sharing after Version 1.0
- In-game profile credits
- Byte-for-byte CDB formatting preservation

---

# Current Priorities

1. Documentation complete.
2. Commit and push Version 0.9.1.
3. Profile polish investigation and implementation.
4. UI polish investigation and implementation.
5. Gameplay roadmap audit.
6. Bundled gameplay feature development.

---

# Most Recent Accomplishment

Completed and verified Version 0.9.1 World Convenience, including
Overworld Movement Speed, Rain Frequency, and profile restoration
improvements.

The editor now safely creates new gameplay content through reusable project operations, mutation-based rollback, nested object mutation, operation validation, and reusable content creation infrastructure.

All implemented gameplay features have been successfully verified through extended in-game testing.

---

# Notes

This dashboard provides the current executive overview of the project.

Detailed implementation information belongs in:

- Architecture.md
- DevelopmentJournal.md
- Changelog.md
- CurrentTask.md
- ProjectSnapshot.md
