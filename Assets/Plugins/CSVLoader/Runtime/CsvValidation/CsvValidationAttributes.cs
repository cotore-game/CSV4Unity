using System;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// 列の各値が空でなく、一意であることを要求します。
    /// </summary>
    /// <remarks>値はデコード済み文字列として、大文字小文字を区別して比較されます。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute
    {
    }

    /// <summary>
    /// セルが空でないことを要求します。
    /// </summary>
    /// <remarks>空文字列を未入力として扱います。空白だけの文字列は空とはみなしません。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class NotNullAttribute : Attribute
    {
    }

    /// <summary>
    /// セルを指定型へ変換できることを要求します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TypeConstraintAttribute : Attribute
    {
        /// <summary>要求する変換先型を取得します。</summary>
        public Type ExpectedType { get; }

        /// <summary>
        /// 型制約を設定します。
        /// </summary>
        /// <param name="expectedType">要求する変換先型。</param>
        public TypeConstraintAttribute(Type expectedType)
        {
            ExpectedType = expectedType;
        }
    }

    /// <summary>
    /// 数値が指定範囲内にあることを要求します。
    /// </summary>
    /// <remarks>最小値と最大値を含む範囲として、検証時の形式プロバイダーを使ってdoubleへ変換します。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RangeAttribute : Attribute
    {
        /// <summary>許可する最小値を取得します。</summary>
        public double Min { get; }

        /// <summary>許可する最大値を取得します。</summary>
        public double Max { get; }

        /// <summary>
        /// 数値の範囲制約を設定します。
        /// </summary>
        /// <param name="min">許可する最小値。</param>
        /// <param name="max">許可する最大値。</param>
        public RangeAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// 空でないセルの値が一意であることを要求します。
    /// </summary>
    /// <remarks>空セルは検証対象から除外します。値は大文字小文字を区別して比較されます。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class UniqueAttribute : Attribute
    {
    }

    /// <summary>
    /// セル文字列が指定した正規表現に一致することを要求します。
    /// </summary>
    /// <remarks>正規表現は<see cref="System.Text.RegularExpressions.RegexOptions.CultureInvariant"/>で生成されます。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RegexAttribute : Attribute
    {
        /// <summary>正規表現パターンを取得します。</summary>
        public string Pattern { get; }

        /// <summary>
        /// 正規表現パターンを設定します。
        /// </summary>
        /// <param name="pattern">検証に使用する正規表現パターン。</param>
        public RegexAttribute(string pattern)
        {
            Pattern = pattern;
        }
    }

    /// <summary>
    /// セル文字列が許可値のいずれかと一致することを要求します。
    /// </summary>
    /// <remarks>許可値はInvariantCultureで文字列化し、大文字小文字を区別して比較されます。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AllowedValuesAttribute : Attribute
    {
        /// <summary>指定された許可値を取得します。</summary>
        public object[] AllowedValues { get; }

        /// <summary>
        /// 許可される値を設定します。
        /// </summary>
        /// <param name="allowedValues">許可する値。</param>
        public AllowedValuesAttribute(params object[] allowedValues)
        {
            AllowedValues = allowedValues;
        }
    }

    /// <summary>
    /// セル文字列が参照先テーブルの指定列に存在することを要求します。
    /// </summary>
    /// <remarks>
    /// 同じEnum型を指定した場合は検証対象テーブル内を参照します。別のEnum型を指定する場合は、
    /// <see cref="CsvValidationContext.Register{TField}(CsvTable{TField})"/>で参照先を登録します。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        /// <summary>参照先テーブルを識別するEnum型を取得します。</summary>
        public Type ReferenceEnumType { get; }

        /// <summary>参照先のヘッダー名を取得します。</summary>
        public string ReferenceField { get; }

        /// <summary>
        /// 外部キー制約を設定します。
        /// </summary>
        /// <param name="referenceEnumType">参照先テーブルを識別するEnum型。</param>
        /// <param name="referenceField">参照先のヘッダー名。</param>
        public ForeignKeyAttribute(Type referenceEnumType, string referenceField)
        {
            ReferenceEnumType = referenceEnumType;
            ReferenceField = referenceField;
        }
    }

    /// <summary>
    /// セル文字列が指定した最小長以上であることを要求します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MinLengthAttribute : Attribute
    {
        /// <summary><see cref="string.Length"/>で判定する最小長を取得します。</summary>
        public int MinLength { get; }

        /// <summary>文字列の最小長を設定します。</summary>
        /// <param name="minLength"><see cref="string.Length"/>で判定する最小長。</param>
        public MinLengthAttribute(int minLength)
        {
            MinLength = minLength;
        }
    }

    /// <summary>
    /// セル文字列が指定した最大長以下であることを要求します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MaxLengthAttribute : Attribute
    {
        /// <summary><see cref="string.Length"/>で判定する最大長を取得します。</summary>
        public int MaxLength { get; }

        /// <summary>文字列の最大長を設定します。</summary>
        /// <param name="maxLength"><see cref="string.Length"/>で判定する最大長。</param>
        public MaxLengthAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }
    }
}
