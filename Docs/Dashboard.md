# Wartales Editor Dashboard

**Application Version:** 0.4.0
**Document Version:** 1.3
**Last Updated:** 2026-07-12

---

# Project Status

**Current Phase:** Phase 5 – Editing Platform

**Overall Progress:** Approximately 65%

The project has evolved from a functional gameplay editor into a robust editing platform with intelligent editing, safe editing, and reusable editing infrastructure.

The editor now provides a reliable workflow for editing, tracking, reverting, and saving gameplay changes while maintaining a clean MVVM architecture.

---

# Current Milestone

## Milestone 0.5.0 – Change Summary (Pass 1)

### Objective

Provide users with a clear summary of every modification made during the current editing session before saving.

### Planned Features

- [ ] Modified property collection
- [ ] Change Summary window
- [ ] Group changes by Category
- [ ] Group changes by Setting
- [ ] Display Original Value
- [ ] Display Current Value
- [ ] Navigate directly to changed properties
- [ ] Foundation for future batch editing

---

# Completed Milestones

## Milestone 0.1.0 – Project Foundation

Completed

### Features

- WPF application
- MVVM architecture
- Git repository
- GitHub integration
- Documentation system
- JSON loading

---

## Milestone 0.2.0 – Data Browser

Completed

### Features

- Categories
- Settings
- Properties
- Three-pane interface
- PropertyModel architecture

---

## Milestone 0.3.0 – First Functional Editor

Completed

### Features

- Editable properties
- RootDocument synchronization
- Save modified CDB
- Reload edited files
- In-game verification
- Show Empty Categories
- Search scopes

---

## Milestone 0.3.1 – Find Anything & Smart Editors

Completed

### Features

- Global Find Anything
- Localization-aware search
- Search by:
  - Internal IDs
  - English names
  - Property names
  - Property values
- Direct navigation
- Type-aware property editors
- Validation framework
- Reference-aware dropdown editors
- Smart property editor selection

---

## Milestone 0.4.0 – Safe Editing

Completed

### Features

- Property modification tracking
- Project modification tracking
- Original value capture
- Reset Property
- Modified indicators
- Modification status
- Unlimited Undo
- Unlimited Redo
- Ctrl+Z / Ctrl+Y
- EditHistoryService
- PropertyEditAction
- Reusable editing history architecture

---

# Upcoming Milestones

## Milestone 0.5.0

Change Summary

- Review all pending edits
- Group changes
- Navigation to modified properties
- Save review workflow

---

## Milestone 0.6.0

Workflow Improvements

- QuickBMS integration
- Recent Files
- Save & Exit
- Backup on Save
- Remember preferences

---

## Version 1.0

Public Release

- Complete editing workflow
- Validation
- Batch editing
- Import / Merge
- Mod Profiles
- Localization support
- Documentation complete

---

# Current Workflow

```text
Open

↓

Find Anything

↓

Edit

↓

Track Changes

↓

Undo / Redo

↓

Save

↓

Package

↓

Play
```

This workflow has been verified through live gameplay testing.

---

# Current Capabilities

The editor currently supports:

## Navigation

- Open CDB files
- Browse Categories
- Browse Settings
- Browse Properties
- Global Find Anything
- Localization-aware searching
- Direct navigation

## Editing

- Edit gameplay properties
- Type-aware editors
- Reference-aware dropdowns
- Validation
- RootDocument synchronization
- Save edited CDB files

## Safe Editing

- Property tracking
- Project tracking
- Reset Property
- Modified indicators
- Window title dirty indicator
- Modification status
- Unlimited Undo
- Unlimited Redo

## Verification

- Successful in-game gameplay verification

---

# Current Priorities

## Priority 1

Change Summary

Focus:

- Summarize edits
- Review changes
- Navigation from summary

---

## Priority 2

Workflow Improvements

- QuickBMS integration
- Recent Files
- Save & Exit
- Backup on Save

---

## Priority 3

Advanced Editing

- Batch editing
- Import / Merge
- Mod Profiles

---

## Priority 4

Quality of Life

- Property descriptions
- Developer mode
- Better diagnostics
- Better navigation

---

# Backlog Highlights

High Priority

- Change Summary
- Batch Editing
- Import / Merge

Medium Priority

- QuickBMS integration
- Mod Profiles
- Property descriptions

Low Priority

- Preserve text caret position during Undo/Redo in text editors
- Developer mode
- Performance diagnostics

Future Ideas

- Compare CDB files
- Plugin architecture
- Rule-based validation
- Advanced diagnostics

---

# Most Recent Accomplishment

Completed **Safe Editing**.

The editor now supports:

- Property modification tracking
- Project dirty-state tracking
- Reset Property
- Unlimited Undo/Redo
- Reusable editing history infrastructure

This establishes the foundation for all future editing workflows.

---

# Notes

This document serves as the project's executive overview.

Detailed implementation information belongs in:

- CurrentTask.md
- DevelopmentJournal.md
- Architecture.md
- KnowledgeBase.md
- Roadmap.md

The Dashboard should always answer one question:

> **"What is the current state of the project today?"**