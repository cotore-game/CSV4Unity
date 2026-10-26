# CSV4Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-0.3.0-brightgreen)](https://github.com/cotore-game/CSV4Unity/releases/tag/v0.3.0)
[![Unity Tests](https://github.com/cotore-game/CSV4Unity/actions/workflows/unity-tests.yml/badge.svg?branch=main)](https://github.com/cotore-game/CSV4Unity/actions/workflows/unity-tests.yml)

CSV tooling for Unity with enum-based column access, conditional validation, and an Editor viewer.

Read CSV data without mapping every row to a class. Values stay as text until you request a type, so scenario commands can use the same argument column for strings, numbers, or booleans on different rows.

[日本語](README_JA.md)

> [!IMPORTANT]
> CSV4Unity is pre-1.0. APIs may change between releases. Pin a release tag or commit when installing.

## Contents

- [Installation](#installation)
- [Quick start](#quick-start)
- [Features](#features)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)

## Installation

In Unity Package Manager, select **Add package from git URL...** and enter:

```text
https://github.com/cotore-game/CSV4Unity.git?path=/Assets/Plugins/CSVLoader#v0.3.0
```

The package declares Unity 6000.0 or later. Development and tests use Unity **6000.0.73f1**.

## Quick start

Save this as a UTF-8 CSV file in your Unity project's Assets folder:

```csv
Command,Arg1,Text
Wait,500,
SetFlag,true,
Text,,Hello!
```

Define an enum matching the headers and assign the CSV asset in the Inspector:

```csharp
using CSV4Unity;
using UnityEngine;

public enum ScenarioField
{
    Command,
    Arg1,
    Text
}

public sealed class ScenarioReader : MonoBehaviour
{
    [SerializeField] private TextAsset scenarioCsv;

    private void Start()
    {
        CsvTable<ScenarioField> table = CSVLoader.LoadTable<ScenarioField>(scenarioCsv);
        int milliseconds = table.Row(0)[ScenarioField.Arg1].Get<int>();
        bool enabled = table.Row(1)[ScenarioField.Arg1].Get<bool>();
        string text = table.Row(2)[ScenarioField.Text].GetString();
        Debug.Log($"Wait: {milliseconds}, flag: {enabled}, text: {text}");
    }
}
```

Row indices start at zero and exclude the header. Enum access checks column names; cell values are converted at runtime. Use `CSVLoader.LoadDocument` for header-name or column-index access without an enum.

## Features

- **CSV parsing:** RFC 4180 quoting, escaped quotes, commas, and embedded line breaks.
- **Flexible access:** row, column, and cell views; enum fields, header names, or column indices; header aliases and patterns.
- **Explicit conversion and indexing:** convert cells when needed and build search indices for repeated lookups.
- **Validation:** enum attributes for required values, uniqueness, types, ranges, and more; conditions apply rules to matching rows.
- **Unity Editor tools:** schema discovery with `[CsvSchema]`, manual validation, encoding inspection, UTF-8 conversion with restore support, and a read-only viewer with search and zoom.

Loading and validation are separate operations. CSV editing and cross-file foreign-key validation are not currently supported.

## Documentation

- [User guide](docs/en/guide.md) — access, validation, encoding, and Editor tools.
- [API reference](https://cotore-game.github.io/CSV4Unity/api/CSV4Unity.html) — generated from Runtime XML comments, currently in Japanese.
- [Core architecture](docs/en/architecture.md) — contributor notes on ownership and class boundaries.
- [Development documentation](docs/README.md) — repository layout, tests, and documentation builds.
- [Release notes](https://github.com/cotore-game/CSV4Unity/releases).

## Contributing

Bug reports and pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for setup, tests, and the contribution process.

## License

[MIT](LICENSE)
