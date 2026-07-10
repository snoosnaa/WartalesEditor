# Decision Log

**Version:** 0.1
**Status:** Active
**Last Updated:** 2026-07-10
**Applies To:** Entire Project

---

# Table of Contents

- Decision 0001
- Decision 0002
- Decision 0003
- Decision 0004
- Decision 0005

---

# Decision 0001

## Title

Use the original `data.cdb`.

## Status

Accepted

## Reason

The original file provides the most accurate representation of the game's data and formatting.

---

# Decision 0002

## Title

Use MVVM.

## Status

Accepted

## Reason

Separates the user interface from application logic and improves maintainability.

---

# Decision 0003

## Title

Three-pane interface.

## Status

Accepted

## Reason

Separating Sheets, Entries, and Properties scales better than a TreeView and matches professional editors.

---

# Decision 0004

## Title

Gameplay-first interface.

## Status

Accepted

## Reason

Users should edit gameplay concepts rather than JSON structures.

---

# Decision 0005

## Title

Incremental development.

## Status

Accepted

## Reason

Develop the application in small, testable milestones with successful builds after each milestone.