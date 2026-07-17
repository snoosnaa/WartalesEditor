# Wartales Editor

## Project Snapshot

**Application Version:** 0.8.0  
**Documentation Version:** 0.9  
**Last Updated:** 2026-07-17

---

# Project Vision

Wartales Editor is a desktop WPF application for editing Wartales game data safely, intelligently, and efficiently.

The editor focuses on gameplay concepts rather than raw JSON while maintaining a clean, extensible MVVM architecture.

The long-term goal is to become a professional-quality editor supporting both casual modders and advanced content creators while remaining resilient across future Wartales updates.

---

# Project Status

**Status:** Builds Successfully ✅

**Current Release:** Version 0.8.0

**Current Milestone Status:** Validation Framework Complete

The application now provides:

- Intelligent navigation
- Localization-aware search
- Type-aware property editors
- Safe modification tracking
- Unlimited Undo / Redo
- Live Change Summary
- Complete Snapshot workflow
- Complete Mod Profile workflow
- Validation Framework
- Validation Results window
- Save validation
- Constructor-injected services
- Workflow-based architecture

The next milestone is **Version 0.9.0 – Content Creation Tools (Pass 1).**

---

# Current Architecture

The editor is built around four reusable subsystems:

- Editing
- Snapshots
- Profiles
- Validation

Property modification remains the single source of truth.

Profiles compose the Snapshot workflow.

Validation composes the existing editing workflow.

No duplicate implementations exist for:

- Property modification tracking
- Snapshot Matching
- Snapshot Preview
- Snapshot Application
- Validation Pipeline

`PropertyModel.IsModified` remains the single source of truth.

---

# Completed Milestones

- ✅ Project Foundation
- ✅ Data Browser
- ✅ First Functional Editor
- ✅ Find Anything
- ✅ Smart Property Editors
- ✅ Safe Editing
- ✅ Change Summary
- ✅ Snapshot Workflow
- ✅ Complete Profile Manager
- ✅ Validation Framework

---

# Current Roadmap

## Version 0.9.0

Content Creation Tools (Pass 1)

Initial goals:

- Add Camp Anvil
- Make All Equipment Upgradeable

These tools will automatically integrate with:

- Undo / Redo
- Change Summary
- Snapshots
- Profiles
- Validation

## Following Milestone

UI Modernization

Including:

- Utility window sizing
- Utility window placement
- Same-monitor window placement
- Taskbar buttons for utility windows
- Keyboard shortcuts
- Workflow polish

## Future

- Validation Expansion
- Batch Editing
- Merge Preview
- Community profile sharing
- In-game profile credits
- Byte-for-byte CDB formatting preservation

---

# Current Task

Documentation update in progress.

Next steps:

1. Complete remaining documentation.
2. Final build verification.
3. Commit Version 0.8.0.
4. Push to GitHub.
5. Begin Version 0.9.0 – Content Creation Tools (Pass 1).