# Decision Log

**Version:** 0.3
**Status:** Active
**Last Updated:** 2026-09-10
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
- Decision 0009
- Decision 0010
- Decision 0011

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

---

# Decision 0009

## Title

Use historical clean-baseline impact for Profile Changes.

## Status

Accepted

## Reason

A target-relative count cannot truthfully describe a profile after that profile
has already been applied. Format 5 therefore permits an optional Profile Impact
Manifest established against exact verified pristine source bytes. It provides
stable reporting authority for one profile gameplay revision while remaining
strictly separate from Apply, mutation, compatibility, Restore Previous Values,
and Gameplay Operation State authority. Missing or invalid authority is shown as
Unavailable rather than replaced with a fabricated count.

---

# Decision 0010

## Title

Discover procedural recruit traits semantically and preserve portable missing
trait intent.

## Status

Accepted

## Reason

Fixed physical trait ranges are not complete recruitment authority. Random
Trait Exclusions preserves legacy Starting/Recruitment candidates and also
discovers Positive/Negative personality traits with finite positive numeric
recruitment weight, unioned by canonical ID through one existing mutation/state
pipeline. The feature controls procedural recruit assignment rather than every
possible later trait-acquisition system.

Exact-source replay remains strict. Changed-source profiles may omit and report
only a requested trait genuinely absent from the complete destination sheet,
while preserving that source-independent intent in the profile. A present but
ineligible, polarity/group-drifted, or duplicate identity remains incompatible
and fails before mutation.

---

# Decision 0011

## Title

Use a deterministic root launcher and preserve the main application payload
under `App\`.

## Status

Accepted

## Reason

The portable package should present one obvious root executable and visible
public/legal documents without manually relocating the SDK-authored WPF runtime
payload. A dedicated launcher therefore resolves only the exact
`App\WartalesEditor.exe` child from its own package root, forwards arguments,
waits, and propagates the child exit code. The main application remains
self-contained, multi-file, and untrimmed under `App\`; only the launcher is
single-file and trimmed.

The package uses no `Docs\` wrapper, installer, main-application single-file
conversion, update/repair behavior, or old-install cleanup. Updates are
extracted to a fresh folder. Root documents remain directly visible, and Quick
Help uses only executable-adjacent or the bounded immediate-parent package-root
resolution. Tracked construction and SHA-256 validation prove staging identity;
package creation remains separate from release authorization.
