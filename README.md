# Bashkir Keyboard

Type Bashkir letters on the familiar Russian keyboard layout: tap for a Russian letter, hold for a Bashkir letter. A background utility for Windows 10/11.

**[Download](https://github.com/TvoyTezka/bashkir-keyboard/releases/latest) · [Website](https://tvoytezka.github.io/bashkir-keyboard/) · [Try on Hugging Face](https://huggingface.co/spaces/failed09/bashkir-keyboard) · [Report a bug](https://github.com/TvoyTezka/bashkir-keyboard/issues/new/choose)**

## Get started

1. Download the Windows x64 ZIP from the latest release.
2. Extract the **entire archive** to a permanent folder and run `BashkortKeyboard.exe`. No separate .NET installation is needed.
3. Switch to the Russian layout. Hold a mapped key for about half a second: for example, `о` becomes `ө`.

| Tap | Hold |
|---|---|
| А | Ә |
| О | Ө |
| У | Ү |
| К | Ҡ |
| Г | Ғ |
| С | Ҫ |
| З | Ҙ |
| Х | Һ |
| Н | Ң |

Shift and Caps Lock control letter case. Settings let you choose the hold delay, disable individual mappings and start the app with Windows.

Closing the window hides it in the system tray. Double-click the icon to open settings; use its menu to quit. If the tray icon is disabled, the window minimizes to the taskbar.

Run `install-start-menu.ps1` from the extracted folder to add a Start menu shortcut. If you move the folder, recreate the shortcut and turn autostart off and on to update its path.

## Compatibility and privacy

Replacement depends on the editor exposing its text field. If the app cannot verify the caret position, regular typing is preserved. Replacement is disabled in password fields. See [technical limitations](TECHNICAL.md#практические-ограничения) for details (in Russian).

The app has no analytics and never sends your text over the network. [Privacy details](PRIVACY.md) (in Russian).

The app is not digitally signed yet, so Windows may show a warning on first launch. The Windows app currently has a Russian interface; the website supports English and Russian.

## Development

C# / .NET 10, WPF. [Build and contribute](CONTRIBUTING.md) · [Architecture and tests](TECHNICAL.md) · [Changelog](CHANGELOG.md). These developer documents are currently in Russian.

Free and open source under the [MIT license](LICENSE).
