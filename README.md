> :earth_americas: For English documentation, see [README_EN.md](./README_EN.md)

# CSV4Unity

Unity向けの高性能・型安全なCSVローダーライブラリです。**Enum型によるスキーマ定義**、**属性ベースのバリデーション**、**エディタ統合**など、実用的な機能を提供します。

---

## 特徴

* **高速パース**: `ReadOnlySpan<char>`を使用したゼロアロケーションパース
* **型安全なアクセス**: Enumによるスキーマ定義で、コンパイル時の型チェックを実現
* **柔軟なデータアクセス**: 行優先・列優先の両方に対応した効率的なアクセス
* **属性ベースのバリデーション**: `[PrimaryKey]`、`[NotNull]`、`[Range]`などの制約属性
* **エディタ統合**: インスペクタから直接バリデーションを実行可能
* **RFC 4180準拠**: クォートやエスケープ処理に対応
* **LINQ対応**: IEnumerableを実装しているので`Select`など様々なクエリに対応

---

## インストール方法

### Package Manager経由（推奨）

**こちらを推奨します。** `unitypackage` をダウンロードするよりも、バージョン管理が容易になり、アップデート追従がスムーズです。

1.  Unity Editorを開く
2.  `Window` → `Package Manager` を選択
3.  左上の `+` ボタンをクリック
4.  `Add package from git URL...` を選択
5.  以下のURLを入力:


```
https://github.com/cotore-game/CSV4Unity.git?path=Assets/Plugins/CSVLoader
```

### UnityPackage（代替手段）

[Releases](https://github.com/cotore-game/CSV4Unity/releases)から最新の `.unitypackage` をダウンロードして、Unityプロジェクトにインポートすることもできます。

---

## 基本的な使い方

### 1. Enumスキーマの定義

CSVのヘッダーに対応するEnum型を定義します。`CSV4Unity.Fields`名前空間に配置することで、エディタのバリデーション機能が自動的に認識します。

```csharp
using CSV4Unity.Validation;

namespace CSV4Unity.Fields
{
    public enum CharacterFields
    {
        [PrimaryKey]
        [NotNull]
        ID,

        [NotNull]
        [MaxLength(50)]
        Name,

        [Range(1, 100)]
        Level,

        [AllowedValues("Warrior", "Mage", "Rogue")]
        Class,

        HP,
        MP
    }
}

```

### 2. CSVファイルの読み込み

```csharp
using CSV4Unity;
using UnityEngine;

public class CharacterLoader : MonoBehaviour
{
    [SerializeField] private TextAsset csvFile;

    void Start()
    {
        // CSVを読み込み
        var csvData = CSVLoader.LoadCSV<CharacterFields>(csvFile);

        // 行データへのアクセス
        foreach (var row in csvData.Rows)
        {
            int id = row.Get<int>(CharacterFields.ID);
            string name = row.Get<string>(CharacterFields.Name);
            // 値がない場合はデフォルト値(1)を返す
            int level = row.GetOrDefault<int>(CharacterFields.Level, 1);
            
            Debug.Log($"Character: {name} (ID: {id}, Level: {level})");
        }

        // 列データへのアクセス（高速）
        var allNames = csvData.GetColumn<string>(CharacterFields.Name);
        var allLevels = csvData.GetColumn<int>(CharacterFields.Level);
    }
}

```

### 3. データのクエリとフィルタリング

```csharp
// 条件に一致する行を抽出
var highLevelCharacters = csvData.Where(row => 
    row.Get<int>(CharacterFields.Level) >= 50
);

// 特定の値を検索
var character = csvData.FindFirst(CharacterFields.ID, 101);
var allWarriors = csvData.FindAll(CharacterFields.Class, "Warrior");

// グループ化
var byClass = csvData.GroupBy(CharacterFields.Class);
foreach (var group in byClass)
{
    Debug.Log($"Class: {group.Key}, Count: {group.Count()}");
}

// 高速検索用インデックスの作成
var idIndex = csvData.CreateIndex(CharacterFields.ID);
if (idIndex.TryGetValue(101, out var rowIndices))
{
    var character = csvData.Rows[rowIndices[0]];
}

```

----------

## バリデーション機能

### 利用可能な制約属性

**属性**

**説明**

`[PrimaryKey]`

値が**一意かつ非NULL**

`[NotNull]`

値が必須（空文字列やNULLでない）

`[Unique]`

値が重複不可（NULLは許可）

`[TypeConstraint(Type)]`

指定型（`int`, `float`, `string`など）への変換可能性

`[Range(min, max)]`

数値の範囲制約

`[MinLength(n)]`

文字列の最小長

`[MaxLength(n)]`

文字列の最大長

`[Regex(pattern)]`

正規表現パターン (例: `[Regex(@"^\d{3}-\d{4}$")]`)

`[AllowedValues(...)]`

許可される値の列挙 (例: `[AllowedValues("A", "B", "C")]`)

### エディタでのバリデーション

1.  CSVファイル（TextAsset）を選択
    
2.  Inspectorで **`Validation Schema`** ドロップダウンから対応するEnum型を選択
    
3.  **`Validate CSV`** ボタンをクリック
    
4.  バリデーション結果がInspectorに表示されます
    

### コードからのバリデーション

```csharp
// 自動バリデーション（デフォルト有効）: エラー時は CsvValidationException がスローされる
var csvData = CSVLoader.LoadCSV<CharacterFields>(csvFile);

// 手動バリデーションの例
var validationResult = CsvValidator.Validate(csvData);
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Debug.LogError(error.ToString());
    }
}

```

----------

## APIリファレンス

### 1. CSVLoader クラス

#### `LoadCSV<TEnum>(TextAsset csvFile, CsvLoaderOptions options = null, string dataName = null)`

-   **説明**: Enum型をスキーマとしてCSVを読み込みます。
    
-   **パラメータ**:
    
    -   `csvFile`: 読み込むCSVファイル（TextAsset）
        
    -   `options`: ローダーオプション（省略時はデフォルト設定）
        
    -   `dataName`: データの識別名（省略時はファイル名）
        
-   **戻り値**: `CsvData<TEnum>` - パースされたCSVデータ
    

----------

#### `LoadCSV(TextAsset csvFile, CsvLoaderOptions options = null, string dataName = null)`

-   **説明**: 非ジェネリック版。ヘッダー名またはインデックスでアクセス可能になります。
    
-   **戻り値**: `CsvData` - パースされたCSVデータ
    

----------

### 2. CsvLoaderOptions クラス

読み込み動作を細かく制御するためのオプションです。

```csharp
var options = new CsvLoaderOptions
{
    Delimiter = ',',              // フィールド区切り文字（デフォルト: ','）
    HasHeader = true,             // ヘッダー行の有無（デフォルト: true）
    TrimFields = true,            // フィールドのトリム（デフォルト: true）
    IgnoreEmptyLines = true,      // 空行を無視（デフォルト: true）
    CommentPrefix = "#",          // コメント行のプレフィックス（デフォルト: "#"）
    MissingFieldPolicy = MissingFieldPolicy.Throw,  // 欠損フィールドの処理
    ValidationEnabled = true,     // 自動バリデーション（デフォルト: true）
    ThrowOnValidationError = true // バリデーションエラー時に例外（デフォルト: true）
};

```

----------

### 3. CsvData<TEnum> クラス

#### プロパティ

-   `Rows`: `IReadOnlyList<LineData<TEnum>>` - **全行データ**
    
-   `RowCount`: `int` - 行数
    
-   `ColumnCount`: `int` - 列数
    
-   `DataName`: `string` - データ名
    

#### メソッド

#### `GetColumn(TEnum field)` / `GetColumn<T>(TEnum field)`

-   **説明**: 指定列の全データを取得（**列優先アクセス**）。ジェネリック版は型付きで取得します。
    
-   **戻り値**: `IReadOnlyList<object>` または `IReadOnlyList<T>`
    

#### `Where(Func<LineData<TEnum>, bool> predicate)`

-   **説明**: 条件に一致する行をフィルタリングします（LINQ）。
    
-   **戻り値**: `IEnumerable<LineData<TEnum>>`
    

#### `GroupBy(TEnum field)`

-   **説明**: 指定フィールドでグループ化します（LINQ）。
    
-   **戻り値**: `IEnumerable<IGrouping<object, LineData<TEnum>>>`
    

#### `FindFirst(TEnum field, object value)`

-   **説明**: 指定フィールドの値で最初の一致行を検索します。
    
-   **戻り値**: `LineData<TEnum>` または `null`
    

#### `FindAll(TEnum field, object value)`

-   **説明**: 指定フィールドの値で全ての一致行を検索します。
    
-   **戻り値**: `IEnumerable<LineData<TEnum>>`
    

#### `CreateIndex(TEnum field)`

-   **説明**: 高速検索用インデックスを作成します。
    
-   **戻り値**: `Dictionary<object, List<int>>`（値から行インデックスへのマッピング）
    

----------

### 4. LineData<TEnum> クラス

#### インデクサ

```csharp
object value = row[CharacterFields.Name];
row[CharacterFields.Level] = 10; // 値の書き換えも可能

```

#### メソッド

#### `Get<T>(TEnum field)`

-   **説明**: 指定フィールドの値を型付きで取得します。失敗時は例外をスローします。
    
-   **戻り値**: `T`
    

#### `GetOrDefault<T>(TEnum field, T defaultValue = default)`

-   **説明**: 指定フィールドの値を安全に取得します。失敗時は**デフォルト値**を返します。
    
-   **戻り値**: `T`
    

#### `TryGet<T>(TEnum field, out T value)`

-   **説明**: 指定フィールドの値を安全に取得します（tryパターン）。
    
-   **戻り値**: `bool` (成功時は `true`)
    

#### `HasField(TEnum field)`

-   **説明**: 指定フィールドの存在確認をします。
    
-   **戻り値**: `bool`
    

----------

### 5. CsvValidator / CsvValidationResult クラス

#### CsvValidator.Validate<TEnum>(CsvData<TEnum> data)

-   **説明**: Enum型に定義された制約属性に基づいてバリデーションを実行します。
    
-   **戻り値**: `CsvValidationResult`
    

#### CsvValidationResult のプロパティ・メソッド

-   `IsValid`: `bool` - バリデーション成功時は `true`
    
-   `Errors`: `List<ValidationError>` - エラーリスト
    
-   `Warnings`: `List<ValidationWarning>` - 警告リスト
    
-   `GetSummary()`: `string` - サマリー文字列
    
-   `GetErrorMessages()`: `IEnumerable<string>` - エラーメッセージ一覧
    

----------

## 非ジェネリック版の使用

ヘッダー名やインデックスで直接アクセスする場合に使用します。

```csharp
// 読み込み
var csvData = CSVLoader.LoadCSV(csvFile);

// ヘッダー名でアクセス
foreach (var row in csvData.Rows)
{
    string name = row.Get<string>("Name");
    int level = row.GetOrDefault<int>("Level", 1);
}

// インデックスでアクセス（ヘッダーなしCSV用）
var options = new CsvLoaderOptions { HasHeader = false };
var csvData = CSVLoader.LoadCSV(csvFile, options);

foreach (var row in csvData.Rows)
{
    string firstColumn = row.Get<string>(0);
    int secondColumn = row.Get<int>(1);
}

// 列データの取得
var nameColumn = csvData.GetColumn<string>("Name");
var firstColumnData = csvData.GetColumnByIndex<string>(0);

```

----------

## パフォーマンスの最適化

### 列優先アクセス

列全体にアクセスする場合は **`GetColumn`** を使用すると、行をループするより高速です。

```csharp
var allLevels = csvData.GetColumn<int>(CharacterFields.Level);
int sum = allLevels.Sum();

```

### インデックスの作成

同じフィールドで繰り返し検索する場合は、事前に **`CreateIndex`** を作成すると効率的です。

```csharp
// インデックス作成（一度だけ）
var idIndex = csvData.CreateIndex(CharacterFields.ID);

// O(1)で検索可能
if (idIndex.TryGetValue(targetId, out var rowIndices))
{
    var row = csvData.Rows[rowIndices[0]];
    // ... 処理
}

```

### メモリ効率

大量のCSVデータを扱う場合は、必要な列のみを取得することでメモリを節約できます。

```csharp
// 必要な列だけ取得
var names = csvData.GetColumn<string>(CharacterFields.Name);
var levels = csvData.GetColumn<int>(CharacterFields.Level);

```

----------

## FAQ

### Q: CSVファイルのエンコーディングは？

A: **UTF-8（BOM付き/なし両対応）**を推奨します。Unityの `TextAsset` はUTF-8として読み込まれます。

### Q: 大きなCSVファイルも扱える？

A: はい。**`ReadOnlySpan<char>`** によるゼロアロケーションパースを採用しているため、数万行規模のCSVも高速に処理できます。

### Q: クォート内の改行は対応していますか？

A: いいえ、現在のバージョンでは**フィールド内の改行には対応していません**。RFC 4180のクォートとエスケープ処理（`""`）には対応しています。

### Q: バリデーションを部分的に無効化できますか？

A: 特定のフィールドのバリデーションを無効化する機能は現在ありません。`CsvLoaderOptions.ValidationEnabled = false` で全体を無効化するか、Enum定義から属性を削除してください。

## ライセンス

MIT License

## 貢献

バグ報告や機能提案は [Issues](https://github.com/cotore-game/CSV4Unity/issues) からお願いします。プルリクエストも歓迎します。
