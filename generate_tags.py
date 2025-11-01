#!/usr/bin/env python3
"""
Генератор полного файла tags.json со всеми битами контроллера
"""
import json

tags = []

print("Генерация тегов...")

# X0-X1023 (Digital Inputs)
print("  Генерация X0-X1023 (входы)...")
for i in range(1024):
    tags.append({
        'Enabled': True,
        'Name': f'X{i}',
        'Address': i,
        'Register': 2,
        'Type': 5,
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

# Y0-Y1023 (Digital Outputs)
print("  Генерация Y0-Y1023 (выходы)...")
for i in range(1024):
    tags.append({
        'Enabled': True,
        'Name': f'Y{i}',
        'Address': 1536 + i,
        'Register': 2,
        'Type': 5,
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

# M0-M12287 (Auxiliary Relay)
print("  Генерация M0-M12287 (вспомогательные реле)...")
for i in range(12288):
    tags.append({
        'Enabled': True,
        'Name': f'M{i}',
        'Address': 3072 + i,
        'Register': 2,
        'Type': 5,
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

# T0-T1023 (Timers)
print("  Генерация T0-T1023 (таймеры)...")
for i in range(1024):
    tags.append({
        'Enabled': True,
        'Name': f'T{i}',
        'Address': 15360 + i,
        'Register': 2,
        'Type': 5,
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

# C0-C255 (Counters)
print("  Генерация C0-C255 (счётчики)...")
for i in range(256):
    tags.append({
        'Enabled': True,
        'Name': f'C{i}',
        'Address': 16384 + i,
        'Register': 2,
        'Type': 5,
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

# SM0-SM215 (System Status Bits)
print("  Генерация SM0-SM215 (системные биты)...")
for i in range(216):
    tags.append({
        'Enabled': True,
        'Name': f'SM{i}',
        'Address': 16896 + i,
        'Register': 2,
        'Type': 5,
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

# S0-S2047 (Step Relay)
print("  Генерация S0-S2047 (шаговые реле)...")
for i in range(2048):
    tags.append({
        'Enabled': True,
        'Name': f'S{i}',
        'Address': 28672 + i,
        'Register': 2,
        'Type': 5,
        'WordOrder': 0,
        'Scale': 1.0,
        'Offset': 0.0
    })

config = {
    'Tags': tags,
    'Groups': {
        'DigitalInputs': [f'X{i}' for i in range(1024)],
        'DigitalOutputs': [f'Y{i}' for i in range(1024)],
        'InternalRelay': [f'M{i}' for i in range(12288)],
        'Timers': [f'T{i}' for i in range(1024)],
        'Counters': [f'C{i}' for i in range(256)],
        'SystemStatus': [f'SM{i}' for i in range(216)],
        'StepRelay': [f'S{i}' for i in range(2048)]
    },
    'AddressRanges': {
        'X': {'Start': 0, 'End': 1023, 'Description': 'Digital Inputs (X0-X1023)'},
        'Y': {'Start': 1536, 'End': 2559, 'Description': 'Digital Outputs (Y0-Y1023)'},
        'M': {'Start': 3072, 'End': 15359, 'Description': 'Auxiliary Relay (M0-M12287)'},
        'T': {'Start': 15360, 'End': 16383, 'Description': 'Timers (T0-T1023)'},
        'C': {'Start': 16384, 'End': 16639, 'Description': 'Counters (C0-C255)'},
        'SM': {'Start': 16896, 'End': 17111, 'Description': 'System Status Bits (SM0-SM215)'},
        'S': {'Start': 28672, 'End': 30719, 'Description': 'Step Relay (S0-S2047)'}
    }
}

print("\nСохранение в Scada.Client/tags.json...")
with open('Scada.Client/tags.json', 'w', encoding='utf-8') as f:
    json.dump(config, f, indent=2, ensure_ascii=False)

print("\n=== Статистика ===")
print(f'Всего создано тегов: {len(tags)}')
print(f'X (входы):           {sum(1 for t in tags if t["Name"].startswith("X") and not t["Name"].startswith("SM"))} тегов')
print(f'Y (выходы):          {sum(1 for t in tags if t["Name"].startswith("Y"))} тегов')
print(f'M (реле):            {sum(1 for t in tags if t["Name"].startswith("M") and not t["Name"].startswith("SM"))} тегов')
print(f'T (таймеры):         {sum(1 for t in tags if t["Name"].startswith("T"))} тегов')
print(f'C (счётчики):        {sum(1 for t in tags if t["Name"].startswith("C"))} тегов')
print(f'SM (системные):      {sum(1 for t in tags if t["Name"].startswith("SM"))} тегов')
print(f'S (шаговые):         {sum(1 for t in tags if t["Name"].startswith("S") and not t["Name"].startswith("SM"))} тегов')

# Проверка размера файла
import os
file_size = os.path.getsize('Scada.Client/tags.json')
print(f'\nРазмер файла: {file_size:,} байт ({file_size / 1024 / 1024:.2f} МБ)')
print('\nГотово! ✓')
