using System;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// 条件付きValidationで使用する比較方法を表します。
    /// </summary>
    public enum Compare
    {
        /// <summary>値が等しいことを判定します。</summary>
        Equal,

        /// <summary>値が等しくないことを判定します。</summary>
        NotEqual,

        /// <summary>値が比較対象より大きいことを判定します。</summary>
        GreaterThan,

        /// <summary>値が比較対象以上であることを判定します。</summary>
        GreaterThanOrEqual,

        /// <summary>値が比較対象より小さいことを判定します。</summary>
        LessThan,

        /// <summary>値が比較対象以下であることを判定します。</summary>
        LessThanOrEqual,

        /// <summary>セルが空であることを判定します。</summary>
        IsEmpty,

        /// <summary>セルが空でないことを判定します。</summary>
        IsNotEmpty,

        /// <summary>値が候補のいずれかと等しいことを判定します。</summary>
        In,

        /// <summary>値がすべての候補と異なることを判定します。</summary>
        NotIn
    }

    /// <summary>
    /// Validation属性に適用する行条件を定義します。
    /// </summary>
    /// <remarks>
    /// 同じグループに属する条件はANDとして評価されます。条件値に同じEnum型の値を指定すると、
    /// リテラルではなく同じ行の別列を参照します。数値として比較する場合は文字列ではなく数値リテラルを指定してください。
    /// 条件不成立はエラーではなく、対応するValidation属性をその行で実行しないことを意味します。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ConditionAttribute : Attribute
    {
        /// <summary>条件を適用するグループ番号を取得します。</summary>
        public int Group { get; }

        /// <summary>条件判定に使用するEnumフィールドを取得します。</summary>
        public object Field { get; }

        /// <summary>比較方法を取得します。</summary>
        public Compare Comparison { get; }

        /// <summary>比較する値またはEnumフィールドを取得します。</summary>
        public object[] Values { get; }

        /// <summary>文字列比較で大文字小文字を無視するかを取得または設定します。</summary>
        public bool IgnoreCase { get; set; }

        /// <summary>グループ0に行条件を定義します。</summary>
        /// <param name="field">条件判定に使用するEnumフィールド。</param>
        /// <param name="comparison">比較方法。</param>
        /// <param name="values">比較する値。IsEmptyとIsNotEmptyでは省略します。</param>
        public ConditionAttribute(object field, Compare comparison, params object[] values)
            : this(0, field, comparison, values)
        {
        }

        /// <summary>指定グループに行条件を定義します。</summary>
        /// <param name="group">0以上のグループ番号。</param>
        /// <param name="field">条件判定に使用するEnumフィールド。</param>
        /// <param name="comparison">比較方法。</param>
        /// <param name="values">比較する値。IsEmptyとIsNotEmptyでは省略します。</param>
        public ConditionAttribute(int group, object field, Compare comparison, params object[] values)
        {
            Group = group;
            Field = field;
            Comparison = comparison;
            Values = values ?? Array.Empty<object>();
        }
    }

    /// <summary>
    /// Validation属性に共通する条件グループを提供します。
    /// </summary>
    public abstract class CsvValidationAttribute : Attribute
    {
        /// <summary>
        /// 適用条件のグループ番号を取得または設定します。既定値は0で、同じフィールドのConditionと対応します。
        /// </summary>
        /// <remarks>対応するConditionがないグループ0は無条件です。1以上の未定義グループはスキーマエラーになります。</remarks>
        public int ConditionGroup { get; set; }
    }

    /// <summary>
    /// 列の各値が空でなく、一意であることを要求します。
    /// </summary>
    /// <remarks>値はデコード済み文字列として、大文字小文字を区別して比較されます。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class PrimaryKeyAttribute : CsvValidationAttribute
    {
    }

    /// <summary>
    /// セルが空でないことを要求します。
    /// </summary>
    /// <remarks>空文字列を未入力として扱います。空白だけの文字列は空とはみなしません。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class NotNullAttribute : CsvValidationAttribute
    {
    }

    /// <summary>
    /// セルを指定型へ変換できることを要求します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TypeConstraintAttribute : CsvValidationAttribute
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
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class RangeAttribute : CsvValidationAttribute
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
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class UniqueAttribute : CsvValidationAttribute
    {
    }

    /// <summary>
    /// セル文字列が指定した正規表現に一致することを要求します。
    /// </summary>
    /// <remarks>正規表現は<see cref="System.Text.RegularExpressions.RegexOptions.CultureInvariant"/>で生成されます。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class RegexAttribute : CsvValidationAttribute
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
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class AllowedValuesAttribute : CsvValidationAttribute
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
    /// セル文字列が指定した最小長以上であることを要求します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class MinLengthAttribute : CsvValidationAttribute
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
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class MaxLengthAttribute : CsvValidationAttribute
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
