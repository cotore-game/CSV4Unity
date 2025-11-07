> :earth_americas: 日本語バージョンはこちらから [README.md](./README.md)

# CSV4Unity

A high-performance, type-safe CSV loader library for Unity. It provides practical features such as **Enum-based schema definition**, **attribute-based validation**, and **Editor integration**.

----------

## Features

-   **High-Speed Parsing**: Zero-allocation parsing using `ReadOnlySpan<char>`
    
-   **Type-Safe Access**: Schema definition via Enum achieves compile-time type checking
    
-   **Flexible Data Access**: Efficient access optimized for both row-major and column-major retrieval
    
-   **Attribute-Based Validation**: Constraint attributes like `[PrimaryKey]`, `[NotNull]`, and `[Range]`
    
-   **Editor Integration**: Directly perform validation from the Inspector
    
-   **RFC 4180 Compliance**: Supports quoting and escaping rules
    
-   **LINQ Support**: Implements `IEnumerable` for compatibility with various queries like `Select`
    

----------

## Installation

### Via Package Manager (Recommended)

**This method is recommended.** It offers easier version management and smoother library updates compared to downloading and importing a `unitypackage`.

1.  Open the Unity Editor.
    
2.  Select **`Window`** → **`Package Manager`**.
    
3.  Click the **`+`** button in the upper left corner.
    
4.  Select **`Add package from git URL...`**.
    
5.  Enter the following URL:
    

```
https://github.com/cotore-game/CSV4Unity.git?path=Assets/Plugins/CSVLoader
```

### UnityPackage (Alternative)

You can also download the latest **`.unitypackage`** from [Releases](https://www.google.com/search?q=https://github.com/cotore-game/CSV4Unity/releases) and import it into your Unity project.

----------

## Basic Usage

### 1. Defining the Enum Schema

Define an Enum type that corresponds to the CSV headers. Placing it in the `CSV4Unity.Fields` namespace allows the Editor validation feature to automatically recognize it.

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

### 2. Loading the CSV File


```csharp
using CSV4Unity;
using UnityEngine;

public class CharacterLoader : MonoBehaviour
{
    [SerializeField] private TextAsset csvFile;

    void Start()
    {
        // Load the CSV
        var csvData = CSVLoader.LoadCSV<CharacterFields>(csvFile);

        // Accessing row data
        foreach (var row in csvData.Rows)
        {
            int id = row.Get<int>(CharacterFields.ID);
            string name = row.Get<string>(CharacterFields.Name);
            // Returns the default value (1) if the value is missing
            int level = row.GetOrDefault<int>(CharacterFields.Level, 1);
            
            Debug.Log($"Character: {name} (ID: {id}, Level: {level})");
        }

        // Accessing column data (faster)
        var allNames = csvData.GetColumn<string>(CharacterFields.Name);
        var allLevels = csvData.GetColumn<int>(CharacterFields.Level);
    }
}

```

### 3. Querying and Filtering Data

```csharp
// Extract rows that match a condition
var highLevelCharacters = csvData.Where(row => 
    row.Get<int>(CharacterFields.Level) >= 50
);

// Search for a specific value
var character = csvData.FindFirst(CharacterFields.ID, 101);
var allWarriors = csvData.FindAll(CharacterFields.Class, "Warrior");

// Grouping
var byClass = csvData.GroupBy(CharacterFields.Class);
foreach (var group in byClass)
{
    Debug.Log($"Class: {group.Key}, Count: {group.Count()}");
}

// Creating an index for fast searching
var idIndex = csvData.CreateIndex(CharacterFields.ID);
if (idIndex.TryGetValue(101, out var rowIndices))
{
    var character = csvData.Rows[rowIndices[0]];
}

```

----------

## Validation Features

### Available Constraint Attributes

**Attribute**

**Description**

`[PrimaryKey]`

Value must be **unique and NOT NULL**

`[NotNull]`

Value is required (not empty string or NULL)

`[Unique]`

Value must not be duplicated (NULL is allowed)

`[TypeConstraint(Type)]`

Must be convertible to the specified type (`int`, `float`, `string`, etc.)

`[Range(min, max)]`

Numeric range constraint

`[MinLength(n)]`

Minimum string length

`[MaxLength(n)]`

Maximum string length

`[Regex(pattern)]`

Regular expression pattern (e.g., `[Regex(@"^\d{3}-\d{4}$")]`)

`[AllowedValues(...)]`

Enumeration of allowed values (e.g., `[AllowedValues("A", "B", "C")]`)

### Validation in the Editor

1.  Select the CSV file (TextAsset).
    
2.  In the Inspector, select the corresponding Enum type from the **`Validation Schema`** dropdown.
    
3.  Click the **`Validate CSV`** button.
    
4.  The validation result will be displayed in the Inspector.
    

### Validation from Code

```csharp
// Automatic Validation (enabled by default): Throws CsvValidationException on error
var csvData = CSVLoader.LoadCSV<CharacterFields>(csvFile);

// Example of Manual Validation
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

## API Reference

### 1. CSVLoader Class

#### `LoadCSV<TEnum>(TextAsset csvFile, CsvLoaderOptions options = null, string dataName = null)`

-   **Description**: Loads the CSV using the Enum type as the schema.
    
-   **Parameters**:
    
    -   `csvFile`: The CSV file (TextAsset) to load.
        
    -   `options`: Loader options (uses default settings if omitted).
        
    -   `dataName`: Identifier name for the data (uses file name if omitted).
        
-   **Returns**: `CsvData<TEnum>` - The parsed CSV data.
    

----------

#### `LoadCSV(TextAsset csvFile, CsvLoaderOptions options = null, string dataName = null)`

-   **Description**: Non-generic version. Allows access by header name or index.
    
-   **Returns**: `CsvData` - The parsed CSV data.
    

----------

### 2. CsvLoaderOptions Class

Options for fine-grained control over the loading behavior.

```csharp
var options = new CsvLoaderOptions
{
    Delimiter = ',',              // Field delimiter (default: ',')
    HasHeader = true,             // Presence of a header row (default: true)
    TrimFields = true,            // Trim whitespace around fields (default: true)
    IgnoreEmptyLines = true,      // Ignore empty lines (default: true)
    CommentPrefix = "#",          // Prefix for comment lines (default: "#")
    MissingFieldPolicy = MissingFieldPolicy.Throw,  // Policy for handling missing fields
    ValidationEnabled = true,     // Perform automatic validation on load (default: true)
    ThrowOnValidationError = true // Throw exception on validation failure (default: true)
};

```

----------

### 3. CsvData<TEnum> Class

#### Properties

-   `Rows`: `IReadOnlyList<LineData<TEnum>>` - **All row data**
    
-   `RowCount`: `int` - Number of rows
    
-   `ColumnCount`: `int` - Number of columns
    
-   `DataName`: `string` - Data name
    

#### Methods

#### `GetColumn(TEnum field)` / `GetColumn<T>(TEnum field)`

-   **Description**: Gets all data for the specified column (**column-major access**). The generic version gets data with type casting.
    
-   **Returns**: `IReadOnlyList<object>` or `IReadOnlyList<T>`
    

#### `Where(Func<LineData<TEnum>, bool> predicate)`

-   **Description**: Filters rows that match the condition (LINQ).
    
-   **Returns**: `IEnumerable<LineData<TEnum>>`
    

#### `GroupBy(TEnum field)`

-   **Description**: Groups data by the specified field (LINQ).
    
-   **Returns**: `IEnumerable<IGrouping<object, LineData<TEnum>>>`
    

#### `FindFirst(TEnum field, object value)`

-   **Description**: Searches for the first row that matches the specified field value.
    
-   **Returns**: `LineData<TEnum>` or `null`
    

#### `FindAll(TEnum field, object value)`

-   **Description**: Searches for all rows that match the specified field value.
    
-   **Returns**: `IEnumerable<LineData<TEnum>>`
    

#### `CreateIndex(TEnum field)`

-   **Description**: Creates an index for fast searching.
    
-   **Returns**: `Dictionary<object, List<int>>` (Mapping from value to row index)
    

----------

### 4. LineData<TEnum> Class

#### Indexer

C#

```csharp
object value = row[CharacterFields.Name];
row[CharacterFields.Level] = 10; // Value modification is also possible

```

#### Methods

#### `Get<T>(TEnum field)`

-   **Description**: Gets the field value with type casting. Throws an exception on failure.
    
-   **Returns**: `T`
    

#### `GetOrDefault<T>(TEnum field, T defaultValue = default)`

-   **Description**: Safely gets the field value. Returns the **default value** on failure.
    
-   **Returns**: `T`
    

#### `TryGet<T>(TEnum field, out T value)`

-   **Description**: Safely gets the field value (try pattern).
    
-   **Returns**: `bool` (Returns `true` on success)
    

#### `HasField(TEnum field)`

-   **Description**: Checks for the existence of the specified field.
    
-   **Returns**: `bool`
    

----------

### 5. CsvValidator / CsvValidationResult Classes

#### CsvValidator.Validate<TEnum>(CsvData<TEnum> data)

-   **Description**: Executes validation based on the constraint attributes defined in the Enum type.
    
-   **Returns**: `CsvValidationResult`
    

#### CsvValidationResult Properties and Methods

-   `IsValid`: `bool` - `true` if validation succeeded
    
-   `Errors`: `List<ValidationError>` - List of errors
    
-   `Warnings`: `List<ValidationWarning>` - List of warnings
    
-   `GetSummary()`: `string` - Summary string
    
-   `GetErrorMessages()`: `IEnumerable<string>` - List of error messages
    

----------

## Non-Generic Usage

Use this version to access data directly by header name or index.

```csharp
// Loading
var csvData = CSVLoader.LoadCSV(csvFile);

// Access by header name
foreach (var row in csvData.Rows)
{
    string name = row.Get<string>("Name");
    int level = row.GetOrDefault<int>("Level", 1);
}

// Access by index (for CSVs without a header)
var options = new CsvLoaderOptions { HasHeader = false };
var csvData = CSVLoader.LoadCSV(csvFile, options);

foreach (var row in csvData.Rows)
{
    string firstColumn = row.Get<string>(0);
    int secondColumn = row.Get<int>(1);
}

// Getting column data
var nameColumn = csvData.GetColumn<string>("Name");
var firstColumnData = csvData.GetColumnByIndex<string>(0);

```

----------

## Performance Optimization

### Column-Major Access

Using **`GetColumn`** to access the entire column is faster than looping through all rows.

```csharp
var allLevels = csvData.GetColumn<int>(CharacterFields.Level);
int sum = allLevels.Sum();

```

### Index Creation

If searching repeatedly on the same field, creating an index with **`CreateIndex`** beforehand is efficient.

```csharp
// Create index (once)
var idIndex = csvData.CreateIndex(CharacterFields.ID);

// Search is O(1)
if (idIndex.TryGetValue(targetId, out var rowIndices))
{
    foreach (var rowIndex in rowIndices)
    {
        var row = csvData.Rows[rowIndex];
        // ... process
    }
}

```

### Memory Efficiency

When dealing with large CSV files, you can save memory by only retrieving the necessary columns.
```csharp
// Retrieve only the necessary columns
var names = csvData.GetColumn<string>(CharacterFields.Name);
var levels = csvData.GetColumn<int>(CharacterFields.Level);

```

----------

## FAQ

### Q: What encoding should the CSV file use?

A: **UTF-8 (with or without BOM)** is recommended. Unity's `TextAsset` is read as UTF-8.

### Q: Can it handle large CSV files?

A: Yes. It uses **`ReadOnlySpan<char>`** for zero-allocation parsing, allowing fast processing of CSVs with tens of thousands of rows.

### Q: Does it support line breaks within quoted fields?

A: No, the current version **does not support line breaks within fields**. It does support RFC 4180 quoting and escaping rules (`""`).

### Q: Can I partially disable validation?

A: There is currently no feature to disable validation for specific fields. You can either disable validation entirely using `CsvLoaderOptions.ValidationEnabled = false` or remove the attributes from the Enum definition.

----------

## License

MIT License

## Contributing

Please submit bug reports and feature requests via [Issues](https://www.google.com/search?q=https://github.com/cotore-game/CSV4Unity/issues). Pull requests are also welcome.
