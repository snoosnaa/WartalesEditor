# Current Task

**Document Version:** 1.1
**Last Updated:** 2026-07-11

---

# Current Milestone

## Milestone 0.4.0 - Edit Anything

### Goal

Transform the editor from a basic text editor into a safe, intelligent gameplay editor.

The focus of this milestone is improving the editing experience rather than adding new navigation features.

---

# Completed This Milestone

## Find Anything v1

Completed

- Global Find Anything panel
- Search across all Categories
- Search internal IDs
- Search English display names
- Search property names
- Search property values
- Localization-aware searching
- Automatic navigation to search results
- Automatic property selection
- Simplified search interface

---

# Current Task

Implement smarter property editing.

The editor should begin recognizing property types and present controls appropriate for the data being edited.

Examples include:

- Numbers
- Booleans
- Enumerations
- Strings

The initial implementation should remain simple while providing a solid foundation for future editor types.

---

# Next Steps

## Phase 1

- Detect property data types.
- Introduce type-aware property editors.
- Display checkboxes for booleans.
- Display numeric editors for numeric values.
- Continue using text editing for unsupported types.

## Phase 2

- Detect modified values.
- Highlight modified properties.
- Display modified indicators.
- Prepare change tracking.

## Phase 3

- Validate edited values.
- Prevent invalid saves.
- Display validation messages.

---

# Known Future Improvements

- Property descriptions
- Tooltips
- Batch editing
- Mod Profiles
- QuickBMS integration
- Change migration
- Undo / Redo

---

# Notes

The immediate objective is no longer finding data.

The editor can now quickly locate gameplay objects through Find Anything.

The focus now shifts toward making gameplay modifications safer, smarter, and easier to perform.