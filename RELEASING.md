# Studio update and rollback

1. Finish or branch active campaigns before changing package versions.
2. Update one package tag at a time and commit `manifest.json` with
   `packages-lock.json`.
3. Compile, run tests, validators, release audit and a Play/Recorder review.
4. Never point production campaigns at an unpinned branch.

Rollback by restoring the previously approved manifest and package lock commit.
Recorder output, `Library/`, `Temp/` and logs must not be committed.
