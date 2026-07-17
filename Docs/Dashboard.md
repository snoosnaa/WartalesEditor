# Wartales Editor Dashboard

**Application Version:** 0.8.0  
**Document Version:** 1.6  
**Last Updated:** 2026-07-17

---

# Project Status

**Current Phase:** Phase 5 – Editing Platform

Wartales Editor has evolved into a stable editing platform built around reusable infrastructure.

The application now supports intelligent editing, unlimited undo/redo, live change tracking, reusable modification profiles, snapshot migration, and a complete validation framework while maintaining a clean MVVM architecture.

The project's architecture continues to prioritize reusable subsystems over feature-specific implementations.

---

# Current Release

## Version 0.8.0 – Validation Framework (Pass 1)

**Status:** ✅ Complete

### Major Features

- Reusable validation framework
- Validation workflow orchestration
- Extensible validation rule pipeline
- Validation issue and result models
- Validation severity and categories
- Save validation
- Manual project validation
- Validation Results window
- Validation filtering
- Validation navigation
- Copy Results
- Re-run validation
- Single-instance modeless validation workflow

---

# Completed Milestones

## Version 0.1.0 – Project Foundation

Completed

### Highlights

- WPF desktop application
- MVVM architecture
- Git & GitHub integration
- Documentation system
- JSON loading

---

## Version 0.2.0 – Data Browser

Completed

### Highlights

- Three-pane editor
- Categories
- Settings
- Properties
- ProjectModel architecture

---

## Version 0.3.0 – Functional Editing

Completed

### Highlights

- Editable properties
- RootDocument synchronization
- Save modified CDB
- Reload edited files
- Gameplay verification

---

## Version 0.3.1 – Find Anything

Completed

### Highlights

- Global Find Anything
- Localization-aware search
- Intelligent property editors
- Reference-aware dropdowns

---

## Version 0.4.0 – Safe Editing

Completed

### Highlights

- Property modification tracking
- Project dirty-state tracking
- Reset Property
- Unlimited Undo / Redo
- Edit history architecture

---

## Version 0.5.0 – Change Summary

Completed

### Highlights

- Live Change Summary
- Original / Current comparison
- Navigation to modified properties
- Automatic synchronization
- Reusable summary architecture

---

## Version 0.6.0 – Snapshot Workflow

Completed

### Highlights

- Snapshot export
- Snapshot preview
- Snapshot import
- Snapshot matching
- Snapshot application
- Workflow orchestration
- Migration-ready architecture

---

## Version 0.7.0 – Complete Profile Manager

Completed

### Highlights

- Complete profile management
- Profile library
- Create, Rename, Duplicate
- Apply, Import, Export
- Delete Profile
- Profile Details dialog
- Shared snapshot integration
- Complete reusable profile architecture

---

## Version 0.8.0 – Validation Framework

Completed

### Highlights

- Reusable validation architecture
- Validation pipeline
- Validation rules
- Validation Results window
- Validation navigation
- Save validation
- Manual validation
- Single-instance modeless validation UI

---

# Current Workflow

```text
Open Project
        │
        ▼
Find Anything
        │
        ▼
Edit Properties
        │
        ▼
Track Changes
        │
        ▼
Undo / Redo
        │
        ▼
Review Change Summary
        │
        ▼
Create / Apply Profiles
        │
        ▼
Validate Project
        │
        ▼
Save
        │
        ▼
Package
        │
        ▼
Play
```

This workflow has been verified through live Wartales testing.

---

# Current Capabilities

## Navigation

- Open CDB files
- Browse Categories
- Browse Settings
- Browse Properties
- Global Find Anything
- Localization-aware search
- Direct navigation

---

## Editing

- Intelligent property editors
- Reference-aware dropdowns
- RootDocument synchronization
- Safe save workflow

---

## Safe Editing

- Property modification tracking
- Project dirty-state tracking
- Reset Property
- Unlimited Undo / Redo
- Session edit history

---

## Change Review

- Live Change Summary
- Original vs Current comparison
- Navigation to modified properties

---

## Snapshot & Profiles

- Export snapshots
- Import snapshots
- Snapshot matching
- Snapshot preview
- Snapshot application
- Complete profile management
- Reusable modification profiles
- Profile library

---

## Validation

- Validation framework
- Validation pipeline
- Save validation
- Manual validation
- Validation Results window
- Navigation to issues
- Severity filtering
- Validation summary

---

# Roadmap

## Current Milestone

### Content Creation Tools

Primary goals

- Camp structure creation
- Camp anvil support
- Additional game object creation
- Expansion-friendly editing

---

## Following Milestone

### UI Modernization

Primary goals

- Improved utility window management
- Consistent window sizing
- Same-monitor utility window placement
- Taskbar support for utility windows
- Keyboard shortcuts for utility windows
- Improved command area
- Larger action buttons
- Visual polish

---

## Future Milestones

- Merge Preview
- Batch Editing
- Validation reports
- Community profile sharing
- In-game profile credits
- Byte-for-byte CDB formatting preservation

---

# Current Priorities

## Priority 1

Content Creation Tools

---

## Priority 2

UI Modernization

---

# Known Minor Issues

Low Priority

- Programmatic Undo/Redo may reposition the caret within certain WPF text editors.
- Utility window positioning and sizing will be standardized during UI Modernization.

---

# Most Recent Accomplishment

Completed **Version 0.8.0 – Validation Framework (Pass 1).**

The editor now provides a complete editing platform built around four reusable subsystems:

- Editing
- Snapshots
- Profiles
- Validation

This architecture provides the foundation for future Content Creation Tools while ensuring every new feature automatically benefits from:

- Undo / Redo
- Change Summary
- Profiles
- Validation
- Migration

---

# Notes

This document serves as the project's executive overview.

Implementation details belong in:

- CurrentTask.md
- DevelopmentJournal.md
- Architecture.md
- KnowledgeBase.md

The Dashboard should always answer one question:

> **"What is the current state of the project today?"**