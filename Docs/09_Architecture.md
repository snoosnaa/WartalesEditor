Architecture
Version: 0.8  
Status: Active  
Last Updated: 2026-07-17  
Applies To: Entire Project
---
Overview
Wartales Editor follows the Model-View-ViewModel (MVVM) architectural pattern.
The primary architectural goals are:
Separate presentation from business logic.
Keep models independent of the user interface.
Preserve the original Wartales CDB structure.
Modify the loaded JSON document directly.
Build reusable infrastructure before implementing advanced features.
Favor extensible services over feature-specific implementations.
Maintain one implementation for each major responsibility.
Ensure future features compose existing systems instead of introducing parallel workflows.
The editor has evolved from a property editor into a modular editing platform built around four reusable subsystems:
Editing
Snapshots
Profiles
Validation
These systems share the same project state and modification pipeline.
---
Project Structure
```text
WartalesEditor
│
├── Converters
├── Docs
├── Helpers
├── Models
│   ├── Profiles
│   ├── Snapshots
│   └── Validation
├── Selectors
├── Services
│   └── Validation
├── Validation
├── ViewModels
└── Views
```
Each folder owns a specific responsibility.
---
Architectural Layers
```text
Views
  │
  ▼
ViewModels
  │
  ▼
Workflow Services
  │
  ▼
Domain Services
  │
  ▼
Models and RootDocument
```
Views
Views are responsible for layout, data binding, forwarding user interaction, window lifecycle events, and visual presentation.
Views should not contain editing, validation, snapshot, or profile business logic.
ViewModels
ViewModels are responsible for application state, selection, commands, navigation, workflow coordination, status reporting, and modeless utility-window lifecycle.
Workflow Services
Workflow services are responsible for purpose-specific orchestration, creating operation contexts, coordinating reusable lower-level services, and returning structured result models.
Domain Services
Domain services own reusable implementation logic such as loading and saving, searching, localization, edit history, snapshot processing, profile processing, and validation execution.
Models
Models own data, editing state, original and current values, structured workflow results, and serializable profile and snapshot representations.
---
Core Model Hierarchy
```text
ProjectModel
    ↓
SheetModel
    ↓
EntryModel
    ↓
PropertyModel
```
The internal model names intentionally mirror the Wartales data structure while the UI presents gameplay-oriented terminology.
Internal Model	User Interface
SheetModel	Category
EntryModel	Setting
PropertyModel	Property
Supporting models include:
```text
SearchResultModel
ReferenceValueModel
PropertyValueChangedEventArgs
PropertyEditAction
ChangeSummaryItemModel
Snapshot Models
Profile Models
Validation Models
```
---
ProjectModel
`ProjectModel` acts as the root object for an opened project.
It owns:
`RootDocument`
`Sheets`
File metadata
Original JSON text
Project modification state
`RootDocument` remains the authoritative in-memory JSON document that is serialized during Save.
---
SheetModel
`SheetModel` represents one gameplay Category.
It owns the category name and entries.
Future sheet-level features may include statistics, validation summaries, batch operations, and content-creation targets.
---
EntryModel
`EntryModel` represents one gameplay Setting.
It owns the identifier, display name, and properties.
It provides the navigation target used by search, Change Summary, validation, and future editing tools.
---
PropertyModel
`PropertyModel` is the central editing model.
It owns:
Current value
Original value
Source `JProperty`
Modification detection
Reset behavior
Type-aware conversion
Editor selection metadata
Read-only state
Display formatting
Modification events
Value-change events
Original and current token snapshots
`PropertyModel` owns editing behavior, not UI behavior.
Single Source of Truth
Modification state exists in exactly one place:
```text
PropertyModel.IsModified
```
Every feature that answers “What is currently different?” must consume this existing modification state.
Current consumers include:
Project dirty state
Modified property count
Undo / Redo integration
Change Summary
Snapshot capture
Profile creation
Save validation
Future Content Creation Tools
No parallel modification-tracking system is permitted.
---
Navigation Architecture
```text
Project
    ↓
SelectedSheet
    ↓
Entries
    ↓
SelectedEntry
    ↓
Properties
    ↓
SelectedProperty
```
Changing a higher-level selection clears lower-level selections to prevent invalid state.
Navigation targets are shared by Find Anything, Change Summary, Validation Results, and future reports and batch operations.
---
Find Anything Architecture
Search remains independent of editing.
```text
Search Text
        ↓
SearchService
        ↓
LocalizationService
        ↓
SearchResultModel
        ↓
MainViewModel Navigation
        ↓
SelectedSheet
        ↓
SelectedEntry
        ↓
SelectedProperty
```
Search is treated as navigation rather than as a second project state.
---
Property Editing Pipeline
```text
User edits Property
        ↓
PropertyModel.Value
        ↓
Type-aware conversion
        ↓
SourceProperty
        ↓
JProperty
        ↓
RootDocument
        ↓
JsonDataService.SerializeProject()
        ↓
JsonDataService.SaveProject()
        ↓
Modified CDB
```
The loaded JSON document is modified directly.
No replacement object graph is reconstructed during saving.
---
Modification Tracking Architecture
```text
PropertyModel
        │
        ├── IsModified
        └── ModifiedChanged
                │
                ▼
          MainViewModel
                │
                ├── Project.IsModified
                ├── ModifiedPropertyCount
                ├── WindowTitle
                ├── ModificationStatus
                └── ModifiedProperties
```
`PropertyModel` owns modification detection.
`MainViewModel` owns application-level presentation state.
---
Undo / Redo Architecture
```text
PropertyModel
        │
        └── ValueChanged
                │
                ▼
        EditHistoryService
                │
                ▼
        PropertyEditAction
                │
                ├── Undo()
                └── Redo()
```
History recording is independent of UI controls.
Undo and Redo operate through `PropertyModel`, ensuring that modification tracking, Change Summary, snapshots, profiles, and validation remain synchronized.
---
Change Summary Architecture
The Change Summary does not maintain its own change-tracking system.
```text
PropertyModel.IsModified
        ↓
ModificationSnapshotService
        ↓
ChangeSummaryService
        ↓
ChangeSummaryItemModel
        ↓
ChangeSummaryViewModel
        ↓
ChangeSummaryWindow
```
The summary is rebuilt from the current project state whenever modification state changes.
This guarantees that Change Summary reflects the current state rather than historical edit actions.
---
Snapshot Architecture
Snapshots provide a serializable representation of current modifications.
Snapshot Composition
```text
ProjectModel
        ↓
ModificationSnapshotService
        ↓
ModificationSnapshotModel
        ↓
Serialization / Matching / Preview / Apply
```
Snapshot Workflow
```text
Snapshot UI
        ↓
MainViewModel
        ↓
ModificationSnapshotWorkflowService
        ↓
Load / Export / Preview / Import
        ↓
Single Matching Implementation
        ↓
Single Preview Implementation
        ↓
Single Apply Implementation
```
There is exactly one implementation of:
Snapshot matching
Snapshot preview
Snapshot application
All higher-level features must reuse this workflow.
---
Profile Architecture
Mod Profiles compose the existing Snapshot workflow.
```text
Profile Manager UI
        ↓
ProfileManagerViewModel
        ↓
ModProfileWorkflowService
        ↓
ModificationSnapshotWorkflowService
        ↓
Match
Preview
Apply
```
Profiles do not introduce separate change-matching or application logic.
The profile subsystem includes profile metadata, `.wtprofile` serialization, profile library storage, Create, Rename, Duplicate, Import, Export, Delete, and Apply.
Profile creation captures the existing modification state.
Profile application reuses the Snapshot workflow.
---
Validation Architecture
Validation is a reusable subsystem shared by Save and manual validation.
Validation Layers
```text
MainViewModel
        ↓
ValidationWorkflowService
        ↓
ValidationService
        ↓
ValidationPipeline
        ↓
Validation Rules
        ↓
ValidationResultModel
```
ValidationWorkflowService
Responsible for:
Creating `ValidationContext`
Selecting the validation purpose
Gathering modified properties from `PropertyModel.IsModified`
Delegating execution to `ValidationService`
ValidationService
Responsible for:
Owning rule registration
Constructing the validation pipeline
Executing the pipeline
Other application layers do not need to know which validation rules exist.
ValidationPipeline
Responsible for evaluating rule applicability, executing applicable rules, collecting validation issues, and returning `ValidationResultModel`.
Validation Rules
Validation rules implement a shared rule interface.
Rules should:
Validate only conditions that can be established accurately.
Return structured issues.
Avoid changing project state.
Avoid duplicating editing or serialization logic.
Use the original loaded project state where appropriate.
Validation Results
```text
ValidationResultModel
        ↓
ValidationResultsViewModel
        ↓
ValidationResultsWindow
```
The Validation Results window supports severity counts, severity filtering, re-run validation, Copy Results, navigation, and single-instance modeless behavior.
Save Validation
```text
Save Command
        ↓
ValidationWorkflowService.ValidateForSave()
        ↓
Errors?
   ├── Yes → Block Save
   └── No  → Continue
        ↓
JsonDataService.SaveProject()
```
Warnings do not currently block Save.
Serialization validation reuses the same serialization path used by Save.
There is exactly one validation pipeline.
---
JSON Serialization Architecture
`JsonDataService` owns the canonical serialization path.
```text
ProjectModel.RootDocument
        ↓
JsonDataService.SerializeProject()
        ├── Validation serialization check
        └── SaveProject()
```
Validation does not implement a second serializer.
---
Reference Data Architecture
```text
ProjectModel
        ↓
ReferenceDataService
        ↓
Discovered and fallback values
        ↓
ReferenceValueModel
        ↓
Dropdown editors
```
Reference values are associated with the appropriate property context.
Future validation rules may reuse this service only when a reference relationship can be verified reliably.
---
Localization Architecture
```text
export_en.xml
        ↓
LocalizationService
        ↓
Localized name lookup
        ↓
Search / Change Summary / UI display
```
Localization is independent of the stored CDB values.
The editor preserves stored values while presenting English display text when available.
---
Service Responsibilities
JsonDataService
Responsible for loading files, parsing JSON, building `ProjectModel`, capturing original values, serializing `RootDocument`, saving projects, and accepting current values after a successful save.
SearchService
Responsible for global searching, property searching, result generation, and navigation information.
LocalizationService
Responsible for loading localization XML, localized name lookup, and future language support.
ReferenceDataService
Responsible for discovering valid reference values, managing fallback values, and populating dropdown editors.
EditHistoryService
Responsible for recording edits, Undo, Redo, session history, and history notifications.
ChangeSummaryService
Responsible for building presentation-ready change items, reusing current modification state, and localized setting names.
ModificationSnapshotService
Responsible for capturing project modifications into snapshot models.
ModificationSnapshotWorkflowService
Responsible for snapshot loading, export orchestration, preview orchestration, and import and safe-application orchestration.
ModProfileService
Responsible for creating profile models from project modifications.
ModProfileSerializationService
Responsible for reading and writing `.wtprofile` files.
ModProfileWorkflowService
Responsible for profile creation workflow, profile loading and application workflow, and delegating snapshot operations to the Snapshot workflow.
ModProfileLibraryService
Responsible for profile library browsing, Add, Import, Export, Rename, Duplicate, and Delete.
ValidationService
Responsible for rule registration, pipeline construction, and validation execution.
ValidationWorkflowService
Responsible for validation-purpose orchestration, context creation, modified-property collection, and delegation to `ValidationService`.
ValidationPresentationService
Responsible for converting validation results into reusable summaries, titles, severity presentation, and continuation decisions.
It contains no WPF dialog logic.
---
ViewModel Responsibilities
MainViewModel
Coordinates:
Project state
Selection
Search
Navigation
Modification tracking
Undo / Redo
Change Summary
Snapshot commands
Profile workflows
Validation workflows
Utility window lifecycle
Status reporting
`MainViewModel` consumes workflow services rather than reimplementing their logic.
ChangeSummaryViewModel
Responsible for presenting change items, grouping, selection, navigation commands, and live refresh.
ProfileManagerViewModel
Responsible for profile browser state, profile selection, profile commands, profile operation requests, and refresh and selection synchronization.
ValidationResultsViewModel
Responsible for validation issue presentation, severity filtering, counts, selection, navigation command, re-run command, Copy Results command, and refresh behavior.
---
Utility Window Architecture
The following are single-instance modeless utility windows:
Change Summary
Profile Manager
Validation Results
`MainViewModel` owns their lifetime.
```text
Open Command
        ↓
Existing Window?
   ├── Yes → Refresh / Restore / Activate
   └── No  → Create ViewModel and Window
        ↓
Closed Event
        ↓
Release references
```
These windows are independently focusable from the main editor.
Future UI modernization will standardize default sizes, starting positions, same-monitor placement, taskbar visibility, and keyboard shortcuts.
Modal detail dialogs may retain owner relationships when appropriate.
---
User Interface Workflow
```text
Open Project
        ↓
Find Anything
        ↓
Edit Properties
        ↓
Track Changes
        ↓
Undo / Redo
        ↓
Review Change Summary
        ↓
Create / Apply Profiles
        ↓
Validate Project
        ↓
Save
        ↓
Package
        ↓
Play
```
---
Content Creation Tool Requirements
Future Content Creation Tools must use the existing editing pipeline.
```text
Content Creation Command
        ↓
Locate existing PropertyModel targets
        ↓
Apply changes through PropertyModel
        ↓
Existing events and state
        ├── Undo / Redo
        ├── Modification tracking
        ├── Change Summary
        ├── Snapshots
        ├── Profiles
        └── Validation
```
No separate content-editing state or direct JSON mutation system should be introduced unless a tool must create new structure that does not yet have a `PropertyModel` representation.
Any structural creation workflow must still integrate with the same modification, snapshot, and validation architecture.
---
Design Principles
Gameplay First
The editor exists to improve the Wartales modding workflow.
Preserve Original Data
Modify the loaded JSON document whenever practical.
Avoid rebuilding data structures unnecessarily.
Separation of Responsibilities
Models own data and editing behavior.
Services own reusable logic.
Workflow services orchestrate operations.
ViewModels coordinate application state.
Views present information.
Infrastructure Before Features
Reusable infrastructure should be implemented before feature-specific functionality whenever practical.
Single Source of Truth
Every subsystem should consume existing state rather than duplicate it.
One Implementation Per Responsibility
The project must retain one implementation of:
Property modification tracking
Snapshot matching
Snapshot preview
Snapshot application
Validation pipeline
Project serialization
Documentation Before Release
Every completed milestone includes documentation updates before its Git commit.
---
Current Architecture Status
The reusable editor platform is now considered stable through Version 0.8.0.
The application provides four completed foundational subsystems:
Editing
Snapshots
Profiles
Validation
The architecture directly supports future implementation of:
Content Creation Tools
Validation Expansion
Batch Editing
Merge Preview
Advanced migration assistance
Validation reports
Community profile sharing
Additional UI modernization
Future milestones should extend and compose the existing architecture rather than introduce parallel systems.
`PropertyModel.IsModified` remains the single source of truth for modification state.