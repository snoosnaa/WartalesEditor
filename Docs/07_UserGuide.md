
# Version 0.10.0 workflow terminology

# Golden CDB

Use **Tools → Golden CDB...** to manage one optional reference CDB. Golden means
only the exact file you choose as your reference; Wartales Editor checks that it
is structurally usable but does not certify that it is vanilla, pristine,
Steam-verified, or current. The stored copy is
`<Documents>\Wartales Editor\Golden CDB\data.cdb`. Its technical identity remains
internal; the normal Golden window shows the stored filename and useful status or
cleanup messages instead. No metadata file or archive history is created.

**Set Current Project as Golden** requires the open CDB content to match its saved
file. Unsaved scalar, added, removed, null, and other structural CDB changes must
be saved first; a gameplay-state-only change does not block designation because
it does not change the CDB bytes. **Select CDB...** validates and copies a standalone CDB without opening
its adjacent gameplay state. Replacing Golden requires confirmation. The source
file can be moved or deleted after a successful Set. **Remove Golden CDB** removes
only the stored reference; it does not close or alter the current project,
profiles, gameplay state, or Undo history.

**Import Current Wartales CDB as Golden** runs the same read-only **Import From
Wartales** workflow used by the main editor. It extracts and validates the
currently installed game data, publishes and opens the durable imported CDB, and
then offers to designate those exact persisted bytes as Golden. If Golden already
exists, replacement still requires confirmation. Declining changes only the
Golden step: the successful import remains open and the previous Golden remains
unchanged. Import cancellation or failure does not change Golden. This action
does not write to or deploy into `res.pak`.

If the canonical Golden is published but an obsolete temporary transaction file
cannot be removed, the window reports cleanup attention instead of clean success.
Golden remains usable and the next Set/Replace safely clears recognized stale
transaction files before publishing.

**Load Golden CDB** opens a detached, sidecar-free copy through the normal
unsaved-changes prompt. It does not grant Restore Previous Values authority.
When a loaded Golden project is saved, or another project's save destination is
the Golden path, the editor explicitly offers Save Golden Anyway, Choose Another
Location, or Cancel. Choosing another location creates the normal editable copy
and leaves Golden unchanged. This choice is made before ordinary save validation;
Cancel exits immediately, while the final selected destination is still validated
before any write.

**Compare Current to Golden** explicitly compares current live values, including
unsaved edits, without changing either project. Exact bytes receive an exact
match message. Byte-different projects with the same supported editor values
receive a modeled all-clear. Otherwise the grid shows only proven differences.
Records without safe stable identifiers and unsupported structures are reported
separately as incomplete coverage, not counted as differences. If either side is
ambiguous or unsupported, the editor does not also claim the corresponding item
is missing/new and does not compare descendants under that unresolved identity.
Golden comparison
is separate from **Check Compatibility** and never applies or restores changes.

# Language Data

The Detailed Editor can use localized Wartales names from a game export
localization XML file. If language data is not set up, the editor remains fully
available with internal IDs and displays a **Set Up Language Data** action.

Select the Wartales export file for the language you want to use. The embedded
language code controls the setup, even if the file has been renamed. The editor
uses its detected Wartales installation to open the file picker in the game
folder and preselect a valid `export_*.xml` file when available. If detection or
candidate discovery is unavailable, normal manual selection remains available.
The editor validates the file and stores one durable copy at
`<Documents>\Wartales Editor\Language Data\export.xml`. The selected source file
is not needed afterward, and the stored copy loads automatically on later
launches.

Use **Tools → Language Data...** to view the active language code or replace the
stored data. Ready language data is shown with the editor's green success
treatment. Missing or damaged language data falls back to internal IDs without
blocking project loading. Wartales `texts_*.xml` files are not used, and this
feature does not translate the application menus or dialogs.

If replacement fails after it begins, the editor reports whether the previous
language data was restored or whether it must be set up again. If the new data
is active but an obsolete temporary recovery file cannot be removed, the editor
keeps the new language active and displays a cleanup warning.

# Import From Wartales

Choose **File → Import From Wartales...** or **Import From Wartales** on the
welcome screen to extract and open the installed game's current `data.cdb`.
The editor checks the standard Steam Wartales installation and the external
QuickBMS folder on the current user's Desktop, performs extraction in a fresh
temporary folder, validates the result with the normal project loader, promotes
it to `<Wartales installation>\Extracted\data.cdb`, and opens that durable file
as the current project. Temporary extraction staging is disposable.

If `Extracted\data.cdb` already exists, the editor warns before extraction and
replaces it only when the player chooses to continue. Cancel preserves the
existing file and current project. The Extracted folder represents the current
extracted CDB; import does not create numbered copies, archives, or backups.

The live `res.pak` is read-only during this workflow. Import does not install a
modded CDB, replace a game file, create a game-package backup, or perform
reimport. Existing unsaved changes receive the same Save/Discard/Cancel prompt
used when opening another CDB. A failed import preserves the current project
and reports whether the installation, package, tool, script, process, or
extracted CDB prevented import.

The editor refuses temporary extraction paths redirected through Windows
junctions or other reparse points. QuickBMS and any processes it starts are
contained together by Windows while import runs. If that contained process tree
cannot be confirmed completely stopped, no project is loaded and its temporary
folder is retained instead of being deleted while it may still be in use.

# Update Compatibility

Import From Wartales remembers which pristine game-data generation a project
came from. Normal Save preserves that origin while recording the exact saved
file revision. Automatic background checks continue protecting provenance,
state-file trust, and Restore Previous Values, but opening or importing a CDB
does not automatically open the full compatibility window.

After a Wartales update, import the fresh CDB, apply the profile you want, then
choose **Tools → Check Compatibility**. The check evaluates the currently
loaded project, including current in-memory profile changes, without changing
project data or creating Undo history. Run it again after further changes to
replace the previous result. The report lists only gameplay features or
warnings that need attention. If none exist, it displays **No compatibility
issues detected.** rather than a table of compatible features.

Previous values from another or unverified game-data generation remain
non-restorable. Compatible ordinary profile changes remain available for normal
review. Restore Previous Values still requires verified QuickBMS source
provenance and compatible captured history.

Project Owner acceptance verified this workflow in the running editor,
including normal compatibility-window minimize/restore and repeated checks.
Restore Previous Values was also verified on multiple gameplay features and
remained available after closing and reopening their feature windows within the
same project session.

An adjacent state file can be tied to the current saved CDB without proving
which pristine Wartales generation produced it. Ordinary Open treats that state
as unverified and grants no restoration authority; it does not show a separate
notice solely for this bound null-source condition. Import From Wartales reports
that the previous generation could not be verified. Damaged or unreadable prior
state is handled the same conservative way after a successful import.

Opening a CDB without trusted adjacent provenance remains supported. Editing
and ordinary profile comparison work, but generation-sensitive Restore Previous
Values is unavailable until provenance is established by authoritative import.

The main player workflows are:

- Gameplay Tools for guided gameplay changes.
- Detailed Editor for precise category, setting, and property editing.
- Profiles for saving, reusing, importing, and exporting customizations.
- Review Changes for reviewing unsaved changes before saving.
- Check Project for determining whether the project is ready to save.

Profiles are the player-facing persistence workflow. Snapshot infrastructure
continues to support Profiles internally but is not exposed as a standard UI
workflow.

# Final Feature Batch

Lectern Knowledge Gain appears under Progression. Its presets adjust only the
Knowledge earned from using the Lectern during qualifying rests, relative to
the game-data value captured when the tool first takes ownership.

Positive Random Traits appears under Party. Positive Only makes future random
recruits and other eligible procedural units use the game's two-positive-trait
generation branch. It does not alter existing units, and normal eligibility and
trait incompatibility rules still apply.

Random Trait Exclusions also appears under Party. Search the dynamically
discovered Positive and Negative lists, leave a trait checked to allow it in
future standard random generation, or uncheck it to exclude it. Select All and
Clear All change the current selection. Restore Previous Values restores the
exact eligibility captured before the tool was first applied, including
`true`, `false`, and an originally absent `done` property. Existing units and
already generated recruits are unchanged. If Undo or another project action
removes the compatible remembered history while this modeless window is open,
Restore Previous Values is safely rejected instead of applying stale checkbox
choices. Compatible profile state becomes the current remembered authority.

Profile Manager's Update Profile action replaces the explicitly selected
managed profile with the current intended configuration. Existing profile
changes remain included even after they have been saved as the project's
baseline, while changed or restored settings are reconciled by their exact
property paths. The profile keeps its name, file, author, description, tags,
version, and original creation time. The previous managed profile is not
replaced unless the updated candidate can be reloaded and pass independent
checks for retained history, current changes, canonical uniqueness, metadata,
gameplay state, and gameplay-tool requests. Gameplay-state compatibility is
refreshed against the live project before capture. Profiles record historical
property presence separately from a JSON `null` value, so only a property known
to have been created from structural absence can be restored to absence. This workflow
does not require a separately maintained pristine CDB: the selected profile
retains its prior history, while current `IsModified` properties contribute only
the changes made relative to the loaded project baseline. Select a profile and
open a project before choosing Update Profile.

Gameplay tools use Restore Previous Values independently of Update Profile
reconciliation. The action restores the pre-tool baseline retained by Gameplay
Operation State; it does not claim to know universal game defaults.

# Class A Gameplay Tools

The Gameplay Tools dashboard now includes a Professions category for Delicious
Meals, Forging Assistance, Mining & Woodcutting, Fishing, and Lockpicking.

Party includes Run Stamina Recovery. Valour Points also configures the Tent's
tier-based Valour bonus, while Carrying Capacity also configures Hitching Post
bonuses for assigned ponies.

World includes Vendor Refresh, Resource Replenishment, Battle Camera Zoom, Nine
Puzzle Assistance, and Time Between Rests. Camp & Equipment includes Campfire
Expansion, Cooking Pot Food Reduction, Workshop Materials, and Ruby & Sapphire
Value.

Each new preset dialog shows the current detected preset, selected gameplay
values, and a plain-language preview. Restore Previous Values applies the exact
baseline captured before the tool first changed the project. It becomes
available only while compatible captured history exists; missing history is
never reconstructed from current values. Every gameplay Restore Previous Values
button applies restoration immediately; no second Apply click is required.
Party Economy fields and RTE selections update to the values that were applied.
Apply remains available for later manual edits. Each completed restoration
records the complete tool as one Undo/Redo action. A project already at its
previous values safely produces no additional changes. RTE traits that were
already excluded in the captured baseline are not counted as changes, and an
exact restoration produces no RTE Review Changes entry.
If a legacy Valour or Carrying
project contains custom Tent or Hitching Post values, the dialog reports them as
custom and requires an explicit preset selection before expanding ownership.

Movement retains Vanilla as an ordinary fixed preset, but Restore Previous
Values returns to the captured pre-tool movement values instead of fixed 6/11.
Rain likewise retains its ordinary regional presets while restoration returns
the exact captured regional values rather than fixed Vanilla values. Gameplay
Operation State is saved in the `.wtstate` sidecar and compatible state is also
transported by Profiles, so previous values survive supported save/reload and
profile workflows.

Gameplay previous-value history is transferred from a profile or snapshot only
when its current-format file provenance, embedded history provenance, and the
open project's verified source generation all agree. Older portable files still
apply compatible ordinary changes, but do not activate embedded previous-value
history solely because a stored identity string matches.

The Detailed Editor's Reset Property action is separate. It restores the
current project's property baseline and does not use Gameplay Operation State.

After Apply, the active gameplay dialog reports either Applied successfully or
Already applied. Blocking failures continue to use the normal explicit error
dialog. Closing a feature window returns focus to the editor and preserves its
non-minimized state.

Starting Resources notes that long displayed configurations can jump visually
when the pointer moves between them; this does not change the resources.
Movement Speed notes that faster motion can look blurry. Battle Camera Zoom is
battle-only and notes that distant units can look blurry until the view is
zoomed back in. These notes describe visual behavior and do not block Apply.
