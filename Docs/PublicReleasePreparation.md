# Public Release Preparation

**Target release:** Wartales Editor 1.0.0

**Status:** IN PROGRESS

**Phase 1:** COMPLETE — legal, public metadata, repository hygiene, and version
reconciliation implemented and repository-verified

## Initial Release Model

- Free and MIT licensed.
- Copyright © 2026 M. Tyler Spencer.
- Released by TyTech Games.
- Windows 11 x64.
- Steam Wartales at the standard installation path.
- Current release-tested Wartales build only; exact build pending final
  release-candidate validation.
- Self-contained, untrimmed, multi-file `win-x64` portable ZIP.
- Intended artifact: `WartalesEditor-1.0.0-win-x64.zip`.
- No installer, updater, single-file publish, or ReadyToRun.
- External, user-supplied QuickBMS and Shiro Games PAK script.
- Unsigned V1; a published SHA-256 checksum is planned.
- Public source repository.
- No telemetry, analytics, update checks, or network requests.

## Phase 1 Complete

- MIT project license and third-party notices.
- Public README, User Guide onboarding, credits, disclaimer, AI disclosure,
  privacy statement, support boundary, and external-tool policy.
- Application/public version and product metadata reconciled to 1.0.0.
- Repository ignore rules hardened against game data, user state, external
  tools, and release artifacts.
- Public 1.0.0 changelog groundwork.
- Git-history identity inventory and rewrite procedure prepared.

## Remaining Release Preparation

- Sanitize Git author and committer identities in a separately authorized task.
- Define and verify the actual publish process.
- Create the portable release package.
- Generate the SHA-256 checksum.
- Scan the release candidate for malware.
- Perform clean-machine release-candidate validation.
- Record the exact supported Wartales build and validated toolchain versions.
- Confirm and enable GitHub Issues as the general reproducible-issue route.
- Create the `v1.0.0` tag.
- Publish the GitHub Release.
- Publish to Nexus only if separately authorized.

Release is not complete until these items are finished.

## Git-History Privacy Rewrite Plan — Not Yet Executed

The private history contains legacy personal-provider email addresses and one
machine-style identity. The approved public replacement name is
`M. Tyler Spencer`. A replacement email must be supplied or explicitly
confirmed by the Project Owner; no address is inferred.

A later, explicitly authorized task should:

1. Confirm the private remote, current branch, clean worktree, and exact release
   commit.
2. Record all refs and create a private backup ref or bundle that will never be
   published.
3. Record the pre-rewrite commit graph, current tree hash, tags, and unique
   author/committer identities.
4. Rewrite only the approved author and committer name/email fields with a tool
   suited to identity-only filtering.
5. Preserve commit messages, file trees, timestamps where practical, and graph
   topology.
6. Verify every rewritten commit has the same tree as its mapped original and
   verify the rewritten release-tip tree exactly equals the pre-rewrite tree.
7. Re-run the complete identity inventory, repository-content checks, build,
   regression suites, and clean-worktree verification.
8. Inspect tags and all refs so no public ref retains unsanitized identities.
9. After separate Project Owner authorization, update the private remote using
   explicit `--force-with-lease`, never a plain force push.
10. Verify the remote refs and document how existing private clones should be
    replaced rather than accidentally restoring old history.

No history rewrite, tag, commit, push, package, or publication is part of Phase
1.
