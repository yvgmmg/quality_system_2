---
name: generate-ef-entities
description: Автоматическое генерирование C# EF Core сущностей из PostgreSQL SQL‑скрипта
source: auto-skill
extracted_at: '2026-05-29T11:20:00.000Z'
---

## Цель
Создать набор C#‑классов‑сущностей, которые точно отражают структуру таблиц, перечисления и связи, описанные в файле `database_creating.sql`, для использования в проекте на Entity Framework Core.

## Шаги выполнения
1. **Найти SQL‑скрипт** – через `glob`/`list_directory` ищем файл `*.sql` в корне проекта и открываем его (`read_file`).
2. **Сканировать описание таблиц** – последовательно парсим `CREATE TYPE` (для enum‑ов) и `CREATE TABLE`.
3. **Создать enum‑файлы**
   * Имя enum берётся из типа PostgreSQL (например, `public.frame_result`).
   * Формируем C#‑enum c теми же значениями, переводя имена в lower‑case (по‑умолчанию). При необходимости заменяем недопустимые символы (например, `%` → `_`).
4. **Сгенерировать класс‑сущность** для каждой `CREATE TABLE`:
   * Добавляем атрибуты `[Table("table_name")]` и `[Key]`/`[Column]`.
   * Приводим типы колонок к C#‑типам согласно таблице соответствий (serial4 → `int`, int4 → `int`, varchar/text → `string`, date/timestamp → `DateTime`/`DateTime?`, float4 → `float`, float8 → `double`, numeric → `decimal`, bool → `bool`).
   * Для nullable колонок используем `type?`.
   * Добавляем навигационные свойства (`virtual`) для всех внешних ключей, используя имена колонок без суффикса `_id`.
   * Для таблиц‑связей (many‑to‑many) создаём класс‑соединитель с собственным PK (если в скрипте нет составного PK) и навигацию к обеим сторонам.
   * Для вычисляемых столбцов (например, `summ` в `material`) объявляем свойство как `[NotMapped]` с геттером‑вычислением.
5. **Записать файлы** – каждый класс сохраняем по пути `QualityControlSystem.Infrastructure/Entities/<ClassName>.cs`. Каждый enum – в папке `Infrastructure/Enums`.
6. **Обновить уже существующие модели** – если класс уже есть (например, `UserProfile`), добавить недостающие свойства и навигацию, не удаляя существующий код.
7. **Проверка** – после генерации убедиться, что все файлы успешно записаны (открыть несколько через `read_file`).

## Ключевые детали
- **Имена классов** – PascalCase, совпадают с именем таблицы, но без префикса `public.` (e.g., `AccessRight`).
- **Имена enum‑ов** – PascalCase, помещаются в `QualityControlSystem.Infrastructure.Enums`.
- **Навигационные свойства** – `public virtual <Entity>? <PropertyName> { get; set; }`.
- **Nullable типы** – колонка без `NOT NULL` → `type?`.
- **Специальные символы** – в enum‑ах заменяем недопустимые символы (`%` → `_`).
- **Составные ключи** – если в таблице нет явного PK, создаём отдельный `Id`‑поле с атрибутом `[Key]` (можно позже адаптировать к композитному ключу).
- **Триггеры/Sequences** – игнорируются, они не требуют кода.

## Пример шаблона класса
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("table_name")]
    public class ClassName
    {
        [Key]
        [Column("primary_key_column")]
        public int Id { get; set; }

        // Пример колонок
        [Column("some_column")]
        public string? SomeColumn { get; set; }

        // Навигация
        [Column("foreign_key_id")]
        public int? ForeignKeyId { get; set; }
        public virtual ForeignEntity? ForeignEntity { get; set; }
    }
}
```

## Результат
После выполнения всех шагов появляется набор готовых файлов C# (entities + enums), которые можно сразу добавить в `DbContext` проекта.
