# Flipit

**Flipit** is a lightweight Windows background utility that fixes text typed using the wrong keyboard layout.

## What it does

Press **F1** while text is selected (or anywhere on a line) and Flipit instantly converts the text between Hebrew and English keyboard layouts.

**Examples:**
- `יקךךם` → `hello`
- `ghbd` → `עבר`

## Features

- ⚡ Global F1 hotkey — works in any application
- 🔄 Auto-detects direction (Hebrew→English or English→Hebrew)
- 📋 No selection? Automatically selects the current line
- 🔕 No popups, no dialogs — silent & instant
- 🖥️ System tray icon with enable/disable toggle
- 🚀 Optional startup with Windows
- 💡 Near-zero CPU/memory when idle

## Supported Applications

Works in: Notepad, Chrome, Edge, Word, VSCode, IntelliJ IDEA, Slack, Teams, WhatsApp Desktop, and virtually any Windows application.

## Requirements

- Windows 10/11 (x64)
- .NET 8 Runtime (Windows Desktop)

## Building

```bash
dotnet build src/Flipit.App/Flipit.App.csproj
```

## Publishing (single-file executable)

```bash
dotnet publish src/Flipit.App/Flipit.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

## Architecture

```
/src/Flipit.App
  /Core            - Interfaces and orchestrator
  /KeyboardEngine  - Text conversion + keyboard simulation
  /Clipboard       - Win32 clipboard access
  /Hooks           - Global hotkey registration
  /Tray            - System tray + message window
  /Settings        - Persistence (Windows registry)
  /Infrastructure  - Win32 P/Invoke + DI composition root
```

## Keyboard Layout Mapping

Standard Hebrew keyboard layout (physical key positions):

| EN | HE | EN | HE |
|----|----|----|-----|
| e  | ק  | a  | ש  |
| r  | ר  | s  | ד  |
| t  | א  | d  | ג  |
| y  | ט  | f  | כ  |
| u  | ו  | g  | ע  |
| i  | ן  | h  | י  |
| o  | ם  | j  | ח  |
| p  | פ  | k  | ל  |
|    |    | l  | ך  |

## Tray Menu

- **Enabled** — toggle on/off
- **Settings** — open settings window (startup toggle)
- **Exit** — quit the application

## License

MIT

