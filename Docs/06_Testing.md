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

- A valid Wartales export localization file can be selected regardless of its
  filename.
- A validated Wartales installation opens setup/replacement in the game root;
  valid language-agnostic `export_*.xml` candidates are preselected, while no
  candidate or failed detection safely retains manual selection.
- Embedded `lang` metadata controls the active language code.
- The canonical `<Documents>\Wartales Editor\Language Data\export.xml` loads
  automatically after restart.
- Localized names display and participate in searching.
- Internal IDs remain available
- Missing or invalid canonical data does not block startup or project loading.
- Replacing language data refreshes current Detailed Editor presentation. A
  late failure preserves the prior setup only when exact restoration is proven;
  otherwise localization is cleared and invalid state is reported.
- Forced post-promotion failures prove exact rollback restoration, missing and
  locked rollback rejection, cleared invalid state after unrecoverable recovery,
  explicit cleanup warnings, and safe removal of stale rollback ownership on a
  later transaction.
- Source-deletion coverage reloads through a fresh service; replacement coverage
  verifies old/new/internal-ID search, selected-context notification, and open
  Change Summary refresh.
- `texts_*.xml` is neither requested nor required.
- Available state structurally uses the shared green success brushes; missing
  and invalid states retain the non-success informational treatment.

Expected Result

✅ Generic language data, fallback, replacement, and searching remain synchronized.

Final evidence: main and test builds completed with zero warnings/errors; the
focused language suite, real 10,534-entry English export validation, QuickBMS
focused suite, and all 25 Class A groups passed. The Project Owner interactively
passed setup, replacement, restart/persistence, detected source selection, and
green success-state presentation. The renewed acceptance result was **PASS**.

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
# Version 0.10.0 player communication verification

Runtime verification must cover the minimum window width and 100%, 125%, and
150% DPI; all menus and toolbars; Profiles complete, partial, already-applied,
and failed outcomes; Review Changes; all Check Project outcome branches; every
gameplay dialog; confirmations; file filters; About; keyboard access; status
messages; and representative error paths. Verify that no standard workflow
exposes Snapshot or mutation terminology and that no new binding errors occur.

Search Scope Semantics Correction is tracked separately and is not part of the
Pass 5 wording verification.
