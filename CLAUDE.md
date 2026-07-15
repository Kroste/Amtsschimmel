# Amtsschimmel — Das Behörden-Incremental

## Grundlagen

- **Was:** Incremental/Idle-Game im deutschen Behörden-Setting. Währung: **Stempel**. Prestige-Währung: **Paragraphen (§)**.
- **Stack:** C# / .NET 10 / Avalonia 12 (≥ 12.0.4), CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, NLog, xUnit.
- **Struktur:** Flache Projektstruktur (kein `src/`), `.slnx`-Solution, `Directory.Build.props` mit `TreatWarningsAsErrors`.
- **Konventionen:** Compiled Bindings (`AvaloniaUseCompiledBindingsByDefault`), Logs & Savegame unter `%APPDATA%/Amtsschimmel` bzw. `~/.config/Amtsschimmel`.
- Kommunikation auf Deutsch, informelles „du".

## Aktueller Stand (v1.0.0)

- **Kern-Loop:** 10 Ticks/s via `DispatcherTimer`, Delta-Zeit-basiert (robust gegen Jitter).
- **Generatoren:** 10 Stück (Praktikant → KI-Verwaltungscloud), Kostenwachstum ×1,15 pro Einheit, Bulk-Kauf ×10 (geometrische Reihe), progressive Sichtbarkeit (ab 40 % der Basiskosten erspielt).
- **Prestige („Verwaltungsreform"):** ab 1 Mio. verdienter Stempel; Paragraphen = ⌊√(verdient/1e6)⌋; je § +5 % Produktion dauerhaft. Auto-Buyer & Achievements überleben Resets.
- **Auto-Buyer:** pro Generator kaufbar (250× Basiskosten), per ToggleSwitch schaltbar, kauft 1×/Tick wenn bezahlbar.
- **Achievements:** 17 Stück, je +1 % Produktion, Toast-Benachrichtigung, verdeckt bis Freischaltung.
- **Klick-Upgrade („Stempelkissen"):** Klickkraft = 2^Stufe, Kosten ×12 pro Stufe.
- **Offline-Fortschritt:** 50 % Effizienz, Cap 8 h, Banner beim Start.
- **Persistenz:** JSON-Savegame (atomares Schreiben via tmp+move), Autosave 30 s + bei Exit, korrupte Saves werden gesichert statt gelöscht.
- **Tests:** 15 xUnit-Tests für Engine-Logik (Ökonomie, Prestige, Auto-Buyer, Offline, Formatter).

## Roadmap

- Balancing-Pass (Generator-Kurven, Prestige-Formel) — erst nach Feature-Vollständigkeit.
- Weitere Upgrade-Kategorien (generator-spezifische Multiplikatoren, „Goldene Formulare" als Random-Events).
- Statistik-Tab (Lifetime-Werte, Diagramme).
- System-Tray-Minimierung (Muster aus Checkmk Cockpit übernehmbar).
- Optional: Cloud-Save / Export-Import des Spielstands als Base64.

## Referenz

- **Kostenformel:** `cost = base × 1.15^owned`; Bulk: geometrische Reihe.
- **Produktionsformel:** `baseProd × owned × (1 + §×0.05) × (1 + achievements×0.01)`.
- **Engine ist UI-frei** (`Services/GameEngine.cs`) — alle Logikänderungen dort, ViewModels nur Anzeige/Commands.
- Zahlformatierung: `NumberFormatter` (deutsche Suffixe Tsd./Mio./Mrd./Bio./Brd./Trill., ab 1e21 wissenschaftlich).
- Version in `Directory.Build.props` bei jeder Änderung erhöhen.
- **CI:** `.github/workflows/ci.yml` — Build + Tests bei Push/PR auf `main`, NuGet-Cache, TRX-Testresultate als Artefakt. Actions auf Node-24-Stand (checkout@v6, setup-dotnet@v5, cache@v5, upload-artifact@v5).
- **Release:** `.github/workflows/release.yml` — bei `v*`-Tag: Tests, self-contained Single-File-Publish für win-x64 (zip) + linux-x64 (tar.gz), GitHub Release via softprops/action-gh-release@v2. Version wird aus dem Tag abgeleitet (`-p:Version=`), **kein MinVer** — bewusst, damit Builds ohne Git-Repo (z. B. entpacktes Archiv) mit `TreatWarningsAsErrors` nicht scheitern.
- **Dependabot:** wöchentlich (NuGet, Avalonia-Pakete gruppiert + GitHub Actions).
- **VS Code:** `.vscode/tasks.json` (build/test/watch/clean/publish-win-x64/publish-linux-x64), `.vscode/launch.json` (Debug/Release/Attach, Typ `coreclr`).
