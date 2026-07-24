Wartales Editor Roadmap 2.0
Part 1 — Vision, Philosophy, and Version 0.9 Foundation
Wartales Editor Roadmap

Application Roadmap
Document Version: 2.0
Target Baseline: Version 0.8.1+

Vision

Wartales Editor is intended to become the definitive editor for Wartales game data while preserving the stability, safety, and compatibility expected from a professional editing tool.

Unlike many traditional game editors, Wartales Editor is built around reusable editing infrastructure rather than one-off modifications. Every feature should integrate naturally with validation, transactions, undo/redo, profiles, snapshots, and future game updates whenever practical.

The editor has now reached the point where its core architecture is largely complete.

Future development shifts from building infrastructure to creating practical gameplay features that improve the Wartales experience while continuing to strengthen long-term compatibility with future game versions.

Development Philosophy
Gameplay First

The primary objective of Version 0.9 is no longer building editor infrastructure.

The objective is implementing the gameplay features that the editor was created to make possible.

New editor infrastructure should only be added when it directly enables or significantly improves gameplay features.

Infrastructure Before Features

This principle remains unchanged.

When new infrastructure is required:

Build it once.
Build it correctly.
Reuse it everywhere.

No feature should introduce duplicate editing logic when an existing framework can be extended.

Single Source of Truth

Every system should continue relying upon a single authoritative implementation.

Examples include:

Transaction Framework
Validation Framework
Project Operation Framework
Content Creation Framework
Nested Property Infrastructure
Object-Valued Mutation Infrastructure
Modification Tracking
Snapshot Workflow
Profile Management

No feature should bypass these systems.

Safe Editing Above All

Every modification should continue integrating with:

Undo / Redo
Change Summary
Validation
Transactions
Profiles
Snapshot Workflow
Project Operations

Users should never have to sacrifice safety in exchange for convenience.

Update-Friendly Design

Whenever practical, new gameplay features should survive future Wartales updates through existing migration and profile systems.

Long-term compatibility is considered one of the defining goals of the project.

Guiding Principles
Gameplay-first development.
Infrastructure before features.
Single source of truth.
Reusable systems over one-off implementations.
Keep the project compiling after every implementation step.
Preserve existing architecture whenever possible.
Never duplicate editing logic.
Prefer configurable tools over hard-coded mutations.
Preserve unknown game data whenever practical.
Maximize compatibility with future Wartales releases.
Continue documenting major architectural decisions.
Completed Major Milestones
Version 0.1
Project Foundation
Version 0.2
Data Browser
Version 0.3
Functional Editing
Intelligent Property Editing
Find Anything
Version 0.4
Safe Editing Infrastructure
Undo / Redo
Modification Tracking
Version 0.5
Change Summary
Snapshot Workflow
Version 0.6
Snapshot Import / Export
Snapshot Matching
Snapshot Preview
Snapshot Application
Version 0.7
Mod Profiles
Profile Management
Version 0.8

Major architectural completion:

Validation Framework
Project Mutation Layer
Content Creation Infrastructure
Project Operation Framework
Transaction Framework
Nested Property Infrastructure
Object-Valued Mutation Infrastructure

Verified gameplay features:

Camp Facilities
Equipment Upgrade System
Weather Modification Framework

All major infrastructure has now been verified through:

Build testing
Validation testing
Save / Reload testing
Extended in-game verification
Version 0.9 — Personal Gameplay Expansion
Objective

Version 0.9 focuses on implementing practical gameplay improvements using the architecture completed during Versions 0.1 through 0.8.

The emphasis shifts from building editor capabilities to creating high-quality gameplay tools that improve the Wartales experience while remaining fully compatible with the editor's safety systems.

Editor improvements should be implemented only when they directly support gameplay feature development.

Version 0.9.0 — Character Progression & Party Economy
Objective

Provide adjustable progression and economy controls that allow players to customize the pace of a campaign without manually editing game data.

These tools should expose meaningful controls rather than requiring users to understand raw game values.

Character Progression
Character XP requirement adjustment
Profession XP requirement adjustment
Adjustable progression percentages
Reset-to-default support
Profile compatibility
Party Economy
Volunteer wage reduction
Adjustable Valour Point bonuses
Adjustable carrying-capacity bonuses
Adjustable food-consumption reduction
Design Goals

Whenever practical:

percentage sliders
numeric controls
validation
preview
transaction support
profile compatibility

Complexity:

Low–Medium

Primary dependency:

Existing Project Operation Framework.

Version 0.9.1 — World Convenience
Objective

Reduce repetitive waiting and travel while preserving normal gameplay progression.

Planned Features
Travel
Increase overworld movement speed
Vendors
Faster vendor refresh
Resources
Faster resource respawns
Camera
Increased camera zoom distance
Investigate additional camera limitations where data-driven
Weather Controls

Expand the completed weather framework into reusable gameplay tools.

Planned operations include:

Reduce rain frequency
Disable rain globally across all regions
Adjustable regional weather controls
Global weather operations
Weather presets for personal gameplay

The investigation phase has already been completed.

Future work focuses entirely on exposing these capabilities through reusable editor tools.

Complexity:

Low–Medium

Version 0.9.2 — Camp Improvements
Objective

Expand camp customization using the existing Content Creation Infrastructure.

Camp Features
Campfire
Increase campfire size
Increase campfire seating capacity
Increase usable interaction slots where possible
Camp Facilities

Continue expanding the official Camp Facility tools.

Potential additions include:

Additional crafting stations
Additional utility structures
Additional decorative objects
Future camp facilities requested during development

Every new facility should reuse the existing Content Creation Framework rather than introducing one-off mutation logic.

Design Goals
Dedicated feature buttons
Validation integration
Transaction support
Profile compatibility
Snapshot compatibility
Safe rollback

Complexity:

Medium

Part 2 — Gameplay Expansion, Update Survival & Editor Support
Version 0.9.3 — Crafting & Profession Improvements
Objective

Reduce repetitive profession grinding while preserving the core progression and identity of each profession.

The goal is not to automate professions, but to make them more enjoyable during long campaigns.

Forging
Easier forging minigame
Adjustable success requirements
Adjustable timing or tolerance where supported by game data
Mining
Easier mining minigame
Adjustable success requirements
Reduced failure frustration
Woodcutting
Easier woodcutting
Adjustable success requirements
Cooking
Increased Delicious meal chance
Adjustable probability rather than fixed behavior
Preserve existing cooking progression
Design Goals

Whenever practical:

Adjustable percentages
Safe ranges
Preview before application
Validation support
Transaction support
Profile compatibility

Complexity:

Medium

Version 0.9.4 — Exploration Improvements
Objective

Improve exploration without fundamentally changing combat balance.

These features primarily reduce repetitive mechanics rather than difficulty.

Lockpicking
Easier lockpicking
Adjustable lock difficulty
Adjustable success window where supported
Tomb Puzzles

The objective is only to simplify puzzle mechanics.

No combat balance changes are planned.

Possible controls include:

Easier puzzle requirements
Reduced puzzle complexity
Adjustable puzzle difficulty

Combat encounters, rewards, and progression should remain unchanged.

Camera Improvements

Expand camera customization beyond the initial world zoom improvements where data-driven.

Potential future controls include:

Additional zoom limits
Camera distance
Camera movement restrictions

Complexity:

Medium

Version 0.9.5 — Recruitment Improvements
Objective

Improve the quality of recruit generation while preserving natural gameplay progression.

Traits
Positive traits on random recruits
Reduced undesirable trait combinations
Configurable trait generation rules where practical
Recruit Levels
New recruits begin at the highest party member level
Preserve sensible scaling
Validate unusual party progression
Future Recruitment Options

Potential future improvements:

Recruitment cost adjustments
Recruit equipment presets
Recruitment pool customization

Complexity:

Medium–High

Primarily due to investigation of recruit generation logic.

Version 0.9.6 — Special Gameplay Rules
Objective

Expose special gameplay mechanics that normally cannot be modified through ordinary editing.

Boss Capture

Primary goal:

Allow bosses to be captured without unintentionally affecting unrelated enemies.

Investigation areas include:

Capture restrictions
Boss flags
Encounter rules
Unit definitions
Capture eligibility
Special status restrictions
Boss Capture Item

If ordinary capture cannot safely support bosses:

Investigate creating a dedicated capture item specifically intended for bosses.

This should only be pursued if it produces a cleaner implementation than altering global capture behavior.

Future Special Rules

Additional gameplay rules discovered during future investigation may be added here.

Complexity:

High

Requires careful validation and extensive in-game testing.

Version 0.9.7 — Additional Personal Gameplay Features

This milestone serves as the home for future gameplay improvements discovered through continued play.

Examples include:

New quality-of-life adjustments
Campaign convenience tools
Balance tweaks
Additional camp improvements
Additional crafting improvements
Additional gameplay operations

The intent is to continue expanding the editor based on actual gameplay experience rather than speculative feature planning.

Version 0.9.8 — Update Survival & Migration
Objective

Enable users to continue using their preferred gameplay modifications across future Wartales updates with minimal manual work.

This is considered one of the defining capabilities of Wartales Editor.

Long-term compatibility is a higher priority than adding large numbers of unrelated editor features.

Smarter Profile Matching

Improve matching when:

IDs change
Objects move
Structures evolve
Additional fields appear
Improved Migration
Better profile migration
Better snapshot migration
Safer migration diagnostics
Migration confidence scoring
Detection of changed base values
Merge Preview

Allow users to preview every migrated change before application.

Categories should include:

Safe
Changed
Missing
Conflicting
Uncertain
Conflict Resolution

Allow users to choose between:

Base value
Profile value
Manual value

Never silently overwrite uncertain data.

Import Previous Modifications

Support importing modifications from previous Wartales versions.

Goals include:

Preserve years of accumulated edits
Simplify migration after game updates
Avoid rebuilding personal gameplay changes
Compare Projects

Compare any two Wartales projects.

Potential capabilities:

Added records
Removed records
Changed records
Nested property comparison
Object comparison
Selective import

This is valuable both for update migration and for understanding how gameplay modifications affect the underlying data.

Complexity:

High

Benefits from nearly every major framework already implemented.

Editor Features That Directly Support Gameplay

These features exist to make gameplay feature creation easier.

They should only be prioritized when they directly accelerate gameplay development.

Batch Editing
Multi-record operations
Preview affected entries
Transaction support
Undo/Redo integration
Validation integration
Snapshot compatibility
Better Search & Filtering
Modified entries
Validation state
Nested properties
Property values
Current sheet filtering
Advanced search criteria
Better Dropdowns
Friendly display names
Preserve raw values
Context-aware value lists
Improved usability
Reference Browsing
Navigate referenced records
View incoming references
Nested reference support
Cross-sheet navigation
Cross-Reference Navigation

Allow users to quickly move between related records without manually searching.

Property Descriptions

Provide known information about properties including:

Purpose
Expected values
Validation guidance
Related records

Descriptions should clearly distinguish:

Verified behavior
Inferred behavior
Unknown behavior
Change Templates

Save reusable collections of modifications for repeated use.

Potential applications:

Balance presets
Difficulty presets
Economy presets
Gameplay templates
Favorites
Favorite entries
Favorite sheets
Favorite properties
Recent Items
Recently visited entries
Recently opened projects
Quick navigation
Developer Mode

Expose advanced information without cluttering the normal editing experience.

Potential additions:

Raw identifiers
Internal paths
Diagnostics
Operation details
Validation details
Validation Expansion

With the Validation Framework complete, future work focuses on expanding validation knowledge rather than infrastructure.

Planned Validation
Cross-sheet validation
Duplicate detection
Missing references
Invalid references
Gameplay validation
Content creation validation
Nested object validation
Object-valued validation
Validation Presets

Support validation profiles such as:

Standard
Strict
Gameplay-safe
Content creation
Validation Reports

Generate exportable reports including:

Errors
Warnings
Information
Suggested fixes
Migration concerns

Part 3 — Version 1.0, Reliability & Release Readiness
Version 1.0 — Stable Public Release
Objective

Version 1.0 represents the point where Wartales Editor becomes a mature, stable editing application suitable for long-term use.

Version 1.0 is not intended to contain every feature that could ever be imagined.

Instead, it represents a complete, polished editor with reliable gameplay editing, excellent compatibility, and a robust update-survival workflow.

The goal is that a user can confidently build, maintain, and migrate a personal Wartales mod entirely within Wartales Editor.

Version 1.0 Release Criteria

The following objectives define Version 1.0.

Editing Platform

Completed:

Safe editing
Undo / Redo
Intelligent property editing
Nested property editing
Object-valued editing
Search
Change Summary
Transactions
Project Operations
Gameplay Editing

Provide a comprehensive collection of official gameplay tools including:

Character progression
Economy
Travel
Weather
Camp improvements
Profession improvements
Recruitment improvements
Exploration improvements
Special gameplay rules

The editor should contain a practical collection of high-quality gameplay modifications rather than hundreds of unfinished experiments.

Update Survival

Version 1.0 should provide a dependable workflow for preserving personal gameplay modifications after Wartales updates.

This includes:

Profiles
Snapshots
Migration
Merge Preview
Conflict Resolution
Project Comparison

Update survival is considered one of the defining features of Wartales Editor.

Content Creation

The Content Creation Framework should support a curated collection of official tools.

Examples include:

Camp facilities
Upgrade paths
Buildable objects
Recipes
Craftable objects

Future expansion should continue using this framework rather than introducing feature-specific implementations.

Validation

Provide comprehensive validation for supported editing operations.

The goal is preventing accidental corruption while still allowing advanced editing.

Documentation

Documentation should accurately describe:

Architecture
Gameplay tools
Validation
Profiles
Migration
Content creation
Project operations
Development workflow
Reliability

The application should consistently pass:

Build verification
Validation
Save / Reload testing
Extended gameplay verification

Major features should be considered complete only after successful in-game testing.

Data Preservation & Compatibility
Objective

Preserve the original Wartales data as faithfully as possible while modifying only the intended content.

Compatibility with future game versions is one of the project's highest priorities.

Preserve Original Formatting

Whenever practical:

Preserve original formatting.
Preserve ordering.
Preserve whitespace.
Preserve comments if they ever become applicable.
Minimize unrelated file changes.

The long-term objective is to produce output that differs from the original only where intentional modifications have been made.

Byte-for-Byte Preservation

Where practical:

Preserve untouched sections exactly.
Avoid unnecessary serialization changes.
Preserve original numeric formatting.
Preserve original object ordering.
Preserve original array ordering.

This improves:

Compatibility
Comparison
Version control
Future migration
Unknown Data Preservation

The editor should never discard data simply because it is not yet understood.

Goals include:

Preserve unknown objects.
Preserve unknown properties.
Preserve unknown nested structures.
Preserve future game-version additions.

Unknown data should survive editing whenever practical.

Structural Preservation

Maintain the integrity of:

Object hierarchy
Nested collections
References
Data relationships

Editing should modify only the requested values.

Reliability
Automatic Backups

Before overwriting project data:

Create recoverable backups.
Identify backup source.
Preserve previous revisions.
Allow recovery from failed edits.
Structural Validation

Confirm:

Valid document structure
Valid object hierarchy
Expected property types
Valid references
Consistent relationships
Save Verification

After saving:

Confirm successful serialization.
Confirm expected structure.
Confirm readability.
Confirm compatibility with the loading pipeline.
Unknown Future Compatibility

Whenever possible:

Future Wartales versions should continue loading successfully even when introducing additional fields or structures.

The editor should adopt a conservative approach to unfamiliar data rather than rewriting or discarding it.

Performance
Objective

Performance improvements should be driven by measured bottlenecks rather than speculation.

The editor is intended to remain responsive while editing large Wartales projects, but performance work should never compromise correctness or maintainability.

Loading

Continue improving:

Startup time
Project loading
Localization loading
Profile loading
Saving

Optimize:

Serialization
File writing
Transaction processing

without changing save correctness.

Searching

Continue improving:

Search speed
Nested-property searching
Reference searching
Filter performance
Memory Usage

Reduce unnecessary allocations while preserving readability and maintainability.

Background Processing

Where beneficial:

Move long-running operations away from the UI thread while maintaining transaction safety.

Examples include:

Validation
Migration analysis
Project comparison
Large searches
Testing & Release Readiness
Objective

Every major feature should complete a consistent verification process before being considered finished.

Testing is an integral part of development rather than a final phase.

Required Verification

Every completed feature should pass:

Build
Successful compilation
No new warnings introduced where avoidable
Validation
Validation framework passes
No unexpected validation failures
Save / Reload
Save project
Reload project
Verify data integrity
Verify change persistence
Profiles

Confirm compatibility with:

Profiles
Snapshots
Transactions
Change Summary
In-Game Testing

Whenever gameplay is affected:

Launch the game
Verify intended behavior
Verify no unintended side effects
Verify save compatibility
Verify continued gameplay stability

No gameplay feature should be considered complete until it has been successfully verified in-game.

Regression Testing

As the project grows, continue expanding regression coverage for:

Profiles
Migration
Validation
Content creation
Gameplay tools
Nested properties
Object-valued editing
Save compatibility
Version 1.0 Definition

Version 1.0 is achieved when the editor provides:

A mature editing platform.
A comprehensive set of personal gameplay tools.
Reliable update-survival workflows.
Strong validation.
High-confidence save compatibility.
Comprehensive documentation.
Stable in-game behavior.
Proven long-term maintainability.

At that point, future development should shift from completing the editor to expanding what users can create with it.

Part 4 — Post-1.0 Expansion & Long-Term Vision
Post-1.0 — Content Expansion
Objective

After Version 1.0, the editor transitions from completing its core functionality to expanding what players can create.

The emphasis shifts from editor capabilities to game content.

The existing architecture should make most future additions significantly easier than they would have been earlier in development.

Expanded Content Creation
Camp Objects

Continue expanding the library of official camp additions.

Potential examples include:

Additional crafting stations
Utility objects
Decorative objects
Storage improvements
Quality-of-life camp features

Each addition should be implemented using the existing Content Creation Framework rather than introducing feature-specific mutation logic.

Buildable Objects

Expand support for creating additional world objects.

Potential examples:

Structures
Interactive objects
Placeable content
Additional construction options
Upgrade Paths

Expand the existing equipment upgrade framework.

Potential future tools include:

Additional upgrade chains
Custom upgrade trees
Configurable upgrade templates
Recipes

Continue adding official recipe creation tools.

Examples include:

New crafting recipes
Profession recipes
Cooking recipes
Craftable Items

Support creation of additional:

Equipment
Consumables
Components
Utility items
Configurable Content Creation

Whenever practical, replace one-off buttons with configurable creation tools capable of producing multiple variations from a common framework.

Examples include:

Camp object wizard
Recipe wizard
Upgrade wizard
Buildable object wizard
Long-Term Gameplay Expansion

As new Wartales updates introduce mechanics, continue expanding the editor through reusable gameplay operations.

Potential future areas include:

Additional profession adjustments
Difficulty customization
New economy controls
Campaign customization
World customization
Additional convenience tools

The roadmap should remain flexible enough to accommodate future gameplay discoveries without requiring architectural redesign.

Advanced Content Creation

These features are intentionally deferred until after Version 1.0 because they are significantly larger in scope.

NPC Creation

Support creation of new NPC definitions.

Potential capabilities include:

Appearance
Equipment
Statistics
Dialogue references
AI configuration
Spawn information
Encounter Editing

Support editing or creating encounters.

Potential areas include:

Enemy composition
Spawn rules
Rewards
Conditions
Regional encounter tables
Profession Expansion

Potential future support for:

New professions
Profession progression
Profession content
New Items

Support creation of entirely new items using reusable creation workflows.

New Traits

Create additional character traits.

New Status Effects

Support new buffs, debuffs, and status systems.

Future Systems

As the internal game data becomes better understood, additional creation tools may be added without redesigning the editor architecture.

Community Features

Community features are intentionally deferred until the editor is considered complete for personal use.

The primary objective is building the best editor—not building an ecosystem around it.

Profile Sharing

Allow users to share:

Gameplay profiles
Balance presets
Convenience presets
Content Sharing

Support distribution of:

Content creation packages
Gameplay packages
Community-created additions

The editor should support importing and exporting these packages through well-defined formats.

Creator Metadata

Allow creators to include:

Name
Description
Version
Homepage (optional)
Credits
Compatibility information
Package Manifests

Support package metadata describing:

Editor version
Wartales version
Dependencies
Required content
Compatibility notes
Version Compatibility

Provide compatibility information whenever possible.

Potential examples:

Built for game version
Compatible editor version
Migration recommendations
Update warnings
In-Game Credits

Future profile packages may optionally display an informational in-game popup indicating:

Active profile
Profile author
Version
Optional credits

This feature should remain optional and never interfere with gameplay.

Steam Workshop

Steam Workshop support is intentionally excluded.

Wartales does not provide Steam Workshop integration, and the project should instead focus on standalone profile and content sharing if community distribution becomes desirable.

Long-Term Architecture

Although most architectural foundations are complete, future development should continue following the project's core principles.

Continue Reusing Existing Systems

Every future feature should continue integrating with:

Transactions
Validation
Profiles
Snapshots
Project Operations
Undo / Redo
Change Summary

No parallel editing systems should be introduced.

Minimize Technical Debt

Future refactoring should only occur when it:

Reduces complexity
Improves maintainability
Enables significant new functionality

Avoid redesigning stable architecture simply because a different approach becomes available.

Documentation

Continue maintaining:

Architecture
Development Journal
Dashboard
Project Status
Changelog
Lessons Learned
Developer Guide

Documentation should evolve alongside the application rather than becoming an afterthought.

Continuous Improvement

The roadmap is intended to evolve.

Completed features should move into historical milestones.

New gameplay ideas discovered through actual play should replace speculative work whenever practical.

The roadmap should remain a living document rather than a static feature list.

Product Philosophy

The original goal of Wartales Editor was to safely edit Wartales data.

The project has since grown into something much larger.

Its long-term purpose is to provide a reliable platform for creating, maintaining, and preserving personalized Wartales experiences.

The editor should remain:

Safe
Reusable
Extensible
Well-documented
Maintainable
Compatible with future game updates

Above all, development should continue solving real problems encountered during gameplay rather than pursuing features simply because they are technically possible.

Guiding Commitments

As development continues, the project commits to the following principles:

Gameplay-first development after Version 0.8.
Infrastructure before features whenever new architecture is required.
No duplicate editing implementations.
Single source of truth.
Safe editing by default.
Update-friendly design.
Conservative handling of unknown game data.
Long-term maintainability over short-term convenience.
Comprehensive documentation.
Continuous in-game verification of gameplay features.
Roadmap Summary
Version 0.9

Personal Gameplay Expansion

Focus on implementing the gameplay features that motivated the creation of Wartales Editor:

Character progression
Economy
World travel
Weather controls
Camp improvements
Profession improvements
Exploration improvements
Recruitment improvements
Boss capture
Additional quality-of-life gameplay tools
Version 0.95

Update Survival

Strengthen the editor's ability to preserve gameplay modifications across Wartales updates through migration, comparison, merge, and conflict-resolution tools.

Version 1.0

Stable Editor Release

Deliver a mature editing platform featuring:

Reliable gameplay editing
Robust validation
Update resilience
High-confidence save compatibility
Comprehensive documentation
Proven in-game stability
Post-1.0

Expansion Platform

Continue extending the editor through additional content creation, gameplay systems, and optional community features built upon the stable architecture established during Versions 0.1–1.0.

Closing Statement

Wartales Editor is no longer simply a save editor or collection of one-off modifications.

It is a reusable editing platform designed to evolve alongside Wartales itself.

By completing the core architecture first, the project has established a strong foundation that allows future gameplay features, content creation tools, and compatibility improvements to be developed consistently, safely, and with minimal duplication.

The roadmap will continue to evolve as new gameplay ideas emerge, but its guiding philosophy remains constant:

Build reusable systems where necessary, but always prioritize creating the gameplay experience you actually want to play.