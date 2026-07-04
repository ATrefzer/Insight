# GitProvider — Reconstructing File Identity from a Git History

This document explains the algorithm implemented in `Insight.GitProvider/GitProvider.cs`:
what problem it solves, how it works step by step, and which trade-offs it makes.

## The problem: Git has no file identity

All analyses in Insight (hotspots, change coupling, knowledge maps) are built on one
simple question:

> *How often — and together with what — did **this file** change?*

That question sounds trivial, but Git cannot answer it directly. Git is a **content
tracker**: a commit is a snapshot of the whole tree, and a "file" is just a path that
happens to point to a blob. There is no stable identifier that survives a rename:

```
Commit 1:  Add      Utils/Helper.cs        (100 changes over 5 years ...)
Commit N:  Rename   Utils/Helper.cs  ->  Core/TextHelper.cs
Commit M:  Edit     Core/TextHelper.cs
```

If you count changes per *path*, the rename in commit N cuts the history in two:

* `Utils/Helper.cs` — an old, "dead" file with 100 changes,
* `Core/TextHelper.cs` — a seemingly *young* file with 2 changes.

The 5-year hotspot disappears from the analysis at the exact moment somebody cleans up
the folder structure. Long-lived files — the most interesting ones for a crime-scene
analysis — are precisely the files that are most likely to have been renamed or moved
at some point.

### Why the Git built-ins are not enough

| Approach | Why it falls short |
|---|---|
| `git log --name-status -M` | Reports renames as `R` records, but only per commit. Interpreting them linearly breaks down at merges: the same rename is reported on the branch *and* on the merge commit, files can be renamed differently in parallel branches, etc. |
| `git log --follow <file>` | Works for a **single** file only, is a heuristic, disables `--full-history`, silently simplifies the history, and occasionally produces empty output. Running it per file is what `GitProviderFileByFile` does — it works, but it is very slow and inherits all `--follow` quirks. |
| `git log --find-renames` thresholds | Only affects rename *detection* quality, not the identity problem across merges. |

There is no Git command that yields "a stable file id across the whole commit DAG".
So the provider reconstructs one.

### Why a neutral model

The provider imports the whole history into a list of `ChangeSet`s (commit metadata +
`ChangeItem`s, each with a stable `Id`). Everything downstream (analyzers, visualizations)
works only on this model. That has two benefits:

1. Counting changes per file becomes trivial: group items by `Id`.
2. The analyzers are source-control agnostic — `SvnProvider` fills the same model
   (SVN actually *has* copy/move tracking, so it maps naturally).

## The algorithm

High-level pipeline (`UpdateHistory`):

```
1. Read the full commit graph          (fast, LibGit2Sharp, from HEAD)
2. Walk the graph from the roots       (breadth-first, topological)
   - diff each commit against its parents
   - propagate a "scope" (path -> id map) along the graph
   - emit change items with stable ids
3. Verify the final scope against `git ls-tree`
4. Clean up (drop deleted files, empty commits)
5. Save as JSON
```

### Step 1 — The commit graph

`GetGraph` walks from `HEAD` to the roots and builds a `Graph` of `GraphNode`s
(hash, parents, children). Only commits reachable from HEAD are considered — exactly
the commits that contributed to the current state of the branch.

### Step 2 — Topological traversal

`CreateHistory` starts a breadth-first traversal **from all root commits** (a repository
can have several roots, e.g. after `git merge --allow-unrelated-histories`).

The crucial invariant: **a commit is processed only after all of its parents.**
A merge commit that is dequeued while one parent is still unprocessed is skipped —
it is re-enqueued later when the missing parent finishes
(`IsMergeWithUnprocessedParents`).

This gives a topological order: parents always come before children. The final history
is simply this order reversed (newest first). Note that the *commit dates are not used
for ordering* — Git stores timestamps with second precision, and rebased or
cherry-picked commits can carry dates older than their parents.

### The scope: file identity as propagated state

A `Scope` is a bidirectional map `server path <-> GUID`. It answers, for one specific
commit: *"which files exist right now, and which identity does each of them have?"*

The scope is propagated along the graph:

| Commit type | Scope handling |
|---|---|
| Root commit | Start with an empty scope. |
| One parent | Take the parent's scope (cloned if the parent has several children, i.e. a branch point — each branch line gets its own copy). |
| Merge (2+ parents) | Clone the scope of the **first** parent (the branch we merge *into*) and integrate the changes coming from the other parents (see below). |

Applying a diff to a scope is straightforward for regular commits:

| Change | Scope operation |
|---|---|
| Added / Copied | new GUID for the path |
| Modified / TypeChanged | nothing — identity keeps |
| Renamed | the GUID moves from the old path to the new path — **this is where identity survives a rename** |
| Deleted | remove the path; remember `path -> id` so the delete item still gets the right id |

### Diff classification: where did a change come from?

For every commit the tree is diffed against **each parent** (`CalculateDiffs`).
The `Differences` class then classifies (by *path*):

* **`ChangesInCommit`** — paths that differ from **all** parents.
  For a regular commit that is simply the whole diff. For a merge commit these are the
  changes done *in the merge commit itself*: conflict resolutions and "evil merges".
* **`DiffExclusiveToParent1`** — paths that differ from the first parent but match some
  other parent. These changes were made *on the merged branch(es)* and merely arrive
  here through the merge.

> Why compare by path? A conflict resolution differs from parent 1 *and* parent 2, but
> the two diff entries are not equal as objects — they have different "old" blob ids.
> Path equality is the semantic we actually want.

**Change items are created only from `ChangesInCommit`.** This is the rule that prevents
double counting: work done on a feature branch is counted on the branch commits, not
again on the merge commit. A clean merge therefore produces an *empty* change set
(removed later in cleanup) — while a conflict resolution *does* count as a change,
which is exactly right: somebody had to think about that file again.

### Merge handling in detail

For a merge commit the provider processes two groups:

1. **Changes coming from the merged branches** (`DiffExclusiveToParent1`) update the
   scope via `UpdateScopeFromMergeSource`. The interesting case is `Added`: the file
   does not exist in the merge-into parent, but it may already have an identity on the
   branch it comes from. The id is looked up in all merge-source scopes:
   * exactly one id found, unknown in the target scope → adopt it (`MergeAdd`) —
     identity survives the merge;
   * anything ambiguous → *reset tracking* (see below).
2. **Changes done in the merge itself** (`ChangesInCommit`) are applied like a regular
   commit and produce change items.

### Reset on ambiguity — the honest fallback

Real histories contain situations where no unique identity can be derived. Examples:

* the same path was added independently in two branches,
* a file was renamed differently in parallel branches and both survive,
* two merged branches disagree about the id of a path.

In all such cases the provider does **not** guess. It assigns a fresh GUID
(`ResetTracking`) and records a warning:

```
Reset file rename tracking for <path>.
```

The file simply "starts over" at that commit. That deliberately loses some history, but
it never *invents* history — for coupling analyses a false merge of two unrelated files
is far worse than a shortened history. The parsed NUnit repository shows plenty of these
warnings; they are expected.

As a safety net, `VerifyScope` compares the final scope of the head commit with
`git ls-tree -r HEAD`. Any mismatch is reported as a warning, not a crash.

### Step 4/5 — Cleanup and persistence

The raw history still contains files that no longer exist. `CleanupHistory`:

* keeps only items whose id is *alive* (present in the head scope for a currently
  tracked file),
* drops all `Delete` items — a file can be deleted in one branch and alive in another,
  so deletes must not kill an alive id,
* removes change sets that became empty (most merge commits).

The result is saved as `git_history.json` in the cache directory.

## Worked examples

### Example 1 — rename on a branch, identity survives

```
main:     A ---------- C ---- M ---- D
                \             /
Feature:         B (rename) -
```

* `A` adds `Report.cs` → scope: `Report.cs -> id1`
* `B` (Feature) renames `Report.cs -> ReportWriter.cs` → branch scope: `ReportWriter.cs -> id1`
* `C` (main) edits `Report.cs` → main scope unchanged: `Report.cs -> id1`
* `M` merges Feature. Diff to main says: `Renamed Report.cs -> ReportWriter.cs`.
  The rename is *exclusive to parent 1* (it matches the feature parent), so the scope
  is updated (`id1` now lives at `ReportWriter.cs`) and **no change item** is emitted —
  the rename was already counted on `B`.
* `D` edits `ReportWriter.cs` → item with `id1`.

Result: one file, `id1`, with changes in `A`, `B`, `C`, `D`. A path-based count would
have produced two unrelated files.

### Example 2 — conflict resolution counts

```
main:     A ---- C ---- M
             \        /
Feature:      B -----
```

Both `B` and `C` modify `Parser.cs`; `M` resolves the conflict. In `M` the file differs
from **both** parents → it lands in `ChangesInCommit` → `M` gets a `Modified` item for
`Parser.cs`. The file was changed three times (`B`, `C`, `M`) — which reflects reality:
conflict-heavy files are coupling hotspots by definition.

### Example 3 — ambiguity, tracking resets

```
main:     A ------- C ---- M
             \           /
Feature:      B (add X) -
```

`C` (main) *and* `B` (Feature) both add a new file `X.cs`, independently. At `M` the
merged branch delivers `Added X.cs`, but the target scope already knows `X.cs` under a
different id. There is no defensible answer to "which file is it now?" — so `X.cs` gets
a fresh id at `M` plus a warning. History before `M` is intentionally cut for this file.

## Design decisions worth knowing

* **Committer date, not author date.** Author dates survive rebases and can lie about
  ordering. Dates are informational anyway — ordering is topological.
* **Octopus merges** (more than two parents) are handled by the same rules; "changed in
  the commit itself" means "differs from *all* parents".
* **Copies are new files.** A `Copied` entry starts a new identity; anything else would
  double-count the source's history.
* **Deletes are dropped from the final history.** The analyses only care about files
  that exist at head; see `CleanupHistory`.
* **Warnings instead of crashes.** Scope drift (the scope disagreeing with the actual
  tree) is expected in pathological histories. The provider warns and degrades to
  reset-tracking instead of aborting the sync.

## The three provider strategies compared

| | `GitProviderNoRenames` | `GitProviderFileByFile` | `GitProvider` |
|---|---|---|---|
| Idea | one `git log --no-renames`, path = identity | `git log --follow` per file, cut shared history | full graph walk with scope propagation |
| Renames | break the history | followed (Git heuristic) | followed (graph-exact where unambiguous) |
| Merges | not interpreted | simplified away by Git | handled explicitly, conflict fixes counted |
| Speed | fast | very slow (one `git` process per file) | moderate (one diff per commit/parent) |
| Use when | quick first look, huge repos | cross-checking results | default for real analyses |

All three produce the same `ChangeSetHistory` model, so they are interchangeable from
the analyzers' point of view.
