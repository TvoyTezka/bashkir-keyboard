# Technical notes

This document records the input-safety design, known limitations and verification steps for developers and reviewers. Application behaviour and settings are summarized in [README.md](README.md); build and test commands are in [CONTRIBUTING.md](CONTRIBUTING.md).

## Input handling

1. A low-level keyboard hook passes the first keydown through immediately. The hook callback does not read document content or run UI Automation.
2. The app checks settings, modifier keys, the foreground layout and `ToUnicodeEx`. Unsupported and English-layout keys use normal Windows input.
3. Each held key gets an independent ID, state and one-shot timers. The hook has its own message loop; context and timer state are serialized there.
4. A separate MTA thread verifies that a text field is available, editable, non-password and supports `TextPattern`. It saves the caret endpoint after the ordinary letter is entered and checks only one character before the caret.
5. Another key, modifier change, mouse input, or a change of window, focus or keyboard layout cancels the pending replacement. The context is checked about every 20 ms.
6. At the hold threshold, the app rechecks the same UIA element, selection, caret endpoint, preceding letter and process integrity. Responses older than 180 ms are ignored.
7. The hook performs a final context check and sends one `SendInput` sequence: Backspace down/up and Unicode down/up. Injected events are marked so the hook ignores its own input. Physical keyup always passes through.

Autorepeat for a safe pending candidate is suppressed to prevent multiple ordinary letters appearing before the hold threshold. A tap still has no initial delay. After cancellation, normal repeat resumes; skipped repeats are not replayed.

## Compatibility limits

Windows does not provide a universal transaction for replacing the exact character produced by a keydown. There is a small interval between checking the caret and sending replacement input. Editors and accessibility providers can change focus or text during that interval. The app uses conservative checks, but cannot guarantee compatibility with every editor or every edit.

- Replacement requires an editable field with a working UI Automation `TextPattern` and a verifiable caret. If those checks fail, the app preserves ordinary input.
- Password fields are skipped when Windows or the accessibility provider correctly identifies them as passwords.
- After focus moves to a new field, the first key can pass through before the app has identified that field. Slow UIA providers can also cause a replacement to be skipped.
- A normal process cannot send input into an elevated process. The app does not elevate itself.
- Protected View, IMEs, games, terminals, remote desktops and apps with custom text input need individual checks. Partial `SendInput` failures are not retried with another Backspace.
- When two mapped keys overlap, the earlier candidate is cancelled. Windows can remove a timed-out hook; if input stops being transformed, restart the app.

## Settings and logging

Settings are stored in `%LOCALAPPDATA%\BashkortKeyboard\settings.json`. Autostart uses a user-level Startup shortcut named `Bashkort Keyboard.lnk` with `--background`. Turning autostart off removes that shortcut. If the app folder moves, switch autostart off and on again to update the path.

Optional diagnostics are written to `%LOCALAPPDATA%\BashkortKeyboard\diagnostics.log`. The log records timestamps and fixed event categories, never typed characters, key codes, document text, window titles, clipboard contents or passwords.

## Testing

`BashkortKeyboard.Tests` covers short and held presses, case, fast typing order, cancellation and stale asynchronous context checks. `BashkortKeyboard.NativeTests` checks `ToUnicodeEx` results for the mapped keys, Shift and Caps Lock, plus WinAPI structures and process-integrity behaviour.

Native tests do not synthesize keyboard input into another editor. Their assertions do not establish compatibility with a particular text field. Check the target editor manually: type quickly, tap and hold a mapped key, try Shift and Caps Lock, change focus or layout during a hold, and check a password field.

## Windows API references

- [LowLevelKeyboardProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)
- [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)
- [GetGUIThreadInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguithreadinfo)
- [GetKeyboardLayout](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeyboardlayout)
- [ToUnicodeEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-tounicodeex)
- [UI Automation TextPattern](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-textpattern-overview)

---

# Технические подробности

Для разработки нужны Windows и .NET SDK 10. Команды сборки и тестов приведены в [CONTRIBUTING.md](CONTRIBUTING.md).

## Как устроена замена

1. `WH_KEYBOARD_LL` получает физический keydown и **пропускает первый keydown без ожидания**. Сам обработчик не обращается к содержимому документов и не выполняет UI Automation.
2. Проверяются настройки, отсутствие Ctrl/Alt/Win, раскладка потока активного поля и результат `ToUnicodeEx`. Для нестандартной RU-раскладки замена включается только при совпадении фактически переводимой русской буквы. EN и неподдерживаемые клавиши проходят штатно.
3. Каждое удержание получает собственный идентификатор, состояние и два одноразовых `DispatcherTimer`: получение исходного положения курсора через 35 мс и порог удержания. Все состояния и callbacks обслуживаются последовательно отдельным потоком с message pump.
4. UI Automation на отдельном MTA-потоке предварительно проверяет, что поле доступно, редактируемо, не является паролем и поддерживает TextPattern. Затем сохраняется пустой диапазон курсора **после** ввода обычной буквы, идентификатор элемента и, если доступно, позиция нативного caret. Для проверки сравнивается только один символ непосредственно перед курсором; документ целиком не читается.
5. Другая новая клавиша, изменение Shift/модификатора, внешние искусственные события, щелчок/колесо мыши или изменение окна, фокуса/раскладки отменяют ожидающую замену. Проверка контекста выполняется также каждые 20 мс. При перекрывающихся удержаниях старое теряет право на замену; наиболее новая буква может замениться. Уже заменённая клавиша не повторяет русскую букву до отпускания; сочетания с модификаторами всегда имеют приоритет.
6. По достижении порога повторно проверяются тот же UIA-элемент, отсутствие выделения, сохранённый endpoint курсора, буква перед ним, нативный caret и права процесса. Результат принимается только в течение 180 мс; устаревший ответ игнорируется. Неудача безопасно отменяет замену и возвращает обычный автоповтор.
7. На потоке hook выполняется финальная проверка контекста/удержания и один `SendInput` из четырёх событий: Backspace down/up, Unicode down/up. Собственные события помечены `dwExtraInfo`, пропускаются обработчиком и не создают рекурсию. Физический keyup всегда проходит.

Пока безопасный кандидат ожидает замены, автоповтор этой клавиши подавляется, иначе системная задержка повтора может быть меньше 450 мс и в поле уже окажется несколько русских букв. У обычного короткого нажатия задержки нет. После отмены кандидата нормальный повтор возобновляется; уже пропущенные повторы не воспроизводятся задним числом.

## Почему нельзя гарантировать замену во всех приложениях

В Windows отсутствует общий API «замени именно тот символ, который был введён данным keydown». Hook наблюдает клавиатурные события, а `SendInput` отправляет события в текущий фокус; они не дают транзакционного доступа к документу. Между проверкой UIA и обработкой отправленных событий остаётся небольшой интервал. Редактор может изменить текст или фокус программно, расширение может переписать поле, а провайдер UIA — возвращать устаревшие диапазоны. Даже сохранённые UIA-диапазоны могут перемещаться вместе с текстом. Поэтому это **консервативная реализация с проверками**, а не абсолютная гарантия.

Рассмотренные варианты:

- **Безусловный Backspace по таймеру:** отвергнут — удаляет другую букву при быстром вводе или навигации.
- **Ожидание keyup перед вводом:** отвергнуто — задерживает обычную печать.
- **Чтение/перезапись всего значения через ValuePattern или буфер обмена:** отвергнуто — мешает форматированию, выделению, undo и другим изменениям документа; буфер обмена не используется.
- **EM_GETSEL/EM_SETSEL и редакторские сообщения:** применимы только к конкретным нативным Edit/RichEdit; в браузерах и собственных контролах общего решения не дают.
- **UIA-диапазон + контролируемый SendInput:** выбран, поскольку сохраняет мгновенный первый ввод, не перезаписывает документ и позволяет отказаться от неоднозначной замены.
- **Text Services Framework (TSF)/собственный input processor:** подходящий путь для дальнейшего развития с композициями в совместимых редакторах, но требует иной интеграции/регистрации и также не делает произвольные приложения транзакционно редактируемыми. В это MVP/desktop-решение не входит.

### Практические ограничения

- Глобальная обработка охватывает обычный интерактивный сеанс Windows. Secure desktop, экран входа и другие пользовательские сеансы не поддерживаются.
- Замена работает только в полях с корректным **UI Automation TextPattern**, доступной позицией курсора и явным признаком редактируемости. В недоступных/нестандартных полях остаётся обычный ввод. Безопасного «слепого» режима нет. Работа в каждой версии браузера, Word, Telegram, Discord зависит от её accessibility-провайдера; универсальная совместимость не заявляется.
- В известных полях пароля замена отключается: проверяются UIA IsPassword и ES_PASSWORD нативного Edit. Неизвестные/недоступные поля пропускаются. Приложение зависит от корректности маркировки поля его владельцем; кэш признаков фокуса имеет короткий срок жизни.
- При переходе в новое поле первая немедленно нажатая клавиша может не получить замену, пока фоновой проверке не удалось определить поле (обычно до 300 мс). Первый обычный символ всё равно проходит сразу.
- Медленный UIA-провайдер может вызвать пропуск замены. Он не блокирует hook/UI, просроченные ответы отбрасываются. Замена может произойти немного позже заданного порога, но не позднее окна принятия проверки (180 мс сверх порога).
- Обычный процесс не отправляет ввод в процесс с более высоким integrity level. Проверка прав заранее отключает кандидаты для таких окон. Автоматического повышения прав нет. Для повышенного редактора можно вручную запустить приложение с теми же правами; это увеличивает область его доступа.
- Неполный `SendInput` считается ошибкой, без повторного Backspace. Уже доставленные события не имеют общего rollback; целевое приложение может игнорировать Unicode или изменять обработку Backspace. Protected View, IME, игры, терминалы, удалённый рабочий стол и приложения с собственным механизмом ввода требуют отдельной проверки.
- Две одновременно удерживаемые клавиши не заменяются обе: первая отменяется, чтобы исключить удаление неверного символа. Последовательные удержания обеих работают независимо.
- Hook Windows может быть удалён системой при таймауте. Обработчик короткий, логирование и UIA вынесены из него, но автоматическое восстановление hook после удаления системой не реализовано; при пропавших заменах перезапустите приложение.

## Настройки и приватность

Настройки: `%LOCALAPPDATA%\BashkortKeyboard\settings.json`; сохранение через временный файл и замену. Можно отключить отдельные соответствия, настроить задержку, трей и автозапуск.

Автозапуск создаёт пользовательский ярлык `Bashkort Keyboard.lnk` в `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`, с путём к `.exe`, аргументом `--background` и иконкой приложения. Администратор не нужен. Галочка снимается — ярлык удаляется. При обновлении старая запись `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\BashkortKeyboard` автоматически переносится в ярлык, без двойного запуска. Если переносите папку сборки, выключите и снова включите автозапуск, чтобы обновить путь. Само включение автозапуска выполняется только по настройке пользователя. Запуск происходит при входе пользователя в Windows.

Подробная диагностика отключена по умолчанию. При включении `%LOCALAPPDATA%\BashkortKeyboard\diagnostics.log` содержит время и фиксированные категории: keydown/keyup, начало удержания, отмена, отправка/пропуск замены, RU/другая раскладка. Не сохраняются коды клавиш, введённые буквы, текст документов, заголовки окон, буфер обмена или пароли. Запуск/выход записываются всегда. Журнал ограничен примерно 1 МБ + один предыдущий файл; запись выполняется асинхронно с ограниченной очередью.

## Структура

```text
src/BashkortKeyboard.Core/
  Mapping.cs                 соответствия
  AppSettings.cs             модель настроек
  LongPressService.cs        независимая логика удержания
src/BashkortKeyboard/
  Interop/NativeMethods.cs    WinAPI и структуры
  Services/
    GlobalKeyboardHandler.cs keyboard/mouse hooks, отдельный message pump
    KeyboardLayoutService.cs фокус, раскладка, ToUnicodeEx
    TextContextGuard.cs      проверка поля и UIA-диапазона на MTA-потоке
    InputSender.cs           SendInput
    IntegrityService.cs      сравнение прав процессов
    SettingsStore.cs         JSON и автозапуск
    DiagnosticLog.cs         ограниченный журнал без текста
    TrayService.cs           системный трей
  App.xaml(.cs)              жизненный цикл и один экземпляр
  MainWindow.xaml(.cs)       настройки
tests/
  BashkortKeyboard.Tests/       детерминированные сценарии логики
  BashkortKeyboard.NativeTests/ реальные проверки WinAPI без ввода
```

## Проверки

Автотесты логики покрывают мгновенный короткий ввод и «оооо», удержание и повтор до/после порога, верхний/нижний регистр, быструю печать «яратам», отмену при смене контекста/сочетаниях, последовательные и перекрывающиеся удержания, отказ проверки поля, запоздалые async-результаты после отпускания/нового нажатия, отключённые соответствия и несовпадение переведённой буквы. Таймеры и proof в этих тестах контролируемые: они не доказывают совместимость реальных редакторов.

NativeTests проверяет действительный перевод стандартной RU-раскладки через `ToUnicodeEx` для каждой буквы со всеми четырьмя сочетаниями Shift/Caps, EN, размеры структур WinAPI x64 и отказ для неизвестного процесса. По умолчанию используются уже загруженные раскладки; тест не отправляет клавиатурный ввод.

Для CI без загруженной RU-раскладки можно передать NativeTests аргумент `--load-test-layout`: тест временно загружает стандартную RU-раскладку в своём процессе без активации/изменения предпочтительных языков и выгружает её после проверки. `build-release.ps1 -LoadTestLayout` включает этот режим. Production-приложение раскладки не загружает.

На машине разработки дополнительно проверены запуск настоящего окна, отображение девяти соответствий, изменение включения, сохранение JSON и сброс настроек. Полноценный физический long-press в сторонних редакторах не автоматизировался: искусственный ввод специально исключён production-hook. Для приёмки выполните ручную матрицу ниже.

| Проверка в нужном редакторе | Ожидаемый результат |
|---|---|
| Быстрое `оооо` | четыре обычные буквы без задержки |
| Короткое О | `о` |
| Удержание О | одна `ө`, без дальнейшего русского повтора |
| Shift + короткое О | `О` |
| Shift + удержание О | `Ө` |
| Caps Lock / Shift + Caps | верхний / нижний регистр соответственно |
| Быстрое `яратам` | исходный порядок, никаких замен |
| RU → EN | J остаётся J/j, замены отключены |
| Ctrl+C / Ctrl+V / Ctrl+A | стандартное поведение |
| Удержание более секунды | один башкирский символ |
| О отпустить, затем А удерживать | `өә` |
| О удерживать, затем другая клавиша до порога | О не заменяется; новый символ не удаляется |
| Щелчок, стрелка, Tab, смена окна до порога | ожидающая замена отменена |
| Пароль / недоступное поле / elevated-окно | обычный ввод, без замены |

## Документация WinAPI

- [LowLevelKeyboardProc: порядок события, message loop и таймауты](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)
- [SendInput: последовательность событий, UIPI и состояние клавиатуры](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)
- [GetGUIThreadInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguithreadinfo)
- [GetKeyboardLayout](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeyboardlayout)
- [ToUnicodeEx и флаг сохранения состояния](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-tounicodeex)
- [UI Automation TextPattern: пустые диапазоны и ограничения редактирования](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-textpattern-overview)
