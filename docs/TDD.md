# TDD — Merge Blast: Overload v0.3

28.11.2026 • Техническая спецификация. Источник игровых правил — GDD.md.

## 1. Стек и структура
Unity 6000.6.7f1, Universal 2D URP, Android Portrait, 60 FPS target, uGUI + TMP, Input System, Physics2D, DOTween (если доступен и лицензия допускает; иначе простые корутины/Animator). Одна сцена Main. Никаких ECS/DI/EventBus/сохранений. Не подключать Spine: у CraftPix есть PNG кадры.

```
Assets/
  _Project/{Art,Audio,Configs,Prefabs,Scenes,Scripts}/
  _Project/Scripts/{Core,Input,Cannon,Projectiles,Balls,Gameplay,Progression,Configs,UI,Feedback}/
  _Project/Prefabs/{Gameplay,UI,VFX}/
  ThirdParty/                  # только разрешённые к передаче файлы
```
Сторонние архивы и распакованные исходники хранить в `_SourceAssets/` в корне проекта, за пределами `Assets/`. Импортировать отдельные разрешённые файлы по ASSET_LIST. Не коммитить Library, Temp, Logs, Obj, Builds, IDE caches, UserSettings, ключи подписи. Meta-файлы разрешённых ассетов коммитить вместе с ними. Не создавать проект/репозиторий от имени пользователя без запроса.

## 2. Сцена
```
Main
  Main Camera (Orthographic, URP 2D)
  Global Light 2D / Volume (Bloom при необходимости)
  Systems
    GameController
    BattleController
    GameplayInput
    BallSpawner
    MergeSystem
    ProgressionService
    FeedbackService
  Arena
    Background
    Boundaries/{LeftWall,RightWall,TopWall,Floor}
    SpawnArea
    Cannon/{Visual,ShootPoint,MuzzleFlash}
    Runtime/{Balls,Projectiles,Effects}
  Audio/{MusicSource,SFXSource}
  Canvas
    SafeArea/{StartScreen,BattleHUD,ResultsScreen,MetaScreen}
    TransitionLayer
    EffectsLayer
  EventSystem (InputSystemUIInputModule)
```
Reference 1080×1920, CanvasScaler Scale With Screen Size, safe area. Границы вычислять из viewport с резервом HUD сверху, нижней зоны пушки и safe area; clamp по радиусу объекта. Пушка и спавн должны быть в пределах арены на разных аспектах. Не полагаться на фиксированные пиксели. Настраиваемая камера: ширина поля/scale должны сохранять игровой баланс; проверить 9:16 и 9:20.

## 3. Ответственность классов
| Класс | Контракт |
|---|---|
| GameController | Start/Battle/Results/Meta, переходы, единственная точка начала и завершения боя |
| BattleController | Сессия, запуск/остановка систем, очистка runtime, фиксация результата, награда |
| BattleSession | Данные: RemainingTime, Hearts, EarnedCoins, counts, ended; не хранит ссылки на UI |
| GameplayInput | Один активный pointer, классификация tap/drag, mouse/touch, UI exclusion |
| CannonController | Kinematic Rigidbody2D, MovePosition, ограничение, контакты, invulnerability |
| CannonShooter | Cooldown автострельбы, снапшот damage/fireRate/bounces на начало боя, spawn projectile |
| Projectile | FixedUpdate sweep, урон, отражение, остаток дистанции, lifetime |
| Ball | Номинал, HP, радиус, Rigidbody2D, состояние Active/Reserved/Removed, контакт, визуал |
| BombBall | Active/PlayerDetonated/TimedOut/Removed; таймер, успешный тап/штраф |
| BallSpawner | WaveConfig, случайный спавн, лимит, окно контролируемой пары 16, регистрация объединённых шаров |
| MergeSystem | Атомарное резервирование и обработка очереди пар текущей BattleSession |
| OverloadSystem | Снимок шаров, урон в радиусе, отдельный бонус за перегрузку |
| MergeFeedback | Временные визуальные эффекты Merge / Overload, shake, управление временем эффекта |
| ProgressionService | PlayerProgress, цены, проверка и атомарная покупка, WalletChanged |
| UIController | Экраны, подписки, кнопки, обновления, safe area |
| FeedbackService | VFX, SFX, shake, timeScale, анимации; не определяет правила |

Использовать сериализованные ссылки и простые C# events. Вызовы игровой логики прямые; события для представления и наблюдения. Не искать объекты каждый кадр. Подписки с симметричной отпиской. Все игровые мутации только при активной сессии, кроме явной очистки.

## 4. Состояния и жизненный цикл
Start → Battle → Results → Meta → Battle; Results → Battle для quick restart. Начало: остановить старые корутины/tweens, сбросить timeScale и fixedDeltaTime, удалить/деактивировать старый runtime, создать новую BattleSession, применить характеристики, разместить пушку, обновить HUD, включить ввод и спавн. При завершении пометить ended до любых вызовов подписчиков, остановить таймеры, симуляцию, ввод, спавн и выстрелы; завершить/очистить объекты без наград. Зафиксировать immutable BattleResult (win, coins, bonus, destroyed, merged, overloads, bombs tapped/missed). Зачислить ровно один раз; экран только визуализирует. Не использовать глобальный timeScale=0 как основной способ перехода: UI-анимации должны продолжать работать. Проверить повторные клики. При уходе приложения в фон приостановить активный бой и ввод; возобновление без скачка таймеров (предварительное техническое решение).

## 5. Prefabs / Physics
- Cannon: Kinematic RB2D, trigger collider, Visual/Animator, shoot point. CraftPix целые кадры: не требовать отдельных спрайтов ствола/корпуса. Автоанимация перезапускается/ускоряется под fire rate; выстрел не зависит от AnimationEvent.
- Для Ball используются Dynamic RB2D и CircleCollider2D. BallConfig задаёт gravity scale 0,5, начальную горизонтальную скорость 1,4–2,8 units/sec по модулю, минимальную вертикальную скорость отскока от пола 4,5 units/sec, bounciness 1 и friction 0. Визуал отделён от коллайдера. Freeze rotation для текста (или отдельный неповорачивающийся visual). Состояние запрещает попадания/merge после удаления. Эффекты scale не меняют физический радиус.
- BombBall: отдельный Dynamic prefab, world-space radial Filled Image и NumberText, 6 кадров Red Orb.
- Projectile: SpriteRenderer, TrailRenderer; логика CircleCast, без физического RB/collider. Слой Projectile для организации.
- Границы: статические Collider2D, слой Boundary.

Матрица (✓ — включить физический контакт; всё остальное выключить):
| | Cannon | Ball | Bomb | Projectile | Boundary |
|---|---|---|---|---|---|
| Cannon | — | ✓ | — | — | — |
| Ball | ✓ | ✓ | ✓ | — | ✓ |
| Bomb | — | ✓ | — | — | ✓ |
| Projectile | — | — | — | — | — |
| Boundary | — | ✓ | ✓ | — | — |
Cannon ограничивается кодом. Снаряд query mask Ball/Boundary; input query mask Bomb. Для cast исключить triggers; отдельный фильтр hit test для бомбы. Динамические шары использовать continuous detection при необходимости; sweep projectile не требует CCD Rigidbody. Не смешивать два подхода.

## 6. Рикошеты — алгоритм
На создание передать damage, speed, direction, remainingBounces, lifetime. В FixedUpdate: budget = speed * fixedDeltaTime (игровое время). CircleCast вдоль direction на budget. Без hit — переместить на budget. При hit выбрать ближайший допустимый контакт, сохранить normal и point/centroid до урона; переместить центр к centroid с небольшим skin наружу. При Ball нанести damage ровно один раз. Если remainingBounces == 0 — удалить пулю. Иначе decrement, direction = Vector2.Reflect(direction, normal).normalized, продолжить с остатком budget. Угол нормали и повторные контакты обрабатывать с проверкой dot(direction, normal), нулевой дистанции и небольшим skin; ограничить число итераций на шаг (например 6–8). Сохранять оставшуюся дистанцию только в рамках текущего шага; не разгонять пулю. Срок жизни 6 сек независимо от числа рикошетов. В углу один геометрический контакт обрабатывается за раз, не удваивать расход из-за overlap. Уничтожение шара не должно лишать нормали. Визуальный след очищать при повторном использовании. Если нужен пул объектов, внедрять только при измеренной проблеме.

## 7. Merge / Bomb
В OnCollisionEnter2D и OnCollisionStay2D шар передаёт пару MergeSystem. После проверки состояния, номинала, cooldown и текущей BattleSession оба шара сразу переходят в Reserved на главном потоке. В этот момент фиксируются HP, средняя позиция и средняя скорость с вертикальным импульсом из BallConfig. Повторные запросы и урон по этой паре блокируются. BattleController разбирает очередь в Update после физических callbacks, перед очередным спавном. При этом исходные коллайдеры выключаются и исходные шары удаляются без награды. BallSpawner вписывает новую окружность в арену по её радиусу, регистрирует результат и запускает cooldown 0,25 сек игрового времени. Restart и завершение боя очищают очередь и runtime.

Для 16+16 OverloadSystem берёт снимок зарегистрированных шаров, проверяет их состояние, сессию и положение центра относительно радиуса. Бонус берётся из награды условного номинала 32. Уничтоженные взрывом шары начисляют награду через обычный путь BallSpawner. MergeFeedback использует реальное время, а повторная перегрузка продлевает slow motion от момента последнего взрыва. Обычное завершение эффекта и очистка восстанавливают исходные timeScale, fixedDeltaTime и камеру.

BallSpawner проверяет MinSpawnInterval и номер кадра в общей точке спавна волны и контролируемой пары. Сразу после создания он сохраняет Time.timeAsDouble и Time.frameCount. До открытия интервала прогресс хранит максимум один запрос, при заполнении арены сбрасывается. Merge создаёт замену пары отдельным путём. Новые шары волны получают SpawnMergeCooldown 0,75 сек. Таймеры используют игровое время.

Контролируемая пара создаётся последовательно. Для первой попытки нужны два свободных места и время для второго спавна внутри FirstPairSpawnWindow. Для второго шара BallSpawner проверяет свободные верхние позиции и выбирает траекторию с учётом приблизительного отскока первого от пола и стен. Пара может встретиться естественно. При потере первого шара или истечении окна оставшийся запрос отменяется. Restart обнуляет прогресс, дедлайн и состояние пары.

Смертельный урон от Projectile передаёт в MergeFeedback позицию Rigidbody2D и tier до завершения удаления. Коллайдер выключается сразу. Отдельный визуальный объект живёт 0,35 сек реального времени. Урон Overload использует обычный путь наград без отдельного Ball Destroy. BattleController очищает эффекты при Restart и завершении боя.

BombBall.Update уменьшает scaled timer только в Battle. TimerImage.fillAmount = Clamp01(remaining/total). GameplayInput запоминает target на pointer down, проверяет живость при отпускании и Tap threshold. Если бомба уже истекла, тап не активирует её повторно. При тапе сначала атомарно сменить состояние, затем обработать снимок обычных шаров, затем награду/feedback и despawn. При таймауте вызвать DamagePlayer(ignoreInvulnerability: true). При окончании сессии Cancel без штрафа.

### 7.1 Shield Ball & Energy Shield — план P3.1

- Переиспользовать Ball, физику и текущий путь награды для Shield Ball. Типы Normal, Shield и Bomb разграничивают правила Merge, попадания и тап. Shield Ball сохраняет состояния Active → Removed. Ветка Reserved принадлежит обычным парам Merge.
- BallSpawner проверяет ShieldConfig, игровой таймер, наличие активного Shield Ball и общий лимит до выбора замены обычного спавна. Успешное создание использует общий интервал. Дедлайн Shield Ball обновляется после успешного создания. Завершение боя очищает объект и состояние спавна.
- При первом переходе Shield Ball в Removed от смертельного игрового урона начислить обычную награду и восстановить два заряда в текущей BattleSession. Этот путь общий для снаряда, Overload и успешной бомбы. Despawn для очистки обходит выдачу защиты и монет.
- BattleSession хранит заряды, жизни и срок неуязвимости. При контакте BattleController сначала проверяет активность сессии и неуязвимость, затем расходует один заряд или снимает сердце. Принятый контакт запускает неуязвимость 1,5 сек и существующий отскок. Штраф пропущенной бомбы идёт напрямую в потерю сердца. Restart и конец боя очищают заряды и визуальные эффекты.
- ShieldView и BattleHud отображают заряды. Система feedback показывает получение, поглощение и разрушение щита по событиям боя. Коллайдер пушки остаётся неизменным. Настраиваемые параметры будущего ShieldConfig заданы в GDD.

## 8. Конфигурации
Конфигурации ScriptableObject включают BattleConfig (время, сердца, награда за победу, неуязвимость), BallConfig (номиналы, HP, радиус, графика, монеты, физика, SpawnMergeCooldown, MergeCooldown, Overload), CannonConfig (движение и параметры снаряда), ProgressionConfig (10 уровней пушки, 3 уровня рикошета), WaveConfig (фазы, кривая, окно пары 16, MaxActive, MinSpawnInterval), BombConfig (шанс, интервалы, таймер и номиналы) и FeedbackConfig (shake, замедление, pitch и длительности). Значения ассетов сохраняются при запуске боя.

CannonLevelData: Level, UpgradeCost (цена достижения), Damage, FireRate, MaxAvailableBounces, Visual. PlayerProgress: Coins=0, CannonLevel=1, RicochetLevel=0. RicochetData: Level, RequiredCannonLevel, Price. При покупке проверять next level, max, деньги и prerequisite до изменения состояния. Характеристики и визуал из конфигов; только GDD определяет стартовые значения.

WaveConfig задаёт лимит 8 и частоты 0,3 / 0,4 / 0,5 / 0,6 / 0,8 / 1,0 шара/сек на отметках 0 / 15 / 30 / 45 / 60 / 75 сек. Совпадающие касательные в узлах дают плавный монотонный переход, а нулевая касательная на 75 сек согласует кривую с постоянной частотой после этой отметки. При заполнении арены BallSpawner сбрасывает прогресс интервала. После освобождения места он отсчитывает новый интервал и выпускает максимум один шар за обновление. Restart очищает активные объекты и прогресс спавна. В P1 спавнер работает, пока BattleSession активна. Завершение по таймеру добавляется в P4.

## 9. События / аудио
BattleStarted, BattleEnded, HealthChanged, TimerChanged, BattleCoinsChanged, BallHit, BallDestroyed, BallsMerged, OverloadTriggered, BombSpawned, BombDetonated, ProjectileRicocheted, UpgradePurchased, WalletChanged. Позиции/номинал передавать в payload для FX, не зависеть от уже удалённого Transform. Не начислять награду в двух подписчиках. FeedbackService управляет единственным slowdown: хранит исходный fixedDeltaTime, восстанавливает на новом бою/выходе/disable; эффект длится unscaled. UI animation unscaled; звук см. AUDIO_GUIDE. При сильных эффектах не мешать тач-контролю.

## 10. Проверка и сборка
Unity Android Build Support + SDK/NDK/OpenJDK из Hub. Main в Build Profiles, Portrait, package name временно com.example.mergeblast (сменить при необходимости), ARM64, Android минимум по выбранной версии Unity и доступному устройству; APK, не AAB. Проверить на устройстве 60 FPS target, отсутствие ошибок, корректный экран, свайпы/тапы, 3 последовательных боя, покупку и рестарт. WebGL — только если Android недоступен. Не заявлять тест/сборку без фактического запуска. Полный QA checklist — TASKS.md.
