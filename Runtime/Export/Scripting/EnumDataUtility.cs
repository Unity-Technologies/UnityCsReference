// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal struct EnumData
    {
        public Enum[] values;
        public int[] flagValues;
        public string[] displayNames;
        public string[] names;
        public string[] tooltip;
        public bool flags;
        public Type underlyingType;
        public bool unsigned;
        public bool serializable;
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class EnumDataUtility
    {
        private static readonly Dictionary<Type, EnumData> s_NonObsoleteEnumData = new Dictionary<Type, EnumData>();
        private static readonly Dictionary<Type, EnumData> s_EnumData = new Dictionary<Type, EnumData>();

        public static EnumData GetCachedEnumData(Type enumType, bool excludeObsolete = true, Func<string, string> nicifyName = null)
        {
            EnumData enumData;
            if (excludeObsolete && s_NonObsoleteEnumData.TryGetValue(enumType, out enumData))
            {
                return enumData;
            }

            if (!excludeObsolete && s_EnumData.TryGetValue(enumType, out enumData))
            {
                return enumData;
            }

            enumData = new EnumData { underlyingType = Enum.GetUnderlyingType(enumType) };
            enumData.unsigned =
                enumData.underlyingType == typeof(byte)
                || enumData.underlyingType == typeof(ushort)
                || enumData.underlyingType == typeof(uint)
                || enumData.underlyingType == typeof(ulong);
            var enumFields = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
            List<FieldInfo> enumfieldlist = new List<FieldInfo>();
            int enumFieldslen = enumFields.Length;
            for (int j = 0; j < enumFieldslen; j++)
            {
                if (CheckObsoleteAddition(enumFields[j], excludeObsolete))
                    enumfieldlist.Add(enumFields[j]);
            }

            // For Empty List Scenario
            if (!enumfieldlist.Any())
            {
                string[] defaultstr = { "" };
                Enum[] defaultenum = {};
                int[] defaultarr = { 0 };
                enumData.values = defaultenum;
                enumData.flagValues = defaultarr;
                enumData.displayNames = defaultstr;
                enumData.names = defaultstr;
                enumData.tooltip = defaultstr;
                enumData.flags = true;
                enumData.serializable = true;
                return enumData;
            }

            // We can't order the enum from its MetadataToken if its Assembly Dynamic because of a bug in .NET
            try
            {
                var location = enumfieldlist.First().Module.Assembly.Location;
                if (!string.IsNullOrEmpty(location))
                {
                    enumfieldlist = enumfieldlist.OrderBy(f => f.MetadataToken).ToList();
                }
            }
            catch
            {
                // ignored
            }

            enumData.displayNames = enumfieldlist.Select(f => EnumNameFromEnumField(f, nicifyName)).ToArray();
            if (enumData.displayNames.Distinct().Count() != enumData.displayNames.Length)
            {
                Debug.LogWarning(
                    $"Enum {enumType.Name} has multiple entries with the same display name, this prevents selection in EnumPopup.");
            }

            enumData.tooltip = enumfieldlist.Select(f => EnumTooltipFromEnumField(f)).ToArray();
            enumData.values = enumfieldlist.Select(f => (Enum)f.GetValue(null)).ToArray();
            enumData.flagValues = enumData.unsigned
                ? enumData.values.Select(v => unchecked((int)Convert.ToUInt64(v))).ToArray()
                : enumData.values.Select(v => unchecked((int)Convert.ToInt64(v))).ToArray();

            // We use the actual names of the enums for ordering options in the UI, so we cache the values with the rest
            // of its data to avoid doing it repeatedly.
            enumData.names = new string[enumData.values.Length];
            for (int i = 0; i < enumData.values.Length; ++i)
            {
                enumData.names[i] = enumData.values[i].ToString();
            }

            // convert "everything" values to ~0 for unsigned 8- and 16-bit types
            if (enumData.underlyingType == typeof(ushort))
            {
                for (int i = 0, length = enumData.flagValues.Length; i < length; ++i)
                {
                    if (enumData.flagValues[i] == 0xFFFFu)
                        enumData.flagValues[i] = ~0;
                }
            }
            else if (enumData.underlyingType == typeof(byte))
            {
                for (int i = 0, length = enumData.flagValues.Length; i < length; ++i)
                {
                    if (enumData.flagValues[i] == 0xFFu)
                        enumData.flagValues[i] = ~0;
                }
            }

            enumData.flags = enumType.IsDefined(typeof(FlagsAttribute), false);
            enumData.serializable = enumData.underlyingType != typeof(long) && enumData.underlyingType != typeof(ulong);

            HandleInspectorOrderAttribute(enumType, ref enumData);

            if (excludeObsolete)
                s_NonObsoleteEnumData[enumType] = enumData;
            else
                s_EnumData[enumType] = enumData;

            return enumData;
        }

        public static int EnumFlagsToInt(EnumData enumData, Enum enumValue)
        {
            if (enumData.unsigned)
            {
                if (enumData.underlyingType == typeof(uint))
                    return unchecked((int)Convert.ToUInt32(enumValue));

                // ensure unsigned 16- and 8-bit variants will display using "Everything" label
                if (enumData.underlyingType == typeof(ushort))
                {
                    var unsigned = Convert.ToUInt16(enumValue);
                    return unsigned == ushort.MaxValue ? ~0 : unsigned;
                }
                else
                {
                    var unsigned = Convert.ToByte(enumValue);
                    return unsigned == byte.MaxValue ? ~0 : unsigned;
                }
            }

            return Convert.ToInt32(enumValue);
        }

        public static Enum IntToEnumFlags(Type enumType, int value)
        {
            var enumData = GetCachedEnumData(enumType);

            // parsing a string seems to be the only way to go from a flags int to an enum value
            if (enumData.unsigned)
            {
                if (enumData.underlyingType == typeof(uint))
                {
                    var unsigned = unchecked((uint)value);
                    return Enum.Parse(enumType, unsigned.ToString()) as Enum;
                }
                else if (enumData.underlyingType == typeof(ushort))
                {
                    var unsigned = unchecked((ushort)value);
                    return Enum.Parse(enumType, unsigned.ToString()) as Enum;
                }
                else
                {
                    var unsigned = unchecked((byte)value);
                    return Enum.Parse(enumType, unsigned.ToString()) as Enum;
                }
            }

            return Enum.Parse(enumType, value.ToString()) as Enum;
        }

        public static void HandleInspectorOrderAttribute(Type enumType, ref EnumData enumData)
        {
            var attribute = Attribute.GetCustomAttribute(enumType, typeof(InspectorOrderAttribute)) as InspectorOrderAttribute;
            if (attribute == null)
                return;

            int size = enumData.displayNames.Length;
            int[] indexes = new int[size];

            for (int i = 0; i < size; i++)
            {
                indexes[i] = i;
            }

            switch (attribute.m_inspectorSort)
            {
                case InspectorSort.ByValue:
                    int[] data = new int[size];
                    Array.Copy(enumData.flagValues, data, size);
                    Array.Sort(data, indexes);
                    break;
                default:
                    string[] sortData = new string[size];
                    Array.Copy(enumData.displayNames, sortData, size);
                    Array.Sort(sortData, indexes, StringComparer.Ordinal);
                    break;
            }

            if (attribute.m_sortDirection == InspectorSortDirection.Descending)
                Array.Reverse(indexes);

            Enum[] values = new Enum[size];
            int[] flagValues = new int[size];
            string[] displayNames = new string[size];
            string[] names = new string[size];
            string[] tooltip = new string[size];

            for (int i = 0; i < size; i++)
            {
                int index = indexes[i];
                values[i] = enumData.values[index];
                flagValues[i] = enumData.flagValues[index];
                displayNames[i] = enumData.displayNames[index];
                names[i] = enumData.names[index];
                tooltip[i] = enumData.tooltip[index];
            }

            enumData.values = values;
            enumData.flagValues = flagValues;
            enumData.displayNames = displayNames;
            enumData.names = names;
            enumData.tooltip = tooltip;
        }

        private static bool CheckObsoleteAddition(FieldInfo field, bool excludeObsolete)
        {
            var obsolete = field.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            if (obsolete.Length > 0)
            {
                if (excludeObsolete)
                {
                    return false;
                }

                return !((ObsoleteAttribute)obsolete.First()).IsError;
            }

            return true;
        }

        private static string EnumTooltipFromEnumField(FieldInfo field)
        {
            var tooltip = field.GetCustomAttributes(typeof(TooltipAttribute), false);
            if (tooltip.Length > 0)
            {
                return ((TooltipAttribute)tooltip.First()).tooltip;
            }

            return string.Empty;
        }

        private static string EnumNameFromEnumField(FieldInfo field, Func<string, string> nicifyName)
        {
            var description = field.GetCustomAttributes(typeof(InspectorNameAttribute), false);
            if (description.Length > 0)
            {
                return ((InspectorNameAttribute)description.First()).displayName;
            }

            string NicifyName()
            {
                return nicifyName == null ? field.Name : nicifyName.Invoke(field.Name);
            }

            if (field.IsDefined(typeof(ObsoleteAttribute), false))
            {
                return $"{NicifyName()} (Obsolete)";
            }

            return NicifyName();
        }
    }
}
