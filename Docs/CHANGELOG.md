# Changelog

All notable changes to Wartales Editor are documented in this file.

The format is inspired by Keep a Changelog and adapted for this project.

---

# Version 0.7.0 - Complete Profile Manager

**Released:** 2026-07-16

## Added

### Profile Management

- Create Profile
- Rename Profile
- Duplicate Profile
- Apply Profile
- Import Profile
- Export Profile
- Delete Profile
- Complete Profile Manager workflow

### Profile Architecture

- ModProfileModel
- ModProfileMetadataModel
- ModProfileFormat
- ModProfileService
- ModProfileSerializationService
- ModProfileWorkflowService
- ModProfileLibraryService
- ModProfileLibraryPathService
- ModProfileSummaryModel
- Reusable Profile Details dialog
- Unified profile request model

### User Interface

- Profile Manager window
- Profile Browser
- Profile metadata display
- Profile toolbar
- Profile Details dialog
- Profile creation workflow
- Profile rename workflow
- Profile duplication workflow

## Changed

- Mod Profiles now compose the existing Snapshot workflow instead of introducing a parallel implementation.
- Profile application reuses the existing Snapshot Match, Preview, and Apply pipeline.
- Profile creation captures the current modification state using the existing editing infrastructure.
- Profile statistics simplified to display **Modified Properties**.
- Profile Manager usability improved with additional UI polish.
- Improved startup sizing for smaller displays.
- Improved Profile Details dialog layout.
- Improved profile selection behavior after profile operations.

## Fixed

- Corrected Profile Manager selection synchronization after profile operations.
- Corrected Profile Manager selection visibility.
- Corrected Profile Details dialog sizing on smaller displays.
- Improved main window startup sizing across different monitor resolutions.

## Verified

Successfully verified:

- Create Profile
- Rename Profile
- Duplicate Profile
- Export Profile
- Delete Profile
- Import Profile
- Apply Profile
- Undo compatibility
- Redo compatibility
- Change Summary integration
- Modification tracking
- Snapshot application
- Successful builds throughout implementation

---

# Version 0.6.0 - Snapshot UI – Pass 1

**Released:** 2026-07-13

## Added

### Snapshot User Interface

- Export Snapshot
- Preview Snapshot
- Import Snapshot
- Complete end-to-end snapshot workflow
- Snapshot workflow success summaries
- Snapshot preview dialog
- Snapshot import dialog
- Snapshot export dialog

### Workflow Integration

- Snapshot UI connected to `ModificationSnapshotWorkflowService`
- Automatic modification tracking refresh after snapshot import
- Automatic Change Summary refresh after snapshot import
- Seamless integration with the existing editing workflow

## Changed

- Completed the first fully functional snapshot user workflow.
- Snapshot functionality now operates entirely through the reusable workflow infrastructure.
- Snapshot import behaves identically to manual editing, preserving existing application behavior.
- Existing editing architecture reused without introducing duplicate modification tracking.

## Verified

Successfully verified:

- Export Snapshot
- Preview Snapshot
- Import Snapshot
- Modification tracking refresh
- Change Summary refresh
- Undo compatibility
- Redo compatibility
- Save compatibility
- Live Wartales testing
- Successful builds throughout implementation

---

# Version 0.5.1 - Snapshot Workflow Foundation

**Released:** 2026-07-12

## Added

### Snapshot Architecture

- ModificationSnapshotWorkflowService
- Snapshot workflow orchestration
- Snapshot export workflow
- Snapshot preview workflow
- Snapshot import workflow
- Snapshot workflow result models
- Snapshot import result model
- Snapshot export result model

### Dialog Infrastructure

- IFileDialogService
- IMessageDialogService
- WpfFileDialogService
- WpfMessageDialogService

### User Interface

- Standard application menu bar
- File menu
- Edit menu
- View menu
- Tools menu
- Help menu
- Snapshot menu foundation
- Validation menu placeholder
- Developer Tools placeholder

### Architecture

- Constructor injection for MainViewModel services
- Separation of workflow orchestration from UI
- Separation of file dialogs from ViewModel logic
- Separation of message dialogs from ViewModel logic

## Changed

- MainViewModel no longer creates WPF file dialogs directly.
- MainViewModel now receives required services through constructor injection.
- MainWindow now composes application services during ViewModel construction.
- Open and Save operations now use the dialog abstraction layer.
- Editor architecture is prepared for Snapshot UI integration.

## Fixed

- Corrected Change Summary Navigate button command-state updates after introducing explicit command notifications.
- Preserved existing editor behavior after constructor injection refactor.
- Preserved Undo/Redo, Reset Property, Change Summary, and search functionality following dialog abstraction.

## Verified

Successfully verified:

- Constructor injection
- File dialog abstraction
- Message dialog abstraction
- Menu bar integration
- File menu commands
- Edit menu commands
- View menu commands
- Ctrl+O
- Ctrl+S
- Undo
- Redo
- Reset Property
- Change Summary
- Navigate button
- Double-click navigation
- Successful build after refactoring

---

# Version 0.5.0 - Change Summary

**Released:** 2026-07-12

*(No changes to this section.)*

---

# Version 0.4.0 - Safe Editing & Undo/Redo

**Released:** 2026-07-12

*(No changes to this section.)*

---

# Version 0.2.0 - Find Anything

**Released:** 2026-07-11

*(No changes to this section.)*

---

# Version 0.1.0 - First Functional Editor

**Released:** 2026-07-11

*(No changes to this section.)*

---

# Future Releases

Future releases will continue documenting:

- New features
- Architectural improvements
- User interface enhancements
- Bug fixes
- Performance improvements
- Documentation updates