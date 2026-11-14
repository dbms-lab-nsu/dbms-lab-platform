ямлик в
```.github/workflows/ci.yml```


GitHub Secrets
CR_PAT (токен для ghcr.io):
Создайте новый токен GitHub на странице Settings -> Developer settings -> Personal access tokens.
Выберите права write:packages, read:packages, delete:packages.
Сохраните токен как CR_PAT в GitHub Secrets.
Другие секреты, если нужны:
Если у вас есть другие секреты, добавьте их аналогично CR_PAT.
План внедрения
Фаза 1 (сейчас): lint + build
Основной пайплайн ci.yml содержит триггеры для push в main и pull request.
Jobs включают linting Dockerfile и сборку Docker образов.
Используется кэширование Docker-слоев.
Фаза 2 (потом): push в registry
После успешного прохождения lint и build, образы будут пушиться в ghcr.io с тегом ${GITHUB_SHA}.