# Developer Guide

**Document Version:** 1.2  
**Last Updated:** 2026-09-10

---

# Purpose

This guide describes how development is performed for Wartales Editor.

It is intended for future contributors, future AI development sessions, and future versions of the project.

The goal is to keep development consistent regardless of who is implementing new features.

---

# Development Philosophy

The project follows several core principles.

## Build Infrastructure Before Features

Whenever practical:

1. Build reusable infrastructure.
2. Verify the infrastructure.
3. Build features on top of it.

Avoid implementing one-off solutions that duplicate existing systems.

---

## Compose Existing Systems

New features should extend the existing architecture rather than introducing parallel implementations.

Examples include:

- Profiles compose the Snapshot workflow.
- Save validation composes the Validation pipeline.
- Validation Results reuse editor navigation.
- Future Content Creation Tools will reuse the existing editing pipeline.

Whenever possible, compose rather than duplicate.

---

## Single Source of Truth

Whenever a feature needs to answer:

> **"What is currently different?"**

it should consume the existing modification state rather than creating another tracking system.

The current modification state lives in:

```text
PropertyModel.IsModified
```

This principle currently applies to:

- Change Summary
- Snapshots
- Profiles
- Save validation
- Future Batch Editing
- Merge Preview
- Validation reports
- Modified-only filtering

Future development should continue extending this architecture rather than introducing parallel systems.

---

# MVVM First

Responsibilities are divided as follows.

## Models

Models own:

- Data
- Editing behavior
- Modification detection
- Original values
- Value change notifications

Models should never depend on the user interface.

---

## Services

Services own reusable business logic.

Current services include:

- JsonDataService
- SearchService
- LocalizationService
- ReferenceDataService
- PropertyDefinitionService
- EditHistoryService
- ChangeSummaryService
- ModificationSnapshotService
- ModificationSnapshotWorkflowService
- ModProfileWorkflowService
- ValidationService
- ValidationWorkflowService

If logic could reasonably be reused elsewhere, it probably belongs in a Service.

---

## ViewModels

ViewModels coordinate application state.

Responsibilities include:

- Selection
- Commands
- Navigation
- Status reporting
- Window lifecycle
- Interaction between Models and Services

Business logic should remain outside ViewModels whenever practical.

Dedicated ViewModels should own presentation logic for reusable windows.

Examples include:

- ChangeSummaryViewModel
- ProfileManagerViewModel
- ValidationResultsViewModel

---

## Views

Views present information.

Avoid:

- Business logic
- Game logic
- Editing logic

Code-behind should remain minimal and limited to view-specific interaction.

---

# Development Workflow

Every milestone follows the same workflow.

1. Design the feature.
2. Implement one logical milestone.
3. Keep every build compiling.
4. Runtime test completed functionality.
5. Perform regression testing when shared infrastructure changes.
6. Perform UI polish.
7. Update documentation.
8. Commit.
9. Push.

Avoid partially implemented features.

---

# Code Generation Guidelines

## Small and Medium Files

Prefer replacing the complete file.

## Large Files

Examples include:

- MainViewModel.cs
- MainWindow.xaml

Requirements:

- Design the complete solution before generating code.
- Provide exact locations for partial replacements.
- Include approximate line numbers.
- Include enough surrounding code to verify placement.
- Keep the project compiling after every implementation step.

---

# Core Architecture

The editor currently consists of four reusable subsystems:

- Editing
- Snapshots
- Profiles
- Validation

Every future feature should integrate with these systems whenever practical.

---

# Editing Pipeline

```text
User Edit
        ↓
PropertyModel
        ↓
RootDocument
        ↓
Modification Tracking
        ↓
Undo / Redo
        ↓
Change Summary
        ↓
Snapshots
        ↓
Profiles
        ↓
Validation
        ↓
Save
```

Future editing features should extend this pipeline rather than replacing it.

---

# Modification Tracking

Modification tracking is owned by `PropertyModel`.

Project-level modification state is coordinated by `MainViewModel`.

Do not introduce duplicate modification tracking.

Current modification state is always authoritative.

---

# Undo / Redo

Undo and Redo are implemented through `EditHistoryService`.

Future editing features should integrate with EditHistoryService rather than implementing separate history mechanisms.

Remember:

- Edit history answers **"What happened?"**
- Modification tracking answers **"What is currently different?"**

These responsibilities should remain separate.

---

# Snapshot Workflow

Snapshots provide reusable representations of project modifications.

There must remain exactly one implementation of:

- Snapshot Matching
- Snapshot Preview
- Snapshot Application

Higher-level systems should compose this workflow rather than replacing it.

---

# Profile Workflow

Profiles extend the Snapshot workflow.

Do not introduce separate profile matching or application logic.

Profile creation should continue capturing the existing modification state.

---

# Validation Framework

Validation is now a reusable subsystem.

Future validation rules should:

- Validate only information that can be verified accurately.
- Avoid changing project state.
- Return structured validation issues.
- Reuse existing services whenever practical.

Future validation improvements should expand the rule library rather than replacing the framework.

---

# Utility Windows

Current reusable utility windows include:

- Change Summary
- Profile Manager
- Validation Results

These windows should:

- Remain single-instance.
- Be modeless.
- Support independent focus.
- Share consistent lifecycle behavior.

Future UI modernization will standardize sizing, placement, taskbar behavior, and keyboard shortcuts.

---

# Property Editors

Editors are selected automatically.

Current editor types include:

- Text
- Number
- Boolean
- Dropdown
- Read Only
- Complex Placeholder

Future editors should integrate through the existing editor-selection framework.

---

# Documentation Requirements

Documentation is considered part of development.

Before completing a milestone update:

- Development Journal
- Changelog
- Dashboard
- Current Task
- Architecture
- Roadmap
- Project Snapshot

Documentation should remain synchronized with the codebase.

---

# Testing Expectations

Every completed feature should be verified.

Standard workflow:

```text
Build
        ↓
Runtime Test
        ↓
Regression Test
        ↓
UI Polish
        ↓
Documentation
        ↓
Commit
```

Shared infrastructure changes require regression testing.

Features are not complete until runtime verification succeeds.

---

# Portable Package Workflow

The solution has two release projects that share the repository version
properties authority. `WartalesEditor.csproj` is the real WPF application and
remains self-contained, multi-file, untrimmed `win-x64` with ReadyToRun
disabled. The dedicated launcher project is self-contained, trimmed,
single-file `win-x64`, with ReadyToRun disabled and no WPF/WinForms dependency.
The launcher is not independently versioned.

Use the tracked portable-package build flow rather than manually assembling a
release folder. The release workflow builds/tests first; the package script then
publishes the main application into the package's `App\` subtree, publishes the
launcher separately to the root, copies the five approved public documents, and
validates the result. Staging must be a
proper descendant of the repository's exact `output\` directory; path,
reparse-point, and input-containment checks must complete before any recursive
recreation.

Package validation compares the complete allowed main-publish relative path set
and the SHA-256 of every file, rejects ambiguous, missing, extra, unreadable, or
same-name/different-content files, and proves the root launcher against its
authoritative publish executable by SHA-256. It also enforces the root/`App\`
layout, required documents and runtime metadata, culture/native structure, and
PDB/root-runtime exclusions. A local validation ZIP may be created only when
requested; tagging and publication remain separate authorized lifecycle steps.

---

# Coding Standards

Always:

- Follow MVVM.
- Prefer small, focused classes.
- Favor reusable Services.
- Compose existing systems.
- Keep every build compiling.
- Prefer extensible solutions.
- Minimize duplication.
- Runtime test completed work.
- Document completed milestones.

Avoid:

- Business logic in Views.
- Duplicate modification tracking.
- Duplicate snapshot implementations.
- Duplicate validation systems.
- Hardcoded gameplay data whenever practical.
- Reconstructing project files from memory.

---

# AI Development Guidelines

When continuing development:

- Always work from the latest supplied files.
- Never assume older file contents.
- Preserve completed architecture.
- Implement one milestone at a time.
- Prefer complete file replacements whenever practical.
- For large files, provide exact replacement locations with approximate line numbers.
- Keep the project compiling after every implementation step.
- Runtime test completed behavior.
- Complete documentation before release commits.

---

# Long-Term Goal

The objective is not simply to build an editor.

The objective is to build a maintainable editing platform that can continue evolving without major architectural redesign while remaining safe, extensible, and enjoyable to use.

Every future feature should strengthen the existing architecture rather than increasing technical debt.
