# Current Task

**Document Version:** 1.0  
**Last Updated:** 2026-07-10

---

# Current Milestone

## Milestone 0.3.0 - Property Editing

### Goal

Transform the property viewer into a fully functional property editor.

---

# Completed This Milestone

- Added SelectedEntry to the ViewModel.
- Completed the MVVM selection pipeline.
- Implemented the three-pane editor layout.
- Displayed properties for the selected entry.
- Introduced PropertyModel.
- Replaced KeyValuePair with PropertyModel throughout the application.
- Refactored JsonDataService.
- Updated UI bindings.

---

# Current Task

Make property values editable.

The first implementation should focus on simple value types while maintaining the existing architecture.

---

# Next Steps

1. Replace read-only property values with editable controls.
2. Update PropertyModel when values change.
3. Track modified properties.
4. Highlight modified values.
5. Save modified CDB files.

---

# Known Future Improvements

- Group empty sheets into a collapsible section.
- Display entry counts beside sheet names.
- Improve the status bar.
- Add property descriptions.
- Add batch editing.
- Add change migration between game versions.

---

# Notes

The current focus is completing the core editing workflow before adding quality-of-life features.