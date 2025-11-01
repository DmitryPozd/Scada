# Быстрая справка по адресам

## Формулы расчёта адресов Modbus

```
X[n]  = 0 + n          // Входы:          X0=0,     X10=10,    X1023=1023
Y[n]  = 1536 + n       // Выходы:         Y0=1536,  Y10=1546,  Y1023=2559
M[n]  = 3072 + n       // Вспом. реле:    M0=3072,  M100=3172, M12287=15359
T[n]  = 15360 + n      // Таймеры:        T0=15360, T50=15410, T1023=16383
C[n]  = 16384 + n      // Счётчики:       C0=16384, C100=16484, C255=16639
SM[n] = 16896 + n      // Систем. биты:   SM0=16896, SM1=16897, SM215=17111
S[n]  = 28672 + n      // Шаговые реле:   S0=28672, S100=28772, S2047=30719
```

## Шаблон для копирования

### Входной бит (X)
```json
{
  "Enabled": true,
  "Name": "X10",
  "Address": 10,
  "Register": 2,
  "Type": 5,
  "WordOrder": 0,
  "Scale": 1.0,
  "Offset": 0.0
}
```

### Выходной бит (Y)
```json
{
  "Enabled": true,
  "Name": "Y5",
  "Address": 1541,
  "Register": 2,
  "Type": 5,
  "WordOrder": 0,
  "Scale": 1.0,
  "Offset": 0.0
}
```

### Вспомогательное реле (M)
```json
{
  "Enabled": true,
  "Name": "M100",
  "Address": 3172,
  "Register": 2,
  "Type": 5,
  "WordOrder": 0,
  "Scale": 1.0,
  "Offset": 0.0
}
```

## Диапазоны для InputBitsIndicator

- Входы X0-X7: StartAddress = **0**
- Входы X8-X15: StartAddress = **8**
- Выходы Y0-Y7: StartAddress = **1536**
- Реле M0-M7: StartAddress = **3072**
- Таймеры T0-T7: StartAddress = **15360**

## Быстрая генерация серии тегов

### PowerShell (X0-X15):
```powershell
0..15 | ForEach-Object {
    @"
    {
      "Enabled": true,
      "Name": "X$_",
      "Address": $_,
      "Register": 2,
      "Type": 5,
      "WordOrder": 0,
      "Scale": 1.0,
      "Offset": 0.0
    },
"@
}
```

### Python (Y0-Y15):
```python
for i in range(16):
    print(f'''    {{
      "Enabled": true,
      "Name": "Y{i}",
      "Address": {1536 + i},
      "Register": 2,
      "Type": 5,
      "WordOrder": 0,
      "Scale": 1.0,
      "Offset": 0.0
    }},''')
```

## Частые ошибки

❌ **Неправильно** (забыли базовый адрес):
```json
{"Name": "Y0", "Address": 0}
```

✅ **Правильно**:
```json
{"Name": "Y0", "Address": 1536}
```

---

❌ **Неправильно** (Type=0 для бита):
```json
{"Name": "X0", "Type": 0}
```

✅ **Правильно**:
```json
{"Name": "X0", "Type": 5}
```

---

❌ **Неправильно** (Register=0 для coil):
```json
{"Name": "M0", "Register": 0}
```

✅ **Правильно**:
```json
{"Name": "M0", "Register": 2}
```
