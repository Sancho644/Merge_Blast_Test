# Asset List & лицензии

27.11.2026. Список для локального импорта. Сами архивы/графика/музыка в комплект документации не включены. Названия файлов проверены по загруженным архивам (ZIP/7z). Источники — ранее подобранные страницы; сведения без приложенного файла лицензии требуют сохранения подтверждения с сайта до сдачи. Лицензия на готовую игру не равна разрешению распространять исходные ассеты.

## 1. Источники
| Asset | Локальный файл | Источник | Лицензия / статус |
|---|---|---|---|
| CraftPix Free Idle Turret Defense Asset Kit | craftpix-net-206389-free-idle-turret-defense-asset-kit.zip | https://craftpix.net/freebies/free-idle-turret-defense-asset-kit/ | Собственная; license.txt содержит ссылку https://craftpix.net/file-licenses/; **блокер открытой передачи исходников** |
| Colored Spheres | coloredspheres.7z | https://opengameart.org/content/colored-spheres | CC0 по предыдущему подбору; в архиве нет лицензии, сохранить страницу |
| Glow Circle | glowCircle.png | https://opengameart.org/content/glow-circle | CC0 по предыдущему подбору; сохранить страницу |
| Animated Orb — Joseph Crown | Animated Orb.zip | https://opengameart.org/content/powerup-animated-orb | README: attribution free, commercial/noncommercial; CC0 по предыдущему подбору, сохранить страницу |
| Kenney Particle Pack 1.1 | kenney_particle-pack.zip | https://kenney.nl/assets/particle-pack | CC0 подтверждена License.txt |
| Bomb SVG | bomb.svg | предположительно https://opengameart.org/content/2d-bomb или https://opengameart.org/content/bomb-sprite-vector-image | Точная страница не установлена по файлу; резерв, не использовать/не включать до проверки |
| Wenrexa Minimalism SciFi | wenrexaassetsui_scifi.zip | https://opengameart.org/content/assets-ui-minimalism-scifi | CC0 по предыдущему подбору, в архиве нет лицензии, сохранить страницу |
| Late Night UI Kit v1.0 — Lazy Neon Studio | lazyneon-late-night-ui-kit-vol1-v1.0.zip | https://lazyneonstudio.itch.io/late-night-ui-kit-vol1 | CC0 подтверждена license.txt; AI+code, бренд/логотип/обложка исключены |
| Kenney Game Icons | kenney_game-icons.zip | https://kenney.nl/assets/game-icons | CC0 подтверждена license.txt |
| 50 CC0 Retro / Synth SFX — rubberduck | 50-CC0-retro-synth-SFX.zip | https://opengameart.org/content/50-cc0-retro-synth-sfx | CC0 по названию/ранее найденной странице; в архиве нет отдельной лицензии |
| Synth Wave | Synth Wave.mp3 | https://opengameart.org/content/synth-wave | CC0 по предыдущему подбору, сохранить страницу и автора |
| Neon Sign Circuit | neon_sign_circuit_bpm145.ogg | https://opengameart.org/content/neon-sign-circuit | CC0 по предыдущему подбору, сохранить страницу и автора |
| Bebas Neue | ещё не загружен | https://fonts.google.com/specimen/Bebas+Neue | OFL ожидается; сохранить OFL.txt из официального скачивания |
| Teko | ещё не загружен | https://fonts.google.com/specimen/Teko | OFL ожидается; сохранить OFL.txt из официального скачивания |

CC0: https://creativecommons.org/publicdomain/zero/1.0/ . Сохранить подтверждения лицензий и кредиты авторов в THIRD_PARTY_NOTICES.md при импорте. Не включать обложки, ненужные исходники PSD/AI/EPS/Spine/XPF, а также неустановленные лицензии. Не копировать файлы шрифтов в документацию.

## 2. Manifest: графика
Пути относительно распакованного архива; шаблоны означают последовательности файлов, не literal path. Переносить только необходимое; сохранять исходные имена или записать mapping в Unity. 

| Пак | Исходный путь / имя | Использование |
|---|---|---|
| CraftPix | Merge Turrets/Png/Turrets/T{1..10}/T{n}-Shoot_{00..14}.png | 150 кадров 10 пушек |
| CraftPix | Merge Turrets/Png/Projectile/Projectile{1..3}.png | Пули |
| CraftPix | Merge Turrets/Png/Shoot Fx/Shoot-animation_{00..14}.png | Вспышка выстрела |
| CraftPix | Merge Turrets/Png/Explosion 1/*.png; Explosion 2/*.png | Взрывы, по необходимости |
| CraftPix | Merge Turrets/Png/User Interfaces/MoneyIcon.png; UpBar{0..3}.png | Валюта, запасные иконки |
| Spheres | coloredspheres/sphere-13.png, sphere-23.png, sphere-02.png, sphere-12.png, sphere-22.png | Номиналы 2,4,8,16,32 |
| Orb | Spnning Orb/Red/frame {1..6}.png | Бомба (опечатка Spnning — имя в архиве) |
| Glow | glowCircle.png | Запасной glow/shockwave |
| Particle | PNG (Transparent)/circle_01.png, circle_02.png, circle_03.png, circle_05.png | Glow/rings/particles |
| Particle | PNG (Transparent)/light_01.png, light_03.png, magic_03.png, spark_01.png, spark_02.png, star_06.png, trace_06.png, symbol_01.png | Feedback VFX |
| Wenrexa | PNG/MainPanel02.png, MainPanel03.png, TitlePanel02.png, Button11.png, Grid.png | Панели, disabled, фон |
| Late Night | late-night-ui-kit-vol1/png/cyan/ln-ui-button-round-cyan.png | Play / Continue |
| Late Night | late-night-ui-kit-vol1/png/green/ln-ui-button-round-green.png | Cannon Upgrade |
| Late Night | late-night-ui-kit-vol1/png/purple/ln-ui-button-round-purple.png | Ricochet Upgrade |
| Late Night | late-night-ui-kit-vol1/png/cyan/ln-ui-bar-0-cyan.png | Фон таймера, отдельный fill |
| Icons | PNG/White/2x/locked.png, unlocked.png, up.png, trophy.png, target.png, power.png, arrowLeft.png, arrowRight.png, cross.png | Иконки UI, по необходимости |

Прочее — не импортировать: 11–20 турели, враги CraftPix, фоны, альтернативные цвета Orb, неиспользуемые UI-элементы, архивы исходников. SFX/BGM точные имена — AUDIO_GUIDE.md.

## 3. Блокер CraftPix и порядок сдачи
ТЗ допускает бесплатные паки **с открытой лицензией** и требует репозиторий или архив проекта. Бесплатность CraftPix не доказывает соответствие этому требованию. Пока можно подготовить локальный visual preview с предоставленным архивом, но нельзя считать право на передачу исходников установленным. Не публиковать CraftPix-файлы, включая производные, без подтверждения допустимости конкретного использования; приватный репозиторий сам по себе не снимает вопрос.

Предпочтительный порядок: (1) уточнить у работодателя допустимость пакета с собственной лицензией и проверить право на передачу исходников у правообладателя; (2) если нет подтверждения — заменить CraftPix на CC0/разрешённую открытую графику или собственную AI-графику и обновить manifest; (3) если допускают локальный импорт из самостоятельно скачанного пакета, оформить инструкцию импорта без передачи защищённых файлов и получить согласие работодателя, что такой проект принимается. Не обходить лицензию переименованием или перекодированием. Не заявлять, что APK или исходники готовы, до выполнения этой проверки. Остальные открытые ассеты тоже проверить по списку.
