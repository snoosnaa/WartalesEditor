# Wartales Editor Dashboard

**Document Version:** 1.0  
**Last Updated:** 2026-07-10

---

# Project Status

**Current Phase:** Phase 2 - Property Editing

**Overall Progress:** Approximately 25%

The application has successfully transitioned from a basic file loader into a functional data browser with a three-pane interface. The current focus is converting the property viewer into a fully functional property editor.

---

# Current Milestone

## Milestone 0.3.0 - Property Editing

### Objective

Allow users to modify property values and prepare the application for saving changes back to the CDB.

### Current Progress

- [ ] Editable property values
- [ ] Detect modified values
- [ ] Visual indication of modified values
- [ ] Save modified CDB

---

# Completed Milestones

## Milestone 0.1.0 - Project Foundation

Completed

Features

- Project created
- MVVM architecture established
- Git repository created
- GitHub repository connected
- Documentation structure created
- JSON loading implemented

---

## Milestone 0.2.0 - Browse Entries

Completed

Features

- Open CDB files
- Load project
- Display sheets
- Display entries
- Display entry properties
- Three-pane interface
- Entry selection
- Property viewing
- Internal ID display
- PropertyModel architecture introduced

---

# Upcoming Milestones

## Milestone 0.4.0

Saving

- Save modified CDB
- Preserve file structure
- Prevent accidental data loss

---

## Milestone 0.5.0

Search & Navigation

- Search sheets
- Search entries
- Search properties
- Navigation improvements

---

## Version 1.1

Quality of Life

- Group empty sheets
- Display entry counts
- Improved status bar
- Better property descriptions

---

## Version 1.2

Batch Operations

- Update multiple entries
- Filtering
- Preview changes
- Set/Add/Multiply operations

---

## Version 1.3

Change Migration

- Compare CDB files
- Export change sets
- Import change sets
- Conflict detection
- Apply edits to updated game versions

---

# Current Architecture

Current data flow

Project

↓

Sheets

↓

SelectedSheet

↓

Entries

↓

SelectedEntry

↓

Properties

---

# Current Capabilities

The editor can currently:

- Open CDB files
- Parse project data
- Display all sheets
- Display entries for a selected sheet
- Display properties for a selected entry
- Navigate using a three-pane interface

The editor cannot yet:

- Edit property values
- Save changes
- Search data
- Perform batch operations

---

# Current Priorities

Priority 1

Finish property editing.

Priority 2

Implement saving.

Priority 3

Search and navigation improvements.

Priority 4

Quality of life features.

---

# Backlog Highlights

- Batch property editing
- Change migration between game versions
- Undo / Redo
- Compare two CDB files
- Property descriptions
- Optional grouping of empty sheets
- Raw JSON viewer
- Plugin architecture (future consideration)

---

# Notes

This document serves as the project's high-level overview.

Detailed implementation notes belong in:

- CurrentTask.md
- DevelopmentJournal.md
- KnowledgeBase.md
- Roadmap.md

The Dashboard should always answer the question:

**"What is the current state of the project?"**