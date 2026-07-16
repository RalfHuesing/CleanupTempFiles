# CleanupTempFiles

Kommandozeilen-Tool zum Aufräumen temporärer Dateien in einer Reihe konfigurierter Verzeichnisse – z. B. `%TEMP%` der einzelnen Benutzerprofile unter `C:\Users` oder `C:\Windows\Temp`.

Damit nichts versehentlich gelöscht wird, fasst das Tool nur Verzeichnisse an, in denen explizit eine **Marker-Datei** liegt. Diese Marker-Datei definiert per Wildcard-Muster und Alters-Schwellwert, welche Dateien unter welchen Bedingungen gelöscht werden dürfen.

Gedacht für den periodischen Aufruf über die Windows-Aufgabenplanung.

## Funktionsweise

1. In `appsettings.json` (liegt neben der `.exe`) steht eine Liste von Verzeichnissen, die das Tool überhaupt betrachtet.
2. Für jedes dieser Verzeichnisse wird nach einer Marker-Datei (Standardname `.cleanuptempfiles.json`) gesucht.
3. **Keine Marker-Datei vorhanden → Verzeichnis wird komplett übersprungen.** Es wird nie etwas gelöscht, ohne dass im Zielverzeichnis explizit "erlaubt" wurde.
4. Ist eine Marker-Datei vorhanden, wird sie gelesen: Sie enthält eine Liste von Regeln (Wildcard-Muster + Mindestalter) und ein `recursive`-Flag.
5. Für jede gefundene Datei wird die **erste** Regel angewendet, deren Muster passt (Reihenfolge in der Marker-Datei = Priorität). Ist die Datei mindestens so alt wie der Schwellwert dieser Regel, wird sie gelöscht.
6. Ohne `--execute` läuft das Tool immer nur als **Dry-Run**: Es protokolliert, was es löschen würde, löscht aber nichts.

## appsettings.json

```json
{
  "markerFileName": ".cleanuptempfiles.json",
  "directories": [
    "C:\\Users\\ralf\\AppData\\Local\\Temp",
    "C:\\Windows\\Temp"
  ],
  "deniedDirectories": [
    "C:\\Windows",
    "C:\\Program Files",
    "C:\\Program Files (x86)",
    "C:\\ProgramData",
    "C:\\Users"
  ],
  "runTimeoutMinutes": 30
}
```

- `markerFileName` – Name der Marker-Datei, nach der in jedem der `directories` gesucht wird.
- `directories` – feste Liste der zu betrachtenden Verzeichnisse (keine Wildcards/Platzhalter). Müssen absolute Pfade sein. Ist ein Verzeichnis nicht erreichbar oder fehlerhaft, wird nur dieses übersprungen – die übrigen werden trotzdem bearbeitet.
- `deniedDirectories` – zusätzliche Sperrliste gegen Konfigurationsfehler: Ist eines der konfigurierten `directories` (exakt, nicht als Unterordner) in dieser Liste, weigert sich das Tool schon beim Start, die appsettings.json zu laden. Optional – ohne diesen Schlüssel gelten die oben gezeigten Standardwerte. **Laufwerkswurzeln (`C:\`, `D:\`, …) sind zusätzlich immer gesperrt, unabhängig von dieser Liste.** Ein Unterordner wie `C:\Windows\Temp` bleibt davon unberührt und ist weiterhin ein gültiges Ziel.
- `runTimeoutMinutes` – bricht einen hängenden Lauf (z. B. durch ein nicht antwortendes Netzlaufwerk) nach dieser Zeit ab, damit die Mutex-Sperre nicht dauerhaft blockiert. Optional, Standard 30 Minuten. Muss > 0 sein.

## Marker-Datei

Liegt direkt im jeweiligen Zielverzeichnis, z. B. `C:\Windows\Temp\.cleanuptempfiles.json`:

```json
{
  "recursive": true,
  "rules": [
    { "pattern": "*.pdf", "olderThan": "2.00:00:00" },
    { "pattern": "*.tmp", "olderThan": "00:10:00" },
    { "pattern": "*.*",   "olderThan": "1.00:00:00" }
  ],
  "exclude": [ "important.log" ]
}
```

- `recursive` – `true`: Regeln gelten für den gesamten Unterbaum (auch ohne eigene Marker-Datei in den Unterverzeichnissen). `false`: nur Dateien direkt im Verzeichnis. Symlinks/Junctions werden dabei nicht verfolgt – ein rekursiver Lauf verlässt das Zielverzeichnis nie. Bei `--execute` werden zusätzlich Unterverzeichnisse entfernt, die nach dem Aufräumen leer sind (kaskadierend, auch mehrstufig) – das Zielverzeichnis selbst (mit der Marker-Datei) wird dabei nie angefasst. Im Dry-Run passiert das bewusst nicht: Da Dateien dort nie wirklich gelöscht werden, wäre eine Vorschau nur eine unvollständige Untergrenze.
- `rules` – Liste von Regeln, Reihenfolge = Priorität. Für jede Datei gilt die *erste* Regel, deren `pattern` passt.
  - `pattern` – Wildcard-Ausdruck (`*`, `?`), z. B. `*.pdf`. `*.*` ist als Spezialfall "alle Dateien" zu verstehen (auch ohne Dateiendung), wie unter Windows historisch üblich.
  - `olderThan` – Mindestalter (Schreibdatum) im .NET-`TimeSpan`-Format `d.hh:mm:ss`, z. B. `00:10:00` (10 Minuten) oder `2.00:00:00` (2 Tage). Muss ≥ 0 sein.
- `exclude` – optionale Liste von Wildcard-Mustern (gleiche Syntax wie `pattern`). Eine Datei, die auf ein `exclude`-Muster passt, wird **nie** gelöscht, egal welche `rules` sonst zutreffen würden. Prüfung erfolgt nur gegen den Dateinamen, nicht rekursiv gegen Unterverzeichnisnamen.
- Es werden auch versteckte Dateien und Dateien mit System-Attribut erfasst – im Temp-Verzeichnis üblich, würden sonst nie aufgeräumt.
- Die Marker-Datei selbst wird nie gelöscht, auch wenn eine Regel wie `*.*` darauf passen würde.
- Fehlt ein Pflichtfeld oder ist ein Wert ungültig (z. B. negatives `olderThan`, leeres `exclude`-Muster), wird die gesamte Marker-Datei als fehlerhaft verworfen und das Verzeichnis übersprungen – lieber nichts tun als etwas Falsches.

## CLI

```
CleanupTempFiles.exe             # Dry-Run: protokolliert nur, löscht nichts
CleanupTempFiles.exe --execute   # löscht tatsächlich
CleanupTempFiles.exe --validate  # prüft appsettings.json + alle Marker-Dateien, löscht/simuliert nichts
```

`--execute` und `--validate` schließen sich gegenseitig aus.

Am Ende eines Dry-Run- oder Execute-Laufs protokolliert das Tool eine Zusammenfassung, z. B.:

```
Lauf abgeschlossen: 12 Verzeichnis(se) verarbeitet, 143 Datei(en) gelöscht (52428800 Bytes), 4 leere Verzeichnis(se) entfernt, 1 Verzeichnis(se) mit Problemen.
```

### Exit Codes

| Code | Bedeutung |
| --- | --- |
| `0` | Alles ok |
| `1` | Konfigurationsfehler (appsettings.json ungültig, Denylist-Verstoß) oder unerwarteter Fehler; bei `--validate` auch: mindestens eine Marker-Datei fehlerhaft |
| `2` | Es läuft bereits eine andere Instanz |
| `3` | Lauf abgeschlossen, aber mit Teilfehlern (mindestens ein Verzeichnis fehlte, hatte eine fehlerhafte Marker-Datei oder eine Datei konnte nicht gelöscht werden) |
| `4` | Zeitüberschreitung (`runTimeoutMinutes`) – der Lauf wurde abgebrochen |

## Windows-Aufgabenplanung

Aufgabe anlegen, die periodisch `CleanupTempFiles.exe --execute` ausführt. Läuft eine vorherige Instanz noch (z. B. bei sehr kurzen Intervallen und vielen Dateien), bricht die neue Instanz sofort mit Exit Code `2` ab, statt parallel zu laufen. `CleanupTempFiles.exe --validate` eignet sich, um appsettings.json/Marker-Dateien nach einer Config-Änderung schnell zu prüfen, ohne auf den nächsten geplanten Lauf zu warten.

## Build

Voraussetzung: .NET 10 SDK.

```
dotnet build
dotnet test --project test/CleanupTempFiles.Tests/CleanupTempFiles.Tests.csproj
dotnet publish src/CleanupTempFiles/CleanupTempFiles.csproj -c Release
```

Das Ergebnis von `dotnet publish` liegt unter `src/CleanupTempFiles/bin/Release/net10.0/win-x64/publish/`: eine einzelne, self-contained `CleanupTempFiles.exe` (kein .NET auf dem Zielsystem nötig, keine losen DLLs) plus die editierbare `appsettings.json`.

## Logs

Serilog schreibt nach `logs/log-YYYYMMDD.txt`, relativ zum Verzeichnis der `.exe`, mit täglichem Rollover und 14 Tagen Aufbewahrung.

## Projektstruktur

Siehe [CLAUDE.md](CLAUDE.md) und die Regeln unter [.agents/rules/](.agents/rules/).
