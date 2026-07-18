using System;
using System.Collections.Generic;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// ForeignKey検証で参照する別CSVテーブルを保持します。
    /// </summary>
    public sealed class CsvValidationContext
    {
        private readonly Dictionary<Type, CsvDocument> _documents = new Dictionary<Type, CsvDocument>();

        /// <summary>Enum型を識別子として参照先テーブルを登録します。</summary>
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
