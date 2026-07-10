# Architecture

**Status:** Active  
**Last Updated:** 2026-07-10  
**Applies To:** Source Code

---

# Table of Contents

- Overview
- Design Goals
- Architecture Layers
- Project Structure
- Data Flow
- MVVM Pattern
- Future Architecture

---

# Overview

Wartales Editor is built as a WPF desktop application using the Model-View-ViewModel (MVVM) architectural pattern.

The project is organized into logical layers so that the user interface remains independent from the file parsing and business logic.

---

# Design Goals

- Separate UI from data processing.
- Keep models independent of the user interface.
- Support future expansion without major refactoring.
- Make each component easy to test.
- Favor readability over unnecessary complexity.

---

# Architecture Layers

UI (Views)

↓

ViewModels

↓

Models

↓

Services

↓

data.cdb

---

# Current Project Structure

WartalesEditor

- Helpers
- Models
- Services
- ViewModels
- Views
- Docs
- Resources
- Samples
- Tests

---

# Data Flow

1. User opens a data.cdb file.
2. JsonDataService parses the file.
3. A ProjectModel is created.
4. ProjectModel contains SheetModel objects.
5. Each SheetModel contains EntryModel objects.
6. MainViewModel exposes these collections to the UI.
7. WPF data binding updates the interface automatically.

---

# MVVM Pattern

## Models

Represent the Wartales data.

Examples:

- ProjectModel
- SheetModel
- EntryModel

---

## ViewModels

Provide data and commands to the UI.

Current:

- MainViewModel

---

## Services

Responsible for reading and writing files.

Current:

- JsonDataService

---

## Helpers

Infrastructure shared across the application.

Examples:

- ObservableObject
- RelayCommand

---

# Future Architecture

Planned additions include:

- PropertyModel
- SaveService
- Undo/Redo Manager
- Search Service
- Backup Service
- Validation Service