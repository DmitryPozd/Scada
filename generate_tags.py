import json

tags = []

# Входные биты X (Discrete Inputs) - только чтение
print("Generating X tags (Input bits)...")
for i in range(1024):  # X0 - X1023
    tags.append({
        "name": f"X{i}",
        "address": i,
        "register": "Input",
        "type": "Bool",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Выходные биты Y (Coils) - чтение/запись
print("Generating Y tags (Output bits)...")
for i in range(1024):  # Y0 - Y1023
    tags.append({
        "name": f"Y{i}",
        "address": 1536 + i,
        "register": "Coils",
        "type": "Bool",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Вспомогательное реле M (Coils) - чтение/запись
print("Generating M tags (Auxiliary relay)...")
for i in range(12288):  # M0 - M12287
    tags.append({
        "name": f"M{i}",
        "address": 3072 + i,
        "register": "Coils",
        "type": "Bool",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Таймеры T (Coils) - чтение/запись
print("Generating T tags (Timer bits)...")
for i in range(1024):  # T0 - T1023
    tags.append({
        "name": f"T{i}",
        "address": 15360 + i,
        "register": "Coils",
        "type": "Bool",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Счетчики C (Coils) - чтение/запись
print("Generating C tags (Counter bits)...")
for i in range(256):  # C0 - C255
    tags.append({
        "name": f"C{i}",
        "address": 16384 + i,
        "register": "Coils",
        "type": "Bool",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Системные биты SM (Coils) - чтение/запись
print("Generating SM tags (System status bits)...")
for i in range(216):  # SM0 - SM215
    tags.append({
        "name": f"SM{i}",
        "address": 16896 + i,
        "register": "Coils",
        "type": "Bool",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Пошаговое реле S (Coils) - чтение/запись
print("Generating S tags (Step relay)...")
for i in range(2048):  # S0 - S2047
    tags.append({
        "name": f"S{i}",
        "address": 28672 + i,
        "register": "Coils",
        "type": "Bool",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Аналоговые входы AI (Input Registers) - только чтение
print("Generating AI tags (Analog inputs)...")
for i in range(256):  # AI0 - AI255
    tags.append({
        "name": f"AI{i}",
        "address": i,
        "register": "Input",
        "type": "UInt16",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Аналоговые выходы AQ (Holding Registers) - только чтение
print("Generating AQ tags (Analog outputs)...")
for i in range(256):  # AQ0 - AQ255
    tags.append({
        "name": f"AQ{i}",
        "address": 256 + i,
        "register": "Holding",
        "type": "UInt16",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Регистры данных V (Holding Registers) - чтение/запись
print("Generating V tags (Data registers - 16-bit)...")
for i in range(14848):  # V0 - V14847
    tags.append({
        "name": f"V{i}",
        "address": 512 + i,
        "register": "Holding",
        "type": "Int16",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Текущие значения таймеров TV (Holding Registers) - чтение/запись
print("Generating TV tags (Timer current values)...")
for i in range(1024):  # TV0 - TV1023
    tags.append({
        "name": f"TV{i}",
        "address": 15360 + i,
        "register": "Holding",
        "type": "UInt16",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Текущие значения счетчиков CV (Holding Registers) - чтение/запись
print("Generating CV tags (Counter current values)...")
for i in range(256):  # CV0 - CV255
    if i < 48:  # CV0-CV47 - 16 бит
        tags.append({
            "name": f"CV{i}",
            "address": 16384 + i,
            "register": "Holding",
            "type": "UInt16",
            "scale": 1.0,
            "offset": 0.0,
            "enabled": True
        })
    else:  # CV48-CV255 - 32 бита
        tags.append({
            "name": f"CV{i}",
            "address": 16384 + i,
            "register": "Holding",
            "type": "Int32",
            "scale": 1.0,
            "offset": 0.0,
            "enabled": True
        })

# Системные специальные регистры SV (Holding Registers) - чтение/запись
print("Generating SV tags (System special registers)...")
for i in range(901):  # SV0 - SV900
    tags.append({
        "name": f"SV{i}",
        "address": 17408 + i,
        "register": "Holding",
        "type": "UInt16",
        "scale": 1.0,
        "offset": 0.0,
        "enabled": True
    })

# Создаем JSON объект
output = {
    "description": "Complete PLC address mapping",
    "totalTags": len(tags),
    "tags": tags
}

# Сохраняем в файл
print(f"\nSaving {len(tags)} tags to tags.json...")
with open("Scada.Client/tags.json", "w", encoding="utf-8") as f:
    json.dump(output, f, indent=2, ensure_ascii=False)

print(f"Done! Generated {len(tags)} tags")
print("\nTag statistics:")
print(f"  X (Input bits): 1024")
print(f"  Y (Output bits): 1024")
print(f"  M (Auxiliary relay): 12288")
print(f"  T (Timer bits): 1024")
print(f"  C (Counter bits): 256")
print(f"  SM (System bits): 216")
print(f"  S (Step relay): 2048")
print(f"  AI (Analog inputs): 256")
print(f"  AQ (Analog outputs): 256")
print(f"  V (Data registers): 14848")
print(f"  TV (Timer values): 1024")
print(f"  CV (Counter values): 256")
print(f"  SV (System registers): 901")
print(f"  TOTAL: {len(tags)}")
