# Player First Design

**Document Version:** 1.0  
**Last Updated:** July 2026

---

# Purpose

This document defines the long-term product philosophy of the Wartales Editor.

Unlike the Architecture document, this document is not concerned with implementation.

It answers a different question:

> What kind of application are we trying to build?

Whenever multiple implementation approaches are technically correct, this document should guide the product decision.

The Player First philosophy takes priority over implementation convenience whenever it can be achieved without compromising safety, maintainability, or architectural integrity.

---

# Vision

Wartales Editor is designed for Wartales players.

It is not intended to become a generic database editor.

The goal is to create a polished companion application that allows players to customize their game safely, easily, and confidently.

Players should be able to achieve meaningful gameplay changes without needing to understand how Wartales stores its data internally.

---

# The Gameplay Outcome Is the Feature

Players care about gameplay.

They do not care about implementation details.

For example, a player wants:

- More Starting Resources
- Larger Saddlebag Capacity
- No Volunteer Wages
- More Valour
- Additional Camp Facilities
- Better Weather

They do not need to know whether those features are implemented through:

- Scalar mutations
- Array mutations
- Nested properties
- Gameplay Operation State
- Transactions
- Validation fingerprints
- Runtime paths
- CDB records

Those implementation details exist to support the gameplay outcome.

The gameplay outcome is the feature.

---

# Gameplay Tools Over Raw Editing

Whenever practical, build gameplay tools instead of exposing raw game data.

Good examples include:

- Starting Resources
- Camp Facilities
- Character XP
- Profession XP
- Carrying Capacity
- Volunteer Wages
- Weather Controls

Avoid exposing implementation concepts such as:

- Property paths
- JSON
- CDB structure
- Internal identifiers
- Metadata
- Runtime tokens

The editor should feel like a Wartales companion rather than a database editor.

---

# Design Around Player Intent

Design features around what the player wants to accomplish.

Good examples:

- Add Camp Facilities
- Upgrade All Equipment
- Increase Carrying Capacity
- Disable Rain
- Improve Recruitment

Poor examples:

- Edit props.startQuantity
- Modify ActionPointBaseMax
- Change Transport/defaultValue

The first describes a gameplay goal.

The second describes implementation.

Players should rarely need to think about implementation.

---

# One Tool, One Purpose

Every gameplay tool should have a clear purpose.

Examples:

- Starting Resources
- Volunteer Trait
- Valour Points
- Carrying Capacity

Each window should solve one gameplay problem.

Avoid creating large "miscellaneous" dialogs simply because several features happen to edit nearby data.

Organize tools around gameplay, not file structure.

---

# Hide Technical Complexity

Whenever possible, implementation details should remain invisible.

Players should not need to understand:

- Mutation layers
- Transactions
- Validation
- Snapshots
- Profiles
- Fingerprints
- Compatibility checks

These systems are essential to the editor but should remain behind the scenes.

Complex architecture should create a simpler experience, not a more complicated one.

---

# Preserve Existing Gameplay Identity

Whenever practical:

- Preserve origin identity.
- Preserve party identity.
- Preserve existing bonuses.
- Preserve existing penalties.
- Preserve existing equipment.
- Preserve existing progression.

Prefer adding to existing systems instead of replacing them.

Examples:

Good:

- Add Starting Resources
- Add Camp Facilities
- Increase Carrying Capacity

Avoid unnecessarily replacing or rebuilding existing Wartales systems.

---

# Prefer Additive Design

Whenever practical:

Add rather than replace.

Examples:

- Add resources instead of replacing inventory.
- Increase existing carrying capacity instead of rebuilding Transport.
- Extend existing gameplay systems rather than bypassing them.

Additive changes generally preserve compatibility, reduce unintended side effects, and make future game updates easier to support.

---

# Investigation Before Implementation

Never assume:

- Runtime property names
- Array mappings
- Data relationships
- Game behavior

Investigate first.

Design second.

Implement third.

Verify last.

Good investigation consistently produces better software.

---

# Architecture Exists to Support Gameplay

The architecture is one of the project's greatest strengths.

However, architecture is not the product.

Its purpose is to make gameplay features:

- safer
- easier to implement
- easier to validate
- easier to maintain

When reusable infrastructure already exists, prefer using it.

New architecture should only be introduced when existing systems genuinely cannot support an approved feature.

---

# Safety Before Convenience

The editor should make it difficult for players to accidentally damage their projects.

Favor:

- Validation
- Undo / Redo
- Atomic operations
- Baseline-derived reapplication
- Safe defaults
- Structural compatibility checks

Convenience should never compromise project safety.

---

# Build Trust

Players should feel confident using the editor.

That confidence comes from:

- Predictable behavior
- Honest validation
- Clear messages
- Reliable saves
- Stable profiles
- Safe snapshots
- Consistent workflows

Never claim a feature has been verified unless it has actually been verified.

Honest reporting builds long-term trust.

---

# Use Wartales Language

Whenever practical, use terminology that players already know.

Prefer:

- Camp Facilities
- Valour
- Krowns
- Pony
- Saddlebag
- Profession
- Companion

Avoid internal development terminology in the user interface.

---

# The Interface Should Teach Itself

A player should rarely need documentation.

Whenever possible:

- Labels should be descriptive.
- Tooltips should explain gameplay outcomes.
- Buttons should describe their results.
- Preview text should explain what will happen.

The interface should answer the player's questions before they need to ask them.

---

# Architecture Should Become Invisible

As the project matures, less effort should be spent creating new infrastructure and more effort should be spent creating meaningful gameplay features.

A stable architecture allows future development to focus on Wartales itself rather than the editor's internals.

Success is measured by how rarely the architecture needs to change.

---

# Long-Term Goal

The long-term goal is to create the definitive companion application for Wartales.

Players should be able to:

- Customize their game.
- Experiment safely.
- Preserve their work across updates.
- Share creations with the community.
- Spend their time enjoying Wartales instead of editing game files.

Every feature should move the project closer to that goal.

---

# Guiding Principle

When faced with multiple technically correct solutions, choose the one that produces the best experience for the player while preserving the safety, stability, and maintainability of the editor.

The player comes first.

---

# Represent Outcomes, Not Implementation

Player-facing information should describe the gameplay result a player
will experience, not the internal mechanism used to produce it.

For profiles, the complete gameplay result is represented as Changes.

It should not separate that result into internal storage or execution
concepts such as snapshot properties, gameplay-operation requests, or
replayed operations. Those distinctions may remain available for
diagnostics, validation, and rollback, but they should not define the
normal player experience.
