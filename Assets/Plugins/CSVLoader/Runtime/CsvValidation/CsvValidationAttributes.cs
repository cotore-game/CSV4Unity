using System;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// 主キー制約 - 値が一意である必要があります
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute
    {
    }

    /// <summary>
    /// NOT NULL制約 - 値が必須です
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class NotNullAttribute : Attribute
    {
    }

    /// <summary>
    /// 型制約 - 指定された型に変換可能である必要があります
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TypeConstraintAttribute : Attribute
    {
        public Type ExpectedType { get; }

        /// <summary>
        /// 型制約を設定します
        /// </summary>
        /// <param name="expectedType">期待される型（int, float, string, boolなど）</param>
        public TypeConstraintAttribute(Type expectedType)
        {
            ExpectedType = expectedType;
        }
    }

    /// <summary>
    /// 範囲制約 - 数値が指定範囲内である必要があります
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RangeAttribute : Attribute
    {
        public double Min { get; }
        public double Max { get; }

        /// <summary>
        /// 数値の範囲制約を設定します
        /// </summary>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        public RangeAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// ユニーク制約 - 値が重複してはいけません（NULLは許可）
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class UniqueAttribute : Attribute
    {
    }

    /// <summary>
    /// 正規表現制約 - 指定されたパターンに一致する必要があります
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RegexAttribute : Attribute
    {
        public string Pattern { get; }

        /// <summary>
        /// 正規表現パターンを設定します
        /// </summary>
        /// <param name="pattern">正規表現パターン</param>
        public RegexAttribute(string pattern)
        {
            Pattern = pattern;
        }
    }

    /// <summary>
    /// 列挙値制約 - 指定された値のいずれかである必要があります
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AllowedValuesAttribute : Attribute
    {
        public object[] AllowedValues { get; }

        /// <summary>
        /// 許可される値を設定します
        /// </summary>
        /// <param name="allowedValues">許可される値の配列</param>
        public AllowedValuesAttribute(params object[] allowedValues)
        {
            AllowedValues = allowedValues;
        }
    }

    /// <summary>
    /// 外部キー制約 - 他のCSVの指定列に存在する値である必要があります
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        public Type ReferenceEnumType { get; }
        public string ReferenceField { get; }

        /// <summary>
        /// 外部キー制約を設定します
        /// </summary>
        /// <param name="referenceEnumType">参照先のEnum型</param>
        /// <param name="referenceField">参照先のフィールド名</param>
        public ForeignKeyAttribute(Type referenceEnumType, string referenceField)
        {
            ReferenceEnumType = referenceEnumType;
            ReferenceField = referenceField;
        }
    }

    /// <summary>
    /// 最小長制約 - 文字列の最小長を指定します
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MinLengthAttribute : Attribute
    {
        public int MinLength { get; }

        public MinLengthAttribute(int minLength)
        {
            MinLength = minLength;
        }
    }

    /// <summary>
    /// 最大長制約 - 文字列の最大長を指定します
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MaxLengthAttribute : Attribute
    {
        public int MaxLength { get; }

        public MaxLengthAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }
    }
}
