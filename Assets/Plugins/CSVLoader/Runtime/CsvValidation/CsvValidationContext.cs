using System;
using System.Collections.Generic;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// ForeignKey検証で参照する別CSVテーブルを保持します。
    /// </summary>
    /// <remarks>参照先はEnum型をキーとして保持し、同じ型を再登録した場合は後のテーブルで置き換えます。</remarks>
    public sealed class CsvValidationContext
    {
        private readonly Dictionary<Type, CsvDocument> _documents = new Dictionary<Type, CsvDocument>();

        /// <summary>Enum型を識別子として参照先テーブルを登録します。</summary>
        /// <typeparam name="TField">参照先テーブルの列を表すEnum型。</typeparam>
        /// <param name="table">登録する参照先テーブル。</param>
        /// <returns>連続して登録できるよう、このContext自身を返します。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="table"/>が<see langword="null"/>です。</exception>
        public CsvValidationContext Register<TField>(CsvTable<TField> table) where TField : struct, Enum
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            _documents[typeof(TField)] = table.Document;
            return this;
        }

        internal bool TryGetColumn(Type enumType, string fieldName, out CsvColumn column)
        {
            if (_documents.TryGetValue(enumType, out CsvDocument document))
            {
                try
                {
                    column = document.Column(fieldName);
                    return true;
                }
                catch (KeyNotFoundException)
                {
                }
            }

            column = default;
            return false;
        }
    }
}
