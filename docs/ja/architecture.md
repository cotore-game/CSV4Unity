# CSV4Unity コア設計

## 最初に知っておくこと

CSV4Unityは、CSVをクラスへ一括変換するライブラリではありません。

CSVの文字列をそのまま保持し、必要になったセルだけを文字列・整数・真偽値などへ変換します。これにより、ADVの `Arg1` のように、行によって型が変わる列も扱えます。

利用時に中心となる型は、ひとまず次の3つです。

| 型 | 意味 |
|---|---|
| `CsvTable<TField>` | Enumで列を指定できるCSV全体 |
| `CsvRow<TField>` | CSVの1行 |
| `CsvCell` | 1つのセル。必要な型を指定して値を取得する |

```csharp
CsvTable<ScenarioField> table = CSVLoader.LoadTable<ScenarioField>(csvAsset);
CsvRow<ScenarioField> row = table.Row(0);

string command = row[ScenarioField.Command].GetString();
float seconds = row[ScenarioField.Arg1].Get<float>();
```

`Arg1`をロード時に特定の型へ固定しないため、別の行では次のように取得できます。

```csharp
bool enabled = table.Row(1)[ScenarioField.Arg1].Get<bool>();
int count = table.Row(2)[ScenarioField.Arg1].Get<int>();
```

## データが読み込まれる流れ

```text
Unity TextAsset
    ↓ CSVLoader
CSV文字列
    ↓ CsvParser
CsvDocument
    ├── CsvRow / CsvColumn / CsvCell
    └── CsvEnumSchema<TField>
            ↓
        CsvTable<TField>
            ├── CsvRow<TField>
            └── CsvColumn<TField>
```

1. `CSVLoader` がUnityの `TextAsset` から文字列を取り出します。
2. `CsvParser` がCSVの構造を解析します。
3. `CsvDocument` が元文字列と各セルの位置を保持します。
4. `CsvEnumSchema<TField>` がEnumとCSV列を対応付けます。
5. `CsvTable<TField>` から行・列・セルへアクセスします。

## データを誰が所有するか

セルの実データを所有するのは `CsvDocument` だけです。

`CsvRow`、`CsvColumn`、`CsvCell`、`CsvTable<TField>` は、すべて `CsvDocument` を参照するためのビューです。行用・列用に同じ値を複製しません。

```text
CsvDocument
  ├── 元のCSV文字列
  ├── セルの開始位置と長さの配列
  ├── ヘッダー配列
  └── ヘッダー名から列番号への対応表

CsvRow / CsvColumn / CsvCell
  └── CsvDocumentとインデックスだけを参照
```

現在の `CsvDocument` は読み取り専用です。将来CSV編集を実装する場合は、直接可変にせず、編集専用の別クラスを用意します。

## クラスごとの役割

### 読み込み

| 型 | 役割 | やらないこと |
|---|---|---|
| `CSVLoader` | `TextAsset` とPure C#コアを接続する | CSV解析、型変換、検証処理 |
| `CsvParser` | RFC 4180形式のCSV構造を解析する | 値の型推測、Enum対応、Validation |
| `CsvParseOptions` | ヘッダー、区切り文字、空行などを指定する | 読み込み結果の保持 |
| `CsvParseException` | CSV構文エラーの位置を伝える | Validationエラーの表現 |

### データ保持とアクセス

| 型 | 役割 |
|---|---|
| `CsvDocument` | 読み込んだCSV全体を一度だけ保持する |
| `CsvCellRange` | 元文字列上でのセル開始位置・長さを内部保持する |
| `CsvCell` | 1セルを参照し、文字列化や型付き取得を提供する |
| `CsvRow` | 列番号・ヘッダー名でアクセスできる行ビュー |
| `CsvColumn` | 行番号でアクセスできる列ビュー |

### Enumアクセス

| 型 | 役割 |
|---|---|
| `CsvEnumSchema<TField>` | Enum値とCSV列番号を対応付ける |
| `CsvSchemaAttribute` | EnumをUnity Inspectorのスキーマ候補として登録する |
| `CsvHeaderAttribute` | Enum名と異なるヘッダー名を指定する |
| `CsvHeaderPatternAttribute` | 複数表記を正規表現で一意に対応付ける |
| `CsvTable<TField>` | `CsvDocument` とEnumスキーマを組み合わせる |
| `CsvRow<TField>` | Enumを使って1行のセルへアクセスする |
| `CsvColumn<TField>` | Enumで選択した列へアクセスする |
| `CsvSchemaException` | 必須ヘッダー不足などのスキーマ不一致を表す |

属性を指定しない場合、Enum名とCSVヘッダー名は大文字小文字まで一致している必要があります。

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
Text,,,こんにちは
```

CSV側の命名を変更できない場合は `CsvHeader` で別名を指定できます。

```csharp
public enum ItemField
{
    [CsvHeader("Item ID")]
    Id,

    [CsvHeader("DISPLAY NAME", IgnoreCase = true)]
    DisplayName
}
```

複数の表記を許可する必要がある場合だけ `CsvHeaderPattern` を使用します。パターンは部分一致ではなくヘッダー名全体へ適用されます。

```csharp
using System.Text.RegularExpressions;

public enum ItemField
{
    [CsvHeaderPattern(@"item[_\s-]?id", RegexOptions.IgnoreCase)]
    Id
}
```

対応候補が0件または複数件の場合や、複数のEnumフィールドが同じCSV列へ対応した場合は、曖昧なスキーマとして `CsvSchemaException` を送出します。

### Unity Editor

| 型 | 役割 |
|---|---|
| `CsvInspectorEditor` | CSVの文字コード確認・UTF-8変換と、Viewer・Validationの入口を提供する |
| `CsvEncodingUtility` | 元バイト列の文字コードを検査し、明示された文字コードからUTF-8へ変換する |
| `CsvViewerWindow` | CSVアセットの選択、解析、検索条件、再読込を管理する |
| `CsvViewerTable` | 表示範囲の行だけを描画し、列幅変更とコピー操作を提供する |

ViewerはEditor専用であり、`CsvDocument`を読み取り専用データとして利用します。表示用文字列は最大256行分だけキャッシュし、CSV全体を表示専用の二次元文字列配列へ複製しません。編集や書き出しは別の責務とします。

文字コード検査はUnityが生成した`TextAsset.text`ではなく、プロジェクト内の元CSVファイルをバイト列として読み取ります。BOMを優先し、BOMなしは厳密なUTF-8、次にShift_JIS（CP932）として検査します。誤判定時はInspectorで変換元を指定できます。ファイルの自動書き換えは行わず、確認ダイアログを伴う手動操作だけでUTF-8（BOMなし）へ変換します。

### 型変換

| 型 | 役割 |
|---|---|
| `CsvValueConverter` | セル文字列を指定された型へ変換する |
| `CsvConversionException` | 型変換に失敗した値と変換先型を伝える |

ロード時の自動型推測は行いません。`001` はロード後も `001` のままです。

```csharp
CsvCell cell = row[ScenarioField.Arg1];

string raw = cell.GetString(); // "001"
int number = cell.Get<int>();  // 1
```

### 検索インデックス

| 型 | 役割 |
|---|---|
| `CsvIndex<TKey>` | 指定した列の値から行番号を検索する |
| `CsvIndexMatches` | 一致した複数の行番号を返す |

インデックスは自動では作りません。必要な列にだけ明示的に作成します。

```csharp
CsvColumn<ScenarioField> commandColumn = table.Column(ScenarioField.Command);
CsvIndex<string> index = CsvIndex<string>.Create(commandColumn);

if (index.TryFindFirst("Text", out int rowIndex))
{
    CsvRow<ScenarioField> textRow = table.Row(rowIndex);
}
```

### Validation

| 型 | 役割 |
|---|---|
| `CsvValidationSchema<TField>` | Enum属性を一度読み取り、検証規則へ変換する |
| `CsvConditionEvaluator` | コンパイル済みConditionを行ごとに評価する内部クラス |
| `CsvValidator` | `CsvTable<TField>` を規則に従って検証する |
| `CsvValidationResult` | エラーと警告を保持する |

Validationは次の2種類へ分けます。

| 種類 | 制約 |
|---|---|
| セル・行単位 | `NotNull`、`TypeConstraint`、`Range`、`Regex`、`AllowedValues`、文字列長 |
| 列全体 | `PrimaryKey`、`Unique` |

`ConditionAttribute`は対象フィールドに付いたValidation属性の適用行を限定します。スキーマ生成時に条件列をEnumへ解決し、Reflectionは行評価中に実行しません。同じ`ConditionGroup`の条件はANDで評価され、Validation属性は同じ番号の条件グループだけを参照します。グループ0にConditionがなければ、従来どおり無条件で適用します。

Validation属性は属性1個につき内部規則1個へ変換します。このため、同じ列へCommand別の`TypeConstraint`を複数定義できます。条件は制約を実行するかだけを決め、条件不成立自体をValidationエラーにはしません。

### Unity Inspectorでのスキーマ発見

`CsvSchemaAttribute`をEnumへ付けると、Unity Editorは`TypeCache`を使ってその型を発見し、CSV Inspectorの`Validation Schema`候補へ表示します。Enumを特定の名前空間へ置く必要はありません。

この属性はEditor上の発見だけを担当します。Runtimeの`CsvDocument.WithFields<TField>()`や`CSVLoader.LoadTable<TField>()`は、属性がないEnumも従来どおり利用できます。

v0.xでは互換性のため、`CSV4Unity.Fields`名前空間にある属性なしEnumも候補へ含めます。この名前空間規約は新規コードでは使用せず、明示的に`CsvSchemaAttribute`を付けます。

`PrimaryKey` と `Unique` は条件に一致する行集合を一度だけ走査します。無条件の場合も、各行のセル検証中に列全体を繰り返し走査しません。

## 依存関係のルール

下位のクラスは上位の都合を知りません。

```text
Unity連携 → Parser → Document
                       ↑
Schema ────────────────┘

Conversion → Cellが利用
Indexing   → Columnを利用
Validation → Table / Schema / Cellを利用
```

- `CsvDocument` はUnityを知りません。
- `CsvDocument` はValidationを知りません。
- `CsvParser` はEnumや属性を知りません。
- ValidationはParser内部へ入りません。
- 新コアは旧 `CsvData` / `LineData` に依存しません。

## 旧APIについて

再設計前の `LoadCSV`、`CsvData`、`LineData` と旧Validatorは削除しました。
読み込みには `LoadDocument` または `LoadTable<TField>` を使用します。

## 今後の予定

1. Validationのエラー表現と日本語開発者向け説明を整える。
2. CSV編集用の可変モデルと書き出し処理を別レイヤーとして設計する。
3. APIが固まった段階で英語文書とHTML APIリファレンスを整備する。
