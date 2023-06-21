// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using static UnityEngine.EnumDataUtility;

namespace UnityEditor
{
    internal static class EnumDataUtility
    {
        internal static EnumData GetCachedEnumData(Type enumType, bool excludeObsolete = true)
        {
            return UnityEngine.EnumDataUtility.GetCachedEnumData(enumType, excludeObsolete ? CachedType.ExcludeObsolete : CachedType.IncludeObsoleteExceptErrors, ObjectNames.NicifyVariableName);
        }

        internal static EnumData GetCachedEnumData(Type enumType, CachedType cachedType)
        {
            return UnityEngine.EnumDataUtility.GetCachedEnumData(enumType, cachedType, ObjectNames.NicifyVariableName);
        }

        internal static int EnumFlagsToInt(EnumData enumData, Enum enumValue)
        {
            return UnityEngine.EnumDataUtility.EnumFlagsToInt(enumData, enumValue);
        }

        internal static Enum IntToEnumFlags(Type enumType, int value)
        {
            return UnityEngine.EnumDataUtility.IntToEnumFlags(enumType, value);
        }
    }
}
