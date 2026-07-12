# Developer Guide

**Document Version:** 1.0  
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

Examples:

- JsonDataService
- SearchService
- LocalizationService
- ReferenceDataService
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

Business logic should remain outside the ViewModel whenever practical.

---

### Views

Views present information.

Avoid:

- Business logic
- Game logic
- Editing logic

Code-behind should remain minimal.

---

# Development Workflow

Every milestone follows the same workflow.

1. Design the feature.
2. Implement one logical milestone at a time.
3. Keep every build compiling.
4. Test thoroughly.
5. Update documentation.
6. Commit.
7. Push.

Avoid partially implemented features.

---

# Code Generation Guidelines

Small and medium files:

- Replace the complete file.

Large files:

- Fully design the replacement before writing code.
- Split across multiple responses only when necessary.
- Never redesign while generating.

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

Save
```

Future editing features should extend this pipeline instead of replacing it.

---

# Modification Tracking

Modification tracking is owned by PropertyModel.

Project-level modification state is coordinated by MainViewModel.

Do not introduce duplicate change-tracking systems.

---

# Undo / Redo

Undo and Redo are implemented through EditHistoryService.

Future editing features should integrate with EditHistoryService rather than implementing separate history mechanisms.

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

Future editors should integrate through the existing editor selection framework.

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

Additional documents should be updated whenever they become outdated.

---

# Testing Expectations

Every completed feature should be verified.

Typical workflow:

Build

↓

Test

↓

Verify

↓

Document

↓

Commit

Regression testing should be performed whenever shared infrastructure changes.

---

# Coding Standards

Always:

- Follow MVVM.
- Prefer small focused classes.
- Favor reusable Services.
- Keep builds compiling.
- Prefer extensible solutions.
- Minimize code duplication.

Avoid:

- Business logic in Views.
- Hardcoded gameplay data whenever practical.
- Duplicate infrastructure.

---

# AI Development Guidelines

When continuing development:

- Always work from the latest supplied files.
- Never assume older file contents.
- Preserve completed architecture.
- Implement one milestone at a time.
- Prefer complete file replacements whenever practical.
- Split only large files when required.
- Maintain documentation before commits.

---

# Long-Term Goal

The objective is not simply to build an editor.

The objective is to build a maintainable editing platform that can continue evolving without requiring major architectural redesign.