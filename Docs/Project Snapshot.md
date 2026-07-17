# Wartales Editor

## Project Snapshot

**Application Version:** 0.7.0\
**Documentation Version:** 0.8\
**Last Updated:** 2026-07-16

------------------------------------------------------------------------

# Project Vision

Wartales Editor is a desktop WPF application for editing Wartales game
data safely, intelligently, and efficiently.

The editor focuses on gameplay concepts rather than raw JSON while
maintaining a clean, extensible MVVM architecture.

The long-term goal is to become a professional-quality editor supporting
both casual modders and advanced content creators while remaining
resilient across future Wartales updates.

------------------------------------------------------------------------

# Project Status

**Status:** Builds Successfully ✅

**Current Release:** Version 0.7.0

**Current Milestone Status:** Complete Profile Manager

The application now provides:

-   Intelligent navigation
-   Localization-aware search
-   Type-aware property editors
-   Safe modification tracking
-   Unlimited Undo / Redo
-   Live Change Summary
-   Complete Snapshot workflow
-   Complete Mod Profile workflow
-   Import / Export profiles
-   Profile library management
-   Constructor-injected services
-   Dialog abstraction
-   Workflow-based architecture

The next milestone is **Version 0.8.0 -- Validation Framework (Pass
1).**

------------------------------------------------------------------------

# Current Architecture

    PropertyModel
            │
    ModifiedChanged
            ▼
    MainViewModel
            ▼
    Project.IsModified

    Profile Manager UI
            │
            ▼
    ProfileManagerViewModel
            │
            ▼
    ModProfileWorkflowService
            │
            ▼
    ModificationSnapshotWorkflowService
            │
            ▼
    Match
    Preview
    Apply

Only one implementation exists for:

-   Snapshot Matching
-   Snapshot Preview
-   Snapshot Application
-   Property modification tracking

`PropertyModel.IsModified` remains the single source of truth.

------------------------------------------------------------------------

# Completed Milestones

-   ✅ Project Foundation
-   ✅ Data Browser
-   ✅ First Functional Editor
-   ✅ Find Anything
-   ✅ Smart Property Editors
-   ✅ Safe Editing
-   ✅ Change Summary
-   ✅ Snapshot Workflow Foundation
-   ✅ Snapshot UI
-   ✅ Complete Profile Manager

------------------------------------------------------------------------

# Current Roadmap

## Version 0.8.0

Validation Framework

Goals:

-   Save validation
-   Profile validation
-   Missing reference detection
-   Invalid references
-   Invalid values
-   Validation reports
-   Extensible validation architecture

## Future Priorities

1.  Content Creation Tools

    -   Camp anvil
    -   Equipment upgrades
    -   Guided gameplay tools

2.  UI Modernization

3.  Post-1.0 Features

    -   Optional community profile sharing
    -   Merge Preview
    -   Batch Editing
    -   Validation reports
    -   In-game profile credits
    -   Byte-for-byte CDB formatting preservation improvements

------------------------------------------------------------------------

# Current Task

Documentation is complete.

Next steps:

1.  Final build.
2.  Commit Version 0.7.0.
3.  Push to GitHub.
4.  Begin Version 0.8.0 -- Validation Framework.