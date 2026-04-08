# Система сохранений — План реализации

> **Технология:** JSON + `JsonUtility` (встроенный в Unity)
> **Слоты:** 3 слота сохранения
> **Автосохранение:** Каждые 5 минут + ручное через ESC-меню
> **Путь:** `Application.persistentDataPath + "/save_slot_N.json"`

---

## Сравнение технологий сохранения

### Вариант 1: PlayerPrefs (встроенный Unity)

**Что это:** Встроенное key-value хранилище Unity. Данные хранятся в реестре Windows (`HKCU\Software\Unity\...`), на Mac — в plist, на Android — в SharedPreferences.

```csharp
PlayerPrefs.SetFloat("growth", 45.5f);
float growth = PlayerPrefs.GetFloat("growth");
```

| ✅ Плюсы | ❌ Минусы |
|----------|----------|
| Встроенный, 0 строк настройки | Только примитивы: int, float, string |
| Максимально простой API | **Не файл** — реестр Windows, нельзя показать на защите |
| Работает на всех платформах | Нет структуры — 20+ отдельных ключей, легко запутаться |
| | Лимит размера (~1 МБ) |
| | Пользователь может случайно стереть при очистке реестра |
| | Невозможно поддерживать несколько слотов без костылей |
| | При изменении структуры данных — нужно руками мигрировать каждый ключ |

**Вердикт:** Подходит для настроек (громкость, разрешение). Для полноценных сохранений игры — **не рекомендуется**.

---

### Вариант 2: JSON + JsonUtility ⭐ ВЫБРАН

**Что это:** Сериализация C# объекта в JSON-строку встроенным Unity-сериализатором и запись в `.json` файл через `System.IO`.

```csharp
string json = JsonUtility.ToJson(saveData, true);
File.WriteAllText(path, json);

string loaded = File.ReadAllText(path);
SaveData data = JsonUtility.FromJson<SaveData>(loaded);
```

| ✅ Плюсы | ❌ Минусы |
|----------|----------|
| **Встроенный** в Unity — 0 зависимостей, 0 NuGet | Файл человекочитаемый — игрок может отредактировать |
| Одна структура `SaveData` = одно целое сохранение | Чуть больше кода чем PlayerPrefs (~15-20 строк) |
| **Человекочитаемый** файл — легко дебажить и показать на защите | `JsonUtility` не поддерживает Dictionary (нам не нужен) |
| Легко расширять — добавил поле в класс, старые сейвы не ломаются | |
| `Application.persistentDataPath` — кросс-платформенный путь | |
| Несколько слотов = несколько файлов, элементарно | |
| Быстрый (~1мс на наш объём данных) | |
| Понятная архитектура для дипломного проекта | |

**Вердикт:** Идеальный баланс простоты, надёжности и презентабельности. Стандарт индустрии для indie игр.

---

### Вариант 3: BinaryFormatter

**Что это:** Бинарная сериализация .NET. Превращает объект в поток байтов.

```csharp
BinaryFormatter bf = new BinaryFormatter();
bf.Serialize(fileStream, saveData);
```

| ✅ Плюсы | ❌ Минусы |
|----------|----------|
| Компактный файл (байты вместо текста) | ⚠️ **Microsoft официально не рекомендует** (security risk) |
| Нечитаемый — игрок не отредактирует | **Deprecated** в .NET 5+ |
| | При ЛЮБОМ изменении класса — старые сейвы ломаются |
| | Уязвимости десериализации (Remote Code Execution) |
| | Unity показывает предупреждение в консоли |

**Вердикт:** Устаревший и **небезопасный** подход. Microsoft прямо пишет: "BinaryFormatter is insecure and can't be made secure". Не для нового проекта.

---

### Вариант 4: SQLite / база данных

**Что это:** Реляционная БД в одном файле. Требует библиотеку `sqlite-net` или `Mono.Data.Sqlite`.

```csharp
var db = new SQLiteConnection(path);
db.CreateTable<SaveData>();
db.Insert(saveData);
```

| ✅ Плюсы | ❌ Минусы |
|----------|----------|
| Мощные SQL-запросы | **Полный оверкилл** для 3 слотов сохранения |
| Хорошо для MMO/серверов | Внешняя библиотека — нужно настраивать |
| Транзакции, целостность данных | В 5-10 раз больше кода |
| | Сложнее дебажить |
| | Избыточно для дипломного проекта |

**Вердикт:** Для MMO с тысячами игроков — да. Для синглплеера с тремя слотами — абсолютный оверкилл.

---

### Вариант 5: Newtonsoft JSON (Json.NET)

**Что это:** Сторонняя библиотека для JSON, самая популярная в .NET мире.

| ✅ Плюсы | ❌ Минусы |
|----------|----------|
| Поддерживает Dictionary, Queue, любые типы | **Внешняя зависимость** — нужно устанавливать |
| Гибкая настройка сериализации | Для наших целей `JsonUtility` делает то же самое |
| | Немного медленнее чем JsonUtility |

**Вердикт:** Мощнее, но нам не нужна эта мощь. `JsonUtility` покрывает 100% наших потребностей без внешних зависимостей.

---

## Почему JSON + JsonUtility

1. **0 зависимостей** — ничего не нужно устанавливать, всё есть в Unity
2. **Человекочитаемый** — открываешь файл и видишь `"speciesName": "Тиранозавр"`, а не поток байтов
3. **На защите диплома** — можно открыть файл сохранения и показать комиссии что именно сохраняется
4. **Расширяемость** — добавить новое поле = добавить строку в класс, старые сейвы продолжают работать
5. **Надёжность** — если файл повреждён, JsonUtility бросит исключение, которое мы обработаем
6. **Стандарт индустрии** — большинство indie игр используют именно JSON для сохранений

---

## Структура данных сохранения

```csharp
[System.Serializable]
public class SaveData
{
    // Вид
    public string speciesName;

    // Позиция
    public float posX, posY, posZ;
    public float rotationY;

    // Рост
    public float currentGrowth;
    public int growthStageIndex;

    // Здоровье
    public int currentHP;

    // Опыт
    public int currentXP;
    public int currentLevel;

    // Прокачка
    public int availablePoints;
    public int bonusHP;
    public int bonusATK;
    public float bonusSPD;

    // Потребности
    public float currentHunger;
    public float currentThirst;

    // Мета
    public string saveTimestamp;
    public int slotIndex;
}
```

---

## Новые файлы (4 шт.)

### 1. `Assets/Scripts/Save/SaveData.cs`
Сериализуемый класс — структура данных сохранения (см. выше).

### 2. `Assets/Scripts/Save/SaveSystem.cs`
Статический класс, управляет файлами:

```
SaveSystem
├── Save(int slotIndex)       — собрать данные → JSON → файл
├── Load(int slotIndex)       — файл → JSON → SaveData
├── HasSave(int slotIndex)    — bool: есть ли файл
├── DeleteSave(int slotIndex) — удалить файл
├── GetSaveInfo(int slotIndex)— SaveData (для UI списка: время, вид)
├── GetSavePath(int slotIndex)— string: путь к файлу
└── MAX_SLOTS = 3             — константа
```

**Сбор данных для Save():**
- Находит Player (по тегу или FindObjectOfType)
- Читает из: PlayerGrowth, ExperienceSystem, StatUpgradeSystem, PlayerHealth, PlayerNeeds
- Каждая система получит публичный метод для экспорта своего состояния

### 3. `Assets/Scripts/Save/AutoSaveManager.cs`
MonoBehaviour, висит на объекте в игровой сцене:

```
AutoSaveManager
├── autoSaveInterval = 300f   — 5 минут
├── activeSlot = 0            — в какой слот автосейв
├── Start(): запуск корутины
├── AutoSaveLoop(): каждые N секунд → SaveSystem.Save(activeSlot)
└── OnDestroy(): автосейв при выходе со сцены
```

### 4. `Assets/Scripts/UI/PauseMenuUI.cs`
MonoBehaviour, висит на Canvas в игровой сцене:

```
PauseMenuUI
├── ESC → toggle панель паузы
├── Time.timeScale = 0/1      — пауза мира
├── Cursor lock/unlock
├── Кнопки:
│   ├── "Сохранить" → показать панель слотов
│   ├── "Загрузить" → показать панель слотов
│   ├── "В главное меню" → сохранить + LoadScene("MainMenu")
│   └── "Продолжить" → закрыть меню
├── Панель слотов:
│   ├── Слот 1 [кнопка] — "Тирекс, уровень 5, 12:30 03.04.2026"
│   ├── Слот 2 [кнопка] — "Пустой слот"
│   └── Слот 3 [кнопка] — "Пектинодон, уровень 2, 11:00 03.04.2026"
└── PlayerInput блокировка      — при паузе input отключён
```

---

## Модификации существующих файлов (7 шт.)

### 5. `PlayerGrowth.cs`
**Добавить:**
- `public float CurrentGrowth => currentGrowth;` — геттер
- `public int CurrentStageIndex => currentStageIndex;` — геттер
- `public void LoadState(float growth, int stageIndex)` — восстановить рост из сейва, пересчитать статы, продолжить GrowthLoop с нужного места

### 6. `ExperienceSystem.cs`
**Добавить:**
- `public void LoadState(int xp, int level)` — установить XP и уровень, опубликовать событие

### 7. `StatUpgradeSystem.cs`
**Добавить:**
- `public void LoadState(int points, int hp, int atk, float spd)` — восстановить очки и бонусы, опубликовать события

### 8. `PlayerHealth.cs`
**Добавить:**
- `public void LoadHP(int hp)` — установить текущее HP без триггера смерти

### 9. `PlayerNeeds.cs`
**Добавить:**
- `public void LoadNeeds(float hunger, float thirst)` — установить текущие потребности

### 10. `GameSession.cs`
**Добавить:**
- `public static bool IsLoadingFromSave;` — флаг: загружаемся из сейва
- `public static int ActiveSaveSlot;` — в какой слот был последний save/load

### 11. `DinosaurInitializer.cs`
**Изменить Start/Awake:**
```
Если GameSession.IsLoadingFromSave:
    1. Инициализировать вид как обычно (модель, способность, контроллер)
    2. SaveData data = SaveSystem.Load(GameSession.ActiveSaveSlot)
    3. Вызвать LoadState() на каждой системе
    4. Поставить transform.position из SaveData
    5. GameSession.IsLoadingFromSave = false
Иначе:
    Обычная инициализация (как сейчас)
```

### 12. `PlayerInput.cs`
**Добавить:**
- `public bool PauseInput => Input.GetKeyDown(KeyCode.Escape);` — для ESC-меню
- Поле `isMenuOpen` и метод `SetMenuLock(bool locked)` — блокировка инпута при паузе

### 13. `MainMenuManager.cs`
**Добавить:**
- Ссылка на панель загрузки (`loadPanel`)
- `OnContinueClicked()` → показать панель слотов с сохранениями
- `OnSlotSelected(int slot)` → загрузить из слота
- При старте: проверить `SaveSystem.HasSave(0..2)` → если хотя бы один есть, кнопка "Продолжить" активна

---

## Порядок реализации

```
Фаза 1: Ядро (SaveData + SaveSystem)
    └── Создать SaveData.cs и SaveSystem.cs
    └── Добавить геттеры/LoadState во все системы игрока

Фаза 2: Паузменю (PauseMenuUI)
    └── Создать PauseMenuUI.cs
    └── Добавить ESC в PlayerInput
    └── Ручное сохранение/загрузка через панель слотов

Фаза 3: Автосохранение (AutoSaveManager)
    └── Создать AutoSaveManager.cs
    └── Автосейв каждые 5 минут

Фаза 4: Главное меню (MainMenuManager)
    └── Кнопка "Продолжить" + панель слотов

Фаза 5: Тестирование
    └── Полный цикл: новая игра → играть → сохранить → выйти → загрузить
```

---

## Что делает пользователь в Unity

После написания кода:
1. **Создать PauseMenu Canvas** в игровой сцене (панель паузы, панель слотов, кнопки)
2. **Повесить PauseMenuUI** на Canvas, подключить ссылки
3. **Повесить AutoSaveManager** на пустой объект в игровой сцене
4. **Обновить MainMenu** — добавить кнопку "Продолжить", панель слотов загрузки
5. **Подключить кнопки через Inspector** (onClick)

---

## Пример JSON файла сохранения

```json
{
    "speciesName": "Тиранозавр",
    "posX": 125.3,
    "posY": 15.1, 
    "posZ": -42.7,
    "rotationY": 180.0,
    "currentGrowth": 45.5,
    "growthStageIndex": 2,
    "currentHP": 180,
    "currentXP": 340,
    "currentLevel": 4,
    "availablePoints": 2,
    "bonusHP": 30,
    "bonusATK": 6,
    "bonusSPD": 0.6,
    "currentHunger": 67.3,
    "currentThirst": 89.1,
    "saveTimestamp": "07.04.2026 20:15",
    "slotIndex": 0
}
```
