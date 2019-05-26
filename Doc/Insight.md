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