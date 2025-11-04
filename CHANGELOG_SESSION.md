# Changelog - Сессия 4 ноября 2025

## Последние изменения

### Коммит: b39daaf - "feat: Add editable data types for tags with validation rules"

#### Добавлено:
1. **Правила для типов данных тегов** (`TagDataTypeRules` в `Models/TagDefinition.cs`):
   - X, Y, M, T, C, SM, S → только `Bool`
   - AI, AQ → только `Int16` (signed 16-bit, -32768~32767)
   - TV, CV, SV → `Int16` или `Int32`
   - V → `Int16`, `Int32`, или `Float32`
   - Метод `GetAllowedDataTypes(string tagName)` - возвращает допустимые типы
   - Метод `GetDefaultDataType(string tagName)` - возвращает тип по умолчанию

2. **Свойство `AllowedDataTypes`** в `TagDefinition`:
   - Динамически вычисляется на основе имени тега
   - Используется для заполнения ComboBox в UI
   - Атрибут `[JsonIgnore]` - не сериализуется

3. **Редактируемая колонка типа данных** в `TagsEditorWindow`:
   - ComboBox для выбора типа данных
   - Ограничение допустимых значений по правилам
   - Режим редактирования по двойному клику

4. **Python скрипт `update_tags_types.py`**:
   - Аккуратно обновляет типы данных в tags.json
   - Сохраняет всю структуру файла
   - Обновлено 2693 тега из 35421

#### Обновлено:
1. **tags.json** - типы данных обновлены согласно схеме:
   - Bool теги: X0-X1023, Y0-Y1023, M0-M7999, T0-T511, C0-C255, SM0-SM2047, S0-S1023
   - Int16 теги: AI0-AI16, AQ0-AQ16, V0-V7999, TV0-TV511, CV0-CV255, SV0-SV999
   - Все изменения применены без нарушения структуры файла

#### Технические детали:
- DataGridTemplateColumn с CellTemplate и CellEditingTemplate
- Двусторонняя привязка `{Binding Type, Mode=TwoWay}`
- ComboBox привязан к `AllowedDataTypes` для фильтрации
- Python скрипт использует json.load/dump с сохранением отступов
- Обработка 35421 тегов за несколько секунд

---

## Сессия: Добавление ImageControl и улучшения UI

### Коммит: 645ec3a - "feat: Add ImageControl, improve dialogs, fix ImageButton scaling"

#### Добавлено:
1. **ImageControl** - новый элемент управления для вставки изображений на мнемосхему
   - Файлы: `Views/Controls/ImageControl.axaml`, `Views/Controls/ImageControl.axaml.cs`
   - Возможности:
     - Выбор изображения через файловый диалог (PNG, JPG, JPEG, BMP, GIF)
     - Настройка ширины и высоты (10-1000 пикселей)
     - Редактирование названия и подписи
     - Опция показа/скрытия подписи
     - Удаление элемента
   - Интеграция с MainWindow для сохранения/загрузки из settings.json

2. **ImageElement** в Models/MnemoschemeElement.cs
   - Свойства: ImagePath, Width, Height, Label
   - Полиморфная сериализация с discriminator "image"

#### Улучшено:
1. **Диалоговые окна** - добавлен ScrollViewer для всех настроек:
   - SliderControl: 600x500 с прокруткой
   - NumericInputControl: 600x450 с прокруткой
   - DisplayControl: 600x450 с прокруткой
   - ImageControl: 600x600 с прокруткой
   - ImageButton: 600x700 с прокруткой
   - MainWindow (добавление элементов): 600x700 с прокруткой

2. **Поля ввода диапазонов** - увеличены с 100px до 150px:
   - SliderControl (Мин/Макс)
   - ImageControl (Ширина/Высота)
   - ImageButton (Ширина/Высота)

3. **ImageButton** - улучшено масштабирование:
   - Добавлены свойства ImageWidth и ImageHeight
   - Viewbox заменен на Border + Grid для корректного масштабирования
   - Изображения теперь масштабируются с сохранением пропорций
   - Настройка размеров через диалог (20-500 пикселей)
   - Размер контрола автоматически подстраивается под изображение

#### Технические детали:
- Все диалоги используют ScrollViewer с `HorizontalScrollBarVisibility.Disabled` и `VerticalScrollBarVisibility.Auto`
- ImageControl использует Avalonia StorageProvider для выбора файлов
- ImageButton поддерживает динамическое изменение размеров без перезагрузки
- Сохранение состояния в MainWindow через события ImageChanged и SizeChanged

#### Файлы изменены:
- Scada.Client/Models/MnemoschemeElement.cs
- Scada.Client/Views/MainWindow.axaml.cs
- Scada.Client/Views/Controls/SliderControl.axaml.cs
- Scada.Client/Views/Controls/NumericInputControl.axaml.cs
- Scada.Client/Views/Controls/DisplayControl.axaml.cs
- Scada.Client/Views/Controls/ImageButton.axaml
- Scada.Client/Views/Controls/ImageButton.axaml.cs

#### Новые файлы:
- Scada.Client/Views/Controls/ImageControl.axaml
- Scada.Client/Views/Controls/ImageControl.axaml.cs
- Вспомогательные скрипты: build.bat, run.bat, run.ps1, и др.

---

## Предыдущие коммиты

### Коммит: 536f976 - "feat: Add register controls, auto-polling, window state saving"
(Базовый коммит для текущей сессии)

---

## Инструкции по откату

### Откат к началу сессии:
```powershell
git reset --hard 536f976
```

### Откат на один коммит назад:
```powershell
git reset --hard HEAD~1
```

### Просмотр изменений:
```powershell
git log --oneline
git diff 536f976 645ec3a
```

### Восстановление конкретного файла:
```powershell
git checkout 536f976 -- путь/к/файлу
```
