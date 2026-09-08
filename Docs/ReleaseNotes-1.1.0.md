# Wartales Editor 1.1.0

## Highlights

- New Paths tools for adjusting global Path level requirements and the ordinary
  XP rewards for each of the four main Paths.
- More reliable Profiles when moving compatible settings to updated Wartales
  data.
- Expanded Starting Resources, Party Economy, and QuickBMS setup options.

## New Gameplay Tools

**Path Level Requirements** offers Original, 80%, 60%, 40%, and 20% choices.
**Path XP Rewards** provides independent Original, 2×, 3×, 4×, and 5× controls
for Power and Glory, Trade and Craftsmanship, Crime and Chaos, and Mysteries and
Wisdom. Each Path is applied and restored independently. Special rewards,
including MerchAttack and `reward.pathXp` structures, are intentionally left
unchanged.

## Profiles and Update Survival

Profiles now preserve supported Gameplay Tool choices more reliably across
compatible Wartales updates. Settings are reapplied against the destination
game data, while Restore Previous Values captures authority from that
destination. Profile application remains validated, atomic, and compatible
with Undo and Redo.

## Starting Resources and Party Economy

Starting Resources now supports Hemp and groups fields into Currency, Food, and
Materials. Food shortcuts affect Bread and Apples; Materials shortcuts include
Iron Ore, Wood, Cloth, and Hemp. Clear Extras covers every supported resource.
Rope remains unsupported, and the maximum remains 1,000,000.

Valour Points adds the 3 / 4 / 5 Tent preset. Valid existing Tent or Hitching
Post values that do not match a named preset now appear as Custom and remain
unchanged while independent Valour or carrying-capacity settings are applied.
Selecting a named preset explicitly replaces those values.

## QuickBMS Setup

QuickBMS may now be kept in any chosen folder. Place `quickbms.exe` and
`Shiro_Games_PAK_script.bms` together, then choose **Tools → QuickBMS
Location...**. Wartales Editor remembers that machine-local folder for Import,
Export, and Golden CDB acquisition. The historical `<Desktop>\quickbms` folder
remains a fallback.

QuickBMS and the Shiro script remain external, user-supplied dependencies and
are not included in Wartales Editor.

## Fixes and Usability

- Added the 4× Lectern Knowledge Gain choice.
- Added the 5 / 10 / 16 Cooking Pot Food Reduction choice.
- Added Quick Help with direct access to the packaged User Guide.
- Corrected the Import From Wartales welcome-button clipping.
- Clarified Run Speed labels without changing their gameplay values.
- Corrected Path localization and compacted the Path XP Rewards presentation.
- Improved already-configured Profile feedback and dialog refresh behavior.

## Compatibility / Notes

- Historical six-field Starting Resources profile requests remain compatible;
  missing Hemp means no requested Hemp addition.
- Windows 11 x64 and Steam Wartales remain the supported platform.
- Steam BuildID 23361327 is the last exact Wartales build recorded in repository
  release authority. The exact current BuildID requires final Project Owner
  confirmation before publication.
- QuickBMS 0.12.0 and `Shiro_Games_PAK_script.bms` v0.2 dated 10.03.2022 remain
  the validated external toolchain.

## External Requirements

- QuickBMS 0.12.0
- `Shiro_Games_PAK_script.bms` v0.2, dated 10.03.2022
- A Steam installation of Wartales

## Verification

The 1.1.0 candidate passed zero-warning Debug and Release builds and the full
repository regression suite covering Gameplay Tools, Profiles, Update Survival,
QuickBMS integration, Golden CDB, Quick Help, Request Board Rewards, Paths,
Party Economy, Starting Resources, Restore Previous Values, Undo/Redo,
save/reopen behavior, and change counting. Windows file-link tests retain their
documented environment-dependent skip when symbolic-link privilege is
unavailable.
