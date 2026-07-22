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
- 属性によるEnum名とCSVヘッダー名の補正
- ヘッダー名・列番号による非ジェネリックアクセス
- 行アクセスと列アクセス
- 必要なセルだけを明示的に型変換
- 検索用インデックスの明示生成
- Enum属性によるValidation
- CSV Inspectorからの手動Validation
- CSV Inspectorでの文字コード検査とUTF-8変換
- 読み取り専用CSV Viewer

## インストール

Unity Package Managerの `Add package from git URL...` に次のURLを指定します。

```text
https://github.com/cotore-game/CSV4Unity.git?path=/Assets/Plugins/CSVLoader#v0.2.0
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

### Enum名と異なるヘッダーを使う

通常の別名には `CsvHeader` を使用します。`IgnoreCase` を有効にすると、大文字小文字を区別しません。

```csharp
public enum ItemField
{
    [CsvHeader("Item ID")]
    Id,

    [CsvHeader("DISPLAY NAME", IgnoreCase = true)]
    DisplayName
}
```

複数の表記を許可する必要がある場合だけ `CsvHeaderPattern` を使用します。正規表現はヘッダー名全体へ適用されます。

```csharp
using System.Text.RegularExpressions;

public enum ItemField
{
    [CsvHeaderPattern(@"item[_\s-]?id", RegexOptions.IgnoreCase)]
    Id
}
```

属性を指定しないフィールドは、従来どおりEnum名とヘッダー名を大文字小文字まで含めて比較します。候補が0件または複数件の場合や、複数のEnumフィールドが同じ列へ対応した場合は `CsvSchemaException` が送出されます。

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

利用可能な制約は `PrimaryKey`、`NotNull`、`Unique`、`TypeConstraint`、`Range`、`Regex`、`AllowedValues`、`MinLength`、`MaxLength` です。

### 条件付きValidation

`Condition`を付けると、条件が成立した行だけValidation属性を適用できます。同じグループの条件はすべてANDとして評価されます。

```csharp
public enum ScenarioField
{
    Command,
    Enabled,

    [Condition(1, ScenarioField.Command, Compare.Equal, "Wait")]
    [Condition(1, ScenarioField.Enabled, Compare.Equal, true)]
    [NotNull(ConditionGroup = 1)]
    [TypeConstraint(typeof(int), ConditionGroup = 1)]

    [Condition(2, ScenarioField.Command, Compare.Equal, "SetFlag")]
    [NotNull(ConditionGroup = 2)]
    [TypeConstraint(typeof(bool), ConditionGroup = 2)]
    Arg1
}
```

グループを省略した場合はグループ0になります。単一条件なら番号を記述する必要はありません。

```csharp
[Condition(ScenarioField.Command, Compare.In, "Text", "Choice")]
[NotNull]
Text
```

`Compare`は `Equal`、`NotEqual`、`GreaterThan`、`GreaterThanOrEqual`、`LessThan`、`LessThanOrEqual`、`IsEmpty`、`IsNotEmpty`、`In`、`NotIn` を使用できます。文字列比較は既定で大文字小文字を区別し、`IgnoreCase = true`で無視できます。数値比較では数値リテラルを渡してください。

```csharp
[Condition(ScenarioField.Duration, Compare.GreaterThan, 0)]
[Range(0, 10)]
Arg1
```

比較値に同じEnum型のフィールドを指定すると、同じ行の列同士を比較します。

```csharp
[Condition(ScenarioField.Start, Compare.LessThanOrEqual, ScenarioField.End)]
[NotNull]
Text
```

Conditionは上から順に実行される`if / else`ではありません。各グループは独立して評価されるため、条件が重なると複数のValidationが同時に適用されます。else相当は`NotIn`や`NotEqual`で明示してください。

## CSV Encoding

RuntimeのCSV読み込みはUTF-8を前提とします。ProjectウィンドウでCSVを選択すると、Inspectorの `CSV Encoding` に元ファイルの判定結果とデコード後のプレビューが表示されます。

Shift_JIS、UTF-16、UTF-32のCSVは、内容を確認してから `Convert to UTF-8` を押してください。変換結果はUTF-8（BOMなし）で元のCSVへ保存され、Gitの変更対象になります。自動判定が正しくない場合は `Source Encoding` で変換元を明示できます。

CSV4Unityはインポート時にファイルを自動変換しません。変換前のCSVでは文字化けを避けるため、ViewerとInspector Validationを実行できません。

## CSV Viewer

ProjectウィンドウでCSVを選択し、Inspectorの `Open CSV Viewer` を押すと、CSVを読み取り専用の表として確認できます。CSVを右クリックして `Open in CSV Viewer` を選ぶか、`Window > CSV4Unity > CSV Viewer` から開くこともできます。

- `Header` で先頭行をヘッダーとして扱うか切り替え
- 検索欄で全セルを大文字小文字を区別せず絞り込み
- ヘッダー境界のドラッグで列幅を変更
- セルの右クリックでセルまたは行をコピー
- CSVアセット更新時に自動再読込

Viewerは画面に見える行だけを描画し、検索時もセル文字列の全コピーを作りません。CSVの編集や保存は行わず、表示にはRuntimeと同じParserを使用します。

## Inspector Validation

1. Validation用Enumへ `[CsvSchema]` を付けます。
2. UnityのProjectウィンドウでCSVを選択します。
3. Inspectorの `Validation Schema` からEnumを選択します。
4. `Validate CSV` を実行します。

```csharp
using CSV4Unity;
using CSV4Unity.Validation;

namespace MyGame.Data
{
    [CsvSchema]
    public enum ItemFields
    {
        [PrimaryKey]
        Id,

        [NotNull]
        Name
    }
}
```

`CsvSchema`はUnity EditorがInspector候補を発見するための属性です。`WithFields<TField>()`や`CSVLoader.LoadTable<TField>()`をコードから使用するだけであれば必須ではありません。

旧バージョンとの互換性のため、`CSV4Unity.Fields`名前空間のEnumも当面は候補へ表示されます。新しいスキーマでは名前空間規約を使用せず、`CsvSchema`を付けてください。

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
