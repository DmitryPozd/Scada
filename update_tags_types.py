#!/usr/bin/env python3
"""
Скрипт для аккуратного обновления типов данных в tags.json
Сохраняет всю существующую структуру, обновляет только поле 'type'
"""

import json
import sys

def get_default_type_for_tag(tag_name):
    """Определить тип данных по имени тега согласно схеме"""
    if not tag_name:
        return "Bool"
    
    # X, Y, M, T, C, SM, S - Bool
    if tag_name.startswith(('X', 'Y', 'M', 'T', 'C', 'SM', 'S')):
        return "Bool"
    
    # AI, AQ - Int16 (signed 16-bit, 1 register, -32768~32767)
    if tag_name.startswith(('AI', 'AQ')):
        return "Int16"
    
    # TV, CV, SV - Int16 по умолчанию (может быть Int16 или Int32)
    if tag_name.startswith(('TV', 'CV', 'SV')):
        return "Int16"
    
    # V - Int16 по умолчанию (может быть Int16, Int32, Float32)
    if tag_name.startswith('V'):
        return "Int16"
    
    # Остальное - UInt16
    return "UInt16"

def update_tags_json(input_file, output_file):
    """Обновить types в tags.json"""
    print(f"Читаем {input_file}...")
    
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except Exception as e:
        print(f"Ошибка чтения файла: {e}")
        return False
    
    if 'tags' not in data:
        print("Ошибка: поле 'tags' не найдено")
        return False
    
    tags = data['tags']
    total = len(tags)
    updated_count = 0
    
    print(f"Обновляем {total} тегов...")
    
    for i, tag in enumerate(tags):
        if i % 5000 == 0:
            print(f"  Обработано {i}/{total} тегов...")
        
        tag_name = tag.get('name', '')
        current_type = tag.get('type', '')
        correct_type = get_default_type_for_tag(tag_name)
        
        if current_type != correct_type:
            tag['type'] = correct_type
            updated_count += 1
    
    print(f"Обновлено {updated_count} тегов из {total}")
    print(f"Сохраняем в {output_file}...")
    
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        print("✓ Файл успешно сохранен!")
        return True
    except Exception as e:
        print(f"Ошибка сохранения файла: {e}")
        return False

if __name__ == '__main__':
    input_path = 'Scada.Client/tags.json'
    output_path = 'Scada.Client/tags.json.new'
    
    print("=" * 60)
    print("Обновление типов данных в tags.json")
    print("=" * 60)
    
    if update_tags_json(input_path, output_path):
        print("\nФайл обновлен!")
        print(f"Новый файл: {output_path}")
        print("\nПроверьте его и замените исходный:")
        print(f"  move /Y {output_path} {input_path}")
    else:
        print("\n✗ Произошла ошибка!")
        sys.exit(1)
