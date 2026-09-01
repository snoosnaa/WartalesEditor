# Wartales Editor User Manual

This manual covers Wartales Editor 1.0.0. It is written for players and does
not require knowledge of CDB internals.

## 1. What Wartales Editor Is

Wartales Editor is a Windows desktop companion for customizing an extracted
Wartales `data.cdb` file. It provides guided Gameplay Tools for common changes
and a Detailed Editor for direct, type-aware editing. It tracks changes,
validates projects, supports Undo and Redo, and can save reusable Profiles.

The editor can also use a user-supplied QuickBMS toolchain to import the current
Wartales CDB and export a saved edited CDB back to the live game package.
Wartales Editor is an unofficial community project and is not affiliated with
or endorsed by Shiro Games.

## 2. Supported Platform and Game Build

The initial supported environment is:

- Windows 11 x64.
- The Steam version of Wartales.
- Wartales installed at
  `C:\Program Files (x86)\Steam\steamapps\common\Wartales`.
- The exact Wartales build recorded in the final 1.0.0 release notes.
- The user-supplied QuickBMS and Shiro Games PAK script versions recorded in
  the final release notes when integrated Import or Export is used.

Other Windows versions, operating systems, processor architectures, stores,
nonstandard Steam library paths, unverified Wartales builds, co-op behavior,
and arbitrary combinations of third-party CDB changes are not currently part
of the verified support claim.

## 3. Installation

Wartales Editor is distributed as a portable, multi-file ZIP. It has no
installer and does not require a separate .NET runtime.

1. Download `WartalesEditor-1.0.0-win-x64.zip` from the official GitHub or
   authorized Nexus release page.
2. Verify its SHA-256 checksum against the published checksum file.
3. Extract the entire ZIP to a normal user-writable folder.
4. Keep all extracted files together.
5. Run `WartalesEditor.exe` from the extracted folder.

Do not run the application from inside the ZIP. Do not download repackaged
copies from an unknown source.

## 4. First Launch

The application starts with no Wartales file open. The welcome area offers
**Import From Wartales**, **Open Extracted File**, and **Profiles**. Gameplay
Tools and project-specific actions remain unavailable until a project is open.
Language Data and Golden CDB setup are optional and available from **Tools**.

## 5. QuickBMS Setup

QuickBMS and the Shiro Games PAK script are external, user-supplied tools. They
are not bundled, mirrored, or licensed as part of Wartales Editor.

1. Obtain QuickBMS from [Luigi Auriemma's official QuickBMS site](https://aluigi.altervista.org/quickbms.htm).
2. Obtain `Shiro_Games_PAK_script.bms` from the
   [upstream Bartlomiej Duda Tools repository](https://github.com/bartlomiejduda/Tools/blob/master/NEW%20Tools/Shiro%20Games/Shiro_Games_PAK_script.bms).
3. Create a folder named `quickbms` on your Windows Desktop.
4. Place the files at these exact default locations:

```text
<Desktop>\quickbms\quickbms.exe
<Desktop>\quickbms\Shiro_Games_PAK_script.bms
```

Avoid arbitrary repackaged binaries or scripts. Integrated Import and Export
remain unavailable until both expected files and the standard Wartales
installation are present.

## 6. Recommended Workflow

1. Import or open a CDB.
2. If you are working with a fresh or unmodded CDB, apply your existing Profile
   if you want to reuse your saved mod configuration.
3. Make or adjust the changes you want.
4. Review the changes.
5. Save the edited CDB.
6. Create a new Profile or update your existing Profile to preserve your
   personal mod configuration.
7. Export the saved CDB to Wartales when ready.

Saving the CDB preserves the edited game data file. Creating or updating a
Profile preserves your reusable personal mod configuration. They are separate,
and both are useful parts of the normal workflow.

## 7. Opening a CDB Manually

1. Choose **File → Open Wartales File...**, press **Ctrl+O**, use toolbar
   **Open**, or choose **Open Extracted File**.
2. Select an extracted `.cdb` file.

- **Open** reads an existing CDB and does not run QuickBMS.
- **Import From Wartales** extracts the current CDB from the installed Steam
  package, stores it in the game's `Extracted` folder, and opens that durable
  copy.

The editor prompts before abandoning unsaved changes. An invalid or unreadable
CDB is rejected without replacing the current project.

## 8. Main Window Overview

The **Gameplay Tools** workspace presents guided changes grouped by purpose.
The **Detailed Editor** presents searchable sheets, entries, settings, and
properties for direct editing. The toolbar provides Open, Save, Undo, Redo,
Restore Value, Review Changes, Check Project, and Profiles.

The menus contain:

- **File:** Import, Export, Open, Save, and Exit.
- **Edit:** Undo, Redo, and Restore Original Value.
- **View:** Gameplay Tools, Detailed Editor, and Review Changes.
- **Tools:** Profiles, Language Data, Golden CDB, Check Project, and Check
  Compatibility.
- **Help:** About Wartales Editor.

Keyboard shortcuts are **Ctrl+O**, **Ctrl+S**, **Ctrl+Z**, and **Ctrl+Y** for
Open, Save, Undo, and Redo.

## 9. Profiles

Profiles are the main way to save and reuse your personal mod configuration. A
Profile can preserve your preferred collection of gameplay changes - such as
movement, economy, profession, and Request Board Reward settings - for reuse on
the same or another compatible CDB.

Choose **Tools → Profiles** or toolbar **Profiles**. Profile Manager provides:

- **New Profile:** saves your current mod setup as a new Profile.
- **Update Profile:** saves your current setup into the selected Profile without
  creating a new one.
- **Apply Profile:** applies the selected Profile's compatible changes to the
  CDB currently open.
- **Rename**, **Duplicate**, **Import**, **Export**, **Delete**, and **Refresh**
  for managing Profiles.

Profile-applied changes appear in **Review Changes** and can be undone with
Undo. If part of a Profile is not compatible with the open CDB, the editor
reports it instead of guessing.

Saving a CDB and saving a Profile are not the same thing:

- A **CDB** is the actual edited game-data file.
- A **Profile** is your reusable personal mod configuration.

Recommended practice is to save the edited CDB and create or update the Profile
when you want to reuse the setup later. Profiles are stored at:

```text
<Documents>\Wartales Editor\Profiles\
```

## 10. Gameplay Tools

Open a project and select **Gameplay Tools**. Most preset tools follow this
sequence:

1. Review the current setting.
2. Select a preset.
3. Review the preview.
4. Choose **Apply**.
5. Optionally use **Restore Previous Values** when you want to return to the
   settings that existed before this tool first changed them.
6. Choose **Close**.

Apply changes the open project in memory, records one Undo/Redo action, and
does not save or export automatically. Reapplying the same setting is a safe
no-op. Starting Resources, XP Progression, Add Camp Facilities, and Upgrade All
Equipment use their own controls or Undo behavior rather than the common
Restore button.

### Starting Game

#### Starting Resources

Adds extra Krowns, Bread, Apples, Iron Ore, Wood, and Cloth to every standard
new-campaign start. The selected starting group keeps its normal supplies and
bonuses; existing saves are unchanged. First setup suggests 10 Bread and 5
Apples.

1. Enter nonnegative amounts or use **+5 to All Materials**, **+10 to All
   Materials**, or **Clear Extras**.
2. Review the preview.
3. Choose **Apply**.

In Wartales, large starting-resource amounts may cause the resource list to
shift or stutter visually because the extra items extend beyond the normal
display area. This does not affect the actual resource values.

### Progression

#### XP Progression

Provides independent Character XP and Profession XP controls. Both start at
100%, accept 10% through 300%, and update independently. Lower percentages
reduce requirements. **Apply Character XP** and **Apply Profession XP** affect
only their respective progression. **Use Current Values as 100%** deliberately
adopts current values as that side's new scaling baseline; it is not Restore
Previous Values.

#### Lectern Knowledge Gain

Changes Knowledge earned from the Lectern during qualifying rests. Presets are
**Vanilla** (captured rate), **Increased** (2×), **High** (3×), and **Very High**
(5×). Other Knowledge sources are unchanged.

### Professions

#### Delicious Meals

Changes Tier 2 and Tier 3 Cooking Pot Delicious Meal chances. Tier 1 has no
such bonus. Presets are **Vanilla** (15% / 30%), **Improved** (25% / 45%),
**High** (35% / 55%), and **Guaranteed** (50% / 100%).

#### Forging Assistance

Extends the perfect-heat reaction window without removing forging. Presets are
**Vanilla**, **Easier** (2×), **Easy** (3.2×), and **Very Easy** (4.8×).

#### Mining & Woodcutting

Slows the shared timing indicator while preserving both activities. Presets
are **Vanilla** (100%), **Easier** (80%), **Easy** (60%), and **Very Easy** (40%).

#### Fishing

Shortens the fishing control phase without removing the minigame. Presets are
**Vanilla**, **Faster**, **Fast**, and **Very Fast**.

#### Lockpicking

Increases the smallest valid lock zone while preserving the minigame. Presets
are **Vanilla**, **Easier**, **Easy**, and **Very Easy**.

### Camp & Equipment

#### Add Camp Facilities

Enables the Anvil and Apothecary Table and adds their Workshop recipes. Confirm
when prompted. Existing supported data is preserved and an already configured
project produces no new changes. Profiles can capture this operation; Undo can
reverse it before saving.

#### Upgrade All Equipment

Makes supported normally obtainable equipment upgradeable at Brotherhood
Training Grounds. It does not change stats, levels, price, rarity, or other
values. Confirm when prompted. Unsupported or ambiguous projects are blocked
instead of guessed. Profiles can capture this operation.

#### Campfire Expansion

Presets are **Vanilla** and **Expanded**. Expanded makes every tier 6 × 6,
keeps Tier 1 capacity at 4, and raises Tier 2 and Tier 3 capacity to 8 and 12.

#### Cooking Pot Food Reduction

Changes daily food saved by an assigned cook without changing capacity.
Presets are **Vanilla** (2 / 4 / 6 by tier), **Improved** (3 / 6 / 9),
**Strong** (4 / 8 / 12), and **Very Strong** (6 / 12 / 18).

#### Workshop Materials

Changes Raw Materials produced per rest by an assigned Tinkerer. Presets are
**Vanilla** (2 / 2 / 2), **Improved** (2 / 3 / 4), **High** (3 / 4 / 5), and
**Very High** (4 / 5 / 6).

#### Ruby & Sapphire Value

Changes base values of Ruby and Sapphire only. Presets are **Vanilla** (40),
**Higher** (100), **High** (150), and **Very High** (200).

### Party

#### Volunteer Trait

Sets the wage reduction granted by Volunteer. **No Wages** sets the field to
100% but does not Apply. At 100%, Volunteer companions require no Krowns during
wage payments.

#### Valour Points

Configures maximum Valour, Valour restored after rest, and Tent bonuses. Tent
presets are **Vanilla** (1 / 2 / 3) and **Increased** (2 / 3 / 4). Custom
existing Tent values require an explicit supported preset before expanded
bonuses are applied.

#### Carrying Capacity

Configures Saddlebag capacity, Pony starting capacity, and Hitching Post
bonuses. Hitching Post presets are **Vanilla** and **Increased**. Custom
existing bonuses require an explicit preset selection before Apply.

#### Run Stamina Recovery

Speeds overworld running-stamina recovery in normal and exhausted states.
Presets are **Vanilla**, **Faster**, **Fast**, and **Very Fast**.

#### Positive Random Traits

**Positive Only** makes future eligible procedural units use the game's
two-positive-trait branch. **Vanilla** restores captured settings. Existing
units are unchanged; normal eligibility and incompatibility rules still apply.

#### Random Trait Exclusions

Controls which discovered positive and negative traits may appear in future
standard random generation. Checked means allowed; unchecked means excluded.
Search, **Select All**, and **Clear All** assist selection. Existing units and
generated recruits are unchanged. Restore returns the exact prior state,
including a trait that originally had no explicit setting.

### World

#### Movement Speed

Changes only player overworld walking/running speed. Presets are **Vanilla**,
**Faster**, **Fast**, and **Very Fast**. Other roaming parties are unchanged.
Faster motion may appear blurry; this is visual only.

#### Rain Frequency

Changes ordinary regional rain while preserving regional differences. Presets
are **Vanilla**, **Less Rain**, **Rare Rain**, and **No Rain**. Other weather
systems are unchanged.

#### Vendor Refresh

Speeds merchant inventory replenishment while preserving category differences.
Presets are **Vanilla**, **Faster** (2×), **Fast** (3×), and **Very Fast** (5×).

#### Request Board Rewards

Increases the base Krown rewards offered by Tavern Request Board missions using
preset percentages.

#### Resource Replenishment

Speeds supported renewable overworld gathering while preserving Slow, Normal,
and Fast category differences. Presets are **Vanilla**, **Faster** (2×),
**Fast** (3×), and **Very Fast** (5×). It is not a general modifier for
merchants, fishing, hunting, quests, one-time resources, or loot containers.

#### Battle Camera Zoom

Changes maximum battle-only zoom. Presets are **Vanilla**, **Extended**,
**Far**, and **Very Far**. Distant units may look blurry until zoomed back in.

#### Nine Puzzle Assistance

Starts the puzzle with fewer shuffles and more placed tiles. Presets are
**Vanilla**, **Easier**, **Easy**, and **Very Easy**. Very Easy is not an
instant-win mode.

#### Time Between Rests

Changes approximate travel hours before fatigue requires rest. Presets are
**Vanilla** (24), **Longer** (48), **Extended** (72), and **Very Long** (96).

## 11. Request Board Rewards

Changes shared base Krown payout ranges for ordinary Tavern Request Board
missions. Presets are **100%**, **150%**, **200%**, and **300%**, always scaled
from captured prior ranges rather than compounded. Mission-specific and other
modifiers still affect final payout. Higher negotiated rewards may require
more Influence. Restore keeps minimum/maximum ranges together, and compatible
Profiles apply the selected percentage to the values in the open CDB.

## 12. Restore Previous Values

**Restore Previous Values** returns a Gameplay Tool to the values it had before
you first changed it with that tool. For example, if Movement Speed was already
set to a custom value before you used its Gameplay Tool, Restore returns to that
custom value - not necessarily to Wartales defaults.

Restore can continue working after Save and reopen when the editor can still
safely verify the saved previous values. If it cannot safely determine those
values, Restore is unavailable instead of guessing. Restore applies immediately
and can itself be reversed with Undo.

**Restore Original Value** in the Detailed Editor is different: it resets one
property to the value it had when the current CDB was opened.

## 13. Undo and Redo

Undo reverses the latest editor action in the current session; Redo reapplies
it. Use **Edit**, the toolbar, **Ctrl+Z**, or **Ctrl+Y**. Gameplay operations are
kept together as one action. Undo/Redo covers actions from the current session;
Restore Previous Values is the separate tool-specific action described above.

## 14. Detailed Editor

Browse categories, settings, and type-aware properties. To navigate by search:

1. Search localized names, internal IDs, categories, settings, and matched
   properties.
2. Select a result.

**Clear** resets search. Select a property and use **Restore Original Value** to
return it to the value it had when the project opened. Prefer Gameplay Tools
for supported coordinated changes and Detailed Editor for precise changes not
covered by a tool.

## 15. Review Changes

1. Choose **View → Review Changes** or toolbar **Review Changes**.
2. Review the grouped unsaved changes and their original/current values.
3. Select a change and choose **Show in Editor** when you want to locate it.

Review changes before every Save and Export.

## 16. Check Project and Compatibility

**Tools → Check Project** reports errors, warnings, and information, supports
filtering/navigation/copy, and can check again. Errors block Save; warnings
should be reviewed.

**Tools → Check Compatibility** observationally evaluates gameplay features
against current data without modifying it or creating Undo history. It lists
items needing attention or reports **No compatibility issues detected.** It is
not a guarantee for every future update.

## 17. Save

1. Choose **File → Save Modded File...**, **Ctrl+S**, or toolbar **Save**.
2. Select the destination.
3. Confirm if the selected destination is the designated Golden path.

The editor validates and writes the CDB, then records it as the current file.
It also preserves supported Gameplay Tool restore information when that
information can be saved safely.

## 18. Save As

Version 1.0.0 has no separate Save As command. **Save Modded File...** always
opens a destination picker and therefore provides Save As behavior every time.
Choose a new path to preserve a source or Golden reference.

## 19. Update Survival

After Wartales updates:

1. Close Wartales.
2. Use **Import From Wartales** to load the current game data.
3. If you are working with a fresh or unmodded CDB and have an existing
   Profile, apply it if desired.
4. Make or adjust any changes.
5. Run **Check Compatibility**.
6. Review any warnings.
7. Open **Review Changes**.
8. Run **Check Project**.
9. Save the CDB.
10. Create or update the Profile if you want your personal mod configuration
    preserved.
11. Export to Wartales when ready.
12. Test the updated setup in Wartales.

If a Gameplay Tool is still compatible with the updated game data, the editor
can continue using it. If Wartales changed the relevant data in a way the
editor cannot safely recognize, that tool is blocked rather than guessed.

## 20. Golden CDB

Golden is one optional reference designated by you. It is checked for
structural usability but is not certified vanilla, pristine, current, or
Steam-verified. **Tools → Golden CDB...** offers:

- Set/Replace from the current project.
- Select/Replace from another CDB.
- Import Current Wartales CDB as Golden without changing the active project.
- Compare Current to Golden (read-only).
- **Load Golden CDB** opens your Golden CDB as the active project in the editor
  so you can inspect or work with it directly. This is different from simply
  using Golden as a comparison/reference copy.
- Remove Golden CDB without changing the active project.

Refreshing Golden and loading it are separate actions. Storage is:

```text
<Documents>\Wartales Editor\Golden CDB\data.cdb
```

## 21. Language Data

Optional Language Data supplies localized names.

1. Choose **Tools → Language Data...** or **Set Up Language Data**.
2. Select a valid Wartales `export_*.xml` file.

A validated copy is stored and automatically loaded at:

```text
<Documents>\Wartales Editor\Language Data\export.xml
```

The original is not needed afterward. Missing/invalid data falls back to raw
IDs without blocking editing. Replacement refreshes current Detailed Editor,
search, and Review Changes presentation; reopen an existing tool window if it
still shows older labels. Language Data never changes project data.

## 22. Import From Wartales

1. Close Wartales.
2. Complete the QuickBMS setup in Section 5.
3. Choose **File → Import From Wartales...**.

The editor validates the standard installation and toolchain, extracts one
valid CDB without changing `res.pak`, stores it at the durable path, and opens
it:

```text
C:\Program Files (x86)\Steam\steamapps\common\Wartales\Extracted\data.cdb
```

Existing Extracted data and unsaved work are protected by confirmation. Import
does not modify `res.pak`.

## 23. Export Back to Wartales

Export writes a saved edited CDB into live `res.pak`:

1. Close Wartales.
2. Review Changes and Check Project.
3. Save the exact intended CDB.
4. Choose **File → Export Back to Wartales...** and confirm.
5. Do not interrupt preparation, writing, or verification.

The editor uses your saved CDB for the export, writes it back to Wartales, then
re-extracts `data.cdb` and verifies that it matches exactly. If verification
succeeds, the export is complete. If something goes wrong, the editor reports
the failure clearly.

## 24. Export Safety and Recovery

No live package write is risk-free:

1. Save the intended CDB first.
2. Keep Wartales closed.
3. Do not interrupt the critical write.

The editor does not automatically back up or restore `res.pak`; an optional
manual backup is your choice. Steam **Verify integrity of game files** or
reinstall can restore game files and may remove the modification. Preserve the
saved edited CDB if Export fails.

## 25. File and Folder Locations

```text
Profiles:      <Documents>\Wartales Editor\Profiles\
Language Data:<Documents>\Wartales Editor\Language Data\export.xml
Golden CDB:   <Documents>\Wartales Editor\Golden CDB\data.cdb
Imported CDB: C:\Program Files (x86)\Steam\steamapps\common\Wartales\Extracted\data.cdb
QuickBMS:     <Desktop>\quickbms\quickbms.exe
Shiro script: <Desktop>\quickbms\Shiro_Games_PAK_script.bms
```

Modified CDBs go where you select during Save. The editor may keep a companion
file beside a saved CDB so supported Gameplay Tools can remember previous
values. Import/Export preparation uses Windows temporary folders that are not
user libraries; do not manipulate them while an operation is running.

## 26. Updating Wartales Editor

Version 1 has no updater.

1. Read the new release notes.
2. Download the new official ZIP.
3. Verify its checksum.
4. Extract it to a new folder.
5. Preserve any manually saved CDBs and companion files you still need.

Profiles, Language Data, and Golden data stored under Documents normally remain
available.

## 27. Updating Wartales

Do not assume an old edited CDB remains compatible. Follow the Update Survival
workflow in Section 19 and record the Wartales build in any issue report.

## 28. Troubleshooting

- **QuickBMS not found:** verify `<Desktop>\quickbms\quickbms.exe`.
- **Script not found:** verify the exact upstream
  `Shiro_Games_PAK_script.bms` filename and location.
- **Wartales not found:** integrated workflows require the standard Steam path;
  manually Open an already extracted valid CDB if appropriate.
- **Permission/write failure:** use a user-writable save folder, extract the
  application fully, close Wartales for Export, and read the exact error.
- **Language Data missing:** set valid `export_*.xml` or continue with raw IDs.
- **Golden not configured:** explicitly designate one, or continue without it.
- **Compatibility warning:** read the feature-specific message and do not guess.
- **Restore unavailable:** the editor cannot safely verify the saved previous
  values; use current-session Undo when applicable.
- **Export failure:** preserve the saved CDB, keep Wartales closed, verify the
  toolchain, and use Steam recovery if game files may be damaged.
- **SmartScreen warning:** follow Section 31; do not disable security globally.

## 29. Supported and Unsupported Configurations

The supported boundary is Section 2. Unsupported/unclaimed areas include
non-Windows/non-x64 platforms, non-Steam or nonstandard integrated paths,
unverified builds/toolchains, bundled external tools, automatic updates, an
installer, arbitrary third-party combinations, guaranteed future
compatibility, and certification of Golden as pristine.

## 30. Privacy and Offline Behavior

Wartales Editor runs locally and offline. It has no telemetry, analytics,
update check, network requests, or personal-data transmission. Local Profiles,
CDBs, Golden data, Language Data, and state remain on your computer unless you
move/upload them. Do not post proprietary Wartales files publicly.

## 31. SmartScreen and Unsigned Release

The unsigned V1 may trigger SmartScreen.

1. Obtain the editor only from the official release.
2. Verify the published SHA-256 checksum.
3. Confirm the source and version.
4. Scan the extracted files with Windows Security.
5. If you choose to continue, use Windows' per-file details flow.

Do not disable SmartScreen or antivirus globally, and do not assume no warning
will appear.

## 32. Uninstall and Portable Removal

1. Close the editor.
2. Delete its extracted application folder.
3. If desired, separately review and remove optional data under
   `<Documents>\Wartales Editor` and any saved CDBs or companion files.

Uninstalling does not undo changes exported to Wartales; use Steam recovery for
the live package.

## 33. Support and Issue Reporting

GitHub Issues is intended for general reproducible defects after enabled. It
is not a personal support desk. No individual support, response, fix, future
update, or release cadence is guaranteed. Custom-mod requests and one-on-one
troubleshooting are not supported or promised.

Include editor version, Wartales build, Windows version, QuickBMS version when
relevant, exact steps, expected/actual behavior, and editor message text. Do
not publicly upload `res.pak`, proprietary CDBs, Golden data, Profiles, or
companion state files.

## 34. Ko-fi

Wartales Editor is free. Optional support is available at
[https://ko-fi.com/tytechgames](https://ko-fi.com/tytechgames).

## 35. Credits

- **M. Tyler Spencer** — creator and Project Owner; product design,
  requirements, architecture direction, UX decisions, testing, validation,
  engineering-review direction, and release preparation.
- **TyTech Games** — public release and publishing identity.
- **OpenAI ChatGPT and OpenAI Codex** — implementation generation and
  engineering/review assistance under human direction.
- **James Newton-King** — Newtonsoft.Json.
- **Luigi Auriemma** — QuickBMS.
- **Allen, Bartlomiej Duda, Allgames-kari, and applicable upstream
  contributors** — Shiro Games PAK QuickBMS script.
- **Microsoft and .NET contributors** — .NET and WPF.
- **Shiro Games** — Wartales.

These credits do not imply endorsement.

## 36. AI Development Disclosure

Wartales Editor was designed, directed, tested, reviewed, and validated by
M. Tyler Spencer, including more than 60 hours of hands-on human work. All
application code was produced with AI-assisted development tools, primarily
**OpenAI ChatGPT** and **OpenAI Codex**, under human direction and review. Work
used documented requirements, bounded investigations, architecture decisions,
incremental implementation, automated tests, engineering review, Project Owner
testing, real-game validation, and lifecycle reconciliation.

## 37. License and Third-Party Notices

Wartales Editor is free software under the MIT License.

Copyright © 2026 M. Tyler Spencer. Released by TyTech Games.

Read `LICENSE` and `THIRD-PARTY-NOTICES.txt`. QuickBMS, the Shiro Games PAK
script, and Wartales game data are not included in the release.
