#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// Inspectorで選択可能なCSVスキーマを収集します。
    /// </summary>
    internal static class CsvSchemaTypeDiscovery
    {
        internal const string LegacyFieldsNamespace = "CSV4Unity.Fields";

        internal static List<Type> FindAll()
        {
            var schemas = new HashSet<Type>();

            foreach (Type type in TypeCache.GetTypesWithAttribute<CsvSchemaAttribute>())
            {
                if (type.IsEnum) schemas.Add(type);
            }

            // v0.xとの互換性を保つため、旧名前空間規約も候補へ含める。
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                foreach (Type type in GetLoadableTypes(assemblies[i]))
                {
                    if (IsLegacySchema(type)) schemas.Add(type);
                }
            }

            return schemas
                .OrderBy(type => type.FullName ?? type.Name, StringComparer.Ordinal)
                .ToList();
        }

        internal static bool IsLegacySchema(Type type)
        {
            return type != null &&
                   type.IsEnum &&
                   !type.IsDefined(typeof(CsvSchemaAttribute), false) &&
                   type.Namespace != null &&
                   (type.Namespace == LegacyFieldsNamespace ||
                    type.Namespace.StartsWith(LegacyFieldsNamespace + ".", StringComparison.Ordinal));
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }
    }
}
#endif
