# Current Task

## Current Milestone

Gameplay Corrections, UX Consistency, and Resource Replenishment

## Current Status

Implementation, focused compatibility correction, renewed Engineering Review,
and final reconciliation are complete. Resource Replenishment, shared
feature-window lifecycle handling, in-dialog Apply feedback, exact captured-
baseline restoration, and the approved non-blocking visual notes are included.

Project Owner testing on a fresh Wartales installation and fresh extraction
used the complete current mod set. Launch, new game, playable gameplay, save,
full exit, relaunch, and save reload passed. The earlier new-game-load freeze
was non-reproducible after the clean reinstall and extraction; its cause was not
identified.

Campfire output/reference equivalence is established. Tier 1 intentionally
remains at capacity 4; direct Tier 2 and Tier 3 assignment-count verification
remains pending and non-blocking. Resource Replenishment is implemented across
the shared land, fishing, sea, and special renewable refill categories, but
every category has not been exhaustively timed in game.

The integrated Import / Install / Restore investigation is complete.
Implementation, bounded QuickBMS/package-replacement experiments, and Update
Survival have not started.

## Next Authorized Feature Batch

1. Lectern Knowledge Gain
2. Update Existing Profile
3. Positive Random Traits

Update Existing Profile should explicitly select an existing profile and
atomically rebuild it from the complete current effective project state while
preserving appropriate identity and metadata. It must replace the profile, not
append deltas.

Positive Random Traits is expected to provide Vanilla and Positive Only. Its
accepted scope is eligible procedural recruits rather than tavern recruits
alone. Existing units remain unchanged.

After that feature batch:

1. Bounded QuickBMS/package-replacement experiments
2. Integrated Import / Install / Restore
3. Update Survival

Do not begin those later phases as part of the closed gameplay milestone.
