# Flipit

Convert mistyped Hebrew ↔ English text anywhere in Windows with a customizable hotkey.

---

## 🚀 Download

### Latest Release

👉 **[Download Flipit](https://github.com/tova-kahn-git/flipit/releases/latest)**

Download, run, and use the configured hotkey whenever text was typed with the wrong keyboard layout.

---

## ✨ What is Flipit?

Ever typed an entire sentence in the wrong language because the keyboard layout was set incorrectly?

Flipit instantly converts text between Hebrew and English keyboard layouts.

Simply select the text (or place the cursor on the relevant line) and press the configured hotkey.

**By default, Flipit uses `F1`, but the hotkey can be customized in Settings.**

### Examples

| Typed   | Converted |
| ------- | --------- |
| `יקךךם` | `hello`   |
| `ghbd`  | `עבר`     |
| `דקךאש` | `english` |

---

## ⚡ Features

* Global hotkey (default: **F1**)
* Customizable hotkey
* Automatic Hebrew ↔ English detection
* Works in almost any Windows application
* No text selected? Automatically handles the current line
* Silent and instant operation
* System tray icon with enable/disable toggle
* Optional startup with Windows
* Lightweight and resource-efficient

---

## 🖥️ Supported Applications

Flipit works in virtually any Windows application, including:

* Notepad
* Chrome
* Edge
* Microsoft Word
* VS Code
* IntelliJ IDEA
* Slack
* Microsoft Teams
* WhatsApp Desktop

---

## ⚙️ Settings

Flipit can be configured from the Settings window:

* Change the global hotkey
* Enable or disable Flipit
* Start automatically with Windows

---

## 📋 Requirements

* Windows 10 or Windows 11 (64-bit)
* .NET 8 Runtime

---

## 🛠️ Build from Source

```bash
dotnet build src/Flipit.App/Flipit.App.csproj
```

### Publish Single-File Executable

```bash
dotnet publish src/Flipit.App/Flipit.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

---

## 🏗️ Project Structure

```text
/src/Flipit.App
  /Core            - Application orchestration
  /KeyboardEngine  - Layout conversion engine
  /Clipboard       - Clipboard integration
  /Hooks           - Global hotkey handling
  /Tray            - System tray functionality
  /Settings        - User preferences and persistence
  /Infrastructure  - Win32 integration and composition root
```

---

## 🖱️ Tray Menu

* **Enabled** — enable or disable Flipit
* **Settings** — configure hotkey and startup options
* **Exit** — close the application

---

## 📄 License

MIT License
