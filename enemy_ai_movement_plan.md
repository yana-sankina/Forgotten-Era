# Enemy AI: Movement + Combat (v1)

## Goal
Сделать ботов “живыми” без усложнений: патруль, обнаружение, преследование, атака, побег при low HP. Все должно работать на текущей архитектуре (спавнеры/статы/трупы/ивенты) и не требовать ручной настройки каждой мелочи на каждом префабе.

## Chosen Approach (и почему)
### 1) NavMeshAgent + FSM
**Плюсы:**
- Нормально обходит препятствия и рельеф, меньше застреваний на ассетных коллайдерах.
- Быстрее получить “играбельное” поведение, чем писать свой pathfinding.
- Масштабируется на 15–20 врагов при разумных интервалах обновления пути.

**Минусы:**
- Нужно руками запечь NavMesh в Unity (иначе бот не поедет).
- Плохие коллайдеры/слои могут испортить NavMesh (решается настройкой NavMeshSurface: собирать только землю/terrain).

### 2) Атака: FSM включает/выключает EnemyHitbox (без DummyAttackSpam)
**Плюсы:**
- Один источник правды: состояние Attack управляет таймингами и кулдауном.
- Нет двойных атак (DummyAttackSpam + AI) и меньше “магии”.
- Не требует ручной ссылки `attackHitboxObject` (AI сам находит `EnemyHitbox` в детях).

**Минусы:**
- Нужно аккуратно настроить `attackRange` и расположение хитбокса (это все равно делается 1 раз на болванке).

**Решение по DummyAttackSpam:**
- Оптимально **не удалять файл сразу**, чтобы не получить `Missing Script` на уже существующих префабах/сценах.
- В новой системе: `EnemySpawner/EnemyAI` **отключает** `DummyAttackSpam`, если он есть.
- Удаление `DummyAttackSpam.cs` делаем позже, когда ты вручную уберешь компонент со всех префабов.

### 3) Конфиг через EnemyRuntimeProfile (а не отдельный EnemyAIConfig)
**Плюсы:**
- Меньше ассетов: один профиль = модель + статы + AI.
- Уже используется спавнером, значит меньше ручной wiring.

**Минусы:**
- Файл профиля станет “толстым” (визуал + статы + AI). Для 2–3 типов это ок.

## Implementation Plan
### A) EnemyAI.cs (новый)
- Компоненты: `NavMeshAgent`, `Damageable`, `EnemyHitbox` (в детях).
- FSM состояния: `Patrol`, `Chase`, `Attack`, `Flee`, `Dead/Corpse`.
- Общие правила:
- Если `Damageable.IsDead || Damageable.IsCorpse` -> полностью стопаем AI.
- Если `Damageable.IsStunned` -> `agent.isStopped = true`, хитбокс выключен, состояние не атакует.
- Обновление пути не каждый кадр: `repathInterval = 0.2–0.35s`.

**Patrol**
- Выбирает случайные точки в радиусе `patrolRadius` через `NavMesh.SamplePosition`.
- Таймаут на точку (`patrolPointTimeout`), stuck recovery.

**Chase**
- Цель: игрок (по tag `Player`, кешируем transform).
- `SetDestination` по интервалу.
- Потеря цели: если дистанция > `loseTargetRadius` N секунд -> назад в Patrol.

**Attack**
- Входит, если `distance <= attackRange`.
- Останавливает агент (`isStopped = true`), разворачивается к игроку.
- Включает `EnemyHitbox` на `attackDuration`, потом выключает.
- Ждет `attackCooldown`.

**Flee**
- Триггер: `HP% < fleeHpThreshold`.
- Находит точку от игрока (вектор “от него” + `NavMesh.SamplePosition`), бежит `fleeDuration`.
- Потом возвращается в Patrol (или в Chase, если игрок близко и HP >= `reengageHpThreshold`).

### B) EnemyRuntimeProfile.cs (расширить)
Добавить AI-поля с дефолтами:
- `patrolRadius`, `patrolPointTimeout`.
- `detectionRadius`, `loseTargetRadius`.
- `attackRange`, `attackDuration`, `attackCooldown`.
- `patrolSpeed`, `chaseSpeed`, `fleeSpeed`.
- `fleeHpThreshold`, `fleeDuration`, `reengageHpThreshold`.

### C) EnemySpawner.cs (интеграция)
После спавна и назначения профиля:
- Гарантировать наличие `NavMeshAgent` и `EnemyAI`.
- Настроить `NavMeshAgent` под размер:
- если на болванке `CapsuleCollider` -> брать `radius/height` из него;
- иначе fallback на разумные дефолты.
- Передать `EnemyRuntimeProfile` в `EnemyAI`.
- Найти `EnemyHitbox` в детях: убедиться что он выключен по умолчанию, AI будет включать в Attack.
- Если есть `DummyAttackSpam` -> `enabled = false`.

## Unity Manual Setup (ты делаешь руками)
- NavMesh:
- Добавить `NavMeshSurface` (из `com.unity.ai.navigation`) на объект земли.
- В `Collect Objects` лучше собирать только землю/terrain (по Layer), чтобы камни с кривыми коллайдерами не портили навмеш.
- Нажать `Bake`.

- Enemy prefab (болванка):
- На корне: `Damageable` + 1 collider (Capsule/Box).
- Дочерний объект атаки: `EnemyHitbox` на trigger-коллайдере (позиция у пасти/лап). Выключен по умолчанию.
- Убедиться, что игрок имеет tag `Player`.

## Test Plan
- Patrol: ходит по точкам, не дергается, не залипает.
- Chase: стабильно догоняет при входе в `detectionRadius`.
- Attack: атакует только в `attackRange`, хитбокс включается окнами, нет постоянного дамага.
- Flee: при low HP уходит, потом возвращается к патрулю.
- Death/Corpse: при смерти AI и атака выключаются, труп работает как еда.

## Notes / Future
- Позже можно добавить line-of-sight (Raycast), стаи, разные тактики, анимационные события атаки.
- `DummyAttackSpam.cs` удалять только после ручного удаления компонента со всех префабов.
