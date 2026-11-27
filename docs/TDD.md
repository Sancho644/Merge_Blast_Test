# TDD — Merge Blast: Overload v0.2

27.11.2026 • Техническая спецификация, не отчёт о реализации. Источник игровых правил — GDD.md.

## 1. Стек и структура
Unity 6 LTS (точную версию зафиксировать после создания проекта), Universal 2D URP, Android Portrait, 60 FPS target, uGUI + TMP, Input System, Physics2D, DOTween (если доступен и лицензия допускает; иначе простые корутины/Animator). Одна сцена Main. Никаких ECS/DI/EventBus/сохранений. Не подключать Spine: у CraftPix есть PNG кадры.

```
Assets/
  _Project/{Art,Audio,Configs,Prefabs,Scenes,Scripts}/
  _Project/Scripts/{Core,Input,Cannon,Projectiles,Balls,Gameplay,Progression,Configs,UI,Feedback}/
  _Project/Prefabs/{Gameplay,UI,VFX}/
  ThirdParty/                  # только разрешённые к передаче файлы
```
Ограниченные сторонние исходники хранить локально, отдельно от публичного Git (см. ASSET_LIST). Не коммитить Library, Temp, Logs, Obj, Builds, IDE caches, UserSettings, ключи подписи. Meta-файлы разрешённых ассетов коммитить вместе с ними. Не создавать проект/репозиторий от имени пользователя без запроса.

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
| BallSpawner | WaveConfig, случайность, лимит, гарантированный запрос пары 16, учёт бомб |
| MergeSystem | Очередь пар, атомарное резервирование, слияние, Overload, границы |
| ProgressionService | PlayerProgress, цены, проверка и атомарная покупка, WalletChanged |
| UIController | Экраны, подписки, кнопки, обновления, safe area |
| FeedbackService | VFX, SFX, shake, timeScale, анимации; не определяет правила |

Использовать сериализованные ссылки и простые C# events. Вызовы игровой логики прямые; события для представления и наблюдения. Не искать объекты каждый кадр. Подписки с симметричной отпиской. Все игровые мутации только при активной сессии, кроме явной очистки.

## 4. Состояния и жизненный цикл
Start → Battle → Results → Meta → Battle; Results → Battle для quick restart. Начало: остановить старые корутины/tweens, сбросить timeScale и fixedDeltaTime, удалить/деактивировать старый runtime, создать новую BattleSession, применить характеристики, разместить пушку, обновить HUD, включить ввод и спавн. При завершении пометить ended до любых вызовов подписчиков, остановить таймеры, симуляцию, ввод, спавн и выстрелы; завершить/очистить объекты без наград. Зафиксировать immutable BattleResult (win, coins, bonus, destroyed, merged, overloads, bombs tapped/missed). Зачислить ровно один раз; экран только визуализирует. Не использовать глобальный timeScale=0 как основной способ перехода: UI-анимации должны продолжать работать. Проверить повторные клики. При уходе приложения в фон приостановить активный бой и ввод; возобновление без скачка таймеров (предварительное техническое решение).

## 5. Prefabs / Physics
- Cannon: Kinematic RB2D, trigger collider, Visual/Animator, shoot point. CraftPix целые кадры: не требовать отдельных спрайтов ствола/корпуса. Автоанимация перезапускается/ускоряется под fire rate; выстрел не зависит от AnimationEvent.
- Ball: Dynamic RB2D, CircleCollider2D, gravity 0.8, material restitution 1, friction 0; визуал отделён от коллайдера. Freeze rotation для текста (или отдельный неповорачивающийся visual). Min bounce от пола. Состояние запрещает попадания/merge после удаления. Эффекты scale не меняют физический радиус.
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
В OnCollisionEnter/Stay шар передаёт пару MergeSystem. Резервировать Active + Active одинакового номинала, вне cooldown, один запрос на пару. Удаление и создание выполнить после физического callback (очередь), удалить коллайдеры/выключить участие сразу после резервирования; повторные события игнорируются. HP и позиции снапшотить; отмена запроса при недействительной сессии. Overload обходит снимок списка активных обычных шаров в радиусе, не коллекцию, которую модифицирует. Один путь начисления награды за уничтожение и один отдельный бонус Overload.

BombBall.Update уменьшает scaled timer только в Battle. TimerImage.fillAmount = Clamp01(remaining/total). GameplayInput запоминает target на pointer down, проверяет живость при отпускании и Tap threshold. Если бомба уже истекла, тап не активирует её повторно. При тапе сначала атомарно сменить состояние, затем обработать снимок обычных шаров, затем награду/feedback и despawn. При таймауте вызвать DamagePlayer(ignoreInvulnerability: true). При окончании сессии Cancel без штрафа.

## 8. Конфигурации
ScriptableObject: BattleConfig (duration, hearts, victoryBonus, invuln); BallConfig (tiers, HP, radius, sprite, color, reward, physics, mergeCooldown, Overload); CannonConfig (moveSpeed, bulletSpeed, bulletRadius, lifetime); ProgressionConfig (10 CannonLevelData, 3 RicochetData); WaveConfig (phases, spawn curve, first pair 16, maxActive); BombConfig (spawn chance, cooldown, duration, tiers); FeedbackConfig (shake, slowdown, pitches, timing). Не мутировать SO при игре.

CannonLevelData: Level, UpgradeCost (цена достижения), Damage, FireRate, MaxAvailableBounces, Visual. PlayerProgress: Coins=0, CannonLevel=1, RicochetLevel=0. RicochetData: Level, RequiredCannonLevel, Price. При покупке проверять next level, max, деньги и prerequisite до изменения состояния. Характеристики и визуал из конфигов; только GDD определяет стартовые значения.

## 9. События / аудио
BattleStarted, BattleEnded, HealthChanged, TimerChanged, BattleCoinsChanged, BallHit, BallDestroyed, BallsMerged, OverloadTriggered, BombSpawned, BombDetonated, ProjectileRicocheted, UpgradePurchased, WalletChanged. Позиции/номинал передавать в payload для FX, не зависеть от уже удалённого Transform. Не начислять награду в двух подписчиках. FeedbackService управляет единственным slowdown: хранит исходный fixedDeltaTime, восстанавливает на новом бою/выходе/disable; эффект длится unscaled. UI animation unscaled; звук см. AUDIO_GUIDE. При сильных эффектах не мешать тач-контролю.

## 10. Проверка и сборка
Unity Android Build Support + SDK/NDK/OpenJDK из Hub. Main в Build Profiles, Portrait, package name временно com.example.mergeblast (сменить при необходимости), ARM64, Android минимум по выбранной версии Unity и доступному устройству; APK, не AAB. Проверить на устройстве 60 FPS target, отсутствие ошибок, корректный экран, свайпы/тапы, 3 последовательных боя, покупку и рестарт. WebGL — только если Android недоступен. Не заявлять тест/сборку без фактического запуска. Полный QA checklist — TASKS.md.
