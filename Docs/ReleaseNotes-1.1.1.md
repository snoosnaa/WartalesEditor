# Wartales Editor 1.1.1

**Status:** Local release candidate prepared and validated. Version 1.1.1 is not
yet tagged or published.

## Highlights

- Profile Manager now keeps a stable, truthful historical Profile Changes count.
- Random Trait Exclusions recognizes additional procedurally generated recruit
  traits and handles traits removed by compatible game updates more gracefully.
- The portable package now has one obvious root launcher and keeps required
  application files neatly grouped under `App\`.

## Profile Changes

Profile Changes now represents the established historical impact of a Profile,
so the number does not shrink merely because the open game data already contains
those changes. A valid zero remains `0 changes`. When an exact historical count
cannot be established safely, Profile Manager displays `Unavailable` instead of
inventing a number. Existing Profiles remain usable.

## Random Trait Exclusions

Random Trait Exclusions now discovers additional compatible Positive and
Negative personality traits used for procedural recruits, including the current
weighted Hidden traits. When a saved Profile references a trait genuinely
removed by a later compatible game update, that trait is reported unavailable
while compatible remaining exclusions can continue. Changed or ambiguous trait
identity still fails safely.

## Portable Package

The release folder now presents `WartalesEditor.exe` at its root. Required
application files remain together under `App\`, while the README, User Guide,
license, notices, and changelog remain easy to find at the root. Quick Help
continues to open the packaged User Guide.

Extract Version 1.1.1 into a fresh folder and run the root
`WartalesEditor.exe`. Leave the `App\` folder intact and do not overwrite an
older installation, because obsolete root files from the older layout may
otherwise remain.

## Compatibility and Safety

- Windows 11 x64 and Steam Wartales remain the supported environment.
- Existing profile formats remain readable; the new impact information is
  reporting-only and does not change Profile Apply behavior.
- The package remains portable and self-contained. It has no installer or
  automatic updater.
- QuickBMS and the Shiro Games PAK script remain external, user-supplied tools.

## Release State

The candidate must pass complete repository regression, package-integrity,
clean-extraction, and local launch verification before release authorization.
No `v1.1.1` tag or public release exists yet.
