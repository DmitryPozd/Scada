# Добавление регистров в tags.json

## Текущее состояние

✅ **Создано**: Все биты (Coils) - 17,880 тегов  
⏳ **Ожидается**: Регистры (Holding/Input Registers)

## Как добавить регистры

### Шаг 1: Определите типы регистров вашего контроллера

Типичная карта регистров для Delta DVP / аналогичных:

| Тип | Диапазон | Назначение | Register Type | Data Type |
|-----|----------|------------|---------------|-----------|
| **D** | D0-D9999 | Data Registers (RW) | 0 (Holding) | 0-4 |
| **W** | W0-W255 | Word Registers (RW) | 0 (Holding) | 0-4 |
| **H** | H0-H1023 | High-speed Counters | 1 (Input) | 0-4 |

### Шаг 2: Отредактируйте generate_tags.py

Откройте `generate_tags.py` и добавьте ПЕРЕД строкой `config = {...}`:

```python
# D0-D9999 (Data Registers - Holding)
print("  Генерация D0-D9999 (регистры данных)...")
for i in range(10000):
    tags.append({
        'Enabled': True,
        'Name': f'D{i}',
        'Address': i,
        'Register': 0,  # 0 = Holding Register
        'Type': 0,      # 0 = UInt16 (можно изменить на нужный)
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

# Если есть другие регистры, добавьте их аналогично
```

### Шаг 3: Обновите Groups

В секции `config['Groups']` добавьте:

```python
config = {
    'Tags': tags,
    'Groups': {
        'DigitalInputs': [f'X{i}' for i in range(1024)],
        'DigitalOutputs': [f'Y{i}' for i in range(1024)],
        'InternalRelay': [f'M{i}' for i in range(12288)],
        'Timers': [f'T{i}' for i in range(1024)],
        'Counters': [f'C{i}' for i in range(256)],
        'SystemStatus': [f'SM{i}' for i in range(216)],
        'StepRelay': [f'S{i}' for i in range(2048)],
        # НОВОЕ: добавьте группы для регистров
        'DataRegisters': [f'D{i}' for i in range(10000)],
    },
    # ...
}
```

### Шаг 4: Обновите AddressRanges

```python
'AddressRanges': {
    'X': {'Start': 0, 'End': 1023, 'Description': 'Digital Inputs (X0-X1023)'},
    'Y': {'Start': 1536, 'End': 2559, 'Description': 'Digital Outputs (Y0-Y1023)'},
    # ... существующие ...
    # НОВОЕ:
    'D': {'Start': 0, 'End': 9999, 'Description': 'Data Registers (D0-D9999)'},
}
```

### Шаг 5: Запустите генератор

```powershell
python generate_tags.py
```

### Шаг 6: Пересоберите проект

```powershell
dotnet build Scada.sln
```

## Типы данных (Type)

При добавлении регистров выберите правильный тип:

| Type | Описание | Размер | Пример |
|------|----------|--------|--------|
| 0 | UInt16 | 1 регистр | 0-65535 |
| 1 | Int16 | 1 регистр | -32768 - 32767 |
| 2 | UInt32 | 2 регистра | 0-4294967295 |
| 3 | Int32 | 2 регистра | -2147483648 - 2147483647 |
| 4 | Float32 | 2 регистра | IEEE 754 |
| 5 | Bool | 1 бит (coil) | true/false |

## Register Types

| Register | Описание | Чтение | Запись |
|----------|----------|--------|--------|
| 0 | Holding Registers | ✅ | ✅ |
| 1 | Input Registers | ✅ | ❌ |
| 2 | Coils | ✅ | ✅ |

## Пример: Аналоговый вход (Int16 с масштабированием)

```python
tags.append({
    'Enabled': True,
    'Name': 'D100_Temperature',
    'Address': 100,
    'Register': 0,      # Holding Register
    'Type': 1,          # Int16
    'WordOrder': 0,
    'Scale': 0.1,       # Делит значение на 10
    'Offset': -50.0     # Вычитает 50
})
```

**Расчёт**: `Temp = (RawValue × 0.1) - 50`

## Пример: Float32 регистр

```python
tags.append({
    'Enabled': True,
    'Name': 'D200_Pressure',
    'Address': 200,
    'Register': 0,
    'Type': 4,          # Float32
    'WordOrder': 0,
    'Scale': 1.0,
    'Offset': 0.0
})
```

**Занимает 2 регистра**: D200 и D201

## Готовый шаблон для копирования

Вставьте в `generate_tags.py` перед `config = {...}`:

```python
# ===== РЕГИСТРЫ ДАННЫХ =====

# D0-D9999 (Data Registers)
print("  Генерация D0-D9999 (регистры данных)...")
for i in range(10000):
    tags.append({
        'Enabled': True,
        'Name': f'D{i}',
        'Address': i,
        'Register': 0,
        'Type': 0,
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

# Обновите также статистику в конце файла:
print(f'D (регистры):        {sum(1 for t in tags if t["Name"].startswith("D"))} тегов')
```

---

**Когда будете готовы добавить регистры, сообщите диапазоны и типы, я обновлю скрипт!**
