using System;

namespace CSV4Unity
{
    /// <summary>
    /// EnumをCSVスキーマとしてUnity Editorへ登録します。
    /// </summary>
    /// <remarks>
    /// この属性はInspectorのスキーマ候補を発見するために使用します。
    /// <see cref="CsvDocument.WithFields{TField}"/>などのRuntime APIでは必須ではありません。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
    public sealed class CsvSchemaAttribute : Attribute
    {
    }
}
