# Testing Guide

**Document Version:** 1.1  
**Last Updated:** 2026-07-12

---

# Purpose

This document defines the testing strategy for Wartales Editor.

Its goals are to:

- Verify every completed milestone.
- Prevent regressions.
- Standardize testing before commits.
- Provide a repeatable release checklist.

Testing is considered part of development.

No milestone is complete until it has been successfully:

- Built
- Runtime tested
- Documented
- Committed

---

# Testing Philosophy

Every feature follows the same development pipeline.

```text
Implement

↓

Build

↓

Runtime Test

↓

Regression Test

↓

Document

↓

Commit
```

Never assume a feature works simply because it compiles.

---

# Standard Build Test

After every implementation step:

- Build the solution.
- Resolve all compiler errors.
- Resolve new warnings whenever practical.
- Confirm the application launches.

Expected Result

✅ Build succeeds.

---

# Project Loading

Verify:

- Open original `data.cdb`
- Project loads successfully
- Categories populate
- Settings populate
- Properties populate
- No exceptions occur

Expected Result

✅ Original project loads successfully.

---

# Navigation

Verify:

- Category selection
- Setting selection
- Property selection
- Selection synchronization
- Empty Categories
- Show Empty Categories

Expected Result

✅ Navigation remains synchronized.

---

# Find Anything

Verify searching by:

- Internal ID
- English name
- Property name
- Property value

Verify:

- Search results appear
- Correct Category selected
- Correct Setting selected
- Correct Property selected

Expected Result

✅ Direct navigation works correctly.

---

# Localization

Verify:

- `export_en.xml` loads
- English names display
- English names participate in searching
- Internal IDs remain available

Expected Result

✅ Localization and searching remain synchronized.

---

# Property Editors

Verify each editor type.

## Text

- Edit value
- Save
- Reload

---

## Number

Verify:

- Integer values
- Decimal values
- Invalid input
- Validation messages

---

## Boolean

Verify:

- Toggle
- Save
- Reload

---

## Dropdown

Verify:

- Correct values discovered
- Correct value selected
- Save
- Reload

---

## Read Only

Verify:

- Property cannot be edited

---

## Complex Placeholder

Verify:

- Displays correctly
- Remains read-only

Expected Result

✅ Every property editor behaves correctly.

---

# Validation

Verify:

- Invalid numbers rejected
- Valid numbers accepted
- Integer validation
- Decimal validation

Expected Result

✅ Invalid values never overwrite valid data.

---

# Safe Editing

Verify:

- Property modification tracking
- Project modification tracking
- Modified indicators
- Window title indicator
- Modification counter
- Reset Property

Expected Result

✅ Modification state always reflects editor state.

---

# Undo / Redo

Verify:

- Single Undo
- Single Redo
- Multiple Undo
- Multiple Redo
- Toolbar commands
- Ctrl+Z
- Ctrl+Y
- History reset after opening another project

Known Minor Issue

- Programmatic Undo/Redo may reposition the text caret within certain WPF text editors.
- This does not affect data integrity.

Expected Result

✅ Editing history behaves correctly.

---

# Change Summary

Verify:

- Change Summary opens.
- Live modifications appear automatically.
- Original Value is correct.
- Current Value is correct.
- Localized setting names display correctly.
- Category grouping displays correctly.
- Navigate button selects the correct property.
- Double-click navigation selects the correct property.
- Main editor receives focus after navigation.
- Reset Property updates the summary.
- Undo updates the summary.
- Redo updates the summary.
- Save clears the summary.
- Opening another project refreshes the summary.
- Closing and reopening the summary preserves correct state.

Expected Result

✅ Change Summary always reflects the current modification state.

---

# Save

Verify:

- Save succeeds
- File reloads
- Changes persist
- Modification indicators reset
- New editing baseline established
- Change Summary clears

Expected Result

✅ Saved project matches editor state.

---

# Gameplay Verification

Whenever gameplay data changes:

Verify:

```text
Open

↓

Edit

↓

Save

↓

Package

↓

Launch Wartales

↓

Verify Gameplay
```

Expected Result

✅ Gameplay behaves as intended.

---

# Regression Testing

Perform before every major commit.

Verify:

- Loading
- Navigation
- Search
- Localization
- Property editors
- Validation
- Modification tracking
- Undo
- Redo
- Change Summary
- Save

Expected Result

✅ No previously completed feature regresses.

---

# Release Checklist

Before creating a release:

- [ ] Project builds successfully.
- [ ] Runtime testing completed.
- [ ] Regression checklist completed.
- [ ] Documentation updated.
- [ ] CHANGELOG updated.
- [ ] Version numbers updated.
- [ ] Git commit created.
- [ ] Changes pushed to GitHub.

---

# Current Verification Status

The following milestones have been fully verified:

- ✅ Project Foundation
- ✅ Functional Editing
- ✅ Find Anything
- ✅ Smart Property Editors
- ✅ Safe Editing
- ✅ Unlimited Undo / Redo
- ✅ Change Summary

Testing currently consists of:

- Incremental build verification
- Manual runtime testing
- Regression testing
- Live gameplay verification

Automated testing may be introduced in a future version if practical.