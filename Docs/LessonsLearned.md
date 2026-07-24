# Lessons Learned

**Document Version:** 1.1  
**Last Updated:** 2026-07-24

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
- ChangeSummaryViewModel

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

Originally `PropertyModel` represented only data.

As development progressed it became responsible for:

- Type-aware editing
- Validation
- Original value capture
- Modification tracking
- Reset functionality
- Value-change notifications

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
- Change Summary

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

# Lesson 11 — A Single Source of Truth Simplifies Everything

The Change Summary milestone reinforced an important architectural principle.

Instead of introducing another change-tracking system, the Change Summary was built entirely from the existing modification state stored in `PropertyModel`.

This avoided duplicated state, synchronization bugs, and unnecessary complexity.

**Guideline**

Whenever a feature needs to answer:

> **"What is currently different?"**

it should consume the existing modification state rather than maintaining its own copy.

---

# Lesson 12 — Runtime Testing Improves User Experience

The Change Summary feature compiled successfully on the first implementation, but runtime testing revealed several usability improvements that static code review did not.

Examples included:

- Replacing internal IDs with localized display names.
- Simplifying grouping from Category + Setting to Category only.
- Returning focus to the main editor after navigation.
- Correcting Close button behavior.
- Improving overall presentation.

None of these affected the underlying architecture, but they significantly improved the user experience.

**Guideline**

Treat runtime testing as a design activity, not just a bug-finding exercise.

---

# Lesson 13 — Separate Current State from History

Undo history and modification tracking answer different questions.

Edit history answers:

> **"What happened?"**

Modification tracking answers:

> **"What is currently different?"**

Keeping these responsibilities separate produced a much simpler architecture and made the Change Summary implementation straightforward.

**Guideline**

Avoid combining history systems with current-state systems unless there is a compelling architectural reason.

---

# Lesson 14 — Composition Is Better Than Duplication

Two major milestones reinforced the value of composing existing systems rather than introducing parallel implementations.

Examples include:

- Mod Profiles composing the Snapshot workflow.
- Save validation composing the Validation pipeline.
- Validation Results reusing existing editor navigation.
- Future Content Creation Tools reusing the editing pipeline.

Each new feature became significantly smaller because it built upon infrastructure that already existed.

**Guideline**

Whenever possible, extend existing workflows rather than creating new implementations that solve the same problem.

---

# Lesson 15 — Validate Only What Can Be Verified

The Validation Framework intentionally avoids making assumptions about Wartales data.

Instead, validation is limited to rules that can be verified accurately using the currently loaded project and known editor metadata.

This minimizes false positives while ensuring users can trust every reported issue.

**Guideline**

Prefer fewer highly reliable validation rules over many uncertain ones.

---

# Lesson 16 — Modeless Utility Windows Should Behave Consistently

As the number of utility windows increased, consistency became more important than individual window behavior.

Profile Manager, Change Summary, and Validation Results all benefit from:

- Single-instance behavior.
- Independent focus.
- Shared navigation patterns.
- Consistent lifecycle management.

Future UI improvements will continue building on this shared model.

**Guideline**

Treat reusable utility windows as a unified subsystem rather than individual dialogs.

---

# Lesson 17 — Runtime Polish Should Immediately Follow Feature Completion

The Validation milestone demonstrated that the majority of remaining work after implementation often involves usability rather than functionality.

Examples included:

- Window focus behavior.
- Validation Results presentation.
- Window lifecycle improvements.
- Validation tooltip handling.
- Independent utility window focus.

Addressing these refinements before the release commit produced a significantly more polished editor without requiring architectural changes.

**Guideline**

Reserve time after functional completion for runtime polish before finalizing a milestone.

---

# Lesson 18 — Recover Infrastructure Instead of Patching Features

During development of the transactional content creation platform, merge integration temporarily broke the Add Camp Facilities implementation.

Rather than repairing the individual feature directly, the underlying Project Mutation infrastructure was extended to support reusable object-valued mutations.

Once the infrastructure correctly handled object-valued JSON, multiple higher-level systems immediately benefited:

- ContentCreationService
- Add Camp Facilities
- Operation validation
- Future content creation features

The feature-specific problem disappeared because the underlying architecture became more capable.

**Guideline**

When several features exhibit similar failures, first determine whether the underlying infrastructure should be extended before introducing feature-specific fixes.

Reusable infrastructure generally produces a more maintainable solution than repairing each affected feature independently.

---

# Lesson 19 — Architecture, Implementation, and Verification Are Different Responsibilities

As Wartales Editor grew, separating responsibilities produced better engineering results.

Architecture, implementation, compilation, runtime verification, documentation, and roadmap planning each require different kinds of thinking.

The project now uses a structured workflow where:

- Architecture and long-term design are planned before implementation.
- Implementation follows the established architecture.
- Compilation verifies correctness.
- Runtime testing verifies behavior.
- Documentation records the completed milestone.
- Version control captures a verified checkpoint before continuing.

This separation reduced context switching, improved implementation quality, and made larger milestones easier to complete without compromising architectural consistency.

**Guideline**

Treat architecture, implementation, testing, documentation, and source control as distinct phases of every milestone.

Do not consider a milestone complete until all phases have been successfully completed.

---

# Lesson 20 — Stable Architecture Accelerates Gameplay Development

Version 0.9.1 demonstrated that mature operation, mutation, validation,
state, profile, snapshot, and history systems allow new gameplay tools
to be implemented and verified without architectural redesign.

**Guideline**

After architecture matures, group compatible gameplay features into
focused development bundles and reuse the verified platform.

---

# Lesson 21 — Represent Outcomes, Not Implementation

Profile restoration exposed several internal storage and replay concepts
that were technically accurate but not useful to players. Presenting one
effective Changes result produced a clearer and more consistent
experience.

**Guideline**

Player-facing language should describe what a feature does. Keep
transactions, mutation ownership, snapshots, and replay mechanics in
diagnostics rather than normal workflows.

---

# Current Philosophy

The project now follows these guiding principles:

- Build infrastructure before features.
- Maintain a single source of truth.
- Keep MVVM boundaries clean.
- Prefer reusable Services.
- Keep models responsible for editing behavior.
- Prevent mistakes whenever possible.
- Verify every milestone through runtime testing and UI polish.
- Keep documentation synchronized.
- Favor maintainability over shortcuts.
- Represent gameplay outcomes rather than implementation details.
- Bundle compatible gameplay work after the supporting architecture is stable.

These principles have consistently produced the most reliable results throughout development.

---

# Future Lessons

This document should continue growing as the project evolves.

Only add lessons that have been validated through experience rather than speculation.
