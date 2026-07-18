> English overview: [README_EN.md](./README_EN.md)

# CSV4Unity

CSV4Unityは、Unityの `TextAsset` またはCSV文字列を読み取り、行・列・セルのどの方向からでも参照できるCSVライブラリです。

CSVをクラスへ一括変換せず、セルを必要なときに指定した型へ変換します。そのため、ADVシナリオの `Arg1` のように、行ごとに文字列・整数・真偽値などが混在する列も扱えます。

> [!IMPORTANT]
> 現在は1.0未満の再設計中です。APIは今後も変更される可能性があります。利用時はGitのタグまたはコミットを固定してください。

## ドキュメント

- [APIリファレンス](https://cotore-game.github.io/CSV4Unity/)
- [コア設計（日本語）](./docs/ja/architecture.md)
- [Core architecture (English)](./docs/en/architecture.md)

## 主な機能

- RFC 4180形式のクォート、カンマ、二重引用符、クォート内改行を解析
- Enumによる列指定
- ヘッダー名・列番号による非ジェネリックアクセス
- 行アクセスと列アクセス
- 必要なセルだけを明示的に型変換
- 検索用インデックスの明示生成
- Enum属性によるValidation
- CSV Inspectorからの手動Validation

## インストール

Unity Package Managerの `Add package from git URL...` に次のURLを指定します。

```text
https://github.com/cotore-game/CSV4Unity.git?path=Assets/Plugins/CSVLoader
```

安定した利用には、リリースタグまたはコミットを固定したURLを使用してください。

## Enumで列を指定する

CSVヘッダーと同じ名前のEnumを定義します。

```csharp
public enum ScenarioField
{
    Command,
    Arg1,
    Arg2,
    Text
}
```

```csv
Command,Arg1,Arg2,Text
Bg,room,0.5,
Wait,500,,
Text,,,こんにちは
```

`LoadTable<TField>` で読み込むと、列名を文字列で記述せずにアクセスできます。

```csharp
using CSV4Unity;
using UnityEngine;

public sealed class ScenarioReader : MonoBehaviour
{
    [SerializeField] private TextAsset scenarioCsv;

    private void Start()
    {
        CsvTable<ScenarioField> table = CSVLoader.LoadTable<ScenarioField>(scenarioCsv);

        string command = table.Row(0)[ScenarioField.Command].GetString();
        float duration = table.Row(0)[ScenarioField.Arg2].Get<float>();
        int milliseconds = table.Row(1)[ScenarioField.Arg1].Get<int>();
    }
}
```

同じ `Arg1` 列でも、行のCommandに応じて異なる型として取得できます。

```csharp
string background = table.Row(0)[ScenarioField.Arg1].GetString();
int milliseconds = table.Row(1)[ScenarioField.Arg1].Get<int>();
```

## ヘッダー名・列番号で読む

Enumが不要な場合は `CsvDocument` を使用します。

```csharp
CsvDocument document = CSVLoader.LoadDocument(csvAsset);

string name = document.Row(0)["Name"].GetString();
int level = document.Column("Level")[0].Get<int>();
```

ヘッダーなしCSVは列番号で参照します。

```csharp
var options = new CsvParseOptions { HasHeader = false };
CsvDocument document = CSVLoader.LoadDocument("Wait,500\nText,Hello", options);

string command = document.Row(0)[0].GetString();
int argument = document.Row(0)[1].Get<int>();
```

## 列アクセスと検索

```csharp
CsvColumn<ScenarioField> commands = table.Column(ScenarioField.Command);

for (int rowIndex = 0; rowIndex < commands.Count; rowIndex++)
{
    Debug.Log(commands[rowIndex].GetString());
}
```

同じ列を繰り返し検索する場合だけ、明示的にインデックスを作成します。

```csharp
CsvIndex<string> index = CsvIndex<string>.Create(commands);

if (index.TryFindFirst("Text", out int rowIndex))
{
    CsvRow<ScenarioField> row = table.Row(rowIndex);
}
```

## Validation

Enumフィールドへ制約属性を付けます。

```csharp
using CSV4Unity.Validation;

public enum CharacterField
{
    [PrimaryKey]
    [TypeConstraint(typeof(int))]
    Id,

    [NotNull]
    [MaxLength(32)]
    Name,

    [Range(1, 100)]
    Level
}
```

読み込みとValidationは分離されています。必要な場所で明示的に実行してください。

```csharp
CsvTable<CharacterField> table = CSVLoader.LoadTable<CharacterField>(csvAsset);
CsvValidationResult result = CsvValidator.Validate(table);

foreach (ValidationError error in result.Errors)
{
    Debug.LogError(error);
}
```

利用可能な制約は `PrimaryKey`、`NotNull`、`Unique`、`TypeConstraint`、`Range`、`Regex`、`AllowedValues`、`MinLength`、`MaxLength`、`ForeignKey` です。

## Inspector Validation

1. `CSV4Unity.Fields` 名前空間へValidation用Enumを定義します。
2. UnityのProjectウィンドウでCSVを選択します。
3. Inspectorの `Validation Schema` からEnumを選択します。
4. `Validate CSV` を実行します。

## Parser設定

```csharp
var options = new CsvParseOptions
{
    HasHeader = true,
    Delimiter = ',',
    IgnoreEmptyRecords = true,
    TrimUnquotedFields = true
};
```

## 設計資料

- [日本語コア設計](./docs/ja/architecture.md)
- [English core architecture](./docs/en/architecture.md)

## 開発用ファイル

`Assets/Scripts` と `Assets/TestData/CSV4Unity` は、このリポジトリ自身のExample・テスト用です。UPMで指定する `Assets/Plugins/CSVLoader` には含まれません。

## ライセンス

[MIT License](./LICENSE)
