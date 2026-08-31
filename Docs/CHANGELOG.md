# Changelog

All notable changes to Wartales Editor are documented in this file.

The format is inspired by Keep a Changelog and adapted for this project.

---

# Version 1.0.0 — Public Release Preparation

**Status:** Phase 1 complete; final package validation and publication pending

## User-facing release scope

- Gameplay-focused tools across progression, party, professions, world, camp,
  and equipment, including Request Board Rewards.
- Detailed Editor with search, localized names, type-aware editing, and
  property reset.
- Profiles, effective change accounting, and compatible Restore Previous Values
  history.
- Review Changes, Check Project, validation-before-save, and atomic Undo/Redo.
- Update Survival compatibility assessment for freshly imported game data.
- Optional Golden CDB reference management and difference comparison.
- Optional Language Data setup for localized Wartales names.
- Safe QuickBMS Import From Wartales and directly verified Export Back to
  Wartales using an external, user-supplied toolchain.

## Release preparation

- Added the MIT project license, third-party notices, public credits,
  unofficial-project disclaimer, privacy statement, SmartScreen guidance, and
  prominent AI development disclosure.
- Added a public-first root README and reconciled the User Guide for onboarding,
  support boundaries, QuickBMS setup, export recovery, and portable removal.
- Reconciled application and public metadata to version 1.0.0 and hardened
  repository ignore rules against proprietary game data and user-local state.

The self-contained release package, checksum, final supported Wartales/toolchain
version record, clean-machine validation, tag, and public release publication
remain pending. No package has been published by this entry.

---

# Request Board Rewards V1 and Shared Restore Authority

**Status:** Complete and closed; focused Engineering Reviews passed with
non-blocking hardening notes, runtime/editor acceptance passed, and final
reconciliation completed

## Added

- **Gameplay Tools → World → Request Board Rewards** with 100%, 150%, 200%, and
  300% captured-baseline presets plus Restore Previous Values.
- Dynamic discriminator-based ownership of the complete
  `MissionGoldMinDifficulty/valueDifficulty` and
  `MissionGoldMaxDifficulty/valueDifficulty` arrays, preserving order, unknown
  members, integer tokens, and unrelated reward modifiers.
- Decimal, checked, non-compounding scaling with midpoint rounding away from
  zero; both arrays mutate atomically as one Undo/Redo action.
- Percentage-intent profile capture and destination-baseline replay, Update
  Survival classification, Golden visibility, and 101 focused checks.
- Optional Version 2 `LocalRestoreContentIdentity` for bounded same-project
  Restore authority when ordinary Open has unknown pristine-source provenance.

## Corrected

- Ordinary-open projects can now Restore Previous Values immediately, after
  Save, and after Save/reopen without inventing verified source provenance.
- Active local state survives exact CDB/sidecar rebinding, while unknown or
  legacy history, content mismatch, incompatible targets, and expected-current
  fingerprint mismatch remain blocked.
- Profiles and snapshots strip local-only authority. Cross-generation,
  historical, rebase, and Update Survival restoration continues to require
  verified matching source-generation authority.

## Accepted

- Project Owner editor acceptance exercised Restore Previous Values across
  multiple gameplay features and the profile/save/export workflow.
- Real Wartales validation launched successfully, started a new game, showed
  changed Request Board base Krown rewards, completed a battle and save, and
  successfully loaded that save after a full exit.
- Final verification passed Request Board 101, Golden 199, Export 202, Update
  Survival 180, focused QuickBMS Import, Language Data, focused Restore, and all
  25 Class A groups with zero build warnings or errors.

---

# QuickBMS Export Back to Wartales Version 1

**Status:** Complete and closed; Engineering Review and Project Owner live-package
acceptance passed, exact verification completed, and final reconciliation completed

## Added

- **File → Export Back to Wartales...** with normal save-first validation and
  exact persisted-content identity authority.
- Marked GUID temporary sessions containing only `Modded\data.cdb` plus a
  verification folder created after a confirmed write.
- Filtered `-w -r -r -f "{}data.cdb"` reimport through the existing contained
  process runner, followed by filtered read-only extraction and exact SHA-256/
  length comparison.
- Shared Import/Export busy state and a small owned progress window with safe
  preparation cancellation and non-cancellable write/verify stages.
- Package signature, non-reparse, and exclusive read/write preflight plus a pure
  exact-one-file QuickBMS output parser.
- 202 isolated export transport, WPF lifecycle, save-first, source-race,
  timeout, cleanup, state-neutrality, and UI-presentation regression checks.
  Directory reparse boundaries are exercised with junctions; file-link cases
  retain the repository's privilege-dependent skip. No installed package was
  written during implementation or correction.

## Corrected after Focused Engineering Review

- Source identity and staging now consume one accepted in-memory byte snapshot;
  the source path is never reopened as transport authority afterward.
- Structured post-write outcomes are captured before presentation, so a dialog
  failure cannot replace success or a known failure with a false unchanged-game
  claim.
- Cleanup failure is preserved and surfaced independently for preparation,
  cancellation, success, and transport failure outcomes.
- Verified Success is finalized before best-effort Completed progress reporting,
  so an observer failure cannot rewrite the package outcome.
- Partial workspace-creation failure now preserves the primary error, retained
  editor-owned path, and cleanup status; the next safe run reconciles residue.
- Behavioral tests cover owner failure before dialog tracking, the actual
  MainWindow close/retry chain, and complete state neutrality for Success and
  VerificationFailed outcomes.

## Boundaries

- No automatic package backup/restore, manifest, lineage, provenance/generation
  gate, Golden integration, or project mutation. Manual backup and Steam Verify/
  reinstall remain the recovery model.

## Accepted

- Project Owner live acceptance completed the authorized direct write, exact
  verification, modified-file detection, new-game launch, and in-game Rusty Shiv
  sale-price check successfully.
- The post-acceptance MainWindow maximize cleanup passed focused review and Project
  Owner visual acceptance without changing Export behavior.

---

# Golden CDB Version 1

**Status:** Complete and closed; Final Renewed Focused Engineering Review passed
with non-blocking notes, Project Owner acceptance and re-test passed, and final
documentation reconciliation completed

## Added

- One optional user-authoritative Golden reference at
  `<Documents>\Wartales Editor\Golden CDB\data.cdb` with exact-byte SHA-256
  identity and no metadata/archive companion.
- **Tools → Golden CDB...** as one owned modeless window for Set/Replace,
  standalone selection, read-only current-game import and designation, Load,
  Compare, and Remove.
- Shared sidecar-free reference loading that hashes and parses the same bytes,
  uses the existing project model factory, and grants no source provenance or
  gameplay-state authority.
- Atomic exact-byte initial publication/replacement with staged validation,
  rollback verification, failure recovery, and transient cleanup.
- Explicit save-over-Golden protection for both a loaded canonical project and
  another project selecting the canonical destination.
- Difference-only live comparison with exact/modeled all-clear states, stable
  modeled identity, array shape/value classification, and separate aggregated
  unsupported-coverage reporting.
- 188 isolated permanent Golden CDB checks covering storage, validation,
  atomicity, removal, load, save protection, overwrite reconciliation,
  comparison, caching, no-mutation boundaries, and modeless UI structure.

## Corrected after Focused Engineering Review

- Retained unresolved sheet, entry, and property identities in comparison indexes
  so ambiguity/unsupported coverage cannot also become false Missing/New results.
- Replaced attached-property-only Set Current detection with a live-versus-fresh-
  persisted CDB content comparison, covering structural removal plus gameplay
  state while preserving genuine state-only designation.
- Added explicit cleanup-warning outcomes, stale transaction ownership cleanup,
  failure-atomic Load Golden publication coverage, and cold-cache source deletion
  coverage.
- Resolved Golden save intent and final destination before running the unchanged
  ordinary save-validation workflow.

## Corrected after Project Owner testing

- Corrected **Import Current Wartales CDB as Golden** to reuse QuickBMS durable
  acquisition mechanics without publishing to normal `Extracted\data.cdb` or
  publishing the acquired CDB as the active editor project. The validated CDB now
  remains in a detached temporary `GoldenImport` workspace until exact-byte Golden
  designation completes, then the workspace is cleaned.
  Dirty current projects no longer receive an abandon-unsaved prompt; project
  identity, JSON, gameplay state, Undo/Redo, compatibility, references, and
  sidecar state remain unchanged even when the active project is the exact normal
  Extracted destination. Normal Import still durably publishes and opens the acquired project,
  and Load Golden remains the explicit Golden-opening action.
- Preserved detached cleanup warnings after successful designation, replacement
  decline, and designation failure. Same-path production regressions raise the
  Golden suite from 193 to 197 checks.
- Corrected retained detached-session accumulation. A temporary exact ownership
  marker now permits the next Golden acquisition to reconcile safe stale GUID
  sessions before creating one fresh session; unrecognized or undeletable content
  blocks refresh. Production retry and local WPF warning coverage raise the Golden
  suite to 199 checks. Final Narrow Engineering Review returned **PASS** with no
  findings, and the Project Owner brief re-test passed.
- Superseded the earlier deferred classification of Golden-window-local feedback.
  The owned Golden ViewModel now presents one ephemeral wrapping progress/result
  message for Import, Load, Set/Replace, Select, Remove, Compare, cancellation,
  split import/designation failures, and cleanup warnings. Reopening starts clean,
  and technical identity/hash remains hidden.

- Added **Import Current Wartales CDB as Golden**, which reuses the existing
  read-only acquisition and designates its exact durable imported CDB through
  `GoldenCdbService` without active-project publication.
- Preserved separate Golden replacement confirmation and truthful cancellation,
  import-failure, replacement-decline, and post-import designation-failure
  outcomes.
- Removed the visible identity/hash row from the normal Golden window while
  preserving internal SHA-256 authority and player-facing status/cleanup text.
- Added behavioral production-path regressions proving that the original Import
  From Wartales command has no Golden side effect, accepted current-game import
  replaces an existing Golden with exact durable bytes, and three Golden window
  close/reopen/import cycles retain one handler with no stale callback.

## Accepted

- Project Owner testing and the brief corrected-functionality re-test passed for
  the Golden window, current Wartales import/designation, hidden technical hash,
  and retained Load Golden workflow.
- Final Renewed Focused Engineering Review returned **PASS WITH NON-BLOCKING
  NOTES** with no production defect, unsafe test seam, artifact residue, or
  remaining closure blocker.
- Golden-specific progress and result messaging is implemented inside the Golden
  window; its earlier deferred classification was superseded.

## Preserved

- Golden is observational and does not alter profiles, snapshots, `.wtstate`,
  Update Survival provenance, Restore Previous Values, Check Compatibility,
  transactions, Undo/Redo, or `PropertyModel.IsModified` authority.
- No Steam API, pristine/vanilla certification, automatic comparison,
  Golden-based restore/mutation, QuickBMS write-back, or public-release work was
  introduced.

# Update Survival

**Status:** Complete; final Renewed Focused Engineering Review passed with
non-blocking notes and final Project Owner Interactive Acceptance passed

## Added

- Separate exact-byte source-generation and current-content CDB identities.
- Version 2 `.wtstate` provenance manifest with bounded historical gameplay
  state and conservative Version 1 migration.
- Profile Version 3 and snapshot Version 2 source-generation diagnostics.
- Mutation-free gameplay compatibility assessment and a modeless, reopenable
  Update Compatibility report.
- Explicit gameplay-operation execution journals with exception rollback.
- 180 focused permanent Update Survival checks, including the provenance,
  portable-state, rollback, preflight, compatibility, history, and unknown-data
  regressions required by the first Focused Engineering Review.

## Changed

- QuickBMS is the authoritative pristine-source boundary and now classifies
  first, same-source, changed-source, and unknown-prior imports.
- Ordinary Open trusts source provenance only when manifest current-content
  identity matches the exact parsed file bytes.
- Save and Save As preserve verified source identity while advancing the
  current-content binding.
- Cross-generation or unknown gameplay state remains non-restorable history;
  compatible ordinary profile changes continue independently.
- Exact current-content binding and verified source provenance are represented
  separately. Unknown/untrusted history loses actionable source authority;
  malformed and unreadable prior sidecars remain explicit conservative states.
- Portable gameplay state requires a current provenance-aware container and
  matching valid root, record, and verified target source identities.
- Rollback is attempted once and fails fatally if restoration cannot complete.
  Add Camp Facilities preflights its complete merge/craft scope, and Upgrade All
  Equipment requires exactly one entry per approved catalog ID.
- Source-inconsistent active records are downgraded to scrubbed unknown history,
  including later-source non-reactivation. Add Camp recipe creation now validates
  its backing craft source and `lines` array before mutation. Compatibility
  categories are exercised through real Add Camp and Upgrade assessment paths,
  with report-level `AssessmentFailed` coverage.
- Background provenance safety remains automatic, while the full compatibility
  window is opened only through **Tools → Check Compatibility**. The command
  reassesses current in-memory project data and replaces prior results.
- Normal presentation hides compatible rows, shows only affected features and
  warnings, and provides a concise all-clear state. The window uses the same
  owned, taskbar-visible modeless pattern as the working utility windows.
- Project Owner acceptance confirmed no automatic popup, explicit current-
  project checking, issue-only/all-clear presentation, normal minimize/restore,
  close/reopen/re-run behavior, and Restore Previous Values across multiple
  tested gameplay features. General gameplay tools, modifications, and profiles
  remained stable.

## Excluded

- Golden CDB, Steam APIs, heuristic target migration, mandatory migration UI,
  package writing, and project reconstruction rollback.

---

# Generic Wartales Language Data Setup

**Status:** Complete, Renewed Focused Engineering Review passed with
non-blocking notes, and Project Owner Interactive Acceptance passed

## Added

- One-time selection of any valid Wartales export localization XML file, with
  embedded `lang` metadata as the language authority.
- One canonical durable copy at
  `<Documents>\Wartales Editor\Language Data\export.xml`.
- Automatic nonfatal startup loading, raw-ID fallback, a Detailed Editor setup
  state, and **Tools → Language Data...** status/replacement UI.
- Failure-atomic source validation, stored-candidate revalidation, replacement,
  and permanent language-data regression coverage.

## Changed

- Localization is application-level presentation state rather than part of CDB
  project promotion.
- Runtime setup/replacement refreshes Detailed Editor selection/search and open
  Change Summary presentation.
- Setup and replacement reuse the existing validated Wartales installation
  context, preselect valid generic `export_*.xml` candidates, and preserve the
  manual picker when detection or discovery is unavailable.
- Available Language Data now uses the existing green success treatment;
  missing and invalid states retain their non-success presentation.

## Fixed

- Late replacement failure now republishes prior state only after exact
  fingerprint-verified canonical restoration. Missing or unusable rollback data
  produces cleared invalid state instead of disk/memory incoherence.
- Rollback cleanup failure is surfaced distinctly while preserving a coherent
  successful replacement, and later transactions safely remove stale ownership.
- Setup, restored-replacement, unrecoverable-recovery, and cleanup messages now
  describe their actual outcomes.
- Permanent regressions now force the production post-promotion path and cover
  fresh-service reload plus runtime search, context, and Change Summary refresh.
- Project Owner testing passed setup, canonical persistence, localized names,
  restart behavior, replacement, detected source selection, and the green
  success treatment. Renewed acceptance concluded: “Pass. Both work well.”

## Excluded

- `texts_*.xml`, full application localization, multiple stored languages,
  QuickBMS coupling, and all project/gameplay mutation state.

---

# QuickBMS Integration Milestone 1 — Safe CDB Import

**Status:** Complete and accepted; Renewed Focused Engineering Review passed
with non-blocking notes and Project Owner Interactive Acceptance passed

## Added

- **Import From Wartales** in the File menu and welcome workspace.
- Separate owners for Wartales package validation, external QuickBMS toolchain
  validation, argument-safe process execution, unique temporary staging,
  SHA-256 fingerprinting, and extraction/import orchestration.
- Direct invocation of external `quickbms.exe` and the Shiro Games PAK script
  with absolute script, `res.pak`, and staging paths.
- Durable promotion of the validated CDB to
  `<Wartales installation>\Extracted\data.cdb`, providing the imported project
  with a stable file identity for existing adjacent state persistence.
- Permanent fake-runner coverage for validation, start/timeout/exit failures,
  missing/ambiguous/invalid output, source identity drift, staging freshness and
  cleanup, production loading, and failure-safe promotion.

## Safety

- The milestone exposes no write or reimport flags and never uses batch files.
- Every attempt starts in an empty unique temporary directory; output is never
  reused from an earlier run.
- Project promotion occurs only after QuickBMS succeeds, one non-empty
  `data.cdb` is found, its hash is recorded, and `JsonDataService.LoadProject`
  constructs a non-empty project.
- Existing unsaved-project protection remains authoritative.
- Existing `Extracted\data.cdb` requires player confirmation before extraction;
  the service independently rejects an unapproved replacement. Promotion uses
  a fingerprint-verified `data.cdb.importing` file and publishes no project on
  failure.
- QuickBMS is launched suspended, assigned to an editor-owned Windows Job
  Object, and resumed only after containment. Descendants inherit the job.
  Normal completion, timeout, and cancellation require a bounded query to reach
  zero active job processes. Unproven termination blocks post-hashing,
  promotion, and cleanup, retaining staging for safety.
- Staging root/session components reject reparse points. Cleanup validates the
  exact workspace and refuses recursive deletion when any reparse entry exists.
- CDB discovery now traverses directories explicitly, skips junctions, rejects
  reparse candidates, and independently verifies final containment.
- Project-derived reference state is prepared without live mutation before
  shared Open/Import project publication. Application language data is loaded
  independently at startup, so unavailable localization cannot block a valid
  project.

## Verified

- The exact supplied external QuickBMS tool and script extracted the exact live
  Wartales `res.pak` with exit code 0.
- Extracted `data.cdb`: 6,691,681 bytes, SHA-256
  `29BE149FD1AD68D849FA498F671A8E71868117EB3AA15B25643D86C647E76576`,
  loaded through the production path as 395 sheets.
- Live `res.pak`: 791,334,661 bytes and SHA-256
  `665BAF4E4240D8822178D634D8A8CD830B961781D77B1687B9CF24052D95CAC9`
  both before and after extraction.
- Production-path regressions prove timeout and cancellation remove a harmless
  parent/child/grandchild Job Object tree and that root exit is not mistaken for
  complete-tree exit. Staging-root junctions are rejected, replaced
  session cleanup is refused safely, recursive discovery does not follow a
  junction, empty CDB output is rejected, and executable-adjacent localization
  files have no project-promotion authority. File-symlink creation is unavailable under the current
  Windows test privilege; production rejection remains implemented.
- Durable-promotion regressions cover first import, unapproved replacement,
  approved replacement, failed promotion, importing-artifact cleanup, durable
  project identity, and `.wtstate` creation beside the promoted CDB.
- The post-correction real extraction promoted the same 395-sheet,
  6,691,681-byte CDB to `Extracted\data.cdb`; a stateful Starting Resources
  operation and `.wtstate` creation succeeded, staging cleaned, and `res.pak`
  retained its exact size and SHA-256.
- Project Owner interactive testing confirmed successful Import From Wartales,
  the durable `Wartales\Extracted\data.cdb`, its adjacent `data.cdb.wtstate`, and
  Starting Resources operation without the former missing-file-path failure.
- Reimport/install, package backup/replacement, Update Survival, Golden CDB,
  and QuickBMS/script redistribution remain explicitly deferred.

---

# Restore Previous Values

**Status:** Implemented, repository-verified, Renewed Focused Engineering
Review passed with non-blocking notes, and Project Owner accepted; final commit
and push pending

## Changed

- Standardized all gameplay reset controls as **Restore Previous Values**.
- Compatible Gameplay Operation State now provides the single captured
  pre-tool authority for the 17 shared preset tools, Party Economy, Random
  Trait Exclusions, Overworld Movement Speed, and Rain Frequency.
- Restore controls remain unavailable when compatible historical state is
  absent, so current configured values are never fabricated as prior history.
- Overworld Movement restoration now returns to captured walk/run values rather
  than fixed 6/11. Vanilla remains an ordinary selectable movement preset.
- Rain restoration now returns to captured regional values rather than fixed
  Vanilla values. The existing regional presets remain selectable.
- Random Trait Exclusions continues to restore exact `true`, `false`, and
  absent `done` baselines.
- Random Trait Exclusions now resolves that baseline from current compatible
  Gameplay Operation State at click time. A stale modeless dialog cannot issue
  Apply after Undo removes its history, and compatible profile/state replacement
  supersedes cached dialog-open baseline data.
- Every gameplay Restore Previous Values button now applies immediately through
  its existing validated operation path. Party Economy fields update and apply
  in the same click; Apply remains available for later manual edits.
- Random Trait Exclusions effective accounting now compares exact current
  `done` presence/value with the captured baseline. Baseline-existing exclusions
  are not changes, and exact restoration removes the synthetic summary row.
- Detailed Editor Reset Property and `PropertyModel.IsModified` are unchanged.

## Verified

- Non-catalog starting values, multiple preset changes, missing-state safety,
  `.wtstate` reload, profile transport, exact trait restoration, Movement and
  Rain captured baselines, and atomic Undo/Redo have permanent compatibility
  coverage.
- Permanent Random Trait Exclusions lifecycle coverage includes open-dialog
  Undo rejection, authoritative state replacement, normal exact restoration,
  direct missing-history safety, and Restore Undo/Redo.
- Consistency coverage includes RTE `0 → 3 → 0 → 3 → 0` accounting across
  Apply, Restore, Undo, and Redo, plus immediate Party Economy restoration for
  Volunteer, Valour, and Carrying Capacity.
- Sequential main/test builds complete with zero warnings and zero errors, the
  focused Restore Previous Values suite passes, and all 25 Class A compatibility
  groups pass.
- Renewed Focused Engineering Review returned **PASS WITH NON-BLOCKING NOTES**.
  The sole note is that automated coverage exercises production
  ViewModel/service/operation paths instead of synthesizing WPF button clicks.
- Project Owner interactive evidence covered multiple gameplay features,
  immediate Positive Random Traits restoration, Party Economy consistency, and
  exact Random Trait Exclusions restoration/accounting. After the final
  corrections, the Project Owner explicitly returned **PASS**.

---

# Random Trait Exclusions

**Status:** Accepted; renewed focused Engineering Review and Project Owner
interactive acceptance passed, with positive runtime evidence

## Added

- A Party Gameplay Tool with searchable Positive and Negative trait checklists,
  Select All, Clear All, Restore Previous Values, Apply, and shared feedback.
- Dynamic candidate discovery for compatible Starting/Recruitment traits; no
  trait identifiers or candidate counts are hard-coded.
- Feature-specific operation state and validation for stable trait ownership,
  exact Boolean/absent baselines, fingerprints, and update compatibility.
- Candidate preflight now requires an explicit nonblank source `id`, exact
  source/model identity, and connected `done` models before mutation begins.

## Changed

- Unchecked traits receive `done=false`; checked traits preserve an eligible
  absent baseline or use `done=true` when explicitly enabling a pre-disabled
  trait. Existing units are unchanged.
- Restore Previous Values uses the approved property-removal primitive to recover
  an originally absent `done` leaf exactly.
- Snapshot/profile application restores deterministic exclusion state before
  ordinary property matching so absent-baseline leaves can be recreated safely
  without a new profile format.
- Review Changes shows a player-facing operation outcome when an exact
  absent-baseline restore has no attached modified property row.
- Review Changes now derives that fallback from the persisted/current Random
  Trait Exclusions state specifically, so unrelated gameplay-state changes do
  not create or count an exclusions outcome.
- Operation validation now requires exact set equality between the requested
  allowed traits, recorded allowed traits, and resolved owned candidates.

## Verified

- Dynamic grouping, true/false/absent baselines, mixed selections, Select All,
  Clear All, exact restore, idempotence, validation rollback, Undo/Redo,
  state persistence, snapshot/profile replay, update expansion, and independent
  Positive Random Traits operation state.
- Disconnected-target atomic preflight, stable source identity, cross-operation
  Review Changes attribution, requested/result mismatch rollback, and complete
  same-file Update Profile replay are covered by permanent smoke tests.
- Direct Snapshot Preview remains read-only and may conservatively report a
  missing absent-baseline leaf; profile application materializes exclusion state
  before matching and is unaffected.
- During an observed Project Owner session lasting more than one hour, no
  recruit received a trait disabled by Random Trait Exclusions. This is positive
  runtime behavioral evidence, not statistical proof that excluded traits can
  never occur.
- Renewed focused Engineering Review passed with non-blocking notes. Project
  Owner interactive acceptance confirmed that the dialog opened, the feature
  applied, exactly five traits were unchecked, and exactly five changes were
  reported.

---

# Property Removal Mutation Primitive

**Status:** Focused Engineering Review passed; approved for feature use

## Added

- `ProjectMutationService.RemovePropertyByPath` for strict removal of a known
  project-model property and its connected source `JProperty`.
- A dedicated removed-property rollback record preserving exact object identity,
  parent, source/model ordering, effective path, and prior modification state.

## Changed

- Project mutation results, transaction rollback/replay, and operation history
  now compose known-property removals with existing create and modify mutations.
- The removal API now explicitly rejects object-valued properties before any
  mutation or rollback record is created.

## Verified

- Nested removal, missing-target failure, exact rollback, repeated Undo / Redo,
  source/model ordering, created-property symmetry, empty-parent preservation,
  and forced validator rollback of modify/create/remove mutations.
- Public-factory object rejection, ambiguous/disconnected targets,
  same-property modify/remove, create/modify/remove, multiple-removal index
  drift, and exact first/last restoration.
- The capability remains limited to known properties. Random Trait Exclusions
  now uses it only for scalar `done` restoration; generalized deletion remains
  unsupported.

---

# Final Feature Batch

**Status:** Accepted and reconciled; implementation, automated verification,
Engineering Review, and Project Owner acceptance complete

## Added

- Lectern Knowledge Gain under Progression, using 1×, 2×, 3×, and 5×
  captured-baseline presets for Lectern rest Knowledge only.
- Positive Random Traits under Party. Positive Only selects the current
  two-positive branch with `0 / 1 / 0` for future eligible procedural units;
  existing units are unchanged.
- Update Profile in Profile Manager for explicitly selected managed profiles.

## Changed

- Updating a profile now reconciles prior records with the current intended
  project by stable effective path, preserving baseline-accepted content while
  replacing changed targets and removing profile-relative restorations.
- Updated candidates are staged and reloaded, then checked by an independent
  invariant validator that does not invoke profile construction or high-level
  reconciliation. It verifies retained history, current delta, reversions,
  canonical uniqueness, metadata, refreshed Gameplay Operation State, and
  additive requests before atomic same-path replacement. This preserves prior
  profile history without requiring a separate pristine CDB. Failed validation
  leaves the prior managed profile unchanged.
- Snapshot properties now record historical structural presence independently
  from their JSON value. Proven absent-to-created-to-absent restoration remains
  supported, historically present `null` deletion is rejected, and ambiguous
  legacy-null deletion fails safely.
- Update Profile observationally revalidates Gameplay Operation State against
  current live targets before capture, preventing stale cached compatibility
  from entering a profile.
- Main/project, Review Changes, profile, and apply-result accounting now share
  effective-leaf semantics for updated and created live properties. Supported
  removal mutations remain counted by apply feedback; arbitrary clean-baseline
  deletion is not a profile capability. Additive output remains deterministic
  and overlap-aware.
- Review Changes resolves nested properties by `EffectivePropertyPath`, so
  duplicate `height` and `width` leaves remain distinct.
- Legacy pathless properties use one shared resolver. Unique matches upgrade to
  canonical paths; ambiguity is surfaced and blocks profile replacement.
- Additive profile filtering now uses canonical effective paths, preserving
  unrelated same-leaf properties under different nested paths.
- Pathless legacy `flags` records are no longer guessed to overlap canonical
  Upgrade All Equipment `props.flags` output.

## Verified

- Preset scaling, exact Vanilla restoration, malformed-target rejection,
  idempotence, Undo/Redo, operation state, snapshots, and profile round trips.
- Managed profile selection/path safety, metadata preservation, baseline-
  accepted reconciliation, current-format rewrite, semantic candidate replay,
  failed-serialization/validation preservation, and no-change update safety.
- Mixed profile apply/save/close-reload/update/replay covers ordinary and nested changes,
  created and removed absent-baseline properties, Random Trait Exclusions,
  Campfire Expansion, Add Camp Facilities, Upgrade All Equipment, duplicate
  nested paths, additive overlap, and unified effective counts.
- Candidate validation independently rejects incomplete and duplicate profiles
  plus injected stale gameplay state after reopening the already modified source
  CDB. Historical absence, present-null deletion, present-null value reversion,
  and legacy-null ambiguity have dedicated coverage. Count regressions cover five added changes,
  same-target replacement, one deliberate reversion, and no-new-change updates;
  the no-validation replacement overload fails before touching managed bytes.
- The Renewed Focused Engineering Review passed with non-blocking notes. Its
  sequential main/test builds completed with zero warnings and zero errors, all
  22 Class A compatibility groups passed, `git diff --check` passed, and no
  profile-update temporary artifacts remained.
- Project Owner testing applied a known non-damaged profile, saved and reloaded
  its CDB, made further changes, and updated the same profile successfully. Its
  effective count increased from 633 to 636 and validation reported no issues.
- Review Changes displayed the correct result; the prior discrepancy involving
  the six distinct Firecamp, FirecampT2, and FirecampT3
  `tool.height`/`tool.width` paths was no longer present.
- The full intended 645-effective-change configuration was applied, saved as a
  new profile, and used to launch and play Wartales for more than one hour
  without obvious instability attributable to the editor configuration.
- Lectern Knowledge Gain and Positive Random Traits were each tested, confirmed
  working, and accepted by the Project Owner. No additional test details are
  inferred from those acceptance statements.
- Random Trait Exclusions passed renewed focused Engineering Review and Project
  Owner interactive acceptance. Its later runtime evidence remains positive.
- The previously damaged approximately 554-change `All Mods.wtprofile` is not
  considered repaired; the accepted 645-change configured state was saved as a
  new profile.

---

# Class A Gameplay Expansion

**Status:** Complete; focused compatibility corrections, Resource
Replenishment, UX consistency, Engineering Review, Project Owner runtime smoke,
and final reconciliation verification passed

## Added

- Preset Gameplay Tools for Delicious Meals, Forging Assistance, Mining &
  Woodcutting, Fishing, Lockpicking, Nine Puzzle Assistance, Run Stamina
  Recovery, Battle Camera Zoom, Campfire Expansion, Cooking Pot Food Reduction,
  Workshop Materials, Vendor Refresh, Ruby & Sapphire Value, and Time Between
  Rests.
- A Professions dashboard category and the approved Party, World, and Camp &
  Equipment dashboard entries.
- Feature-specific validator dispatch for every new operation.
- Resource Replenishment presets that scale the captured Slow, Normal, and Fast
  refill categories by 1×, 2×, 3×, or 5× without changing the Extreme factor.

## Changed

- Valour Points now includes Vanilla and Increased Tent Valour tier presets.
- Carrying Capacity now includes Vanilla and Increased Hitching Post base and
  Draught Pony tier bonuses.
- Legacy two-target Valour and Carrying operation states remain valid and are
  upgraded to the expanded state shape on the next explicit safe Apply. Current
  Tent and Hitching Post values are resolved from the project rather than guessed.
- Vanilla preset restoration uses each operation's exact captured baseline.
  Mining and merchant rates scale proportionally from that baseline, while
  Battle Camera preserves the captured minimum distance.
- New snapshots record effective nested property paths. Older snapshots without
  paths retain their original leaf-name matching behavior.
- Gameplay Tool dialogs show in-dialog Applied successfully or Already applied
  feedback, and feature windows explicitly restore their owner after closing.
- The shared restore action applies the captured baseline through the operation
  pipeline, restoring the exact captured baseline with normal operation state,
  Review Changes, and single-action Undo/Redo behavior.
- Starting Resources and Party Economy clear stale success/no-op feedback when
  their current input becomes invalid.
- Starting Resources, Movement Speed, and Battle Camera Zoom show the approved
  non-blocking display/visual notes.

## Verified

- Zero-warning, zero-error build.
- Clean-CDB apply, validation, forced-failure rollback, exact Undo/Redo,
  idempotence, missing/wrong/duplicate target handling, save/reload state
  persistence, profile serialization, mixed profile replay, and effective
  change counting.
- Repository-backed focused coverage for differing-baseline Vanilla restoration,
  Mining and merchant proportional scaling, Battle Camera baseline drift,
  supported and custom legacy Valour/Carrying upgrades, snapshot full-path and
  legacy matching, all preset catalog entries, and representative malformed
  tier/discriminator/state cases.
- Reset coverage for a scalar preset, baseline-scaled preset, and multi-target
  Campfire preset, including recorded Vanilla state and exact Undo/Redo.
- Symmetric Campfire malformed-target coverage for missing and wrong-type
  `tool.toolCapacity` as well as `tool.capacity`.
- Resource Replenishment baseline capture, proportional outputs, exact
  restoration, no compounding, malformed baselines, atomic rollback, Undo/Redo,
  state persistence, snapshot serialization, profile replay, and preservation of
  unrelated values and `GatherRefillFactorExtreme`.
- A fresh-install, fresh-extraction full-mod gameplay smoke launched, started a
  new game, reached play, saved, exited, relaunched, and loaded the save. The
  earlier freeze is non-reproducible after clean reinstall and fresh extraction;
  its cause remains unknown.

## Non-Blocking Notes

- Campfire implementation/reference equivalence is established. Direct in-game
  Tier 2/Tier 3 assignment-count verification remains pending; Tier 1
  intentionally remains at capacity 4.
- Resource Replenishment is not claimed as exhaustively timed across every land,
  fishing, sea, and special renewable category.
- `stacked content: 2` remains a non-blocking observation from one camp-item
  creation session; its origin was not located by the narrow string search.

---

# Version 0.9.1 - World Convenience

**Status:** Complete and verified

## Added

- Rain Frequency as a direct Gameplay Tools item after Overworld
  Movement Speed.
- Vanilla, Less Rain, Rare Rain, and No Rain presets for ordinary
  regional rain.
- Exact preset detection, Custom and unavailable states, persisted
  gameplay-operation state, and a modeless player-facing dialog.

## Changed

- The twelve approved `region` entries can now update only
  `props.meteo.rainDaysPerMonth` as one atomic operation.
- Each preset is calculated from the entry's verified Vanilla baseline
  of 4 or 6; Rare Rain preserves 1.5 for baseline-6 regions.

## Verified

- Build verification, runtime testing, validation, Save / Reload,
  Undo / Redo, Change Summary, Profiles, Snapshots, and multiple in-game
  verification passes.

## Investigated

- Resource Respawn Speed confirmed shared Slow, Normal, and Fast gather
  refill constants.
- Implementation was deferred pending future runtime validation because
  the shared refill architecture may affect excluded gathering systems.
- No Resource Respawn gameplay feature was added.

---

# Version 0.9.1 - Additive Profile Restoration Repair

**Status:** Development; Visual Studio and in-game verification pending

## Added

- Version 2 Mod Profile operation requests with stable identifiers for
  Add Camp Facilities and Upgrade All Equipment.
- Safe request validation, duplicate rejection, deterministic operation
  resolution, and player-facing gameplay-tool result counts.
- Profile-level mutation aggregation for staged rollback and one-action
  Undo/Redo.

## Changed

- New profiles detect valid applied additive operations from project
  content and filter only deterministic operation-owned snapshot records.
- Profile Manager now displays one effective Changes count representing
  ordinary and additive project modifications without exposing their
  internal storage mechanisms.
- Profile application replays Add Camp Facilities and Upgrade All
  Equipment before applying ordinary snapshot properties.
- Version 1 profiles remain loadable without inferred operation requests.
- Direct Snapshot import remains property-target based.
- Overworld Movement Speed now appears directly in Gameplay Tools without
  a World Convenience submenu.

## Fixed

- Clean-project profiles can recreate camp tool structures, Workshop
  recipes, and missing equipment flags through the verified Project
  Operation pipeline.
- Profile apply now refreshes property tracking after structural replay,
  so newly created equipment and camp PropertyModels appear in the main
  modified count and Change Summary.
- Profile apply now reports one player-facing Changes result instead of
  exposing gameplay-tool and snapshot-property categories.
- Corrected prior documentation wording that did not distinguish profile
  capture coverage from verified clean-project additive restoration.

## Verified

- Zero-warning, zero-error build.
- Disposable model-level harness covering serialization, detection,
  filtering, ordered replay, idempotence, rollback, and Undo/Redo.

## Pending

- Full Profile Manager and result-dialog verification in Visual Studio.
- Save/Reload, Save As, reopening, and legacy-profile UI verification.
- In-game Add Camp Facilities, Upgrade All Equipment, and Overworld
  Movement Speed verification.

---

# Version 0.8.1 - Operation Framework & Verified Content Creation

**Released:** 2026-07-18

## Added

### Project Operation Architecture

-   ProjectOperationService
-   IProjectOperation abstraction
-   ProjectOperationResult
-   Operation execution pipeline
-   UI-facing operation orchestration

### Project Mutation Layer

-   ProjectMutationService enhancements
-   Mutation journaling
-   Rollback record models
-   ProjectMutationResult enhancements
-   Structural creation tracking

### Transaction Framework

-   ProjectOperationTransactionService
-   Automatic rollback on failed operation validation
-   Mutation-based rollback
-   Entry rollback
-   Property rollback
-   Updated-property rollback

### Operation Validation

-   Operation validator provider
-   Operation-specific validation architecture
-   AddCampFacilitiesOperationValidator
-   Separation of generic validation from operation validation

### Content Creation

-   AddCampFacilitiesOperation
-   Integration of ProjectOperationService into the application workflow
-   First reusable content creation operation

## Changed

-   Content creation now executes exclusively through the Project
    Operation pipeline.
-   Rollback no longer depends on rebuilding the project model.
-   Generic token-type validation now distinguishes structurally created
    properties from modified existing properties.
-   Validation architecture remains generic while operation-specific
    rules verify newly created content.
-   MainViewModel now executes operations through
    ProjectOperationService instead of directly invoking content
    creation services.

## Fixed

-   Corrected rollback behavior for created entries, created properties,
    and modified properties.
-   Corrected validation handling for structurally added properties.
-   Eliminated the remaining parallel execution path for Add Camp
    Facilities.
- Recovered ContentCreationService after merge corruption.
- Added reusable object-valued mutation support to ProjectMutationService.
- Updated Add Camp Facilities to use object mutation infrastructure.
- Updated Add Camp Facilities validation for nested property architecture.
- Corrected object mutation handling for props, tool, and icon containers.

## Verified

Successfully verified:

-   Transaction rollback after forced validation failure.
-   Successful operation commit.
-   Save validation after structural content creation.
-   Successful serialization.
-   Successful loading of modified data by Wartales.
-   In-game unlocking of camp recipes.
-   Successful construction and use of the Anvil.
-   Correct unlocking of the Blacksmith profession.
-   End-to-end operation pipeline from editor to gameplay.
-   Successful builds throughout implementation.
- Object-valued mutation infrastructure.
- Nested object mutation rollback.
- Nested object mutation validation.
- Add Camp Facilities idempotence.
- Atomic Undo / Redo after object mutations.
- Save / Reload after object mutations.
- Upgrade All Equipment regression testing.
- Extended in-game verification of Add Camp Facilities.
- Extended in-game verification of Upgrade All Equipment.
- Extended in-game verification of weather modifications.

## Notes

This milestone completed the transition from reconstruction-based structural editing to mutation-based transactional content creation.

During final integration, the object mutation layer was extended to support nested JSON objects while preserving rollback, validation, and atomic operation history.

Following recovery of the ContentCreationService and operation validator, all major content creation features were revalidated through extended in-game testing, confirming stable operation of Add Camp Facilities, Upgrade All Equipment, and gameplay weather modifications.

------------------------------------------------------------------------

# Version 0.8.0 - Validation Framework (Pass 1)

**Released:** 2026-07-17

## Added

### Validation Architecture

- Validation service layer
- Validation workflow orchestration
- Validation pipeline
- Validation rule infrastructure
- Validation issue model
- Validation result model
- Validation severity model
- Validation category model
- Extensible validation rule architecture

### Validation Rules

- Read-only property validation
- Property definition validation
- Numeric range validation
- Reference value validation
- Safe gameplay validation
- Validation based on the currently loaded project

### User Interface

- Validate Project command
- Validation Results window
- Validation summary header
- Severity counters
- Severity filtering
- Empty validation success view
- Validation navigation
- Copy Results
- Re-run Validation

## Changed

- Validation now executes through a reusable workflow architecture rather than being embedded in save operations.
- Save validation now reuses the same validation pipeline used by manual validation.
- Validation Results operates as a single-instance modeless utility window.
- Validation navigation integrates directly with the existing editor selection workflow.
- Validation windows now behave consistently with the Profile Manager and Change Summary architecture.

## Fixed

- Corrected validation window lifecycle management.
- Corrected validation window refresh behavior.
- Corrected duplicate validation window creation.
- Improved independent focus behavior for modeless utility windows.
- Corrected a WPF validation tooltip binding warning that could appear during application shutdown.
- Added a reusable converter for safely displaying the first property validation error.

## Verified

Successfully verified:

- Manual validation
- Save validation
- Validation rule execution
- Validation severity reporting
- Validation filtering
- Validation navigation
- Validation refresh
- Validation clipboard export
- Single-instance window behavior
- Independent modeless window focus
- Undo compatibility
- Redo compatibility
- Profile compatibility
- Snapshot compatibility
- Change Summary compatibility
- Successful builds throughout implementation

# Version 0.7.0 - Complete Profile Manager

**Released:** 2026-07-16

## Added

### Profile Management

- Create Profile
- Rename Profile
- Duplicate Profile
- Apply Profile
- Import Profile
- Export Profile
- Delete Profile
- Complete Profile Manager workflow

### Profile Architecture

- ModProfileModel
- ModProfileMetadataModel
- ModProfileFormat
- ModProfileService
- ModProfileSerializationService
- ModProfileWorkflowService
- ModProfileLibraryService
- ModProfileLibraryPathService
- ModProfileSummaryModel
- Reusable Profile Details dialog
- Unified profile request model

### User Interface

- Profile Manager window
- Profile Browser
- Profile metadata display
- Profile toolbar
- Profile Details dialog
- Profile creation workflow
- Profile rename workflow
- Profile duplication workflow

## Changed

- Mod Profiles now compose the existing Snapshot workflow instead of introducing a parallel implementation.
- Profile application reuses the existing Snapshot Match, Preview, and Apply pipeline.
- Profile creation captures the current modification state using the existing editing infrastructure.
- Profile statistics simplified to display **Modified Properties**.
- Profile Manager usability improved with additional UI polish.
- Improved startup sizing for smaller displays.
- Improved Profile Details dialog layout.
- Improved profile selection behavior after profile operations.

## Fixed

- Corrected Profile Manager selection synchronization after profile operations.
- Corrected Profile Manager selection visibility.
- Corrected Profile Details dialog sizing on smaller displays.
- Improved main window startup sizing across different monitor resolutions.

## Verified

Successfully verified:

- Create Profile
- Rename Profile
- Duplicate Profile
- Export Profile
- Delete Profile
- Import Profile
- Apply Profile
- Undo compatibility
- Redo compatibility
- Change Summary integration
- Modification tracking
- Snapshot application
- Successful builds throughout implementation

---

# Version 0.6.0 - Snapshot UI – Pass 1

**Released:** 2026-07-13

## Added

### Snapshot User Interface

- Export Snapshot
- Preview Snapshot
- Import Snapshot
- Complete end-to-end snapshot workflow
- Snapshot workflow success summaries
- Snapshot preview dialog
- Snapshot import dialog
- Snapshot export dialog

### Workflow Integration

- Snapshot UI connected to `ModificationSnapshotWorkflowService`
- Automatic modification tracking refresh after snapshot import
- Automatic Change Summary refresh after snapshot import
- Seamless integration with the existing editing workflow

## Changed

- Completed the first fully functional snapshot user workflow.
- Snapshot functionality now operates entirely through the reusable workflow infrastructure.
- Snapshot import behaves identically to manual editing, preserving existing application behavior.
- Existing editing architecture reused without introducing duplicate modification tracking.

## Verified

Successfully verified:

- Export Snapshot
- Preview Snapshot
- Import Snapshot
- Modification tracking refresh
- Change Summary refresh
- Undo compatibility
- Redo compatibility
- Save compatibility
- Live Wartales testing
- Successful builds throughout implementation

---

# Version 0.5.1 - Snapshot Workflow Foundation

**Released:** 2026-07-12

## Added

### Snapshot Architecture

- ModificationSnapshotWorkflowService
- Snapshot workflow orchestration
- Snapshot export workflow
- Snapshot preview workflow
- Snapshot import workflow
- Snapshot workflow result models
- Snapshot import result model
- Snapshot export result model

### Dialog Infrastructure

- IFileDialogService
- IMessageDialogService
- WpfFileDialogService
- WpfMessageDialogService

### User Interface

- Standard application menu bar
- File menu
- Edit menu
- View menu
- Tools menu
- Help menu
- Snapshot menu foundation
- Validation menu placeholder
- Developer Tools placeholder

### Architecture

- Constructor injection for MainViewModel services
- Separation of workflow orchestration from UI
- Separation of file dialogs from ViewModel logic
- Separation of message dialogs from ViewModel logic

## Changed

- MainViewModel no longer creates WPF file dialogs directly.
- MainViewModel now receives required services through constructor injection.
- MainWindow now composes application services during ViewModel construction.
- Open and Save operations now use the dialog abstraction layer.
- Editor architecture is prepared for Snapshot UI integration.

## Fixed

- Corrected Change Summary Navigate button command-state updates after introducing explicit command notifications.
- Preserved existing editor behavior after constructor injection refactor.
- Preserved Undo/Redo, Reset Property, Change Summary, and search functionality following dialog abstraction.

## Verified

Successfully verified:

- Constructor injection
- File dialog abstraction
- Message dialog abstraction
- Menu bar integration
- File menu commands
- Edit menu commands
- View menu commands
- Ctrl+O
- Ctrl+S
- Undo
- Redo
- Reset Property
- Change Summary
- Navigate button
- Double-click navigation
- Successful build after refactoring

---

# Version 0.5.0 - Change Summary

**Released:** 2026-07-12

*(No changes to this section.)*

---

# Version 0.4.0 - Safe Editing & Undo/Redo

**Released:** 2026-07-12

*(No changes to this section.)*

---

# Version 0.2.0 - Find Anything

**Released:** 2026-07-11

*(No changes to this section.)*

---

# Version 0.1.0 - First Functional Editor

**Released:** 2026-07-11

*(No changes to this section.)*

---

# Future Releases

Future releases will continue documenting:

- New features
- Architectural improvements
- User interface enhancements
- Bug fixes
- Performance improvements
- Documentation updates
# Version 0.10.0 — UI Polish

- Standardized player-facing terminology around Gameplay Tools, Detailed
  Editor, Profiles, Review Changes, and Check Project.
- Made routine profile maintenance and refresh-backed gameplay successes
  nonblocking while retaining blocking safety and compatibility results.
- Reworked standard errors to lead with player outcomes and retain technical
  details secondarily.
- Added an About experience backed by the authoritative assembly version.
- Kept Snapshot infrastructure internal and unchanged.
- Deferred Search Scope Semantics Correction to a separate pre-1.0 task.
