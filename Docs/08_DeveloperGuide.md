# Developer Guide

**Document Version:** 1.1  
**Last Updated:** 2026-07-12

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

## Single Source of Truth

Whenever a feature needs to answer:

> **"What is currently different?"**

it should consume the existing modification state rather than creating another tracking system.

The current modification state lives in:

```text
PropertyModel.IsModified
```

This principle now applies to:

- Change Summary
- Future Batch Editing
- Validation reports
- Import / Merge preview
- Change Export
- Modified-only filtering

Future development should continue extending this architecture rather than introducing parallel systems.

---

## MVVM First

Responsibilities are divided as follows.

### Models

Models own:

- Data
- Editing behavior
- Validation
- Change detection

Models should never depend on the user interface.

---

### Services

Services own reusable business logic.

Current services include:

- JsonDataService
- SearchService
- LocalizationService
- ReferenceDataService
- PropertyDefinitionService
- EditHistoryService

If logic could reasonably be reused elsewhere, it probably belongs in a Service.

---

### ViewModels

ViewModels coordinate application state.

Responsibilities include:

- Selection
- Commands
- Status reporting
- Navigation
- Interaction between Models and Services

Presentation-specific logic belongs in dedicated ViewModels whenever practical.

Example:

- ChangeSummaryViewModel owns presentation and grouping logic for the Change Summary window.

Business logic should remain outside the ViewModel whenever practical.

---

### Views

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
2. Implement one logical milestone at a time.
3. Keep every build compiling.
4. Runtime test completed functionality.
5. Perform regression testing when shared infrastructure changes.
6. Update documentation.
7. Commit.
8. Push.

Avoid partially implemented features.

---

# Code Generation Guidelines

## Small and Medium Files

- Replace the complete file.

## Large Files

Examples:

- MainViewModel.cs
- MainWindow.xaml

Requirements:

- Fully design the replacement before generating code.
- Split across multiple responses only when required by response length.
- Never redesign while generating a split replacement.
- Keep the project compiling after every implementation stage.

---

# Current Editing Architecture

The editing pipeline is:

```text
User Edit

↓

PropertyModel

↓

RootDocument

↓

Modification Tracking

↓

Edit History

↓

Change Summary

↓

Save
```

Future editing features should extend this pipeline rather than replacing it.

---

# Modification Tracking

Modification tracking is owned by `PropertyModel`.

Project-level modification state is coordinated by `MainViewModel`.

Do **not** introduce duplicate change-tracking systems.

Current modification state should always be treated as the authoritative source for editor state.

---

# Undo / Redo

Undo and Redo are implemented through `EditHistoryService`.

Future editing features should integrate with `EditHistoryService` rather than implementing separate history mechanisms.

Remember:

- Edit history answers **"What happened?"**
- Modification tracking answers **"What is currently different?"**

These are complementary systems with different responsibilities.

---

# Change Summary

The Change Summary is intentionally built from temporary snapshots of the current project state.

It should never maintain its own persistent modification tracking.

Current architecture:

```text
PropertyModel.IsModified

↓

MainViewModel

↓

ChangeSummaryItemModel

↓

ChangeSummaryViewModel

↓

ChangeSummaryWindow
```

Future enhancements should continue using snapshot generation rather than synchronized collections whenever practical.

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

Before completing a milestone:

- Update Development Journal
- Update CHANGELOG
- Update Architecture
- Update Dashboard
- Update CurrentTask
- Update Project Snapshot

Update additional documentation whenever implementation changes make it outdated.

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

Document

↓

Commit
```

Regression testing should be performed whenever shared infrastructure changes.

Features are not considered complete until runtime verification succeeds.

---

# Coding Standards

Always:

- Follow MVVM.
- Prefer small, focused classes.
- Favor reusable Services.
- Keep every build compiling.
- Prefer extensible solutions.
- Minimize duplication.
- Reuse existing infrastructure whenever possible.

Avoid:

- Business logic in Views.
- Duplicate change-tracking systems.
- Duplicate editing infrastructure.
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
- Split only large files when required.
- Update documentation before major commits.
- Build after every logical implementation step.
- Runtime test completed behavior before considering a milestone complete.

---

# Long-Term Goal

The objective is not simply to build an editor.

The objective is to build a maintainable editing platform that can continue evolving without major architectural redesign while remaining safe, extensible, and pleasant to use.