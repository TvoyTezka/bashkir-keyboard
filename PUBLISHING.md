# Publishing to GitHub

The Git repository should contain the complete `BashkortKeyboard` project, not the parent `Tools` folder. Commit source files, tests, docs, workflows and website files. Build outputs are excluded by `.gitignore`.

## Connect a repository

Create an empty public repository without an initial README, license or `.gitignore`, then add it and push `main`:

```powershell
git remote add origin https://github.com/OWNER/REPOSITORY.git
git push -u origin main
```

Replace `OWNER/REPOSITORY` with the repository name. Do not put a password or token in the URL. GitHub Desktop can add an existing local repository.

## GitHub Pages

Under GitHub Settings → Pages, select **GitHub Actions** as the source. If needed, run **Publish project website** from Actions after enabling Pages.

The website lives in `docs` and does not require an external site builder. The workflow adds `repository.json` for links and release downloads. If the GitHub API is unavailable, the download button still links to Releases.

Preview it locally with `python -m http.server 8080 --directory docs`, then open `http://localhost:8080`. Without `repository.json`, the button scrolls to the installation instructions. The site's 256 px icon is in `docs/assets/app-icon.png`; the source is `src/BashkortKeyboard/Assets/App.png`.

## Releases

The **Build and test** workflow builds pull requests and `main`, then saves the portable build as a temporary Actions artifact. It does not publish a release.

For a release, update the version in the project file and the changelog, create a version tag and push it:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

**Prepare release** builds and tests the app, then creates a draft Release with a Windows x64 ZIP and SHA-256 checksum. Review it and select **Publish release**. Drafts are not visible to regular visitors.

Native CI tests load the Russian layout only inside the test process if the hosted Windows runner does not have it loaded already.

The ZIP includes the executable, runtime, README, license, privacy notes and Start menu script. User settings and logs under AppData are not included.

The app is not digitally signed. Hosting on GitHub does not sign it. The hook should be checked manually in each target editor; CI does not establish compatibility with every text field.

## License and attribution

The MIT license currently credits `TvoyTezka`, the local Git author, and project contributors. Change the copyright line in `LICENSE` if a different public name is preferred.

## Hugging Face Space

Static page: https://huggingface.co/spaces/failed09/bashkir-keyboard. It uses the same files as GitHub Pages; the Windows app is downloaded from GitHub Releases.

To refresh it after site changes:

```powershell
.\prepare-hf-space.ps1
hf upload failed09/bashkir-keyboard artifacts/hf-space . --type space --commit-message 'Update website'
```

Sign in with `hf auth login`; never store the token in project files. The script packages only the site, license and Space card.

---

# Первая публикация на GitHub

Репозиторием должна быть **папка BashkortKeyboard целиком**, а не вся папка Tools. Исходники, тесты, документы, `.github` и `docs` сохраняются в Git; результаты сборки исключены через `.gitignore`.

## Подключить репозиторий

Создайте пустой публичный репозиторий на GitHub. При создании не добавляйте README, LICENSE и gitignore: они уже подготовлены локально. Затем из папки проекта:

```powershell
git remote add origin https://github.com/OWNER/REPOSITORY.git
git push -u origin main
```

Замените OWNER/REPOSITORY реальным адресом. Пароли и токены не вставляйте в URL. Для подключения через приложение GitHub Desktop выберите существующий локальный репозиторий.

## Страница GitHub Pages

В GitHub откройте Settings → Pages → Source → GitHub Actions. Запустите workflow **Publish project website** через Actions → Run workflow, если первая отправка уже состоялась до включения Pages.

Сайт находится в `docs`, включает иконку и не требует стороннего сборщика. Workflow автоматически подставляет имя репозитория в `repository.json`. Кнопка скачивания ссылается на ZIP последнего **публичного** выпуска; пока выпуска нет, она ведёт к списку Releases. Если GitHub API недоступен, ссылка на Releases остаётся рабочей.

Для локального просмотра запустите `python -m http.server 8080 --directory docs` и откройте `http://localhost:8080`. Без сгенерированного repository.json кнопка ведёт к инструкции установки. Уменьшенная версия иконки для сайта находится в `docs/assets/app-icon.png`, исходник — в `src/BashkortKeyboard/Assets/App.png`.

## Выпуски

При push/pull request workflow **Build and test** собирает приложение и сохраняет переносимую сборку как artifact Actions. Это проверочная сборка, не автоматически опубликованный выпуск.

Для нового выпуска обновите версию в `.csproj`, CHANGELOG и документацию, затем создайте и отправьте тег:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

Workflow **Prepare release** выполняет сборку/тесты и создаёт **черновик** Release с ZIP для Windows x64 и SHA256. Проверьте описание и файлы, затем нажмите Publish release на GitHub. После публикации сайт автоматически увидит последний выпуск при открытии страницы. Черновики не доступны обычным посетителям.

Нативные тесты на GitHub временно загружают RU-раскладку только для процесса теста: на hosted Windows runner её может не быть в списке загруженных раскладок.

В ZIP включаются `.exe`, runtime, README, лицензия, описание приватности и скрипт меню «Пуск». Пользовательские настройки и журнал находятся в AppData и в ZIP не попадают.

Готовое приложение не подписано цифровой подписью. Публикация на GitHub Pages/Releases не заменяет подпись. Работа hook в сторонних редакторах требует ручных проверок из README; прохождение CI не доказывает совместимость каждого поля.

## Лицензия и авторство

Выбрана MIT. В уведомлении об авторстве указан `TvoyTezka`, имя из локальной настройки Git, и участники проекта. Если хотите другое публичное имя, измените строку Copyright в LICENSE перед отправкой.

## Hugging Face Space

Статическая страница: https://huggingface.co/spaces/failed09/bashkir-keyboard.
Используются те же файлы сайта, что и для GitHub Pages. Windows-сборка скачивается из GitHub Releases.

Для обновления страницы после изменений:

```powershell
.\prepare-hf-space.ps1
hf upload failed09/bashkir-keyboard artifacts/hf-space . --type space --commit-message 'Update website'
```

Вход через `hf auth login`; токен не добавляйте в файлы проекта. Скрипт подготавливает только файлы сайта, лицензию и карточку Space.
