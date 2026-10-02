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

Replacement depends on the editor exposing its text field. If the app cannot verify the caret position, regular typing is preserved. Replacement is disabled in password fields. See [technical limitations](TECHNICAL.md#compatibility-limits).

The app has no analytics and never sends your text over the network. See [Privacy details](PRIVACY.md) in English below.

The app is not digitally signed yet, so Windows may show a warning on first launch. The Windows app currently has a Russian interface; the website supports English and Russian.

## Development

C# / .NET 10, WPF. [Build and contribute](CONTRIBUTING.md) · [Architecture and tests](TECHNICAL.md) · [Changelog](CHANGELOG.md). English and Russian versions are provided below.

Free and open source under the [MIT license](LICENSE).

---

# Башҡорт Keyboard — по-русски

Башкирские буквы на привычной русской раскладке: короткое нажатие вводит русскую букву, удержание — башкирскую. Приложение работает в фоне на Windows 10/11.

**[Скачать](https://github.com/TvoyTezka/bashkir-keyboard/releases/latest) · [Сайт](https://tvoytezka.github.io/bashkir-keyboard/?lang=ru) · [Попробовать на Hugging Face](https://huggingface.co/spaces/failed09/bashkir-keyboard) · [Сообщить об ошибке](https://github.com/TvoyTezka/bashkir-keyboard/issues/new/choose)**

## Начать пользоваться

1. Скачайте ZIP для Windows x64 из последнего выпуска.
2. Распакуйте **весь архив** в постоянную папку и запустите `BashkortKeyboard.exe`. Устанавливать .NET отдельно не нужно.
3. Включите русскую раскладку. Удерживайте нужную клавишу около половины секунды: например, `о` превратится в `ө`.

| Короткое нажатие | Удержание |
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

Shift и Caps Lock меняют регистр. В настройках можно выбрать задержку, отключить отдельные замены и включить запуск вместе с Windows.

Закрытие окна скрывает приложение в системный трей. Двойной щелчок по значку открывает настройки; через его меню можно завершить работу. Если значок отключён, окно сворачивается на панель задач.

Для ярлыка в меню «Пуск» запустите `install-start-menu.ps1` из распакованной папки. После переноса папки создайте ярлык заново и выключите/включите автозапуск, чтобы обновить путь.

## Совместимость и приватность

Замена зависит от поддержки текстового поля редактором. Если приложение не может проверить положение курсора, обычный ввод сохраняется. В полях пароля замена отключена. Подробные ограничения описаны в [технической документации](TECHNICAL.md).

У приложения нет аналитики, и оно не отправляет текст по сети. [Подробнее о приватности](PRIVACY.md).

Сборка пока не подписана цифровой подписью: при первом запуске Windows может показать предупреждение. Интерфейс приложения на русском; у сайта есть переключатель английского и русского языков.

## Разработка

C# / .NET 10, WPF. [Сборка и участие в проекте](CONTRIBUTING.md) · [Устройство и проверки](TECHNICAL.md) · [История изменений](CHANGELOG.md).

Бесплатный проект с открытым кодом под [лицензией MIT](LICENSE).
