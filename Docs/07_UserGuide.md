
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
