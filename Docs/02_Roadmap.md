# Wartales Editor Roadmap

**Application Roadmap**

Last Updated: 2026-07-17

---

# Vision

The long-term goal of Wartales Editor is to become the definitive editor for Wartales game data.

The project emphasizes:

- Safe editing
- Reusable architecture
- Intelligent workflows
- Extensibility
- Long-term maintainability

Every major feature should build upon the existing architecture rather than introducing parallel implementations.

---

# Guiding Principles

## Infrastructure Before Features

Reusable systems are built first.

User-facing features are then implemented using those systems.

This approach minimizes duplicated logic and makes future development significantly easier.

---

## Single Source of Truth

Where practical, each major responsibility should have exactly one implementation.

Examples include:

- Property modification tracking
- Snapshot matching
- Snapshot preview
- Snapshot application
- Validation pipeline

Future features should compose these systems rather than replace them.

---

# Completed Milestones

---

## Version 0.1.0 — Project Foundation ✅

Completed

Major accomplishments:

- WPF desktop application
- MVVM architecture
- Git integration
- Documentation system
- JSON loading
- Project infrastructure

---

## Version 0.2.0 — Data Browser ✅

Completed

Major accomplishments:

- Three-pane editor
- Category browsing
- Entry browsing
- Property browsing
- Project model architecture

---

## Version 0.3.0 — Functional Editing ✅

Completed

Major accomplishments:

- Editable properties
- RootDocument synchronization
- Save support
- Live game verification

---

## Version 0.3.1 — Find Anything ✅

Completed

Major accomplishments:

- Global search
- Localization-aware searching
- Intelligent property editors
- Reference-aware dropdowns

---

## Version 0.4.0 — Safe Editing Infrastructure ✅

Completed

Major accomplishments:

- Property modification tracking
- Project dirty-state tracking
- Undo / Redo
- Reset Property
- Edit history architecture

PropertyModel.IsModified remains the single source of truth.

---

## Version 0.5.0 — Change Summary ✅

Completed

Major accomplishments:

- Live Change Summary
- Original vs Current comparison
- Navigation
- Automatic refresh
- Category grouping

---

## Version 0.6.0 — Snapshot Workflow ✅

Completed

Major accomplishments:

- Snapshot capture
- Snapshot serialization
- Snapshot matching
- Snapshot preview
- Snapshot application
- Workflow orchestration

There remains exactly one implementation of:

- Snapshot matching
- Snapshot preview
- Snapshot application

---

## Version 0.7.0 — Complete Profile Manager ✅

Completed

Major accomplishments:

- Complete Profile Manager
- Profile library
- Create
- Rename
- Duplicate
- Apply
- Import
- Export
- Delete
- Profile Details dialog

Profiles compose the existing Snapshot workflow.

No duplicate snapshot implementations exist.

---

## Version 0.8.0 — Validation Framework ✅

Completed

Major accomplishments:

- Validation pipeline
- Validation workflow
- Validation rule architecture
- Validation models
- Validation Results window
- Manual validation
- Save validation
- Validation navigation
- Severity filtering
- Copy Results
- Re-run validation

Validation is now a reusable subsystem available to every future editor feature.

---

# Current Development

## Version 0.9.0 — Content Creation Tools (Pass 1)

This milestone marks the transition from building editor infrastructure to building powerful editor capabilities.

All content creation tools must automatically integrate with:

- Property modification tracking
- Undo / Redo
- Change Summary
- Snapshot workflow
- Profiles
- Validation

No separate editing systems will be introduced.

### Initial tools

- Add Camp Anvil
- Make All Equipment Upgradeable

Additional content creation tools will be added over time as reusable editor commands.

---

# Following Milestone

## UI Modernization

Primary goals:

- Improved toolbar layout
- Larger action buttons
- Improved workflow
- Visual polish
- Future icon support

### Utility Window Improvements

- Consistent default window sizing
- Consistent initial window placement
- Open utility windows on the same monitor as the main editor
- Independent taskbar buttons for utility windows
- Keyboard shortcuts to open or focus:
  - Change Summary
  - Profile Manager
  - Validation Results

These improvements are intended to make the editor feel more like a modern desktop application while preserving the existing architecture.

---

# Future Milestones

## Validation Expansion

The validation framework is complete.

Future work expands the existing framework with additional rules rather than creating another validation system.

Potential additions include:

- Cross-sheet validation
- Duplicate detection
- Missing required properties
- Advanced gameplay validation
- Exportable validation reports
- Richer validation summaries

---

## Advanced Editing

Potential future features:

- Merge Preview
- Batch Editing
- Intelligent bulk operations
- Advanced migration assistance

These features will reuse the existing editing, snapshot, profile, and validation infrastructure.

---

## Community Features

Post-1.0 possibilities include:

- Community profile sharing
- In-game profile credits popup

Steam Workshop support is intentionally excluded because Wartales does not support Steam Workshop.

---

# Long-Term Technical Goals

Future engineering improvements include:

- Byte-for-byte CDB formatting preservation wherever practical
- Continued performance optimization
- Expanded automated testing
- Continued architectural refinement

---

# Development Philosophy

Every milestone should satisfy the following goals:

- Keep the project compiling after every implementation step.
- Prefer long-term architecture over short-term solutions.
- Build reusable infrastructure before user-facing features.
- Avoid duplicate implementations.
- Preserve the existing architecture unless the current milestone requires otherwise.
- Ensure new features automatically benefit from:
  - Undo / Redo
  - Change Summary
  - Profiles
  - Validation
  - Snapshot migration

The objective is not simply to add features, but to build a stable, extensible editor that can continue growing for years without accumulating unnecessary technical debt.