# Public Release Preparation

**Target release:** Wartales Editor 1.0.0

**Status:** IN PROGRESS

**Phase 1:** COMPLETE — legal/public metadata, repository hygiene, public
onboarding, version reconciliation, and Git-history privacy sanitization are
complete locally and remotely.

**History sanitization:** COMPLETE locally and remotely.

**Phase 2:** COMPLETE — the publish/package process, release manifest, complete
public User Manual, checksum/scan procedures, clean-machine plan, issue-report
guidance, and later publication sequence are defined and repository-verified.

**Phase 2 closeout:** User Manual content review: PASS. User Manual PDF Project
Owner visual review: PASS. README content review: PASS. README PDF Project Owner
visual review: PASS. The next product-development task is **Common Actions /
basic help feature — Investigation**.

Phase 2 defines the process. It does not create the final release candidate,
checksum, tag, GitHub Release, or Nexus publication.

## Release Authority

- Free and MIT licensed.
- Copyright © 2026 M. Tyler Spencer.
- Released by TyTech Games.
- Windows 11 x64 and Steam Wartales at the standard installation path.
- Exact release-tested Wartales build only, recorded during final validation.
- Self-contained, untrimmed, multi-file `win-x64` portable ZIP.
- No installer, updater, single-file publish, trimming, or ReadyToRun.
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

## Exact Publish Command

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

## Verified Publish Output

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

## Release ZIP Manifest

Artifact: `WartalesEditor-1.0.0-win-x64.zip`.

Archive entries are placed directly at the ZIP root so a player can extract to
one chosen folder and run the executable. Do not add a second nested wrapper
folder.

```text
WartalesEditor.exe
WartalesEditor.dll
WartalesEditor.deps.json
WartalesEditor.runtimeconfig.json
Newtonsoft.Json.dll
<all other vetted self-contained publish runtime files/directories>
README.pdf
USER-GUIDE.pdf
LICENSE
THIRD-PARTY-NOTICES.txt
CHANGELOG.md
```

The staging directory must contain exactly the vetted publish output minus
`*.pdb`, plus those five public documents. Compare a recursive manifest against
this rule before creating the ZIP.

## Artifact Names

```text
Binary ZIP:      WartalesEditor-1.0.0-win-x64.zip
Checksum:        WartalesEditor-1.0.0-win-x64.sha256
Optional symbols:WartalesEditor-1.0.0-symbols.zip (not approved for V1)
Git tag:         v1.0.0
```

Do not create the tag or artifacts until separately authorized.

## SHA-256 Procedure

Create the checksum only after the final ZIP is immutable. From the artifact
directory:

```powershell
$zip = 'WartalesEditor-1.0.0-win-x64.zip'
$checksum = 'WartalesEditor-1.0.0-win-x64.sha256'
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()
"$hash  $zip" | Set-Content -LiteralPath $checksum -Encoding ascii
Get-Content -LiteralPath $checksum
```

The checksum file contains one uppercase hexadecimal SHA-256 value, two spaces,
and the ZIP filename, followed by a newline.

After upload, download the ZIP and checksum into a clean directory and run:

```powershell
$expected = (Get-Content -LiteralPath 'WartalesEditor-1.0.0-win-x64.sha256').Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)[0]
$actual = (Get-FileHash -LiteralPath 'WartalesEditor-1.0.0-win-x64.zip' -Algorithm SHA256).Hash.ToUpperInvariant()
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
9. Confirm raw-ID fallback and normal startup/close/reopen.
10. Manually open a valid CDB, edit, Save to a new path, close, and reopen it.
11. Exercise representative tools from every Gameplay Tools category.
12. Verify Restore Previous Values and atomic Undo/Redo.
13. Verify Review Changes and Show in Editor.
14. Verify Check Project and Check Compatibility.
15. Create, apply, update, export, import, and delete a Profile.
16. Set up/replace Language Data and verify persistence after relaunch.
17. Set/select, compare, load, import-as-Golden, and remove Golden as planned.
18. Install the approved external QuickBMS/script versions at expected paths.
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

After final clean-machine validation, record the exact accepted Wartales build,
QuickBMS version, and Shiro script version in all four public authorities:

- Root `README.md` / packaged `README.pdf`.
- `Docs/07_UserGuide.md` / packaged `USER-GUIDE.pdf`.
- `Docs/CHANGELOG.md` or the final release notes source.
- The GitHub Release body.

Do not guess or prefill these values.

## Common Actions Feature Boundary

**Common Actions / basic help must complete its normal engineering lifecycle
before the final release-candidate package is generated.** It is not designed
or implemented by Phase 2. Its separate lifecycle is Investigation → Design /
Architecture → Implementation → Focused Engineering Review → Project Owner
Interactive Acceptance → Reconciliation → Commit/Push.

## Exact Later Release Process

1. Verify clean, synchronized, sanitized release source and authorized version.
2. Confirm SDK `10.0.400`, no unexpected refs/tags, and exact dependency state.
3. Clean build all release projects and run the full required regression suite.
4. Delete/recreate known release staging; run the exact publish command once.
5. Validate the publish directory and smoke-launch its executable.
6. Copy publish output except `*.pdb` into empty package staging.
7. Copy the accepted README as `README.pdf`, the accepted manual as
   `USER-GUIDE.pdf`, LICENSE, notices, and changelog.
8. Audit required/prohibited files and retain symbols privately.
9. Construct `WartalesEditor-1.0.0-win-x64.zip` from staging root.
10. Generate the immutable ZIP SHA-256 file.
11. Scan extracted staging and ZIP with updated Windows Defender.
12. Perform the complete clean-machine validation plan.
13. Record exact supported Wartales/toolchain versions in all authorities.
14. Perform final release reconciliation and obtain Project Owner acceptance.
15. Commit/push only the separately approved final source/document state.
16. Create annotated/lightweight `v1.0.0` only as separately authorized.
17. Create the GitHub Release and upload ZIP/checksum.
18. Redownload both published files and verify SHA-256.
19. Extract and launch the downloaded published artifact.
20. Publish to Nexus only if separately authorized, then verify that download.

Each mutating Git/hosting/publication action requires its own applicable
authorization. A failed gate stops the sequence.

## Release Script Decision

A release script is **unnecessary for V1**. The explicit one-project publish,
five-document copy, one PDB exclusion, ZIP, checksum, and audit sequence is
short enough to review directly and avoids introducing unreviewed release
tooling immediately before V1. The documented command sequence is authoritative.
If future releases add multiple packages or repeated channels, investigate a
small fail-fast script then; it should verify source/SDK, publish once, stage
from an allowlisted manifest, reject prohibited files, archive deterministically,
hash, and print an audit without tagging or publishing.

## Remaining Release Preparation

- Complete the Common Actions feature lifecycle.
- Produce the actual release-candidate publish/package.
- Generate the final checksum.
- Perform the final malware scan.
- Complete clean-machine validation.
- Record exact Wartales/QuickBMS/script versions.
- Enable/configure GitHub Issues.
- Complete final release review and reconciliation.
- Create `v1.0.0` only after authorization.
- Publish the GitHub Release only after authorization.
- Publish to Nexus only if separately authorized.

Release preparation remains **IN PROGRESS** until these items are complete.
