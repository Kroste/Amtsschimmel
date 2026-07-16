# Amtsschimmel — Das Behörden-Incremental

## Grundlagen

- **Repo:** PUBLIC auf github.com/Kroste/Amtsschimmel, Lizenz: MIT (LICENSE im Root).
- **Was:** Incremental/Idle-Game im deutschen Behörden-Setting. Währung: **Stempel**. Prestige-Währung: **Paragraphen (§)**.
- **Stack:** C# / .NET 10 / Avalonia 12 (≥ 12.0.4), CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, NLog, xUnit.
- **Struktur:** Flache Projektstruktur (kein `src/`), `.slnx`-Solution, `Directory.Build.props` mit `TreatWarningsAsErrors`.
- **UI-Rahmen (Projektstandard):** Alle Fenster erben von `Views/ChromeWindow` (`WindowDecorations.BorderOnly` — NICHT `None`, sonst fehlen native Resize-Griffe — plus `ExtendClientAreaToDecorationsHint`, `CanResize = true`, `Background = null` → Root-Border liefert Hintergrund). Eigene Titelleiste im MainWindow mit ⓘ Info / — / ☐ / ✕ (Commands im VM), Drag via `BeginMoveDrag`, Doppelklick = Maximieren. InfoBox (`Views/InfoWindow` + `InfoViewModel`): Name, Version aus `AssemblyInformationalVersion`, GitHub- und ☕-Buy-me-a-coffee-Button (URL-Öffnung: Avalonia-Launcher mit Prozess-Fallback). Platzhalter-URLs ggf. ersetzen.
- **Konventionen:** Compiled Bindings (`AvaloniaUseCompiledBindingsByDefault`), Logs & Savegame unter `%APPDATA%/Amtsschimmel` bzw. `~/.config/Amtsschimmel`.
- Kommunikation auf Deutsch, informelles „du".

## Aktueller Stand (v1.9.2)

- **Kern-Loop:** 10 Ticks/s via `DispatcherTimer`, Delta-Zeit-basiert (robust gegen Jitter).
- **Generatoren:** 10 Stück (Praktikant → KI-Verwaltungscloud), Kostenwachstum ×1,15 pro Einheit, Bulk-Kauf ×10 (geometrische Reihe), progressive Sichtbarkeit (ab 40 % der Basiskosten erspielt).
- **Prestige („Verwaltungsreform"):** Schwelle wächst ×10 pro Reform (1e6 → 1e7 → …, `CurrentPrestigeThreshold`); Paragraphen = ⌊√(verdient/Schwelle) × Forschungsbonus⌋; je § +5 % Produktion dauerhaft. Auto-Buyer & Achievements überleben Resets.
- **Reform-Gating:** `MinReformen` an Generator- und Forschungsdefinitionen — Bundesministerium (1), KI-Cloud (2), Stempelautomat (1), lean_admin (1), ki_sachbearbeitung/verwaltungsexzellenz (2), buerokratieabbau (3). UI zeigt 🔒-Hinweis; Engine blockt Kauf in `BuyGenerator`/`CanResearch`.
- **Auto-Stempeln:** Forschung „Pneumatischer Stempelautomat" (Effekttyp `AutoClick`, additiv je Stufe, max. 10 Klicks/s). Erträgt Klickkraft × Rate, zählt NICHT als manueller Klick (Klick-Achievements!), wirkt auch offline (`EffectiveIncomePerSecond`). Visuell: blaue "+X"-Partikel (Timer 250 ms im Code-Behind, visuell auf 4/s gedeckelt — bei höherer Rate trägt jedes Partikel den aggregierten anteiligen Betrag, damit die angezeigte Summe stimmt; pausiert bei minimiertem Fenster).
- **Auto-Buyer:** pro Generator kaufbar (250× Basiskosten), per ToggleSwitch schaltbar, kauft 1×/Tick wenn bezahlbar.
- **Forschung ("Verwaltungsakademie"):** 18 Fortbildungen (inkl. Lars' Matrixorganisation für Fachbereich/Dezernat und Föderalismusreform für Rathaus/Landesbehörde — schließen die Mittelbau-Boost-Lücke) mit Voraussetzungsbaum, **mehrstufig**: `MaxLevel` (1 = einmalig, n = wiederholbar, 0 = endlos, z. B. "Bürokratieabbau" ×1,1 je Stufe), Kosten je Stufe ×`CostGrowth` (Standard ×8). Effekte stapeln multiplikativ je Stufe (`value^level` bzw. `(1−v)^level` bei Rabatten, `(1+v)^level` bei Paragraphen-Boni). Verfallen bei Reformen. 7 Effekttypen: Generator-/Global-/Klick-Multiplikator, Kostenreduktion, Offline-Effizienz (50→75 %), Offline-Cap (8→24 h), Paragraphen-Bonus.
- **Achievements:** 23 Stück (Klick-Achievements zählen nur manuelle Klicks), je +1 % Produktion, Toast-Benachrichtigung, verdeckt bis Freischaltung.
- **Klick-Upgrade („Stempelkissen"):** Klickkraft = 2^Stufe, Kosten ×12 pro Stufe.
- **Offline-Fortschritt:** 50 % Effizienz, Cap 8 h, Banner beim Start.
- **Animationen ("Juice"):** Schwebende "+X"-Klickzahlen (Code-Behind `OnStampClick`, `Animation` auf `Canvas.Top` + `Opacity`, Spam-Schutz bei >30 Partikeln), Stempel-Button-Press (`scale(0.94)` via `:pressed` + `TransformOperationsTransition`), Toast als Overlay unten mittig mit Slide-in/Fade (Klassen-Trick: `Classes.visible="{Binding …}"` + Transitions, verschiebt Layout nicht mehr), pulsierender Reform-Button (Style-Animation `INFINITE` auf Klasse `.ready`; **Falle:** Keyframe-Animationen haben keinen Animator für `RenderTransform` → `ScaleTransform.ScaleX/ScaleY` animieren, `RenderTransform`-Strings nur in Transitions/Settern verwenden), Fortschrittsbalken zur Reform-Schwelle (`PrestigeProgress` 0..1).
- **Meilensteine ("Beförderungen"):** ENDLOSE Folge im 1-2,5-5-Dekadenmuster (`GameDefinitions.MilestoneSequence()`: 10, 25, 50, 100, 250, 500, 1.000, 2.500, …) → je ×2 Produktion (`MilestoneMultiplierFor`), Event `MilestoneReached` (long) beim Überschreiten (auch bei Bulk-Käufen), Toast + Anzeige der nächsten Schwelle in der Generatorkarte.
- **Goldene Formulare:** Spawn-Chance alle 30 s (~17 %, ≈ alle 3 Min.), 12 s sichtbar, zufällige Position (Code-Behind), pulsierender Button-Overlay. Belohnung 50/50: Sofort-Stempel (max(500, 90 s Einkommen + 5 % Kontostand)) oder Buff "Erlassflut" ×7 für 30 s (`ActivateProductionBuff`, nicht persistiert). Zähler `GoldenFormsClicked` persistiert.
- **Amtsblatt-Ticker:** `AmtsblattService` (18 generische + 8 zustandsabhängige Postillon-Meldungen, 40 % konditional wenn verfügbar, keine Direktwiederholung), Statuszeile unten mit Ticker links + "💾 gespeichert vor X s" rechts, Wechsel alle 25 s.
- **Statistik-Tab:** Lifetime-Werte (Spielzeit, Klicks, Rekord-Einkommen, Goldene Formulare …) + Einkommens-Sparkline (Polyline, 60 Samples à 5 s = 5 Min., Normalisierung im VM via `Avalonia.Points`).
- **Update-Check:** `UpdateCheckService` gegen GitHub-Releases-API (User-Agent Pflicht!), proxy-fähig via `HttpClientHandler.DefaultProxyCredentials` (Checkmk-Muster gegen 407 hinterm Unternehmens-Proxy). Beim Start (abschaltbar, 3 s verzögert, Toast) + manuell in der InfoBox. `ParseVersion` normalisiert v-Präfix/Prerelease/Metadaten. Wirft nie — offline/Rate-Limit wird nur geloggt.
- **Einstellungen:** `SettingsService` (settings.json in AppData, `Changed`-Event für Live-Übernahme, Test-Konstruktor mit Verzeichnis-Override). SettingsWindow (⚙ in Titelleiste): Autosave-Intervall (10–300 s, NumericUpDown), Ticker an/aus, Update-Check an/aus, Spielstand-Reset mit Zwei-Klick-Bestätigung.
- **Save-Transfer:** `SaveGameService.Export/TryImport` — Base64(UTF8-JSON) mit Präfix `AMT1:` als Format-Kennung. Kopieren via `ClipboardExtensions.SetTextAsync` (Avalonia 12: Extension in `Avalonia.Input.Platform`, using nicht vergessen!).
- **Hotkeys:** Leertaste = Stempeln (mit Partikel; Button-Fokus setzt e.Handled → kein Doppelklick), 1–5 bzw. NumPad = Tab-Wechsel (`MainTabs.SelectedIndex`).
- **App-Icon:** `Assets/amtsschimmel.png` (generiert), csproj um `<AvaloniaResource Include="Assets\**"/>` ergänzt, Icon zentral im ChromeWindow-Konstruktor via `AssetLoader` (try/catch).
- **Release-Workflow (NetScanner-/Checkmk-Muster):** Drei Jobs — `build-windows` (windows-latest, nativ: Publish → ZIP via Compress-Archive), `build-linux` (Publish → tar.gz + AppImage via `packaging/linux/build-appimage.sh`), `release` (nur bei Tag: sammelt Artifacts via download-artifact, softprops/action-gh-release mit `fail_on_unmatched_files`). Beide Build-Jobs laden ihre Pakete als **Artifacts** hoch (7 Tage) — Download-Pakete gibt es also bei JEDEM Lauf, auch via `workflow_dispatch` ohne Tag (Version dann `0.0.0-dev.<run>`); der Release-Job wird dabei übersprungen. Tests laufen in beiden Build-Jobs vor dem Publish.
- **Release-Automation:** `scripts/release.sh` + `scripts/release.ps1` (Version aus `Directory.Build.props`, prüft uncommittete/ungepushte Änderungen, Tag-Kollision mit Rückfrage, annotierter Tag + Push). Pure ASCII (PowerShell-5.1-ANSI-Falle), `$PSNativeCommandUseErrorActionPreference = $false` gegen pwsh-7.4-Abbrüche bei git-Exit-Codes. VS-Code-Tasks: `release (tag + push)` und `clean-hard`.
- Avalonia-12-Falle: `Watermark` heißt jetzt `PlaceholderText`.
- **Siegesbedingung ("Verwaltungsvollendung"):** Finales Kaufziel "Goldener Aktendeckel" im Reform-Tab. Bedingungen (Checkliste live): alle Fortbildungen ≥ Stufe 1, ≥ 10 Reformen (`VictoryMinReformen`), 1e18 Stempel (`VictoryCost`). Kauf → `GameEngine.Win()` setzt `HasWon`/`WonAtUtc`/`WonAfterPlaySeconds`, öffnet `VictoryWindow` (Sieg-Statistiken, "Weiterstempeln"). Danach Endlosmodus — Reformen etc. laufen weiter, Sieg bleibt. Achievement Nr. 23 "Der Amtsschimmel". double-Falle in Tests: `1e18 − 1 == 1e18` (ULP ≈ 128).
- **Persistenz:** JSON-Savegame (atomares Schreiben via tmp+move), Autosave 30 s + bei Exit, korrupte Saves werden gesichert statt gelöscht.
- **Tests:** 66 xUnit-Tests (inkl. Export/Import-Roundtrip, Versions-Parsing, Settings-Roundtrip, SettingsWindow-Smoke-Test): Engine + UI-Smoke-Tests (`UiSmokeTests` via `Avalonia.Headless` 12.0.5 + `HeadlessUnitTestSession` — Avalonia.Headless.XUnit 12.x braucht xunit v3 und kollidiert mit xunit 2.9.x, daher die Session-Variante; Session ist statisch geteilt, da Avalonia nur einmal pro Prozess initialisiert werden darf). Fangen XAML-Populate-Fehler in CI ab. (inkl. Baum-Integritätstests: alle Prerequisite-/Target-Ids müssen existieren) für Engine-Logik (Ökonomie, Prestige, Auto-Buyer, Offline, Formatter).

## Roadmap

- WICHTIG (Prozess): Vor dem Einspielen von Claude-ZIPs immer den Repo-Stand abgleichen — v1.8.2 (Matrixorg/Föderalismus, von Lars) wurde einmal von einem ZIP überschrieben und musste aus der Git-Historie restauriert werden (v1.9.2).
- Balancing-Beobachtung (Realdaten): 7 Reformen ≈ 200 Paragraphen bei komplett erforschtem Baum — Overshoot pro Run funktioniert wie beabsichtigt. Sieg ist auf Reform 10 kalibriert (Schwelle dort 1e16, Aktendeckel 1e18 = ×100). Kein akuter Handlungsbedarf; Feintuning erst nach Feedback zum Endgame.
- Optional: Soundeffekte (Stempelgeräusch) via minimaler Audio-Lib — bewusst noch nicht drin.
- Forschungsbaum ggf. visuell als Graph statt Liste darstellen.
- System-Tray-Minimierung (Muster aus Checkmk Cockpit übernehmbar).
- InfoWindow-URLs (GitHub-Repo, BMC-Handle) verifizieren.
- Update-Check könnte künftig das passende Release-Asset direkt herunterladen (Client-Update wie im Checkmk Cockpit).
- Optional: Cloud-Save / Export-Import des Spielstands als Base64.

## Referenz

- **Kostenformel:** `cost = base × 1.15^owned`; Bulk: geometrische Reihe.
- **Produktionsformel:** `baseProd × owned × (1 + §×0.05) × (1 + achievements×0.01)`.
- **Forschung:** `Models/ResearchDefinitions.cs` (Baum) + Effektauswertung in `GameEngine` (`ResearchMultiplierFor`, `CostFactor`, `OfflineEfficiency`, `OfflineCap`, `ParagraphMultiplier`). Levels in `GameState.ResearchLevels` (Dictionary Id→Level); altes `ResearchedIds`-Set (≤ v1.1.0) wird in `GameEngine.LoadState` migriert und darf nicht entfernt werden, solange alte Saves existieren können. Neue Effekttypen im Enum `ResearchEffectType` ergänzen.
- **Farb-Emojis:** `Program.BuildAvaloniaApp` registriert einen expliziten `FontFallback` auf den System-Emoji-Font (Windows: Segoe UI Emoji, Linux: Noto Color Emoji, macOS: Apple Color Emoji). Achtung: das `.With(new FontManagerOptions…)` ersetzt die Options von `WithInterFont()`, deshalb muss `DefaultFamilyName = "fonts:Inter#Inter"` dort erneut gesetzt werden. Unter Linux muss Noto Color Emoji installiert sein (Bazzite: vorhanden).
- **Engine ist UI-frei** (`Services/GameEngine.cs`) — alle Logikänderungen dort, ViewModels nur Anzeige/Commands.
- Zahlformatierung: `NumberFormatter` (deutsche Suffixe Tsd./Mio./Mrd./Bio./Brd./Trill., ab 1e21 wissenschaftlich).
- Version in `Directory.Build.props` bei jeder Änderung erhöhen.
- **CI:** `.github/workflows/ci.yml` — Build + Tests bei Push/PR auf `main`, NuGet-Cache, TRX-Testresultate als Artefakt. Actions auf Node-24-Stand (checkout@v6, setup-dotnet@v5, cache@v5, upload-artifact@v5).
- **Release:** `.github/workflows/release.yml` — bei `v*`-Tag: Tests, self-contained Single-File-Publish für win-x64 (zip) + linux-x64 (tar.gz), GitHub Release via softprops/action-gh-release@v2. Version wird aus dem Tag abgeleitet (`-p:Version=`), **kein MinVer** — bewusst, damit Builds ohne Git-Repo (z. B. entpacktes Archiv) mit `TreatWarningsAsErrors` nicht scheitern.
- **Dependabot:** wöchentlich (NuGet, Avalonia-Pakete gruppiert + GitHub Actions).
- **VS Code:** `.vscode/tasks.json` (build/test/watch/clean/publish-win-x64/publish-linux-x64), `.vscode/launch.json` (Debug/Release/Attach, Typ `coreclr`).
