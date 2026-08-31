# Wartales Editor Roadmap

**Application Roadmap**\
**Last Updated:** 2026-08-31

------------------------------------------------------------------------

# Vision

The long-term goal of Wartales Editor is to become the definitive editor
for Wartales game data while also supporting a highly customizable
personal gameplay experience through safe, reusable editing tools.

------------------------------------------------------------------------

# Guiding Principles

-   Infrastructure before features.
-   Single source of truth.
-   Reusable systems over one-off implementations.
-   Keep the project compiling after every implementation step.
-   Automatically integrate new features with Modification Tracking,
    Undo/Redo, Change Summary, Snapshot Workflow, Profiles, and
    Validation whenever applicable.

------------------------------------------------------------------------

# Completed Milestones

-   0.1.0 Project Foundation ✅
-   0.2.0 Data Browser ✅
-   0.3.0 Functional Editing ✅
-   0.3.1 Find Anything ✅
-   0.4.0 Safe Editing Infrastructure ✅
-   0.5.0 Change Summary ✅
-   0.6.0 Snapshot Workflow ✅
-   0.7.0 Profile Manager ✅
-   0.8.0 Validation Framework ✅
-   0.9.0 Character Progression & Party Economy ✅
-   0.9.1 World Convenience — Overworld Movement Speed and Rain Frequency ✅

## Completed Infrastructure Milestone

-   QuickBMS Export Back to Wartales Version 1 — Engineering reviewed, exact-byte
    verification complete, Project Owner live-package accepted, and closed ✅
-   Shared Restore Previous Values local-chain authority correction — reviewed,
    Project Owner accepted, and closed ✅
-   Request Board Rewards V1 — reviewed, real-game validated, Project Owner
    accepted, and closed ✅

## Next Milestone

-   Public Release Preparation

------------------------------------------------------------------------

# Priority 1 -- Personal Gameplay Tools

## Character Progression

-   Character XP requirement slider ✅
-   Profession XP requirement slider ✅

## Equipment

-   Make All Equipment Upgradeable ✅

## Economy & Bonuses

-   Volunteer wages reduced to 0% ✅
-   Adjustable Valour Point bonuses ✅
-   Adjustable Carrying Capacity bonuses ✅
-   Adjustable Food Consumption reduction

## World

-   Increase overworld movement speed ✅
-   Faster vendor refresh
-   Faster resource respawns — deferred pending future runtime validation
-   Reduce rain frequency ✅
-   Increase camera zoom distance (investigate)

## Camp

-   Increase campfire size
-   Increase campfire seating capacity

## Crafting & Professions

-   Easier forging
-   Easier mining
-   Easier woodcutting
-   Increased Delicious cooking chance

## Recruitment

-   Positive traits on random recruits
-   Recruits start at highest party member level

## Gameplay

-   Easier tombs
-   Easier lockpicking
-   Allow boss capture
-   Investigate special boss capture item

## Investigation

-   Weather system
-   Camera limits
-   Camp interaction limits
-   Recruit level calculations
-   Hidden gameplay systems

------------------------------------------------------------------------

# Priority 2 -- Content Creation Framework

## Initial Tools

-   Add Camp Anvil
-   Make All Equipment Upgradeable

## Framework

-   Reusable content creation command architecture
-   No duplicate editing implementations
-   Automatic integration with existing editor infrastructure

------------------------------------------------------------------------

# Priority 3 -- Editor Experience

-   UI modernization
-   Improved toolbar
-   Modern icons
-   Better utility windows
-   Window state persistence
-   Better navigation
-   Better search and filtering
-   Favorites
-   Recent items

------------------------------------------------------------------------

# Priority 4 -- Advanced Editing

-   Batch editing
-   Merge preview
-   Profile migration improvements
-   Conflict resolution
-   Side-by-side comparison
-   Better dropdowns
-   Reference browsing
-   Cross-reference navigation

------------------------------------------------------------------------

# Priority 5 -- Validation Expansion

-   Cross-sheet validation
-   Duplicate detection
-   Missing/invalid references
-   Gameplay validation
-   Validation presets
-   Exportable reports

------------------------------------------------------------------------

# Priority 6 -- Additional Content Creation

-   Additional camp objects
-   Additional buildable objects
-   Additional upgrade paths
-   Additional recipes
-   Additional craftable items
-   Configurable content creation tools

### Future Expansion

-   NPC creation
-   Encounter editing
-   New professions
-   New items
-   New traits
-   New status effects

------------------------------------------------------------------------

# Priority 7 -- Data Preservation & Compatibility

-   Preserve original CDB formatting
-   Byte-for-byte preservation where practical
-   Preserve unknown data
-   Maximize compatibility with future updates

------------------------------------------------------------------------

# Priority 8 -- Performance

-   Optimize loading
-   Optimize searching
-   Optimize saving
-   Background processing
-   Memory optimization

------------------------------------------------------------------------

# Priority 9 -- Community Features (Post-1.0)

-   Community profile sharing
-   Community content sharing
-   Creator metadata
-   In-game credits popup
-   Package manifests
-   Version compatibility

Steam Workshop support is intentionally excluded.

------------------------------------------------------------------------

# Priority 10 -- Testing & Reliability

-   Regression testing
-   Snapshot migration tests
-   Validation tests
-   Content creation tests
-   Large project testing
-   Automated testing where practical

------------------------------------------------------------------------

# Priority 11 -- Long-Term Architecture

-   Infrastructure before features
-   Single source of truth
-   Clean MVVM architecture
-   Reusable services
-   Dependency injection
-   Continued documentation
-   Minimize technical debt
