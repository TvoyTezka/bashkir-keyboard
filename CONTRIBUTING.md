# Contributing

Bug reports and fixes for unsupported text fields are welcome.

See [TECHNICAL.md](TECHNICAL.md) for architecture, limitations and manual checks.

To build and test, use Windows with the .NET 10 SDK. Open `BashkortKeyboard.sln` or run:

```powershell
dotnet build BashkortKeyboard.sln -c Release
dotnet run --project tests/BashkortKeyboard.Tests -c Release
dotnet run --project tests/BashkortKeyboard.NativeTests -c Release -- --load-test-layout
```

If Russian is not already loaded, `--load-test-layout` temporarily loads the standard layout in the test process without activating it or changing preferred languages. The app never uses this option.

When editing keyboard handling, keep the first keydown immediate, preserve fast typing order, cancel pending replacements after unrelated input, and skip a replacement when caret position cannot be verified. Never send a blind Backspace or log typed text or key codes.

For a bug fix, add a scenario that reproduces it. Describe completed checks in the pull request. Keep simulated timer tests distinct from checks in real editors.

Issue reports should include the Windows version, app and editor, keyboard layout and steps to reproduce. Use sample text; do not attach passwords, personal documents or tokens.

The project is licensed under MIT. Contributions must be compatible with that license.

---

# Как помочь проекту

Приветствуются исправления и сообщения о несовместимых текстовых полях.

Устройство приложения, ограничения и ручные проверки описаны в [TECHNICAL.md](TECHNICAL.md).

Для разработки нужны Windows и .NET SDK 10. Откройте `BashkortKeyboard.sln` или выполните:

```powershell
dotnet build BashkortKeyboard.sln -c Release
dotnet run --project tests/BashkortKeyboard.Tests -c Release
dotnet run --project tests/BashkortKeyboard.NativeTests -c Release -- --load-test-layout
```

При отсутствии RU-раскладки флаг `--load-test-layout` временно загружает её в процессе теста, без активации и изменения списка предпочтительных языков пользователя. Само приложение этот флаг не использует.

При работе над вводом сохраняйте главные свойства: первый keydown проходит немедленно, порядок быстрой печати не меняется, сторонний ввод отменяет замену, отсутствие доказательства положения курсора означает отказ от замены. Не добавляйте «слепой» Backspace и не записывайте текст или коды клавиш в журнал.

Для исправления ошибки добавьте сценарий, который воспроизводит её. В pull request укажите результат и выполненные проверки. Проверки на реальном редакторе описывайте отдельно от тестов с искусственным временем.

В issue укажите версию Windows, приложения и редактора, раскладку и минимальные шаги воспроизведения. Используйте выдуманный тестовый текст; не присылайте пароли, личные документы и токены.

Код проекта распространяется под MIT. Изменения, предлагаемые для включения в проект, должны быть совместимы с этой лицензией.
