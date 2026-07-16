# CleanupTempFiles

Kommandozeilen-Tool (.NET 10), das temporäre Dateien in konfigurierten Verzeichnissen löscht – aber nur, wenn im jeweiligen Verzeichnis eine Marker-Datei liegt, die per Wildcard-Regeln und Alters-Schwellwert definiert, was gelöscht werden darf. Gedacht für den periodischen Aufruf über die Windows-Aufgabenplanung.

## Projektstruktur

- `src/CleanupTempFiles.Core` – Kernlogik (Models, `RuleMatcher`, `DirectoryCleaner`, Loader). Reine Class Library, keine Abhängigkeit von Konsole/CLI.
- `src/CleanupTempFiles` – Konsolen-Host (`Program.cs`, CLI-Parsing, Logging-Setup, `appsettings.json`). Wird als self-contained Single-File-Exe (`win-x64`) veröffentlicht.
- `test/CleanupTempFiles.Tests` – xUnit v3 Tests.

Der Split in Core + Host ist keine Architektur-Spielerei, sondern notwendig: eine self-contained Single-File-Exe kann von keinem anderen Projekt referenziert werden (`NETSDK1151`), das Testprojekt muss daher gegen die Core-Bibliothek testen.

## Regeln

Verbindliche Projekt-Regeln liegen unter [`.agents/rules/`](.agents/rules/):

- [`coding-style.mdc`](.agents/rules/coding-style.mdc) – wie Code hier aussieht (einfach, keine Enterprise-Abstraktionen, kein DI-Container).
- [`testing.mdc`](.agents/rules/testing.mdc) – Teststrategie und Testinfrastruktur.
- [`workflow.mdc`](.agents/rules/workflow.mdc) – autonomes Arbeiten, wann nachgefragt wird, Git-Commit-Verhalten.

## Build & Test

```
dotnet build
dotnet test --project test/CleanupTempFiles.Tests/CleanupTempFiles.Tests.csproj
dotnet publish src/CleanupTempFiles/CleanupTempFiles.csproj -c Release
```

Die Single-File-Exe liegt danach unter `src/CleanupTempFiles/bin/Release/net10.0/win-x64/publish/`, zusammen mit der editierbaren `appsettings.json`.
