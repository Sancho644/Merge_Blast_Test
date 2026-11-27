# Audio Guide — Neon Arcade

Подобранные файлы доступны в загруженных архивах. Назначения SFX предварительные — проверить в бою, не утверждать факт прослушивания каждого клипа. Больше наборов не требуется.

## 1. Музыка
| Состояние | Источник | Импорт | Длительность (ffprobe) |
|---|---|---|---|
| Start / Meta / Results после акцента | Synth Wave.mp3 | Menu.mp3 | 192.055 сек |
| Battle | neon_sign_circuit_bpm145.ogg | Battle.ogg | 99.310 сек |

Оба 44.1 kHz, stereo. Loop включён; MP3 может иметь заметный стык — проверить, при необходимости обрезать/перекодировать с сохранением лицензии. Плавные переходы ~0.3–0.5 сек; отдельные Victory/Defeat сигналы. При повторном запуске боя музыкальная петля начинается заново либо с чистой точки (предварительно — с начала). Не запускать вторую копию трека.

## 2. SFX mapping
Источник: `50-CC0-retro-synth-SFX.zip` (50 OGG в корне, 50 .xpf в source/ — исходники не нужны в Unity). Все имена ниже существуют в архиве. Один клип может обслуживать разные события.

| Event | Файл | Правило |
|---|---|---|
| CannonShoot | shot_01.ogg / shot_02.ogg | Чередование/рандом, pitch 0.95–1.05, тихо |
| BallHit | retro_misc_02.ogg | Короткий, лимит частоты |
| BallDestroy | retro_misc_03.ogg | Pop |
| BallMerge | power_up_01.ogg | Pitch 1.00 / 1.12 / 1.25 / 1.40 по результату 4/8/16/32 |
| Overload | retro_explosion_05.ogg | Приоритетный, duck music |
| Ricochet | synth_laser_03.ogg | Короткий, rate limit |
| BombSpawn | synth_beep_03.ogg | Отличимый сигнал |
| BombTick | synth_beep_01.ogg | Последние 2 сек, ускорение |
| BombSuccess | retro_explosion_01.ogg | Энергетический взрыв |
| BombFail | retro_explosion_03.ogg | Тревожный, не дублировать громкий Damage |
| PlayerDamage | retro_die_01.ogg | При обычном столкновении; при BombFail выбрать баланс слоёв |
| CoinCollect | retro_coin_01.ogg | Ограничить на серии монет |
| ButtonClick | retro_misc_02.ogg | Тихий, не для disabled |
| PanelOpen | synth_misc_12.ogg | Короткий переход |
| RewardCollect | retro_coin_02.ogg | Один акцент + count-up |
| CannonUpgrade | power_up_03.ogg | Один на успешную покупку |
| RicochetUpgrade | power_up_05.ogg | Особый акцент |
| PurchaseFailed | synth_beep_02.ogg | Только попытка покупки, не проигрывать спамом |
| Victory | power_up_03.ogg | Один акцент |
| Defeat | retro_die_03.ogg | Один акцент |

20 событий + альтернативный выстрел = 21 назначение, 19 уникальных SFX-файлов (BallHit/ButtonClick и CannonUpgrade/Victory переиспользуют клипы). Итого 21 уникальный аудиофайл вместе с 2 BGM. Не копировать одинаковый клип в разные папки.

## 3. Mixer
MainAudioMixer: Master, Music, Gameplay SFX, UI SFX. Начальный линейный gain 1.0 / 0.35 / 0.70 / 0.85 (эквивалент 0 / −9.1 / −3.1 / −1.4 dB, формула 20*log10(gain)). Дополнительный per-clip gain для выстрелов, попаданий и тиков. 2D AudioSources. Ограничить одновременные голоса (ориентир 8–12), частые Hit/Ricochet/Coin объединять rate limit ~0.05–0.1 сек, не терять критичные BombFail/Overload/Upgrade. Pitch на отдельных источниках/голосах, не менять общий AudioSource.pitch для уже играющих звуков. Duck music кратко на Overload и результатах. UI звуки продолжают работать при замедлении; не замедлять звук автоматически без осознанного решения.

## 4. Импорт
SFX: Force To Mono, Decompress On Load, PCM, Preload Audio Data On, Loop Off. Music: stereo, Streaming, Vorbis ~70% для импорта, Loop On. Sample Rate Preserve, Spatial Blend 0. Проверить clipping, слышимость BombTick поверх боя, переходы, stop на окончании/рестарте, отсутствие двойных событий. Экран настроек не нужен по ТЗ. Источники и лицензии — ASSET_LIST.md.
