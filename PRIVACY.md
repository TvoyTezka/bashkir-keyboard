# Privacy

Bashkort Keyboard processes keyboard events locally to replace letters when you hold a key. The app makes no network requests and contains no analytics, ads or automatic report uploads.

For a safe replacement, the app checks the active window, keyboard layout, whether the text field is editable and the caret position. In a supported non-password field, UI Automation briefly checks the character directly before the caret to verify the expected letter. The text is not stored or sent. Password detection depends on the field being correctly labelled by its owner; unknown fields are skipped.

Settings and a diagnostic log are stored under `%LOCALAPPDATA%\BashkortKeyboard`. Detailed diagnostics are off by default. The log contains timestamps and fixed event categories, without key codes, characters, document text, window titles or clipboard contents.

When enabled by the user, autostart creates a shortcut in the user's Startup folder. A Start menu shortcut is created by a separate script. The app does not change the system keyboard layout or require elevated privileges.

The project website is hosted on GitHub Pages and fetches release information through the GitHub API. Visiting the website or downloading the app is subject to GitHub's terms and privacy policy; this is separate from the app itself.

---

# Приватность

Башҡорт Keyboard локально обрабатывает события клавиатуры для замены букв при удержании. Приложение не содержит сетевых запросов, аналитики, рекламы или автоматической отправки отчётов.

Для безопасной замены проверяются активное окно, раскладка, доступность редактируемого поля и положение курсора. В поддерживаемом непарольном поле UI Automation кратковременно читает символ непосредственно перед курсором, чтобы проверить ожидаемую букву. Этот текст не сохраняется и не отправляется. Определение пароля зависит от корректного описания поля его владельцем; неизвестные поля пропускаются.

В `%LOCALAPPDATA%\BashkortKeyboard` сохраняются настройки и диагностический журнал. Детальная диагностика по умолчанию выключена. Журнал содержит время и фиксированные категории событий, без кодов клавиш, символов, текста документа, заголовков окон или содержимого буфера обмена.

Автозапуск по желанию пользователя создаёт ярлык в пользовательской папке «Автозагрузка». Ярлык в меню «Пуск» создаётся отдельным скриптом. Приложение не изменяет системную раскладку и не требует повышения прав.

Страница проекта на GitHub Pages размещается у GitHub и загружает информацию о выпуске через GitHub API. При посещении сайта или скачивании сборки действуют условия и политика приватности GitHub; это отдельный от приложения сервис.
