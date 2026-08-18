# Current Task

## Current Milestone

Restore Previous Values correction

## Current Status

The Project Owner standardized every gameplay reset control as **Restore
Previous Values**. The implementation now uses compatible Gameplay Operation
State as the single authority for the values captured before a gameplay tool
first changed its targets. Restore remains unavailable when that history is
missing, and an explicit restore request never adopts current configured values
as fabricated history.

The 17 shared preset tools, Party Economy, Random Trait Exclusions, Overworld
Movement Speed, and Rain Frequency use the accepted contract. Movement and Rain
retain Vanilla as ordinary selectable presets but no longer use those fixed
values as reset authority. `.wtstate`, compatible profile state, mutation-based
rollback, validation, and one-action Undo/Redo remain authoritative. Detailed
Editor Reset Property and `PropertyModel.IsModified` are unchanged. No Golden
CDB support was added.

The focused Random Trait Exclusions correction rejects Restore at the
execution boundary when compatible history has disappeared, without issuing an
Apply request. Restore selections are resolved from current compatible Gameplay
Operation State rather than immutable dialog-open candidate baselines, including
after profile/state replacement. Candidate presentation metadata is refreshed
after project operations.

The final consistency correction establishes one interaction contract across
all 23 gameplay entries: Restore Previous Values immediately applies the
captured pre-tool values through the normal validated operation path. Apply
remains available for ordinary manual configuration. Party Economy no longer
requires a second Apply click. Exact RTE baseline equality contributes zero
effective changes and produces no synthetic Review Changes row; additional
exclusions retain per-trait accounting. Renewed Focused Engineering Review and
final Project Owner acceptance are complete. The review returned **PASS WITH
NON-BLOCKING NOTES**: automated coverage exercises production ViewModel,
service, operation, mutation, validation, and history paths rather than
synthesizing WPF button clicks. Project Owner interactive testing supplies the
runtime evidence and concluded with an explicit **PASS** after the final
corrections. Sequential application and test builds complete with zero warnings
and zero errors, the focused suite passes, and all 25 Class A compatibility
groups pass. Final commit and push are the remaining lifecycle checkpoint.

Previous milestone context:

Update Profile engineering implementation and its Renewed Focused Engineering
Review are complete. The review returned **PASS WITH NON-BLOCKING NOTES — Ready
for Project Owner Interactive Acceptance**. Sequential application and test
builds completed with zero warnings and zero errors, all 22 Class A
compatibility groups passed, `git diff --check` passed, and no profile-update
temporary artifacts remained.

Random Trait Exclusions is implemented under Party with dynamically discovered,
searchable Positive and Negative checklists. Checked traits remain eligible;
unchecked traits receive `done=false`. Exact true, false, and absent baselines
are preserved, and Restore Previous Values uses the approved known-property
removal primitive only for an originally absent scalar `done` leaf.

The feature uses one atomic gameplay operation, mutation-based rollback, one
Undo/Redo history action, feature validation, Gameplay Operation State,
snapshots, profiles, and reconciled Update Profile capture. Review Changes
retains an understandable operation outcome when an exact removal has no
attached PropertyModel row.

Update Profile now reconciles the selected profile's prior effective-path
records with the current intended project instead of replacing them from dirty
leaves alone. This preserves profile content after save/baseline acceptance,
removes records intentionally restored to their profile-stored original, and
retains state-backed ordinary targets and additive requests. Candidate profiles
are staged, reloaded, and checked by an independent invariant validator that
does not call profile construction or high-level reconciliation. Historical
structural presence is explicit and distinct from JSON `null`, while Gameplay
Operation State compatibility is refreshed against current live targets before
capture. Reopening a saved CDB uses the existing profile as authoritative
history and does not require a separately maintained pristine CDB. Failed
validation preserves the previous managed profile.

Change accounting now uses effective leaf identity across current-project,
profile, and mutation-result counts. Supported removal mutations are included in
apply feedback, Random Trait Exclusions state-only outcomes remain synthetic
when necessary, additive output remains deterministic and overlap-aware, and
Review Changes resolves duplicate nested names by `EffectivePropertyPath`.

The correction pass also centralizes legacy pathless resolution, surfaces
ambiguity instead of dropping Review Changes rows, filters additive output by
effective path, and closes the public no-validation replacement route. Generic
deletion of clean-baseline properties is outside the authorized profile scope;
the supported removal case remains exact restoration of a feature-created
absent-baseline scalar property.

The earlier reset-authority investigation was completed separately and led to
the current Restore Previous Values contract.

The focused correction pass now preflights every candidate before mutation,
requires explicit stable source IDs, derives Review Changes from the persisted
Random Trait Exclusions state specifically, and validates exact requested/result
allowed-ID equality. Permanent smoke coverage also includes disconnected-target
atomicity, identity failures, unrelated gameplay-state changes, validator
mismatch rollback, and one complete managed Update Profile replay.

The real-data dialog-open failure is corrected. Trait group membership now comes
from the `trait` sheet's ordered `Starting`, `Hidden`, `Recruitment`, and
`Acquired` separator anchors; the optional numeric per-entry `gen` field is no
longer mistaken for group identity. Operation-state fingerprints retain the
resolved Starting/Recruitment group. Realistic separator-shaped coverage now
constructs the complete dialog ViewModel, proves opening is mutation-free,
checks malformed candidate and noncandidate boundaries, and rejects invalid
separator metadata. The current clean installed CDB resolves 37 candidates
(22 Positive and 15 Negative), with Stoic and Gourmand initially disabled.
This is a feature-specific correction and introduces no architecture changes.

Direct Snapshot Preview remains read-only and can conservatively report an
absent-baseline leaf as missing. Profile application materializes exclusion
state before matching and is unaffected.

Project Owner acceptance is now recorded for the corrected Update Profile,
Review Changes/effective-accounting, and full-configuration runtime workflows.
The owner applied a known non-damaged profile, saved and reloaded its CDB, made
additional edits, and updated the same profile successfully. Its effective
change count increased from 633 to 636 and validation reported no issues.
Review Changes displayed the correct result; the prior six-change discrepancy
for the nested Campfire `tool.height` and `tool.width` paths was not present.

The owner subsequently applied the full intended 645-effective-change
configuration, saved a new profile from that configured state, launched
Wartales, started a game, and played for more than one hour without obvious
instability attributable to the editor configuration. Random Trait Exclusions
behaved as expected during that session: no recruit received a trait that had
been disabled. This is positive runtime evidence, not statistical proof that an
excluded trait can never occur.

The remaining lifecycle evidence is now reconciled. Random Trait Exclusions
passed renewed focused Engineering Review with **PASS WITH NON-BLOCKING NOTES —
Ready for Project Owner Interactive Acceptance**. The owner then confirmed that
the dialog opened, the feature applied, exactly five traits were unchecked, and
exactly five changes were reported. Its later runtime evidence remains positive:
no disabled recruit trait was observed during the over-one-hour session.

The Project Owner has also explicitly confirmed that Lectern Knowledge Gain and
Positive Random Traits were tested, are working, and are accepted. These remain
separate features with their existing documented semantics. No additional test
details are inferred from those acceptance statements.

All Final Feature Batch components have now completed their required
implementation, verification, Engineering Review, Project Owner acceptance,
and documentation reconciliation. The batch is accepted, reconciled, and ready
for its final commit checkpoint.

The previously damaged approximately 554-change `All Mods.wtprofile` is not
considered repaired. The owner created a new profile from the successfully
configured 645-change state.

## Next Required Step

Run the final verification and commit/push checkpoint for the accepted Final
Feature Batch. After that checkpoint, the next authorized engineering activity
was a separate bounded reset-authority investigation.

No commit or push has been performed.

Do not begin QuickBMS integration,
Integrated Import / Install / Restore, Update Survival, or release hardening
during this boundary.
