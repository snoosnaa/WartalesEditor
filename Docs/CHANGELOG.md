# Changelog

All notable changes to Wartales Editor are documented in this file.

The format is inspired by "Keep a Changelog" and adapted for this project.

---

# Version 0.2.0 - Find Anything

**Released:** 2026-07-11

## Added

### Search

- Global **Find Anything** panel
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

## Changed

- Renamed **Search Results** to **Find Anything**
- Simplified the search results interface
- Combined localized names and internal IDs into a single Name column
- Improved search navigation throughout the application

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
- JSON parsing using Newtonsoft.Json

### User Interface

- Three-pane editor layout
- Categories pane
- Settings pane
- Properties pane
- Status bar
- Pane headers
- Search scope selector
- Show Empty Categories option

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
- Automatic selection synchronization

### Editing

- Editable property values
- Direct RootDocument editing
- SourceProperty binding
- Save modified CDB files
- Reload edited CDB files

## Changed

- Renamed UI terminology:
  - Sheets → Categories
  - Entries → Settings
- Hidden empty Categories by default.
- Improved navigation between Categories and Settings.
- Replaced read-only property display with editable controls.

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

Future versions will continue documenting:

- Added features
- Changed behavior
- Bug fixes
- Performance improvements
- Documentation updates