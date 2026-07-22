# Wartales Editor

Wartales Editor is a professional Windows desktop application for safely editing and expanding the game data used by **Wartales**.

The project began as a configuration editor and has evolved into a reusable transactional content creation platform capable of introducing new gameplay content while preserving project integrity.

The long-term goal is to provide a powerful, safe, and extensible editor that allows players and mod creators to modify existing content and eventually create entirely new gameplay content.

---

# Current Status

**Application Version:** 0.8.1 (Development)

Current state:

- Stable development build
- Fully compiling
- Architecturally stable
- Active development

Recently verified:

- Add Camp Facilities
- Upgrade All Equipment
- Weather gameplay modifications
- Transaction rollback
- Save / Reload
- Validation
- Atomic Undo / Redo

---

# Major Features

## Editing Platform

- Intelligent property editing
- Type-aware editors
- English localization
- Search
- Modification tracking
- Undo / Redo
- Change Summary

## Project Management

- Snapshot workflow
- Mod Profiles
- Validation framework
- Save validation

## Content Creation Platform

- Project Mutation Layer
- Project Operations
- Transaction Framework
- Mutation-based rollback
- Nested property editing
- Object-valued mutation
- Operation validation

## Verified Gameplay Features

- Add Camp Facilities
- Upgrade All Equipment
- Weather modifications

---

# Architecture

The editor is built around reusable infrastructure.

Major architectural subsystems include:

- Editing
- Snapshots
- Profiles
- Validation
- Project Mutation
- Content Creation
- Project Operations
- Transaction Framework

New features are expected to extend existing infrastructure rather than introduce parallel implementations.

---

# Technologies

- C#
- .NET 10
- WPF
- MVVM
- Newtonsoft.Json

---

# Development Philosophy

The project emphasizes:

- Clean architecture
- Long-term maintainability
- Infrastructure before features
- Mutation-based editing
- Validation before persistence
- Runtime verification
- Comprehensive documentation

Every milestone is expected to:

- Compile successfully
- Pass runtime testing
- Update documentation
- Be committed only after verification

---

# Documentation

Important project documentation includes:

- ProjectStatus.md
- Architecture.md
- Dashboard.md
- ProjectSnapshot.md
- CurrentTask.md
- DevelopmentJournal.md
- LessonsLearned.md
- Changelog.md

---

# Roadmap

Future development includes:

- Additional content creation tools
- Gameplay modification tools
- NPC creation
- Profession creation
- Advanced batch operations
- Merge Preview
- Additional validation
- Performance improvements

Development is organized into small, fully verified milestones.

---

# License

This project is currently under active development.

Licensing and public release details will be finalized prior to Version 1.0.