# Public Release Preparation

**Published reference release:** Wartales Editor 1.1.0

**Status:** VERSION 1.1.0 PUBLISHED — RELEASE LIFECYCLE COMPLETE; Version 1.1.1
local candidate prepared and technically validated

**Version 1.1.0 scope:** Accepted post-1.0.0 work is committed and includes
Paths Gameplay Tools, stronger Profile Update Survival, Starting Resources
Hemp and grouped controls, additional approved presets, remembered QuickBMS
folder selection, Party Economy Custom preservation, Quick Help, and accepted
usability corrections. The GitHub release was published September 8, 2026.

**Phase 1:** COMPLETE — legal/public metadata, repository hygiene, public
onboarding, version reconciliation, and Git-history privacy sanitization are
complete locally and remotely.

**History sanitization:** COMPLETE locally and remotely.

**Phase 2:** COMPLETE — the publish/package process, release manifest, complete
public User Manual, checksum/scan procedures, clean-machine plan, issue-report
guidance, and later publication sequence are defined and repository-verified.

**Phase 2 closeout:** User Manual content review: PASS. User Manual PDF Project
Owner visual review: PASS. README content review: PASS. README PDF Project Owner
visual review: PASS.

**Version 1.1.0 candidate:** PREPARED AND LOCALLY VERIFIED. The fresh
self-contained, untrimmed, multi-file `win-x64` package contains 405 public
files. It excludes PDBs, QuickBMS, the Shiro script, game/user data, source, and
tests. Staging and clean extraction are byte-for-byte equivalent, the extracted
application started as Version 1.1.0 and closed normally, and Windows Defender
reported no detections for both the extracted package and ZIP. The immutable ZIP
SHA-256 is
`401D5247667A65E93911D5836BD018B14716F960C25E73C6F33F2E1C7003CCCC`.

**Version 1.1.0 game authority:** CONFIRMED. The Project Owner directly verified
Steam App ID `1527950`, Wartales BuildID `25172421`, from installed Steam content
updated September 7, 2026 at 5:39 PM. The prior BuildID confirmation blocker is
closed, and publication is complete.

**Quick Help V1:** CLOSED. Investigation, Design / Architecture,
Implementation, Focused Engineering Review (**PASS**), Project Owner
Interactive Acceptance (**PASS**), footer correction, brief visual re-test
(**PASS**), reconciliation, and the final commit/push checkpoint are complete.

**TyTech Green accent:** REJECTED AND ROLLED BACK. Investigation and
implementation completed, and Engineering Review passed after the required
corrections. Project Owner visual acceptance then failed, the Project Owner
rejected the color update, and the implementation was fully restored to the
pre-experiment UI. Production and tests match their accepted baseline, the
rejected experiment lifecycle is closed, and no further color work is planned
before Version 1.0.0.

**Wartales Modding Community credit:** APPROVED and retained in the authoritative
README and User Manual Markdown as a goodwill acknowledgement without a legal-
attribution role. The final `README.pdf` and `USER-GUIDE.pdf` were regenerated
from those authoritative sources and visually validated.

**Release-build path privacy correction:** ACCEPTED. The first Release Candidate
attempt was rejected before ZIP/checksum creation because the application DLL's
CodeView record contained the local build-machine PDB path. Release builds now
map `$(MSBuildProjectDirectory)` to `/_/`. Focused Engineering Review returned
**PASS** with no findings: the corrected application CodeView path and all 280
portable-PDB document paths use `/_/`, and the corrected publish output contains
no local repository, user-profile, Codex, or old-identity strings. The rejected
RC1 staging is retained only as evidence and must not be packaged.

**Release-build path privacy correction lifecycle:** CLOSED after review, commit,
and push.

**RC2:** GENERATED AND VALIDATED from
`4bd349834585075bf5a02bdce841e4b31d47a751`. Full repository regression, fresh
publish, path-privacy audit, package audit, Defender scans, extracted-package
smoke validation, and clean-machine validation passed. Its immutable SHA-256 is
`4607F2E1F33F17CCC4FA77ADACF07904B07D7622A72E0EE6AE3D3F976622F26E`.

**External version metadata:** CLOSED. Project Owner validation establishes
Wartales Steam BuildID `23361327` as the game build used for accepted
secondary-drive installation resolution, Import, QuickBMS prerequisite setup,
Language Data, full-mod loading, new-game startup, and successful gameplay.
The proven external toolchain is QuickBMS `0.12.0` (SHA-256
`FF812D38E22AEA0CC2CDC13C5C91CA34FAE4443AB12987002105BE6BAB3F4948`) with
`Shiro_Games_PAK_script.bms` v0.2 dated 10.03.2022 (SHA-256
`3FF096363BCDBAEEADBC67B66F91FFD5E0AB424006049B0194BCB0EB433C35B6`).
The public authorities and review PDFs now record this exact combination. RC2
remains immutable, rejected/superseded for publication, and must not be mutated,
relabeled, tagged, or published. It was not reused for later release work.

**Tag:** `v1.1.0`, targeting
`9db2841e9f74bef3eb1e07eddd82265a15b2a3ba`. **GitHub Release:** PUBLISHED at
`https://github.com/snoosnaa/WartalesEditor/releases/tag/v1.1.0` with the ZIP
and checksum assets independently downloaded and hash-verified. **Nexus:** NOT
PUBLISHED. **VirusTotal:** NOT SUBMITTED.

**Non-default Wartales installation support:** IMPLEMENTATION COMPLETE;
ENGINEERING REVIEW PASS; PROJECT OWNER RUNTIME ACCEPTANCE PASS. The shared
resolver is wired to Import, Export Back to Wartales, Golden CDB convenience
import, and Language Data automatic source discovery. Operation-level callers
own installation-resolution failure presentation, with deterministic single-
message coverage. Project Owner testing verified the secondary-drive Steam
installation, automatic Wartales discovery, Import, the QuickBMS prerequisite
flow, User Guide access, and Language Data. The QuickBMS-missing message now
points players to the User Guide for detailed setup instructions. The work is
closed: implemented, reviewed, Project Owner accepted, committed, and pushed.
RC2 remains superseded and was not reused for Version 1.1.1.

## Version 1.1.1 Local Release Candidate

Version 1.1.1 is selected for Profile Impact Manifest and stable historical
Profile Changes, expanded Random Trait Exclusions, and the accepted root
launcher plus `App\` layout. All three workstreams are implemented,
Engineering-reviewed, Project Owner accepted, documented, committed, and
pushed. Shared version/release metadata is reconciled, and a fresh local
candidate has passed repository, package, extraction, version, Defender, normal
non-admin, restricted/non-writable-location, local launcher, and Project Owner
Quick Help PDF-viewer validation. **1.1.1 RELEASE CANDIDATE VALIDATION — PASS.**
SmartScreen / real downloaded Mark-of-the-Web behavior remains unvalidated and
non-blocking until a public distribution context exists. No `v1.1.1` tag,
public release, or upload exists.

```text
WartalesEditor.exe
App\
    WartalesEditor.exe
    WartalesEditor.dll
    WartalesEditor.deps.json
    WartalesEditor.runtimeconfig.json
    [complete self-contained main application payload]
README.pdf
USER-GUIDE.pdf
LICENSE
THIRD-PARTY-NOTICES.txt
CHANGELOG.md
```

The root executable is a dedicated self-contained, trimmed, single-file
`win-x64` launcher. The real WPF application remains self-contained,
multi-file, untrimmed, and ReadyToRun-disabled under `App\`. The launcher uses
only the absolute `App\WartalesEditor.exe` path derived from its own base
directory, sets `App\` as the child working directory, forwards arguments,
waits, and propagates the child exit code. It performs no discovery, update,
repair, cleanup, elevation, or migration.

`Build\WartalesEditor.Version.props` is shared product-version authority for
both projects. The tracked `Scripts\Build-PortablePackage.ps1` flow publishes
the projects separately, constructs a fresh bounded staging tree below the
repository's exact `output\` authority, and invokes the package validator.
Creating local staging or an explicitly requested local validation ZIP does not
authorize tagging or publication.

## Release Authority

- Free and MIT licensed.
- Copyright © 2026 M. Tyler Spencer.
- Released by TyTech Games.
- Windows 11 x64 and Steam Wartales across detected Steam libraries, with manual
  installation-folder selection when needed.
- Steam App ID `1527950`, Wartales BuildID `25172421`, directly confirmed by the
  Project Owner from installed content updated September 7, 2026 at 5:39 PM.
- Self-contained `win-x64` portable ZIP with a single-file root launcher and an
  intact untrimmed, multi-file main application payload under `App\`.
- No installer, updater, main-application single-file conversion, or
  ReadyToRun.
- Unsigned V1 with a published SHA-256 checksum.
- QuickBMS and the Shiro Games PAK script remain external and user-supplied.
- GitHub Issues is for general reproducible defects, not a personal support
  desk or a promise of response, fix, custom mod, or release cadence.

## SDK and Build Requirements

The Phase 2 verification environment used:

```text
.NET SDK: 10.0.400
MSBuild:   18.9.6
Target:    net10.0-windows
RID:       win-x64
```

The repository has no `global.json`. Adding one is unnecessary for V1 because
the release procedure explicitly requires and records SDK `10.0.400` before
publishing. A future SDK change must be an intentional, tested release decision;
the release operator must not silently substitute another SDK for the final
candidate. No project metadata change is required.

Preflight commands:

```powershell
dotnet --version
dotnet --info
git status --short --branch
git rev-parse HEAD
git tag --list
```

## Current Tracked Package Build

After the source and PDF inputs are verified, use the tracked package builder:

```powershell
& .\Scripts\Build-PortablePackage.ps1
```

Optional input and output parameters are described by the script. Its output
must remain a proper descendant of repository `output\`; the script validates
the exact output authority, existing descendant components, reparse-point and
file substitutions, normalized containment, and required-input separation
before recursive recreation.

The script runs two explicit publishes: the main application remains
self-contained, multi-file, untrimmed `win-x64`; the launcher is
self-contained, trimmed, single-file `win-x64`; both disable ReadyToRun. It
copies the complete main payload, except approved exclusions such as PDBs, to
`staging\App\`, copies only the launcher executable to the staging root, copies
the five root documents, and validates the package.

`Scripts\Test-PortablePackageLayout.ps1` requires exact source/staged relative
path sets and compares every main-application file by SHA-256. It rejects
ambiguous, missing, extra, unreadable, and same-name altered content. The root
launcher must also SHA-256-match its authoritative launcher publish. Layout
validation requires one root executable, the expected real application and
runtime metadata under `App\`, culture/native payload structure, no root
runtime clutter, no PDBs, all five documents, and no wrapper directory.

## Historical Version 1.1 Publish Command

Run from the repository root after the release source is clean and all required
regressions pass:

```powershell
dotnet publish WartalesEditor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:PublishReadyToRun=false
```

Expected output:

```text
bin\Release\net10.0-windows\win-x64\publish\
```

The explicit properties are release authority and must not be replaced by
implicit SDK defaults. The final package must be assembled from one fresh
publish invocation, not mixed with earlier output.

Release builds also apply this repository-owned path-privacy mapping:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <PathMap>$(MSBuildProjectDirectory)=/_/</PathMap>
</PropertyGroup>
```

This preserves portable debug metadata while preventing the repository's local
absolute path from being embedded in application CodeView and PDB document
records. Debug builds remain unchanged.

## Historical Version 1.1 Verified Publish Output

The Phase 2 isolated test publish used the exact properties above with an
external temporary output directory. It completed with zero warnings and zero
errors and emitted 401 files totaling 148,081,543 bytes before packaging. The
published executable started without optional data, created a visible main
window titled `Wartales Editor - No Wartales file open`, accepted a normal
close request, and exited with code 0.

### A. Required application/runtime output

Retain the complete SDK-produced publish output except the application PDB:

- `WartalesEditor.exe`, `WartalesEditor.dll`, `.deps.json`, and
  `.runtimeconfig.json`.
- `Newtonsoft.Json.dll`.
- The self-contained .NET and Windows Desktop runtime DLLs/native executables.
- WPF assemblies, native graphics/input components, and satellite resource
  directories emitted by publish.

Do not hand-prune runtime files based on apparent names. Components such as
`createdump.exe`, DAC/DBI libraries, and satellite resources are SDK-authored
self-contained output and remain in the normal package unless a separately
validated SDK-supported publish policy changes them.

### B. Optional diagnostics

- `WartalesEditor.pdb` is the only project symbol file emitted by the verified
  publish. Exclude it from the normal public ZIP.
- Retain the PDB locally with the immutable final build record for future
  troubleshooting.
- Do not publish a symbols artifact for V1 unless separately approved.

### C. Exclude from the user ZIP

- All `*.pdb` files.
- Test projects/results, source, internal development/process documentation,
  `bin`/`obj` parents, Git metadata, logs, and temporary files.
- QuickBMS, the Shiro script, CDBs, `res.pak`, export XML, Profiles, snapshots,
  Golden data, `.wtstate`, local settings, and user data.

### D. Public files copied separately

- Root `README.md`, rendered and visually verified as `README.pdf`.
- `Docs/07_UserGuide.md`, rendered and visually verified as `USER-GUIDE.pdf`.
- Root `LICENSE`.
- Root `THIRD-PARTY-NOTICES.txt`.
- `Docs/CHANGELOG.md`, copied as `CHANGELOG.md`.

## Version 1.1.1 Release ZIP Manifest

The selected local candidate artifact is
`WartalesEditor-1.1.1-win-x64.zip`. The published 1.1.0 artifact remains
historical and unchanged.

Archive entries are placed directly at the ZIP root so a player can extract to
one chosen folder and run the executable. Do not add a second nested wrapper
folder.

```text
WartalesEditor.exe
App\
    WartalesEditor.exe
    WartalesEditor.dll
    WartalesEditor.deps.json
    WartalesEditor.runtimeconfig.json
    <all other vetted self-contained main publish files/directories>
README.pdf
USER-GUIDE.pdf
LICENSE
THIRD-PARTY-NOTICES.txt
CHANGELOG.md
```

The staging directory must contain exactly one root directory, `App`, and the
six root files shown above. Quick Help first checks beside the real executable
and, because it runs from a directory named `App`, may resolve
`USER-GUIDE.pdf` from the immediate parent package root. It does not use the
current working directory or unrestricted upward search.

## Historical Version 1.1 Artifact Names

```text
Binary ZIP:      WartalesEditor-1.1.0-win-x64.zip
Checksum:        WartalesEditor-1.1.0-win-x64.sha256
Optional symbols:WartalesEditor-1.1.0-symbols.zip (not approved)
Git tag:         v1.1.0
```

Do not create the tag or artifacts until separately authorized.

## SHA-256 Procedure

Create the checksum only after the final ZIP is immutable. From the artifact
directory:

```powershell
$zip = 'WartalesEditor-1.1.0-win-x64.zip'
$checksum = 'WartalesEditor-1.1.0-win-x64.sha256'
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()
"$hash  $zip" | Set-Content -LiteralPath $checksum -Encoding ascii
Get-Content -LiteralPath $checksum
```

The checksum file contains one uppercase hexadecimal SHA-256 value, two spaces,
and the ZIP filename, followed by a newline.

After upload, download the ZIP and checksum into a clean directory and run:

```powershell
$expected = (Get-Content -LiteralPath 'WartalesEditor-1.1.0-win-x64.sha256').Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)[0]
$actual = (Get-FileHash -LiteralPath 'WartalesEditor-1.1.0-win-x64.zip' -Algorithm SHA256).Hash.ToUpperInvariant()
if ($actual -ne $expected) { throw 'Published ZIP checksum mismatch.' }
```

## Malware Scan Procedure

Use current Windows Security definitions on the final extracted staging folder
and immutable ZIP. Record date/time, Windows Defender product/engine/signature
versions, scan targets, and clean/detected result in the private release
checklist. Example administrative PowerShell workflow:

```powershell
Update-MpSignature
Get-MpComputerStatus | Select-Object AMProductVersion, AMEngineVersion, AntivirusSignatureVersion, AntivirusSignatureLastUpdated
Start-MpScan -ScanType CustomScan -ScanPath '<final extracted staging folder>'
Start-MpScan -ScanType CustomScan -ScanPath '<final ZIP path>'
```

Do not upload pre-release/private binaries to VirusTotal or another third party.
A VirusTotal scan is optional only after explicit Project Owner authorization
for the public artifact. Any detection blocks publication until investigated.

## SmartScreen Policy

V1 remains unsigned; no signing work is in scope. Public guidance must say to
download only from an official source, verify SHA-256, inspect file/source
details, and use the per-file Windows flow if choosing to continue. Never tell
users to disable SmartScreen or antivirus globally, and never promise that no
warning will appear.

## Complete Public Manual

Repository authority is `Docs/07_UserGuide.md`, titled **Wartales Editor User
Manual**. README links to it from a visible **Full User Manual** section. During
packaging the authoritative Markdown is rendered and visually verified, then
the accepted PDF is copied to ZIP root as `USER-GUIDE.pdf`. The Markdown remains
the single editable source and is not independently forked into a second manual.
The Quick Help documentation added after the earlier PDF review is included in
the regenerated and visually validated final review PDF for RC packaging.

The manual contains all 37 required subjects: platform/install/first launch,
QuickBMS setup, recommended workflow, Main Window, Profiles, every Gameplay
Tool, Restore, Undo/Redo, Detailed Editor, Review/validation, Save behavior,
Update Survival, Golden, Language Data, Import/Export safety, locations,
updates, troubleshooting, support, privacy, SmartScreen, uninstall, credits,
AI disclosure, and legal notices.

## Public README

Repository authority is the root `README.md`, which remains the GitHub source
repository landing page. During packaging the authoritative Markdown is rendered
and visually verified, then the accepted PDF is copied to ZIP root as
`README.pdf`. The packaged PDF links to `USER-GUIDE.pdf` for the complete manual.
The Markdown remains the single editable README source and is not copied into the
binary ZIP as normal end-user documentation.

## Feature Documentation Matrix

| User-facing feature/action | Documented? | Manual section |
|---|---:|---|
| Install, first launch, QuickBMS setup, recommended workflow | Yes | 3–6 |
| Manual Open and Main Window/menu/keyboard overview | Yes | 7–8 |
| Quick Help compact workflow reference and complete-manual action | Yes | 8 / Quick Help |
| Profile create/apply/update/rename/duplicate/import/export/delete | Yes | 9 |
| Starting Resources | Yes | 10 / Starting Game |
| XP Progression; Lectern Knowledge Gain | Yes | 10 / Progression |
| Delicious Meals; Forging; Mining & Woodcutting; Fishing; Lockpicking | Yes | 10 / Professions |
| Add Camp Facilities; Upgrade All Equipment | Yes | 10 / Camp & Equipment |
| Campfire; Cooking Pot; Workshop; Ruby & Sapphire | Yes | 10 / Camp & Equipment |
| Volunteer; Valour; Carrying Capacity | Yes | 10 / Party |
| Run Stamina; Positive Traits; Trait Exclusions | Yes | 10 / Party |
| Movement; Rain; Vendor Refresh | Yes | 10 / World |
| Request Board Rewards | Yes | 10 / World and 11 |
| Resource Replenishment; Battle Zoom; Nine Puzzle; Rest interval | Yes | 10 / World |
| Restore Previous Values; Undo/Redo | Yes | 12–13 |
| Detailed Editor and property restore | Yes | 14 |
| Review Changes | Yes | 15 |
| Check Project and Check Compatibility | Yes | 16 |
| Save and actual Save-As behavior | Yes | 17–18 |
| Update Survival | Yes | 19 and 27 |
| Golden set/select/import/compare/load/remove | Yes | 20 |
| Language Data setup/replace/fallback/storage | Yes | 21 |
| Import From Wartales | Yes | 22 |
| Export Back to Wartales and recovery | Yes | 23–24 |
| Locations, app/game updates, troubleshooting | Yes | 25–29 |
| Privacy, SmartScreen, uninstall, support, Ko-fi, credits/legal | Yes | 30–37 |

## GitHub Issues Preparation

Do not enable/configure Issues remotely until authorized. The approved minimal
bug-report template content is:

```text
Wartales Editor version:
Wartales build:
Windows version:
QuickBMS version (if relevant):
Steps to reproduce:
Expected behavior:
Actual behavior:
Relevant editor message/error text:
```

Template notice:

> GitHub Issues is for general reproducible defects. A response or fix is not
> guaranteed. Individual mod/support requests and one-on-one troubleshooting
> are not provided. Do not upload proprietary Wartales game files, personal
> Profiles, Golden data, or state files.

Do not create a feature-request or personal-support ticket expectation.

## Clean-Machine Validation Plan

Use the actual future immutable release ZIP, never development output:

1. Start with a fresh Windows 11 x64 machine or VM.
2. Confirm no separate .NET runtime is installed/required for the self-contained
   package.
3. Confirm no existing `<Documents>\Wartales Editor` user-data folders.
4. Copy/download the final ZIP and checksum.
5. Verify SHA-256 before extraction.
6. Extract the full ZIP to a normal standard-user folder.
7. Audit the extracted manifest for required and prohibited files.
8. Launch `WartalesEditor.exe` as a standard user with no optional resources.
9. Confirm raw-ID fallback, Quick Help tabs/footer, packaged User Guide opening,
   and normal startup/close/reopen.
10. Manually open a valid CDB, edit, Save to a new path, close, and reopen it.
11. Exercise representative tools from every Gameplay Tools category.
12. Verify Restore Previous Values and atomic Undo/Redo.
13. Verify Review Changes and Show in Editor.
14. Verify Check Project and Check Compatibility.
15. Create, apply, update, export, import, and delete a Profile.
16. Set up/replace Language Data and verify persistence after relaunch.
17. Set/select, compare, load, import-as-Golden, and remove Golden as planned.
18. Place the approved external QuickBMS/script versions together in one test
    folder and select it through **Tools → QuickBMS Location...**; also verify
    the historical `<Desktop>\quickbms` fallback separately.
19. Import From Wartales and verify durable `Extracted\data.cdb`.
20. Run Export preflight through final confirmation boundaries.
21. Only with separate live-write authorization, perform one final Export and
    byte-verification acceptance.
22. Relaunch the editor and verify Profile, Language, Golden, and compatible
    gameplay-state persistence.
23. Verify process and temporary workspace cleanup.
24. Re-audit the ZIP/extraction for prohibited files and unexpected user data.
25. Record Windows, Wartales, SDK, QuickBMS, script, Defender, checksum, and all
    acceptance results.

## Supported Version Recording

The currently validated Wartales build is Steam App ID `1527950`, BuildID
`25172421`, confirmed directly by the Project Owner from installed Steam content
updated September 7, 2026 at 5:39 PM. QuickBMS `0.12.0` and
`Shiro_Games_PAK_script.bms` v0.2 dated 10.03.2022 remain the validated external
toolchain, with hashes recorded in the public documents. Keep these statements
consistent across all four public authorities:

- Root `README.md` / packaged `README.pdf`.
- `Docs/07_UserGuide.md` / packaged `USER-GUIDE.pdf`.
- `Docs/CHANGELOG.md` or the final release notes source.
- The GitHub Release body.

Do not broaden the support claim to unvalidated later versions.

## Quick Help V1 Lifecycle

**Quick Help V1 is complete and closed.** Its Investigation, Design /
Architecture, Implementation, Focused Engineering Review (**PASS**), Project
Owner Interactive Acceptance (**PASS**), footer presentation correction, brief
visual re-test (**PASS**), reconciliation, and final commit/push checkpoint are
complete. The accepted feature is the always-visible main-screen action, owned
modeless five-tab reference window, one shared footer reminder, local packaged
User Guide launcher, and project-independent single-instance lifecycle.

## Pre-Release Save and Run Speed Corrections

Implementation and repository verification are complete. User-invoked Save
shows `Saving…` in the existing status area before entering the unchanged
synchronous persistence pipeline, with one render-priority dispatcher turn and
a command-local reentrancy guard. Run Speed retains its persisted identifiers
and gameplay values while presenting the numerically ascending player-facing
labels Vanilla, Fast, Faster, and Very Fast with distinct descriptions. Focused
Engineering Review passed and Project Owner interactive acceptance passed for
both corrections. Save Feedback and Run Speed Presentation are closed,
committed, and pushed. These corrections have not produced a new release
candidate.

## Exact Later Release Process

1. Verify clean, synchronized, sanitized release source and authorized version.
2. Confirm SDK `10.0.400`, no unexpected refs/tags, and exact dependency state.
3. Clean build both release projects and run the full required regression suite.
4. Run the tracked portable-package builder against a fresh validated
   descendant of repository `output\`.
5. Validate both publishes, the exact root/`App\` layout, all source/staged
   relative paths and SHA-256 values, and root-launcher provenance.
6. Smoke-launch the staged root launcher and confirm it starts the exact
   `App\WartalesEditor.exe`, remains responsive, closes normally, and returns
   the child exit code.
7. Retain symbols and evidence privately.
8. Construct the separately authorized immutable ZIP from staging root without
   an outer wrapper directory.
9. Use the authorized release/version artifact name.
10. Generate the immutable ZIP SHA-256 file.
11. Scan extracted staging and ZIP with updated Windows Defender.
12. Perform the complete clean-machine validation plan.
13. Verify the recorded Wartales/toolchain versions remain consistent in all
    authorities.
14. Perform final release reconciliation and obtain Project Owner acceptance.
15. Commit/push only the separately approved final source/document state.
16. Create only the separately authorized new release tag; never reuse or move
    the published `v1.1.0` tag.
17. Create the separately authorized GitHub Release and upload ZIP/checksum.
18. Redownload both published files and verify SHA-256.
19. Extract and launch the downloaded published artifact.
20. Publish to Nexus only if separately authorized, then verify that download.

Each mutating Git/hosting/publication action requires its own applicable
authorization. A failed gate stops the sequence.

## Release Script Authority

The tracked `Scripts\Build-PortablePackage.ps1` and
`Scripts\Test-PortablePackageLayout.ps1` flow is authoritative for the new
two-project layout. `Scripts\PortablePackagePathSafety.ps1` owns bounded output
validation. The initial Engineering Review failed because output authority
could be a reparse point before recursive deletion and publish equivalence used
filenames rather than content. Focused corrections added authority/descendant
junction checks, SHA-256 payload equivalence, launcher provenance, same-name
corruption and substitution rejection, empty-string argument coverage, and
Unicode package-path coverage. Renewed Engineering Review and Project Owner
Acceptance passed.

The builder does not tag, publish, upload, delete an old installation, or imply
release authorization. Users must extract an update to a fresh folder; the
launcher ignores stale root runtime files but deliberately removes none.

## Post-Release External Follow-Up

- Publish to Nexus only if separately authorized.
- Submit to VirusTotal only if separately authorized.

The Version 1.1.0 GitHub release lifecycle is complete. No further public
release action is active; validation for a later candidate remains deferred and
separately authorized.
