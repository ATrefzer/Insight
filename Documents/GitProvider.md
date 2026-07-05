# GitProvider — Reconstructing File Identity from a Git History

This document explains the algorithm implemented in `Insight.GitProvider/GitProvider.cs`:
what problem it solves and how it works internally, in enough detail that you can follow the logic without the source code open next to you.

## The problem: Git doesn't know what a "file" is

Every analysis in Insight (hotspots, change coupling, knowledge maps) is built on one simple question:

> *How often — and together with what — did **this file** change?*

That sounds trivial, but Git cannot answer it directly. Git is a **content tracker**: a
commit is a snapshot of a whole tree, and a "file" is just a path that happens to point
to some content. There is no ID card attached to a file that survives a rename.

Picture this history:

```
Commit 1:  Add      Utils/Helper.cs        (100 commits touch this file over 5 years)
Commit N:  Rename   Utils/Helper.cs  ->  Core/TextHelper.cs
Commit M:  Edit     Core/TextHelper.cs
```

If you count changes per *path*, the rename in commit N slices the history in two:

* `Utils/Helper.cs` — an old, "dead" file with 100 changes,
* `Core/TextHelper.cs` — a seemingly *brand new* file with 2 changes.

The five-year hotspot disappears from the analysis the moment somebody cleans up the folder structure. That's a real problem for this tool specifically, because long-lived files are exactly the files that are most likely to have been renamed or moved at some point in their life.

Nothing in Git gives you *"a stable id for this file across the whole commit graph"*.
That identity doesn't exist — it has to be reconstructed.

Note: `git log --follow <file>` tracks a single file's renames, but it's a heuristic, it only works for one file at a time, it disables `--full-history`, and it can quietly produce an incomplete or even empty result.

That reconstruction is what `GitProvider.cs` does. The result is a `ChangeSetHistory`: a list of commits, each with a list of changed files, where every file carries a stable `Id` (a GUID) instead of just a path. Everything downstream in Insight only ever groups and counts by this `Id`.

## The basic idea

Think of it as replaying the repository's history like a movie, one commit (one frame) at a time, from the very first commit forward to the current `HEAD`. While the movie plays, you keep a filing cabinet next to you. Every existing file gets an index card in that cabinet: *path → permanent ID*. Whenever a commit does something to a file, you update the card:

| What happens in the commit | What you do with the cards |
|---|---|
| A file is added | Create a new card for it. |
| A file is edited | Nothing — the card doesn't change. |
| A file is renamed | Cross out the old path on the card, write the new one. Same card, same ID. |
| A file is deleted | Take the card out of the cabinet (but remember what was on it). |

That filing cabinet is called a **`Scope`** in the code — a two-way lookup between server path and GUID, `server path <-> Id`. The whole algorithm is really about maintaining one (or several, on branches) of these scopes correctly while walking through the commit graph. Branches and merges make this more interesting than the table above suggests, but the core idea never changes.

## Two passes, two directions

This is the part that's easy to get backwards, so it deserves its own section: the algorithm walks the repository **twice, in opposite directions**, for two different reasons.

### Pass 1 — Discover the graph (`GetGraph`): backward, from HEAD to the roots

Git only stores **parent** pointers on a commit ("I came from this commit"). There is no "child" pointer. So the only way to discover the graph at all is to start at `HEAD` and walk backward, commit by commit, until you run out of parents (the root commit(s)).

```
graph = empty
queue = [ HEAD ]
seen  = {}

while queue not empty:
    commit = queue.pop_front()
    for parent in commit.parents:
        graph.link(child: commit, parent: parent)   # remembers BOTH directions
        if parent not in seen:
            seen.add(parent)
            queue.push_back(parent)
```

The important detail: `graph.link` records the edge in both directions on the node —
each `GraphNode` gets a `Parents` list *and* a `Children` list. Discovering the graph
only ever needs `Parents` (that's all Git gives us), but that one call also builds
`Children` for free. Pass 2 needs exactly that.

This pass is cheap — for a repository the size of NUnit it takes well under a second,
because it only reads commit metadata, no file contents or diffs.

### Pass 2 — Process the commits (`CreateHistory`): forward, from the root(s) to HEAD

Building the filing cabinet (the scope) for a commit requires knowing the filing
cabinet of its parent(s) first — you can't know what changed *since* the parent if you
don't have the parent's state yet. So this pass has to move **forward through time**:
starting at the root commit(s) (the ones with no parent) and following `Children`
toward `HEAD`.

```
queue = all commits with no parent          # the root(s)
done  = {}

while queue not empty:
    commit = queue.pop_front()

    if commit is a merge AND any parent has no scope yet:
        queue.push_back(commit)             # try again once that parent is done
        continue

    if commit in done:
        continue                            # reached again via a second path
    done.add(commit)

    diffs = diff(commit.tree, against each of commit.parents' trees)
    commit.scope = build_scope(commit, diffs)      # see next section
    record a change set for `commit` from diffs.ChangesInCommit

    for child in commit.children:
        queue.push_back(child)

# commits were visited oldest first — the public result should read newest first
history = reverse(collected change sets)
```

A repository can have more than one root commit (for example after
`git merge --allow-unrelated-histories` combined two unrelated projects), so the queue
starts with *all* of them, not just one.

Two details keep this correct:

* **A merge commit waits for all of its parents.** If a merge is dequeued before every
  parent has a finished scope, it's simply pushed to the back of the queue and revisited
  later — its own children stay queued behind it, so nothing downstream gets processed
  out of order.
* **A commit is only ever fully processed once**, even though a merge can be reached
  from more than one path through the graph (`done` set).

So: the graph is *discovered* backward (because that's the only direction Git offers),
but it is *processed* forward (because that's the only direction that makes sense for
computing state). The one and only reversal happens right at the end, to turn the
processing order (oldest → newest) into the public contract of `ChangeSetHistory`
(newest → oldest).

## The scope in motion: a concrete example

Say the repository looks like this:

```
A ---- B ---- C
```

* **`A`** adds `Report.cs`. The scope after `A`:

  | path | id |
  |---|---|
  | `Report.cs` | `id-1` |

* **`B`** edits `Report.cs`. Nothing changes in the scope — same table as above.

* **`C`** renames `Report.cs` to `ReportWriter.cs`. The scope after `C`:

  | path | id |
  |---|---|
  | `ReportWriter.cs` | `id-1` |

If you now ask "how often did the file with `id-1` change?", the answer is *three
times* (`A`, `B`, `C`) — even though it lived under two different names. That's exactly
the problem from the introduction, solved.

## Diffing a commit against its parents

For every commit, `CalculateDiffs` computes the tree diff **against each parent
separately** (a root commit is diffed against an empty tree; a regular commit has one
parent; a merge commit has two or more).

Having one diff per parent is what makes it possible to answer: *"was this change made
on the branch, or was it made right here, in this commit?"* The `Differences` class
sorts the raw diffs into two buckets, by comparing **paths** across all the diffs (not
the diff entries themselves — two entries for the same path coming from different
parents are never equal as objects, since they record different "before" content):

* **`ChangesInCommit`** — paths that differ from **every** parent. For a regular commit
  (one parent) this is simply the entire diff. For a merge commit, these are the files
  that were touched *in the merge itself* — typically conflict resolutions.
* **`DiffExclusiveToParent1`** — only relevant for merges: paths that differ from the
  first parent (the branch being merged *into*) but match one of the other parents.
  These are changes that happened on the branch that's being merged in; they merely
  "arrive" at the merge commit, nobody touched them there.

This distinction is the key to avoiding double counting. **Change items — the things
that actually end up in the history — are only ever created from `ChangesInCommit`.**
Work done on a feature branch gets counted once, on the branch commit where it
happened. A clean merge (nothing but combining two branches, no conflicts) therefore
produces *no* change items at all and is dropped later. A conflict that had to be
resolved by hand, on the other hand, *is* counted on the merge commit — which is the
correct outcome: somebody had to make a decision about that file, again.

## Updating the scope

### Regular commits (0 or 1 parent)

A root commit starts from an empty scope. A commit with one parent starts from a copy
of the parent's scope — a real copy only if the parent is a branch point (has more than
one child), otherwise the parent's scope object is simply reused, since nothing will
ever ask the parent for it again.

Either way, the changes in `ChangesInCommit` are applied one by one:

```
Added                    -> scope.add(path)                 # brand new id
Modified / TypeChanged   -> nothing to do, id is unaffected
Deleted                  -> id = scope.lookup(path)
                             scope.remove(path)
                             remember (path -> id) for this delete
Renamed                  -> scope.move(old_path -> new_path)  # same id, new path
Copied                   -> scope.add(path)                   # treated as a new file
```

Copies get a new id rather than reusing the source's id — following a copy's ancestry
would mean the same id (and the same "line of history") suddenly applies to two files
at once, which breaks the "each file has exactly one id" invariant everything else
relies on.

### Merge commits (2 or more parents)

A merge starts from a **clone** of the first parent's scope (the branch being merged
into — cloned because that parent's scope might still be needed elsewhere in the
graph). Then two things happen, in this order:

**1. Pull in what happened on the other branch(es)** — the changes in
`DiffExclusiveToParent1`:

```
for change in DiffExclusiveToParent1:

    if change is "Added":
        candidates = { the id known for this path in each of the other parents' scopes }
                     (ignoring parents that don't know the path at all)

        if there is exactly one candidate id, and the merge scope doesn't
        already use that id for a different path:
            adopt it — the id from the branch now lives in the merge scope too

        else:
            reset tracking for this path (see next section) — the situation
            is ambiguous

    if change is "Deleted":
        if the merge scope knows the path: remove it, remember its id

    if change is "Renamed":
        if the merge scope knows the old path: move the id to the new path
        else: reset tracking for the new path — we lost track of where it came from

    # Modified / TypeChanged: nothing to do, the id already lives in the merge scope
```

**2. Apply whatever was changed in the merge commit itself** — the changes in
`ChangesInCommit` — using exactly the same rules as for a regular commit (the table in
the previous section).

## When identity can't be determined: reset

Real histories contain situations where there simply is no correct answer. For example:

* the same path was added independently, with unrelated content, on two branches;
* a file was renamed to two different names on two branches, and both survive;
* two merged branches disagree about which id a path should have.

In all of these cases the algorithm refuses to guess. It gives the file a **brand new
id**, as if it had just been added, and writes a warning:

```
Reset file rename tracking for <path>.
```

This deliberately truncates the file's history at that point — but it never invents a
connection that isn't really there. For an analysis that's about *coupling between
files*, silently merging the histories of two unrelated files would be a far worse
mistake than losing a few years of history for one of them. Real-world repositories
(NUnit was used as a test case while building this) produce a fair number of these
warnings; that's expected, not a bug.

As a final safety net, `VerifyScope` compares the scope at `HEAD` against
`git ls-tree -r HEAD` — the actual list of tracked files. Any mismatch is reported as a
warning rather than crashing the whole sync.

## From raw history to the final result

What Pass 2 produces is the *raw* history: every commit, including merge commits with
no change items, and including `Delete` items for files that no longer exist anywhere.
A final cleanup step (`CleanupHistory`) trims this down:

* only items whose id is still *alive* — present in the scope at `HEAD` for a file that
  actually exists there — are kept;
* all `Delete` items are dropped. A file can be deleted on one branch while staying
  alive on another, so a delete item must never remove an id that's still alive
  elsewhere in the history;
* commits that end up with zero items (most non-conflicting merges) are removed
  entirely.

The result is written to `git_history.json` in the cache directory, newest commit
first — the reversal from Pass 2, applied once, right at the point where the internal
"oldest to newest" processing order turns into the public result.

## Worked examples

### Example 1 — a rename on a branch survives the merge

```
main:     A ---------- C ---- M ---- D
                \             /
Feature:         B (rename) -
```

* `A` adds `Report.cs` → id `id-1` at `Report.cs`.
* `B` (on Feature) renames it to `ReportWriter.cs` → on that branch, `id-1` now lives at
  `ReportWriter.cs`.
* `C` (on main) edits `Report.cs` → main's scope is untouched: still `id-1` at
  `Report.cs`.
* `M` merges Feature into main. Relative to main, this shows up as
  `Renamed Report.cs -> ReportWriter.cs`. Since Feature agrees with this rename, it
  lands in `DiffExclusiveToParent1` — the merge scope is updated (`id-1` now lives at
  `ReportWriter.cs`), but **no change item is created**, because the rename was already
  counted at `B`.
* `D` edits `ReportWriter.cs` → another item for `id-1`.

Result: one id, changed in `A`, `B`, `C`, `D`. Counting by path alone would have
produced two disconnected, shorter histories instead.

### Example 2 — a conflict resolution counts as a change

```
main:     A ---- C ---- M
             \        /
Feature:      B -----
```

Both `B` and `C` modify `Parser.cs` independently; `M` resolves the conflict by hand.
At `M`, `Parser.cs` differs from *both* parents, so it lands in `ChangesInCommit` and
`M` gets its own `Modified` item for it. The file changed three times (`B`, `C`, `M`) —
which matches reality: a file that keeps causing merge conflicts is exactly the kind of
coupling hotspot this tool is meant to surface.

### Example 3 — an unresolvable ambiguity resets tracking

```
main:     A ------- C ---- M
             \           /
Feature:      B (add X) -
```

`C` (on main) and `B` (on Feature) both add a file `X.cs`, independently and with
unrelated content. At `M`, the incoming change says `Added X.cs` — but the merge scope
already has a *different* id under that same path (from `C`). There's no way to decide
which one is "the real" `X.cs`, so it gets a fresh id at `M`, plus a warning. Everything
before `M` is intentionally not connected to this new id.

## A few deliberate design choices

* **Committer date, not author date, is used for the commit timestamp.** Author dates
  survive rebases and cherry-picks and can end up *older* than a commit's own parent.
  Since ordering is topological anyway (see "Two passes, two directions"), the date is
  purely informational, but it should still make chronological sense when displayed.
* **Octopus merges** (more than two parents) follow the exact same rules as a two-parent
  merge — "changed in the commit itself" simply means "differs from *every* parent",
  regardless of how many there are.
* **A copy always starts a new identity.** The alternative — inheriting the source
  file's id — would mean one id suddenly represents two files at once.
* **Deletes never survive into the final result.** They exist only as a bookkeeping
  step while walking the graph (Insight only cares about files that exist right now).
* **Scope disagreements produce warnings, not crashes.** A pathological or unusual
  history can leave the reconstructed scope slightly out of sync with reality; the
  algorithm degrades to resetting tracking for the affected file and keeps going,
  rather than aborting the whole sync.
