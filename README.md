# 🏛️ Amtsschimmel — Das Behörden-Incremental

[![CI](https://github.com/Kroste/Amtsschimmel/actions/workflows/ci.yml/badge.svg)](https://github.com/Kroste/Amtsschimmel/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/Kroste/Amtsschimmel)](https://github.com/Kroste/Amtsschimmel/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Ein Incremental Game in C# / .NET 10 / Avalonia 12. Stemple Formulare, stelle Praktikanten,
Sachbearbeiter und ganze Dezernate ein — und wenn der Laden läuft: Verwaltungsreform!

## Features

- 📋 **Klicken:** Manuell stempeln, Klickkraft per „Stempelkissen"-Upgrade verdoppeln
- 🏢 **10 Generatoren:** Vom Praktikanten bis zur KI-Verwaltungscloud (Kosten ×1,15 pro Einheit)
- 📜 **Prestige:** Verwaltungsreform → Paragraphen (je +5 % Produktion, dauerhaft). Schwelle verzehnfacht sich pro Reform; Top-Content (Ministerium, KI-Cloud, Exzellenz-Forschung) schaltet erst nach bestimmten Reformen frei
- 🖨️ **Auto-Stempeln:** Forschbarer „Pneumatischer Stempelautomat" — bis 10 automatische Stempelklicks/s, wirkt auch offline
- 🤖 **Auto-Buyer:** Pro Generator kaufbar, überlebt Reformen, per Schalter steuerbar
- 📚 **Forschung:** 18 mehrstufige Fortbildungen der „Verwaltungsakademie" — Multiplikatoren, Kostenrabatte, Offline-Boosts, Paragraphen-Boni, inkl. endlosem „Bürokratieabbau". Verfallen bei Reformen!
- 🏆 **20 Achievements:** Je +1 % Produktion, mit Toast-Benachrichtigung
- 💤 **Offline-Fortschritt:** 50 % Effizienz, max. 8 h, Bericht beim Start
- 💾 **Autosave:** Alle 30 s + beim Beenden, atomares JSON in `%APPDATA%/Amtsschimmel`

- 🏅 **Meilensteine:** Endlose Beförderungs-Schwellen (10, 25, 50, 100, 250, 500, 1.000, …) — jede verdoppelt die Produktion des Generators
- ✨ **Goldene Formulare:** Zufällig auftauchendes Klick-Event — Sofort-Stempel oder ×7-Produktionsboost
- 📰 **Amtsblatt:** Satirischer Nachrichten-Ticker, der auf den Spielstand reagiert
- 📈 **Statistik:** Lifetime-Werte und Einkommensverlauf als Kurve
- 🔄 **Update-Check:** Prüft GitHub-Releases (proxy-fähig), beim Start und manuell
- ⚙️ **Einstellungen:** Autosave-Intervall, Ticker, Spielstand-Export/-Import als Text, Reset
- ⌨️ **Hotkeys:** Leertaste stempelt, Tasten 1–5 wechseln die Tabs
- 🏆 **Siegesbedingung:** Der Goldene Aktendeckel — alle Fortbildungen, 10 Reformen und 1 Trill. Stempel; danach Endlosmodus

## Lizenz

MIT — siehe [LICENSE](LICENSE).

## Starten

```bash
dotnet run --project Amtsschimmel
```

## Tests

```bash
dotnet test
```
