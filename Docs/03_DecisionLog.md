# Decision Log

**Version:** 0.2
**Status:** Active
**Last Updated:** 2026-07-24
**Applies To:** Entire Project

---

# Table of Contents

- Decision 0001
- Decision 0002
- Decision 0003
- Decision 0004
- Decision 0005
- Decision 0006
- Decision 0007
- Decision 0008

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

---

# Decision 0006

## Title

Represent outcomes, not implementation.

## Status

Accepted

## Reason

Player-facing information should describe the gameplay result rather
than internal mutation, snapshot, replay, or transaction mechanisms.

---

# Decision 0007

## Title

Investigate before implementation.

## Status

Accepted

## Reason

Runtime data relationships and gameplay behavior must be confirmed before
an operation is designed. Unresolved scope or side effects require
deferral rather than assumption.

---

# Decision 0008

## Title

Bundle compatible gameplay features after architectural maturity.

## Status

Accepted

## Reason

The stable gameplay-operation platform now supports related
player-facing features without repeated infrastructure work. Compatible
features may be planned together while retaining focused implementation
and verification boundaries.
