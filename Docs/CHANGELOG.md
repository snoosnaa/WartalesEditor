# Changelog

All notable changes to Wartales Editor are documented in this file.

The format is inspired by Keep a Changelog and adapted for this project.

---

# Version 0.4.0 - Safe Editing Infrastructure

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

- Renamed Search Results → Find Anything
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