# Lessons Learned

**Document Version:** 1.0  
**Last Updated:** 2026-07-12

---

# Purpose

This document records important engineering lessons discovered while developing Wartales Editor.

Unlike the Development Journal, these are not historical events.

These are principles that have repeatedly proven valuable and should continue guiding future development.

---

# Lesson 1 — Infrastructure Before Features

One of the biggest improvements to the project came from building reusable infrastructure before adding user-facing features.

Examples include:

- PropertyModel
- SearchService
- ReferenceDataService
- EditHistoryService

Although infrastructure often feels slower to develop, it dramatically reduces future complexity.

**Guideline**

Whenever practical:

1. Build the reusable system.
2. Verify it.
3. Build features on top of it.

---

# Lesson 2 — Small Verified Milestones Beat Large Implementations

Large implementations are difficult to verify and significantly increase debugging time.

The project has progressed more reliably by:

- Completing one milestone.
- Building immediately.
- Testing immediately.
- Documenting immediately.

**Guideline**

Never begin the next milestone until the current milestone is complete.

---

# Lesson 3 — Models Should Own Editing Behavior

Originally PropertyModel represented only data.

As development progressed it became responsible for:

- Type-aware editing
- Validation
- Original value capture
- Modification tracking
- Reset functionality
- Value change notifications

This greatly simplified the ViewModel.

**Guideline**

Editing behavior belongs with the editable object whenever practical.

---

# Lesson 4 — Services Age Better Than ViewModel Logic

Reusable logic consistently proved easier to maintain inside Services.

Examples include:

- SearchService
- LocalizationService
- ReferenceDataService
- EditHistoryService

Large ViewModels become difficult to understand.

Small focused Services remain reusable.

**Guideline**

If logic could reasonably be reused elsewhere, consider creating a Service.

---

# Lesson 5 — Prevent Mistakes Instead of Correcting Them

Many successful features reduced user mistakes rather than fixing mistakes afterward.

Examples include:

- Read-only properties
- Type-aware editors
- Validation
- Dropdown editors
- Modification tracking
- Undo / Redo

Good editor design should guide users toward valid edits.

---

# Lesson 6 — Build Around Gameplay Rather Than JSON

The application became much easier to use once the interface focused on gameplay concepts instead of raw data structures.

Examples include:

- Categories
- Settings
- Find Anything
- English localization
- Smart property editors

Users should think about Wartales rather than JSON.

---

# Lesson 7 — Documentation Is Part of Development

Documentation updates have consistently improved future development.

Benefits include:

- Easier project handoffs
- Faster onboarding
- Better architectural consistency
- Clear milestone history

Documentation should be completed before major commits.

---

# Lesson 8 — Complete Replacements Reduce Errors

As the project grew, partial code edits became increasingly difficult to apply correctly.

Complete file replacements have proven more reliable.

Exceptions are very large files that exceed response limits.

**Guideline**

Prefer complete replacement files whenever practical.

---

# Lesson 9 — Build Stability Is Non-Negotiable

Keeping the project building after every implementation step has prevented long debugging sessions.

Every milestone should maintain:

- Successful build
- Working application
- Verified functionality

Broken intermediate states should be avoided whenever possible.

---

# Lesson 10 — Good Architecture Compounds

Many recent milestones required surprisingly little redesign because earlier architectural decisions were reusable.

Examples include:

- Safe Editing building on PropertyModel.
- Undo / Redo building on EditHistoryService.
- Change Summary building on modification tracking.
- Future Batch Editing building on the same infrastructure.

Good architecture continues paying dividends long after it is written.

---

# Current Philosophy

The project now follows several guiding principles:

- Build infrastructure before features.
- Keep MVVM boundaries clean.
- Prefer reusable Services.
- Prevent mistakes whenever possible.
- Verify every milestone.
- Keep documentation synchronized.
- Favor maintainability over shortcuts.

These principles have consistently produced the most reliable results throughout development.

---

# Future Lessons

This document should continue growing as the project evolves.

Only add lessons that have been validated through experience rather than speculation.