# Current Task

## Current Milestone

**Snapshot UI – Pass 1**

---

## Current Status

The project builds successfully.

The Snapshot Workflow Foundation milestone has been completed and verified.

Current build status:

✅ Build succeeds.

---

## Recently Completed

### Snapshot Workflow Foundation

- Snapshot capture engine
- Snapshot serialization
- Snapshot loading
- Snapshot matching
- Snapshot preview
- Snapshot application
- ModificationSnapshotWorkflowService
- Snapshot import/export result models

### Architecture Improvements

- Constructor injection for MainViewModel
- IFileDialogService
- IMessageDialogService
- WpfFileDialogService
- WpfMessageDialogService
- Dialog abstraction
- Workflow orchestration layer

### User Interface Foundation

- Standard application menu
- File menu
- Edit menu
- View menu
- Tools menu
- Help menu
- Snapshot menu foundation
- Validation menu placeholder
- Developer Tools placeholder

### Runtime Verification

Successfully verified:

- Constructor injection
- File dialog abstraction
- Message dialog abstraction
- Menu integration
- Open and Save workflows
- Ctrl+O
- Ctrl+S
- Undo
- Redo
- Reset Property
- Change Summary
- Navigate button
- Double-click navigation
- Successful builds after every implementation stage

---

## Current Objective

Implement the first complete Snapshot user workflow.

The initial goal is to connect the existing workflow infrastructure to the user interface.

### Export Snapshot

- SaveFileDialog
- Workflow.Export()
- Success summary

### Preview Snapshot

- OpenFileDialog
- Workflow.Load()
- Workflow.Preview()
- Summary dialog
- No project modifications

### Import Snapshot

- OpenFileDialog
- Workflow.ImportAndApplySafely()
- Summary dialog
- Project modification refresh
- Undo / Redo compatibility

The objective is to complete the first end-to-end snapshot workflow using live Wartales data.

---

## Next Planned Milestones

Priority order:

### 1. Mod Profiles / Change Migration

Primary objective:

Allow users to preserve their edits across future Wartales updates.

Expected capabilities:

- Save reusable mod profiles
- Export only modified values
- Import changes into newer game data
- Intelligent matching
- Merge preview
- Conflict detection

---

### 2. Validation Framework

Primary objective:

Prevent invalid edits before export.

Expected capabilities:

- Missing reference detection
- Invalid reference detection
- Invalid value detection
- Required property validation
- Duplicate detection
- Validation reports

---

### 3. Content Creation Tools

Primary objective:

Allow creation of additional game content.

Initial focus:

- Add Camp Structures
- Add Crafting Stations
- Add Camp Anvil
- Guided gameplay content creation

---

### 4. UI Modernization

Planned improvements:

- Workflow-oriented toolbar
- Larger action buttons
- Improved command organization
- Layout polish
- Future icon support

---

## Architecture Notes

The editing architecture remains stable.

Current editing flow:

```text
PropertyModel
        │
ModifiedChanged
        │
        ▼
MainViewModel
        │
        ▼
Project.IsModified
```

History flow:

```text
PropertyModel
        │
ValueChanged
        │
        ▼
EditHistoryService
        │
        ▼
Undo / Redo
```

Snapshot workflow:

```text
Project
    │
    ▼
Snapshot
    │
    ▼
Workflow Service
    │
 ┌──┼───────────────┐
 ▼  ▼               ▼
Export Preview Import
```

The modification state remains the single source of truth for editing.

Snapshot workflows build on the existing editing architecture rather than introducing duplicate tracking systems.

---

## Known Minor Issues

Low Priority

- Programmatic Undo/Redo may reposition the text caret within certain WPF text editors.
- This does not affect data integrity.

Deferred until the future UI Modernization milestone.

---

## Build Status

✅ Last build successful

---

## Next Session

Implement:

- Export Snapshot
- Preview Snapshot
- Import Snapshot

Then perform complete end-to-end testing using live Wartales data before beginning Mod Profiles.