
# Version 0.10.0 workflow terminology

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
Clear All change the current selection; Restore Game Defaults restores the
loaded game's exact eligibility, including traits disabled by game data and
traits whose original `done` value was absent. Existing units and already
generated recruits are unchanged.

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

Reset to Game Default uses separate gameplay-tool behavior and is not part of
Update Profile reconciliation. Its default-value authority will be reviewed in
a separate investigation.

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
values, and a plain-language preview. Reset to Game Default immediately applies
the exact baseline captured when the tool first took ownership; it is not
necessary to click Apply afterward. Reset and Apply each record the complete
tool as one Undo/Redo action. A successful reset reports Applied successfully;
a project already at its baseline reports Already applied. Applying a matching
preset safely produces no additional changes. If a legacy Valour or Carrying
project contains custom Tent or Hitching Post values, the dialog reports them as
custom and requires an explicit preset selection before expanding ownership.

After Apply, the active gameplay dialog reports either Applied successfully or
Already applied. Blocking failures continue to use the normal explicit error
dialog. Closing a feature window returns focus to the editor and preserves its
non-minimized state.

Starting Resources notes that long displayed configurations can jump visually
when the pointer moves between them; this does not change the resources.
Movement Speed notes that faster motion can look blurry. Battle Camera Zoom is
battle-only and notes that distant units can look blurry until the view is
zoomed back in. These notes describe visual behavior and do not block Apply.
