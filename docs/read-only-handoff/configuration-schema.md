# Схема конфигурации и безопасные аргументы

Offline path принимает только target alias, declared read scope, sanitized fixture
path и optional plan hash. Для live команды допустимы `<safe-alias>`, `--scope full`,
`--manual`, `--live` и новый user-local `--output-root`.

Не помещайте в config/CLI args URL, login, password, cookies, CSRF, authorization
headers, endpoint bodies либо raw lookup values. Они вводятся только в terminal и
живут в памяти. `--live` не разрешает retry: после blocker нужен новый human decision.
