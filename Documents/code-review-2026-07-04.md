# Code Review: GitProvider.cs (+ Einschätzung der beiden anderen Strategien)

**TL;DR:** Die Strategie ist konzeptionell richtig und das Problem ist real — Git hat schlicht keine Datei-Identität, und mit Bordmitteln bekommt man das nicht sauber hin. Deine Graph-Traversierung mit Scope-Propagation und "Reset bei Ambiguität" ist derselbe Ansatz, den z.B. `hercules` (src-d) verfolgt, und genauer als das, was Tornhills eigenes `code-maat` macht (das ignoriert Renames weitgehend). Aber: Ich habe **einen zentralen, stillen Bug gefunden, der die Merge-Logik seit jeher teilweise lahmlegt** (`Differences.Intersect` vergleicht per Referenz — die Schnittmenge ist *immer* leer), plus einen Invarianten-Bruch in `Scope.Update` und einige Crash-Szenarien bei realen Repos (packed refs, Multi-Root-Historien, Octopus-Merges).

------

## 1. Zur Konzeptfrage: Hätte es das gebraucht?

Ja, wenn dir Rename-Tracking wichtig ist — und für "Code as a Crime Scene"-Analysen ist es das:

- **Git-Bordmittel reichen nicht.** `git log --follow` funktioniert nur für *eine* Datei, ist eine Heuristik, deaktiviert `--full-history` und liefert (wie du selbst in [GitCommandLine.cs:158](https://claude.ai/epitaxy/Insight.GitProvider/GitCommandLine.cs:158) dokumentiert hast) manchmal sogar leere Ausgaben. `git log --name-status -M` liefert zwar `R`-Records, aber über Merges hinweg linear ausgewertet ist das falsch — genau deshalb brauchst du den Graphen. Es gibt kein Git-Kommando, das "stabile Datei-ID über die gesamte DAG-Historie" liefert.
- **Ohne Rename-Tracking sind Hotspot-Analysen systematisch verzerrt:** Eine 10 Jahre alte, oft geänderte Datei, die einmal umbenannt oder verschoben wurde (Ordner-Refactoring!), zerfällt in zwei "junge" Dateien. Gerade langlebige Dateien — die interessantesten für die Analyse — sind am ehesten mal umbenannt worden.
- **Die Abstraktion (Import in `ChangeSet`-Liste mit IDs) ist gerechtfertigt**, weil du mit `SvnProvider` einen zweiten Provider hast, und SVN Copy/Move nativ kennt. Dass das Konzept "nicht ins Git-Content-Tracker-Modell passt", ist kein Designfehler deinerseits — es ist genau die Lücke, die dein Code füllt. Der Trick, bei Unklarheit die ID zurückzusetzen statt zu raten, ist der richtige Trade-off: lieber Historie verlieren als falsche Kopplungen erfinden.

Also: Die Kopfschmerzen waren nicht umsonst. Jetzt zu dem, was nicht stimmt.

------

## 2. Bugs in GitProvider.cs (nach Schwere sortiert)

### 🔴 B1: `Differences.Intersect` vergleicht per Referenz — `ChangesInCommit` ist bei Merges *immer* leer

[Differences.cs:36](https://claude.ai/epitaxy/Insight.GitProvider/Differences.cs:36) bildet `DiffToParent1.Intersect(DiffToParent2)`. Ich habe per Reflection am NuGet-Paket (LibGit2Sharp 0.31.0) verifiziert: **`TreeEntryChanges` überschreibt weder `Equals` noch `GetHashCode` und implementiert kein `IEquatable`.** Zwei separate `repo.Diff.Compare`-Aufrufe erzeugen immer verschiedene Instanzen → die Schnittmenge ist per Referenzgleichheit garantiert leer.

Konsequenzen:

1. Der zweite Loop in `UpdateScopeTwoParents` ([GitProvider.cs:278](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:278), "changes done on the merge commit itself") läuft **nie**.
2. Merge-Commits erzeugen **nie** ChangeItems ([GitProvider.cs:172](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:172) iteriert `ChangesInCommit`). Konfliktauflösungen und "evil merges" zählen nicht als Änderung. Schlimmer: Eine Datei, die *im Merge-Commit selbst* hinzugefügt und danach nie angefasst wurde, bekommt zwar eine ID im Scope und landet in `aliveIds`, taucht aber in **keinem** ChangeSet auf — sie fehlt komplett in der Analyse.
3. Alles aus dem Diff zu Parent1 läuft durch `UpdateScopeFromMergeSource`, auch Änderungen, die eigentlich im Merge selbst gemacht wurden → dort greift dann der Reset-Fallback → **ein Teil deiner "Reset file rename tracking"-Warnungen bei NUnit dürfte schlicht dieser Bug sein**, nicht echte Ambiguität.

Zusatzbefund zum Design: Selbst mit korrekter Value-Equality würde der Ansatz Konfliktauflösungen kaum erkennen, weil bei "beide Seiten haben die Datei geändert" die beiden `TreeEntryChanges` unterschiedliche `OldOid`s haben und nie gleich wären. Was du semantisch willst, ist: **Schnittmenge über den `Path`** ("Datei unterscheidet sich von *beiden* Eltern ⇒ im Merge angefasst"). Fix z.B.:

```csharp
var pathsInBoth = new HashSet<string>(DiffToParent2.Select(c => c.Path));
ChangesInCommit = DiffToParent1.Where(c => pathsInBoth.Contains(c.Path)).ToList();
DiffExclusiveToParent1 = DiffToParent1.Where(c => !pathsInBoth.Contains(c.Path)).ToList();
// analog für Parent2
```

Deine Tests merken das nicht, weil alle Merge-Tests konfliktfrei sind — dort *ist* die Schnittmenge auch semantisch leer.

### 🔴 B2: `Scope.Update` pflegt die Rückwärts-Map nicht

[Scope.cs:59-68](https://claude.ai/epitaxy/Insight.GitProvider/Scope.cs:59): Bei einem Rename wird nur `_serverPathToId` aktualisiert — **`_idToServerPath[id]` zeigt danach für immer auf den alten Pfad.** Die Klasseninvariante "beide Maps spiegeln sich" ist nach dem ersten Rename gebrochen. Konsumenten sind genau die heiklen Merge-Checks: [GitProvider.cs:295](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:295) (`GetServerPathOrDefault`) und [GitProvider.cs:305](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:305) (`GetServerPath(...) == change.Path`) — letzterer Vergleich liefert nach einem früheren Rename fälschlich `false` und triggert unnötige Tracking-Resets. Fix ist eine Zeile: in `Update` auch `_idToServerPath[id] = toServerPath;` setzen. Verwandt: `Scope.Add` ([Scope.cs:122-123](https://claude.ai/epitaxy/Insight.GitProvider/Scope.cs:122)) macht `Debug.Assert(nicht enthalten)` und ruft dann trotzdem `_serverPathToId.Remove(serverPath)` auf — falls der Pfad im Release doch existiert, bleibt der alte Eintrag in `_idToServerPath` als Leiche zurück. Nutze dort dein eigenes `Remove(serverPath)`, das beide Maps räumt.

### 🟠 B3: Crash *vor* der Warnung, die ihn erklären würde

[GitProvider.cs:70](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:70): `headNode.Scope.GetId(file)` wirft `KeyNotFoundException`, wenn eine getrackte Datei nicht im Scope ist. Genau diese Abweichung ist aber ein *erwarteter* Zustand — dafür existiert `VerifyScope`, das nur Warnungen schreibt. Nur läuft `VerifyScope` erst in Zeile 72, also **nach** dem Crash. `GetIdOrDefault` + Nulls filtern, und die `VerifyScope`-Zeile davor ziehen.

### 🟠 B4: `GetMasterHead` liest die Ref-Datei direkt — bricht bei packed refs

[GitCommandLine.cs:195-205](https://claude.ai/epitaxy/Insight.GitProvider/GitCommandLine.cs:195) liest `.git\refs\heads\<branch>` vom Dateisystem. Nach `git gc` / `git pack-refs` (passiert automatisch!) existiert die lose Ref-Datei nicht mehr → "Can't locate master's head." Bei detached HEAD ebenfalls Exception. Außerdem benutzt `CreateHistory`/`GetGraph` konsequent `repo.Head.Tip.Sha` (LibGit2Sharp), `UpdateHistory` aber `GetMasterHead()` — zwei Wahrheitsquellen für denselben Head. Nimm überall `repo.Head.Tip.Sha`, dann verschwindet die Methode ersatzlos. (Der Name lügt übrigens auch: sie liest den *aktuell ausgecheckten* Branch, nicht master.)

### 🟠 B5: Author-Date statt Committer-Date — entgegen deinem eigenen Kommentar

[GitCommandLine.cs:12](https://claude.ai/epitaxy/Insight.GitProvider/GitCommandLine.cs:12) sagt wörtlich: "Use Committer Date. Otherwise children of a commit may appear before the parent." Aber das `LogFormat` (Zeile 25) benutzt `%ad` und [GitProvider.cs:407](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:407) `commit.Author.When`. Bei Rebases/Cherry-Picks liegen Author-Dates beliebig in der Vergangenheit → die absteigende Sortierung ([GitProvider.cs:187](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:187)) ist falsch. Das ist nicht kosmetisch: `GetArtifactSummary` in [ChangeSetHistory.cs:49](https://claude.ai/epitaxy/Insight.Shared/Model/ChangeSetHistory.cs:49) verlässt sich auf "erste Sichtung = neueste Version" (inkl. finalem Servernamen!), und der `Debug.Assert` in Zeile 84 dort würde es im Debug-Build anzeigen. Besser noch als Committer-Date: Du *hast* im GitProvider bereits die topologische Ordnung (deine BFS) — vergib eine Sequenznummer, statt nach Datum zu sortieren. Datum ist bei Commits in derselben Sekunde (deine Test-Repos!) ohnehin nicht deterministisch.

### 🟡 B6: Reale Repos, die hart scheitern

- **Mehrere Root-Commits** (`git merge --allow-unrelated-histories`, importierte Projekte — gar nicht so selten): `Debug.Assert(initialNodes.Count == 1)` + `First()` in [GitProvider.cs:136](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:136). Der zweite Root wird nie enqueued, der verbindende Merge wartet ewig auf den Scope des unerreichbaren Parents, alles dahinter wird nie verarbeitet → `VerifyScope` wirft "Node has node scope assigned!" (Tippfehler: "has *no* scope"). Fix: alle Roots enqueuen; `ApplyChangesToScope` behandelt `Parents.Count == 0` ja bereits.
- **Octopus-Merges** (>2 Eltern): `IsMerge` prüft `== 2`, `CalculateDiffs` asserted `== 2`. Im Release bleibt der Scope `null` → `NullReferenceException`. Selten, aber existiert in freier Wildbahn (Linux-Kernel-Stil). Mindestens: sauber erkennen und Tracking für die betroffenen Dateien resetten statt crashen.
- **Unbehandelte ChangeKinds:** `ApplyChangesToScope` ([GitProvider.cs:373](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:373)) kennt nur Add/Modify/Delete/Rename. `ChangeKind.TypeChanged` (Datei↔Symlink) läuft im Release stumm durch, dann stirbt `CreateChangeItem` an `deletedServerPathToId[change.Path]` mit kryptischer `KeyNotFoundException`. Der CLI-Parser kennt `T` übrigens ([Parser.cs:241](https://claude.ai/epitaxy/Insight.GitProvider/Parser.cs:241)) — der LibGit2Sharp-Pfad nicht.

### 🟡 Kleinigkeiten

- `VerifyScope` auf dem Head läuft doppelt (Ende `CreateHistory` + `UpdateHistory`) → alle Scope-Warnungen erscheinen doppelt.
- [GitProvider.cs:216-228](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:216): `scope = parent.Scope;` wird dreimal zugewiesen; der `else`-Zweig ist tot. Das Scope-Aliasing (Parent-Scope wiederverwenden, wenn nur ein Kind) ist nach meiner Analyse übrigens *korrekt* — jeder Zweitparent eines Merges hat zwangsläufig ≥2 Kinder und wird geklont — aber das ist ein fragiles, unkommentiertes Argument; genau so etwas gehört in einen deiner (sonst guten) Kommentare.
- `BreakOnHash` ([GitProvider.cs:191](https://claude.ai/epitaxy/Insight.GitProvider/GitProvider.cs:191)) ist Debug-Restmüll; in [Graph.cs](https://claude.ai/epitaxy/Insight.GitProvider/Graph.cs:12) liegen unbenutzte Experimente (`GitNode`, `GraphNode<T>`, `GraphNode.Commit`) mit kaputter Einrückung.
- `Scope.Update` wirft eine nackte `Exception`, wenn der Quellpfad fehlt — inkonsistent zur sonstigen Philosophie "bei Unklarheit resetten statt sterben". Ein gedrifteter Scope (den du per Warnung tolerierst) kann so den ganzen Sync abbrechen.

### Bonus (Basisklasse, betrifft alle drei): Cache seit dem .NET-10-Upgrade wirkungslos

[GitProviderBase.cs:253](https://claude.ai/epitaxy/Insight.GitProvider/GitProviderBase.cs:253): `localFile.FullName.GetHashCode()` im Cache-Dateinamen. Auf modernem .NET ist String-Hashing **pro Prozess randomisiert** — der Export-Cache ("Download if not already in cache") verfehlt nach jedem Neustart und wächst unbegrenzt. Auf .NET Framework war das noch stabil; seit deinem Upgrade nicht mehr. Nimm einen stabilen Hash (z.B. SHA1/XxHash über den Pfad). Außerdem: `ExecuteCommandLine` wirft bei *jeder* stderr-Ausgabe ([GitCommandLine.cs:187](https://claude.ai/epitaxy/Insight.GitProvider/GitCommandLine.cs:187)) — Git schreibt aber auch harmlose Warnungen (CRLF-Hinweise, rename-limit) nach stderr; prüfe stattdessen den ExitCode. Und `CalculateContributionsParallel` ([GitProviderBase.cs:189](https://claude.ai/epitaxy/Insight.GitProvider/GitProviderBase.cs:189)) kann bei zwei nur in Groß-/Kleinschreibung verschiedenen Pfaden an `ToDictionary(ToLowerInvariant())` sterben.

------

## 3. Die anderen beiden Strategien

**GitProviderFileByFile** — konzeptionell die pragmatische Mitte: `git log --follow` pro Datei, dann geteilte Historien wieder wegschneiden. Die `FindSharedHistory`/`DeleteSharedHistory`-Idee ist clever und die Kommentare erklären sie gut. Zwei Punkte:

1. **Echter Concurrency-Bug:** [GitProviderFileByFile.cs:166](https://claude.ai/epitaxy/Insight.GitProvider/GitProviderFileByFile.cs:166) — `_idToLocalFile.Add(...)` läuft im `Parallel.ForEach` *außerhalb* des Locks. `Dictionary.Add` ist nicht threadsicher; das kann sporadisch werfen oder still korrumpieren. Eine Zeile in den Lock ziehen (oder `ConcurrentDictionary`).
2. Strategisch ist sie durch den GitProvider überholt: O(Dateien) Git-Prozesse, und `--follow` bringt eigene Fehlerquellen mit (Simplifizierung, dein dokumentierter Empty-Output-Fall). Als Referenz-/Vergleichsimplementierung behalten ist aber legitim.

**GitProviderNoRenames** — als schnelle Baseline genau richtig (entspricht dem, was code-maat macht). Ein Bug: In [GitProviderNoRenames.cs:71-84](https://claude.ai/epitaxy/Insight.GitProvider/GitProviderNoRenames.cs:71) bekommt nur `IsEdit()` die ID; beim ersten Nicht-Edit (rückwärts = der **Add**) wird das Tracking beendet, *bevor* der Add die ID erhält. `CleanupHistory` wirft ihn dann raus → **jede Datei ist um einen Commit untererfasst, und Dateien mit nur einem Add-Commit verschwinden komplett** (dein eigener `SingleBranch_SingleAdd`-Test würde das für diesen Provider zeigen — er läuft nur gegen `GitProvider`). Außerdem stoppt ein Delete aus irgendeinem gemergten Branch das Tracking einer am Head quicklebendigen Datei stillschweigend. Und: nutzt ebenfalls das fragile `GetMasterHead()` (B4).

------

## 4. Empfehlungen

1. **B1 fixen (Path-basierte Schnittmenge) und B2 (eine Zeile)** — danach NUnit erneut parsen und schauen, wie viele "Reset rename tracking"-Warnungen übrig bleiben. Ich vermute deutlich weniger.
2. Tests ergänzen, die die Lücken treffen: Merge **mit Konflikt**, Datei **im Merge-Commit selbst** hinzugefügt, Rename **vor** Branch + Add derselben Datei im Feature-Branch, Repo mit zwei Roots. Deine Testinfrastruktur (RepoBuilder) ist dafür schon perfekt aufgestellt — die vorhandenen Tests decken nur die konfliktfreien Pfade ab (deshalb sind sie grün, obwohl B1 existiert). Der leere Test `TwoBranches_ModifySameFile_NoConflicts` wartet ja schon.
3. Sortierung auf topologische Sequenz statt Author-Date umstellen (B5) — du hast den Graphen bereits, es ist fast geschenkt.

Gesamteindruck: Das ist durchdachte Arbeit an einem Problem, das härter ist, als es aussieht — die Kommentare zeigen, dass du die Fallstricke verstanden hast. Die gefundenen Bugs sind fast alle vom Typ "stille Annahme, die reale Repos brechen" (Referenz-Equality, packed refs, Rebases), nicht Denkfehler im Algorithmus selbst. Der Kern — Scope-Propagation durch die DAG mit Reset bei Ambiguität — hält.





| Commit    | Fix                                                          |
| :-------- | :----------------------------------------------------------- |
| `5303187` | Testsuite lauffähig (Adapter + deterministischer Branch-Name) |
| `a220212` | **B1:** Merge-Schnittmenge pfadbasiert statt Referenz-Equality; + Regressionstest Konflikt-Merge |
| `c2ff6eb` | **B2:** `Scope.Update`/`Add` halten beide Maps synchron; + Unit-Tests |
| `26ecfd1` | **B3:** `VerifyScope` vor `aliveIds`, `GetIdOrDefault` statt Crash |
| `e681457` | **B4:** `git rev-parse HEAD` statt Ref-Datei lesen (packed refs) |
| `d678898` | **B5a:** Committer- statt Author-Date (`%cd`, `Committer.When`) |
| `2bbd49d` | **B5b:** topologische Ordnung statt Datums-Sortierung; Test-Helper-Annahme korrigiert |
| `1dcd2b7` | **B6a:** mehrere Root-Commits; + Test mit unrelated histories |
| `28799ca` | **B6b:** Octopus-Merges — `Differences` auf N Eltern generalisiert (der Ein-Eltern-Fall fällt als Spezialfall heraus), ID-Suche über alle Quell-Scopes; + Test |
| `a16b0b5` | **B6c:** `TypeChanged`/`Copied`/unbekannte Kinds → tolerantes Verhalten mit Warnung statt Release-Crash; `Warnings`-Init in `GetRawHistory` |
| `a83a63d` | Cleanup: doppeltes `VerifyScope`, `BreakOnHash`, toter `Initialize`-Override, Typo |
| `f5a1218` | **FileByFile:** `_idToLocalFile.Add` in den Lock             |
| `953c97c` | **NoRenames:** Add-Commit bekommt die ID (Untererfassung behoben) |
| `aea3d57` | Export-Cache: stabiler SHA-256-Hash statt randomisiertem `GetHashCode` |
| `9dade4d` | Git-Fehler per Exit-Code statt stderr (mit Opt-out für `diff-index --quiet` und `symbolic-ref -q`, die Exit-Codes als Antwort nutzen) |
| `f988547` | Contribution-Dictionary: `GroupBy` statt `ToDictionary` — **auch im SvnProvider**, dort stand dieselbe Zeile |