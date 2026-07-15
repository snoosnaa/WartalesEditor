# Changelog

All notable changes to Wartales Editor are documented in this file.

The format is inspired by Keep a Changelog and adapted for this project.

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

## Added

### Change Summary

- Read-only Change Summary window
- Live summary of all modified properties
- Category grouping
- Original value display
- Current value display
- Automatic refresh after edits
- Automatic refresh after Undo
- Automatic refresh after Redo
- Automatic refresh after Reset Property
- Automatic refresh after Save
- Automatic refresh after opening another project
- Navigation from Change Summary back to the editor
- Double-click navigation
- Change Summary toolbar command

### Editing Infrastructure

- ChangeSummaryItemModel
- ChangeSummaryViewModel
- Snapshot-based Change Summary architecture
- Localized setting names within Change Summary
- Navigation callback architecture for editor integration

## Changed

- Change Summary now uses the existing `PropertyModel` modification state as its single source of truth.
- Eliminated duplicate change tracking.
- Simplified Change Summary grouping to Category level.
- Improved editor focus after navigation.
- Improved Change Summary presentation using localized setting names.
- Improved Change Summary window behavior and interaction.

## Fixed

- Corrected Change Summary window activation behavior.
- Corrected Change Summary Close button behavior.
- Corrected grouped header alignment.
- Corrected display of internal IDs where localized names are available.

## Verified

Successfully verified:

- Live Change Summary updates
- Undo integration
- Redo integration
- Reset Property integration
- Save baseline reset
- Navigation to modified property
- Double-click navigation
- Localized display names
- Category grouping
- Modeless Change Summary window
- Window reopening behavior

---

# Version 0.4.0 - Safe Editing & Undo/Redo

**Released:** 2026-07-12

## Added

### Safe Editing

- Property modification tracking
- Project modification tracking
- Original value capture
- Reset Property
- Modified property indicators
- Modified property counter
- Window title modification indicator
- Modification status reporting

### Undo / Redo

- Unlimited session undo history
- Unlimited session redo history
- Undo toolbar command
- Redo toolbar command
- Ctrl+Z support
- Ctrl+Y support
- Automatic history reset when opening a project

### Editing Infrastructure

- EditHistoryService
- PropertyEditAction
- Property value change events
- Reusable history architecture
- History-aware property editing
- Foundation for Change Summary
- Foundation for Batch Editing
- Foundation for Import/Merge

## Changed

- PropertyModel now tracks original values.
- JsonDataService now captures original values after loading.
- Saving establishes a new editing baseline.
- Modification state now updates automatically throughout the application.
- Undo/Redo integrated into the MVVM editing workflow.

## Verified

Successfully verified:

- Property tracking
- Project tracking
- Reset Property
- Unlimited Undo
- Unlimited Redo
- Toolbar commands
- Keyboard shortcuts
- Save state reset
- History reset when opening another project

---

# Version 0.2.0 - Find Anything

**Released:** 2026-07-11

## Added

### Search

- Global Find Anything panel
- Search across every Category
- Search internal IDs
- Search English localized names
- Search property names
- Search property values
- Automatic navigation to matching search results
- Automatic property selection
- Search result count

### Localization

- LocalizationService
- Import support for `export_en.xml`
- English localization support
- Localization-aware searching
- Combined display of English names and internal IDs

### Smart Editing

- Type-aware property editors
- Validation framework
- Dropdown editor framework
- Category-aware reference discovery
- ReferenceValueModel
- Smart property editor selection

## Changed

- Renamed Search Results → Find Anything.
- Simplified the search interface.
- Combined localized names and internal IDs into a single Name column.
- Improved search navigation.
- Built an extensible property editor architecture.

---

# Version 0.1.0 - First Functional Editor

**Released:** 2026-07-11

## Added

### Project Foundation

- WPF desktop application
- MVVM architecture
- Git repository
- GitHub integration
- Project documentation
- Newtonsoft.Json integration

### User Interface

- Three-pane editor
- Categories pane
- Settings pane
- Properties pane
- Status bar
- Pane headers
- Search scope selector
- Show Empty Categories

### Data Model

- ProjectModel
- SheetModel
- EntryModel
- PropertyModel

### Navigation

- Category selection
- Setting selection
- Property viewing
- Category search
- Setting search
- Selection synchronization

### Editing

- Editable property values
- RootDocument editing
- SourceProperty binding
- Save modified CDB files
- Reload edited CDB files

## Changed

- Renamed:
  - Sheets → Categories
  - Entries → Settings
- Hidden empty Categories by default.
- Improved navigation.
- Replaced read-only properties with editable controls.

## Verified

Successfully completed the first end-to-end editing workflow.

Verified:

- Open original CDB
- Browse Categories
- Browse Settings
- Edit property values
- Save modified CDB
- Reload edited CDB
- Package using QuickBMS
- Launch Wartales
- Verify gameplay modifications

### First Verified Gameplay Edit

Item

- DaggerStart (Rusty Shiv)

Property

- price

Modified

- 50 → 999

Successfully verified inside Wartales.

---

# Future Releases

Future releases will continue documenting:

- New features
- Architectural improvements
- User interface enhancements
- Bug fixes
- Performance improvements
- Documentation updates