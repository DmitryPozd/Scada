# Changelog - Сессия 4 ноября 2025

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
