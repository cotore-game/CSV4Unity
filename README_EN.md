> 日本語ドキュメント: [README.md](./README.md)

# CSV4Unity

CSV4Unity reads CSV text into row, column, and cell views for Unity. Values remain text until the caller explicitly requests a type, which allows a column such as an ADV scenario argument to contain different value types on different rows.

> [!IMPORTANT]
> The library is currently being redesigned before 1.0. Pin a release tag or commit when using the Git package URL.

## Documentation

- [API reference](https://cotore-game.github.io/CSV4Unity/)
- [Core architecture (Japanese)](./docs/ja/architecture.md)
- [Core architecture (English)](./docs/en/architecture.md)

## Features

- RFC 4180 quoted fields, escaped quotes, commas, and embedded line breaks
- Enum-based column access
- Header-name and column-index access
- Row and column views over one document
- Explicit cell conversion
- Explicitly created search indices
- Attribute-based validation
- Unity Inspector validation
- Read-only CSV Viewer

## Installation

Add this URL through Unity Package Manager:

```text
https://github.com/cotore-game/CSV4Unity.git?path=/Assets/Plugins/CSVLoader#v0.2.0
```

## Basic usage

```csharp
public enum ScenarioField
{
    Command,
    Arg1,
    Text
}

CsvTable<ScenarioField> table = CSVLoader.LoadTable<ScenarioField>(csvAsset);
string command = table.Row(0)[ScenarioField.Command].GetString();
int argument = table.Row(0)[ScenarioField.Arg1].Get<int>();
```

Use `CsvDocument` when an Enum schema is unnecessary:

```csharp
CsvDocument document = CSVLoader.LoadDocument(csvAsset);
string name = document.Row(0)["Name"].GetString();
```

## Validation

```csharp
public enum CharacterField
{
    [PrimaryKey]
    [TypeConstraint(typeof(int))]
    Id,

    [NotNull]
    Name
}

CsvTable<CharacterField> table = CSVLoader.LoadTable<CharacterField>(csvAsset);
CsvValidationResult result = CsvValidator.Validate(table);
```

Use `Condition` to apply validation attributes only to matching rows. Conditions in the same group are combined with AND.

```csharp
public enum ScenarioField
{
    Command,

    [Condition(1, ScenarioField.Command, Compare.Equal, "Wait")]
    [NotNull(ConditionGroup = 1)]
    [TypeConstraint(typeof(int), ConditionGroup = 1)]

    [Condition(2, ScenarioField.Command, Compare.Equal, "SetFlag")]
    [TypeConstraint(typeof(bool), ConditionGroup = 2)]
    Arg1
}
```

The default group is zero. Supported comparisons are `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `IsEmpty`, `IsNotEmpty`, `In`, and `NotIn`. Groups are declarative and have no `if / else` execution order.

## CSV Viewer

Select a CSV asset and click `Open CSV Viewer` in the Inspector. The viewer is also available from `Assets > Open in CSV Viewer` and `Window > CSV4Unity > CSV Viewer`.

The read-only table supports headerless files, case-insensitive search, resizable columns, cell or row copying, and automatic reload after asset changes. It virtualizes row drawing and uses the same parser as the Runtime API.

The Japanese README is the canonical user documentation while the API is being stabilized. See [the architecture document](./docs/en/architecture.md) for the current class boundaries.

## License

[MIT License](./LICENSE)
