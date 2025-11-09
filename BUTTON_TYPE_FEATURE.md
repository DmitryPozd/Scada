# Унификация кнопок управления катушками

## Описание

Реализована унификация элементов управления `CoilButton` и `CoilMomentaryButton` в один универсальный контрол `CoilButton` с настраиваемым типом поведения.

## Изменения

### 1. Новый enum `CoilButtonType`

**Файл**: `Scada.Client/Models/CoilButtonType.cs`

```csharp
public enum CoilButtonType
{
    Toggle = 0,      // С фиксацией (переключатель)
    Momentary = 1    // Моментальная (удержание)
}
```

### 2. Обновлен `CoilButton`

**Файлы**: 
- `Scada.Client/Views/Controls/CoilButton.axaml`
- `Scada.Client/Views/Controls/CoilButton.axaml.cs`

**Добавлено**:
- Свойство `ButtonType` типа `CoilButtonType` (по умолчанию `Toggle`)
- Обработчики `OnMainButtonPressed` и `OnMainButtonReleased` для моментального режима
- Поле `_isMomentaryPressed` для отслеживания состояния моментальной кнопки
- ComboBox в контекстном меню для выбора типа кнопки

**Поведение**:
- **Toggle** (с фиксацией): клик переключает состояние ON/OFF
- **Momentary** (моментальная): активна только пока нажата (удержание)

### 3. Обновлен `ImageButton`

**Файлы**:
- `Scada.Client/Views/Controls/ImageButton.axaml`
- `Scada.Client/Views/Controls/ImageButton.axaml.cs`

**Добавлено**:
- Свойство `ButtonType` типа `CoilButtonType`
- Обработчики `OnMainButtonPressed` и `OnMainButtonReleased`
- Поддержка моментального режима аналогично `CoilButton`

### 4. Обновлена модель данных

**Файл**: `Scada.Client/Models/MnemoschemeElement.cs`

В класс `CoilElement` добавлено:
```csharp
public CoilButtonType ButtonType { get; set; } = CoilButtonType.Toggle;
```

### 5. Обновлено сохранение/загрузка

**Файл**: `Scada.Client/Views/MainWindow.axaml.cs`

**Сохранение**:
- `CollectMnemoschemeElements()`: добавлено `ButtonType = coilBtn.ButtonType` при сохранении в settings.json

**Загрузка**:
- `RestoreMnemoschemeElements()`: добавлено `ButtonType = coilElem.ButtonType` при восстановлении из settings.json

## Использование

### В контекстном меню кнопки

1. Правый клик на кнопку → "Настройки кнопки"
2. В диалоге появился новый ComboBox **"Тип кнопки"**:
   - "С фиксацией (переключатель)" - Toggle режим
   - "Моментальная (удержание)" - Momentary режим
3. Выбор сохраняется при нажатии OK

### Программно

```csharp
var btn = new CoilButton
{
    Label = "Насос",
    CoilAddress = 100,
    ButtonType = CoilButtonType.Momentary // или Toggle
};
```

## Обратная совместимость

- Старые settings.json без поля `ButtonType` будут работать (используется значение по умолчанию `Toggle`)
- `CoilMomentaryButton` сохраняется с `ButtonType = Momentary` автоматически
- При загрузке `CoilMomentaryButton` восстанавливается в `CoilButton` с типом Momentary

## Будущие улучшения

- Можно удалить `CoilMomentaryButton.axaml` и `.axaml.cs`, заменив все использования на `CoilButton` с `ButtonType = Momentary`
- Обновить инструменты в Toolbox для создания кнопок с выбором типа
- Добавить визуальное отображение типа кнопки (иконка или текст на кнопке)

## Дата реализации

**2025-01-XX** - унификация кнопок управления катушками
