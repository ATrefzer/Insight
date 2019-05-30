Insight.Metrics

Das Modul Insight.Metrics erlaubt es auf einem Verzeichnis oder einer einzelnen Datei Codemetriken zu berechnen.

Unterstützt werden

- Lines of code
- Inverted space metric

Beide Metriken werden nach außen über MetricProvider Facade bereitgestellt.

![](D:\Git Repositories\Insight\Doc\assets\Insight.Metrics.png)

Refactorings:

- Die einzelnen Metriken werden nicht mehr direkt von außen zugegriffen.
- Die Lines of Code Metrik bekommt nun den Pfad zum Cloc reingereicht, anstatt selber den relativen Pfad zu kennen. Das Ermitteln des Pfades übernimmt der MetricProvider

TODO Zugriff auf anderes Modul nur über Interface(!) Aber wer legt dann die Instanz an?





# FAQ

Verarbeitungskette



Farben

Damit die Farben stabil bleiben, egal in welcher Reihenfolge man die Analysen durchführt, werden die Farben in einer Konfigurationsdatei gespeichert.

Diese Date wird beim ersten Cache Aufbau erstellt. Ab dann wird sie nie wieder gelöscht. Fall Entwickler neu hinzukommen, werden diese zum Farbschema hinzugefügt und die Datei neu geschrieben.

Grundlage sind alle Entwickler, die in der Historie zu finden sind. Sortiert nach deren Auftreten (Commits)

Das ist die Grundlage um später einen Farbeditor schreiben zu können.

Speichert man ein Bild ab, so wird nun auch ein eigenes Farbschema unter dem Dateinamen *.bin.colors mitgespeichert. Somit hat man nach dem Laden die gleichen Farben wieder zur Verfügung.

Aber Achtung: Wenn man die Arbeit pro Datei aus dem Kontextmenü heraus analysiert verlässt man das gespeicherte Bild und verwendet das normale Farbschema, das aus der Historie erstellt wrude.





