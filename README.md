# Wartales Editor

Wartales Editor is a Windows desktop editor for Wartales `data.cdb` files with
gameplay-focused editing tools, profiles, update compatibility checks, Golden
CDB comparison, and integrated QuickBMS import/export workflows.

Current source version: **1.0.0 (public release preparation)**. The final
portable package has not yet been published.

## AI Development Disclosure

Wartales Editor was designed, directed, tested, reviewed, and validated by
M. Tyler Spencer. More than 60 hours of hands-on human work went into product
design, requirements, architecture and UX decisions, prompting, testing,
engineering review, real-game validation, release preparation, and repository
and commit management.

All application code was produced using AI-assisted development tools,
primarily **OpenAI ChatGPT** and **OpenAI Codex**, under human direction and
review. This was not a one-prompt autonomous generation exercise. Development
used documented requirements, bounded investigations, explicit architecture
decisions, incremental implementation, automated regression testing,
engineering review, interactive Project Owner testing, real Wartales runtime
validation, and lifecycle reconciliation before accepted commits.

## Key Features

- Guided Gameplay Tools for progression, party, professions, world, camp, and
  equipment changes.
- A Detailed Editor with localized names, search, type-aware editing, and
  property-level reset.
- Profiles for saving and applying reusable groups of changes.
- Review Changes, Check Project, and atomic Undo/Redo.
- Restore Previous Values for compatible gameplay-tool history.
- Update Survival compatibility checks after importing updated game data.
- Optional Golden CDB reference management and difference comparison.
- Optional Language Data setup for localized Wartales names.
- Integrated QuickBMS Import From Wartales and verified Export Back to Wartales.

## Supported Platform and Game Version

Wartales Editor 1.0.0 supports **Windows 11 x64** and the **Steam version of
Wartales installed at the standard path**
`C:\Program Files (x86)\Steam\steamapps\common\Wartales`. Integrated Import and Export
require the documented user-supplied QuickBMS toolchain. This release will be
validated against the Wartales build recorded in the final release notes.

Validated Wartales build: **to be recorded during final release-candidate
validation**.

Other operating systems, CPU architectures, stores, nonstandard Steam library
paths, unverified Wartales builds, co-op behavior, and arbitrary third-party
CDB combinations are not currently claimed as supported. Ordinary manual CDB
editing outside this boundary may work, but it is not part of the verified
integrated-support claim.

## Download and Installation

The intended V1 artifact is `WartalesEditor-1.0.0-win-x64.zip`: a free,
self-contained, untrimmed, multi-file Windows x64 portable build. No installer
or updater is included.

Once the release is published:

1. Download the ZIP only from the official GitHub or Nexus release page.
2. Compare its SHA-256 checksum with the published checksum.
3. Extract the complete ZIP to a normal user-writable folder.
4. Run `WartalesEditor.exe` from that extracted folder.

Do not run the executable from inside the ZIP. Keep all extracted files
together.

Release availability will be announced only after the release-candidate checks
and publication phase are complete.

## Quick Start

1. Close Wartales before using integrated Import or Export.
2. Set up the external QuickBMS toolchain described below.
3. Choose **File → Import From Wartales...** to extract and open the current
   Steam `data.cdb`, or use **File → Open...** for an existing CDB.
4. Use Gameplay Tools or the Detailed Editor.
5. Use **Review Changes** and **Check Project** before saving.
6. Choose **Save** or **Save As** and keep the edited CDB.
7. Choose **File → Export Back to Wartales...** only when ready to update the
   live game package.

See the [full User Guide](Docs/07_UserGuide.md) for detailed workflows and
troubleshooting.

## QuickBMS Setup

QuickBMS and the Shiro Games PAK script are external, user-supplied tools. They
are not bundled, mirrored, or licensed as part of Wartales Editor.

The current release expects:

- `<Desktop>\quickbms\quickbms.exe`
- `<Desktop>\quickbms\Shiro_Games_PAK_script.bms`

Obtain QuickBMS from [Luigi Auriemma's official QuickBMS site](https://aluigi.altervista.org/quickbms.htm).
Obtain the script from the [upstream Bartlomiej Duda Tools repository](https://github.com/bartlomiejduda/Tools/blob/master/NEW%20Tools/Shiro%20Games/Shiro_Games_PAK_script.bms).
Avoid arbitrary repackaged binaries. Integrated Import and Export remain
unavailable until both expected files exist. The exact versions validated for
the final release will be listed in its release notes.

## Profiles and Restore Previous Values

Profiles save a reusable gameplay configuration and can combine ordinary edits
with supported gameplay operations. **Restore Previous Values** uses compatible
history captured before a gameplay tool first changed its owned settings; it is
not a universal game-default lookup. The Detailed Editor's **Reset Property** is
a separate property-baseline action.

## Update Survival and Golden CDB

After Wartales updates, import the fresh game data, apply the desired profile,
then choose **Tools → Check Compatibility**. The report is observational and
does not guarantee compatibility with every future game update.

Golden CDB is an optional reference file selected by the user. Wartales Editor
checks that the file is structurally usable, but does not certify that it is
vanilla, pristine, Steam-verified, or current. Golden comparison never changes
the active project.

## Language Data

The editor can store one user-selected Wartales export localization XML file
for localized names. Without it, the editor remains usable and displays
internal IDs. Language Data affects presentation only and does not change the
project.

## Export Safety and Recovery

**Export Back to Wartales writes to the live `res.pak`.** Wartales must be
closed. The editor verifies the write by re-extracting `data.cdb`, but no live
package modification can be made risk-free. Save the edited CDB first. You may
also manually back up `res.pak`; Wartales Editor does not automatically back up
or restore it. Steam **Verify Integrity of Game Files** or reinstalling
Wartales can restore game files if necessary.

## Privacy and Network Behavior

Wartales Editor runs locally and offline. It has no telemetry, analytics,
update check, network requests, or personal-data collection/transmission. It
reads and writes local files required for the workflows the user chooses.

## Windows SmartScreen and Unsigned Builds

The V1 executable may be unsigned, so Windows SmartScreen may show a warning.
That warning does not by itself indicate malware. Download only from the
official GitHub or Nexus release location and verify the published SHA-256
checksum. Do not disable SmartScreen or antivirus globally.

## Support and Bug Reports

GitHub Issues is the intended primary support route once the public repository
and issue tracker are enabled. Wartales Editor is a free community project, and
the issue tracker is for reproducible defects rather than a personal support
desk. Reports may or may not receive a response; fixes, future updates, and an
update or release cadence are not guaranteed. Individual custom-mod requests
and one-on-one troubleshooting are not supported or promised.

A useful report includes:

- Wartales Editor version
- Wartales build
- Windows version
- Exact reproduction steps
- The relevant editor message

Do not publicly upload `res.pak`, proprietary game CDBs, Golden CDB data,
personal profiles, or state files. Data-specific troubleshooting should be
arranged deliberately rather than posted by default.

Wartales Editor is free. If you would like to support its continued work,
[support TyTech Games on Ko-fi](https://ko-fi.com/tytechgames).

## Credits

- **M. Tyler Spencer** — creator and Project Owner; product design,
  requirements, architecture direction, UX decisions, testing, validation,
  engineering-review direction, and release preparation.
- **TyTech Games** — public release and publishing identity.
- **OpenAI ChatGPT and OpenAI Codex** — implementation generation and
  engineering, investigation, and review assistance under human direction and
  validation.
- **James Newton-King** — Newtonsoft.Json.
- **Luigi Auriemma** — QuickBMS.
- **Allen, Bartlomiej Duda, Allgames-kari, and applicable upstream
  contributors** — Shiro Games PAK QuickBMS script.
- **Microsoft and .NET contributors** — .NET and WPF.
- **Shiro Games** — Wartales.

See [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for dependency and
external-tool details. These credits do not imply endorsement.

## License

Wartales Editor is free software released under the [MIT License](LICENSE).

Copyright © 2026 M. Tyler Spencer. Released by TyTech Games.

## Unofficial Project Disclaimer

Wartales Editor is an unofficial community tool and is not affiliated with or
endorsed by Shiro Games. Wartales and related names and content belong to their
respective owners. Users must own and install Wartales to use the integrated
game workflows.
