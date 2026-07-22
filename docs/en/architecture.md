# CSV4Unity Core Architecture

## Design goals

- Parse RFC 4180 records in one forward pass.
- Keep one authoritative copy of CSV data in memory.
- Provide O(1) row, column, header, and enum-field access.
- Preserve the original cell text until the caller requests conversion.
- Keep parsing, access, conversion, indexing, validation, and Unity integration independent.
- Leave room for a future editing model without making the read model mutable.

## Dependency direction

Dependencies point downward only.

```text
Unity facade
  CSVLoader
      |
      v
Parsing                 Schema                 Conversion
  CsvParser ----------> CsvDocument <--------- CsvEnumSchema<TField>
      |                     ^                         |
      v                     |                         v
  CsvCellRange          Data views              CsvTable<TField>
                        CsvCell                 CsvRow<TField>
                        CsvRow                  CsvColumn<TField>
                        CsvColumn                     |
                            ^                         |
                            |                         |
                        CsvIndex<TKey> <--------------+

Validation
  depends on CsvTable<TField>, CsvEnumSchema<TField>, and CsvCell
  core data classes never depend on validation
```

The former `CsvData`, `LineData`, and `LoadCSV` APIs were removed after the new core migration. Use `LoadDocument` or `LoadTable<TField>`.

## Class specifications

### `CsvParser`

Responsibility: lexical and structural CSV parsing only.

- Input: a complete CSV `string` and `CsvParseOptions`.
- Output: one immutable `CsvDocument`.
- Recognizes quoted commas, escaped quotes, and quoted record separators.
- Validates record width and malformed quote placement.
- Does not infer value types, validate business rules, build indices, or log through Unity.

### `CsvDocument`

Responsibility: own the immutable parsed snapshot.

- Owns the source string, row-major `CsvCellRange[]`, headers, and header lookup.
- Is the only class that owns cell storage.
- Provides O(1) lookup by row/column index and by header name.
- Creates lightweight row, column, cell, and enum-table views.
- Does not expose mutation. A future editor must use a separate mutable builder/document type.

### `CsvCell`

Responsibility: represent one non-owning cell view.

- Stores only a source-string reference and a cell range.
- `RawSpan` does not allocate and exposes the encoded cell payload.
- `GetString` allocates only when a `string` is requested; escaped quotes are decoded then.
- Typed access delegates to `CsvValueConverter` and never changes the stored value.

### `CsvRow` and `CsvColumn`

Responsibility: provide indexed views over `CsvDocument`.

- Are readonly structs and own no cell collections.
- Row access calculates `row * columnCount + column`.
- Column access calculates the same position for each requested row.
- Creating a view does not copy row or column values.

### `CsvEnumSchema<TField>`

Responsibility: bind enum fields to document column indices once.

- Reflects enum declarations only when a schema is bound.
- Validates required headers and rejects ambiguous enum aliases.
- Owns only the enum-to-column dictionary, never cell data.
- Can be inspected independently from row access and reused by validators.

### `CsvTable<TField>`

Responsibility: combine one document with one bound enum schema.

- Owns neither cells nor converted values.
- Provides O(1) enum-based row, column, and cell access.
- Rejects a schema bound to another document.
- Is the preferred API for typo-resistant ADV scenario access.

### `CsvValueConverter`

Responsibility: convert text to an explicitly requested type.

- Uses invariant culture by default and accepts an explicit format provider.
- Supports generic conversion plus allocation-conscious primitive paths.
- Never performs automatic type inference while loading.
- Empty text remains empty text; only `Nullable<T>` converts an empty span to `null`.

### `CsvIndex<TKey>`

Responsibility: provide an optional lookup acceleration structure for one column.

- Is created explicitly with `CsvIndex<TKey>.Create(column)`; parsing never creates indices automatically.
- Stores one row integer per unique key.
- Allocates extra row lists only for duplicate keys.
- Returns row indices so it does not depend on typed or untyped row view classes.

### `CSVLoader`

Responsibility: adapt Unity inputs to the pure C# core.

- Converts `TextAsset` to its text and name.
- Delegates all parsing to `CsvParser`.
- Does not contain parsing, conversion, indexing, or validation algorithms.

### Unity Editor tools

- `CsvInspectorEditor` adds the viewer and validation entry points to CSV assets.
- `CsvViewerWindow` owns asset selection, parsing, search state, and reload behavior.
- `CsvViewerTable` draws only visible rows and provides column resizing and copy commands.

The viewer treats `CsvDocument` as read-only data. It caches display strings for at most 256 rows instead of duplicating the complete CSV as a two-dimensional string array. Editing and writing remain separate future responsibilities.

## Validation boundary

`CsvValidationSchema<TField>` compiles attribute metadata once and `CsvValidator` applies the resulting rules to `CsvTable<TField>`. Row-local rules and column/table rules remain separate:

- Row-local: required, type, range, regex, allowed values, length.
- Column/table: primary key and unique.
- Cross-document: foreign key through an explicit validation context.

`ConditionAttribute` limits a validation rule to matching rows. Conditions in the same `ConditionGroup` are combined with AND. Enum fields and groups are resolved while creating the validation schema, so row evaluation performs no reflection. Each validation attribute becomes one internal rule, allowing one CSV column to use different type constraints for different commands.

The internal `CsvConditionEvaluator` owns condition comparison while `CsvValidator` remains responsible for applying validation constraints.

This prevents `Unique` from rescanning an entire column once per row and keeps the core data model independent from reflection and validation attributes.
