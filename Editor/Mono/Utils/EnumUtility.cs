// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEditor
{
    static class EnumUtility
    {
        [RequiredByNativeCode]
        private static string ConvertEnumToString(Type enumType, int enumValue)
        {
            return Enum.GetName(enumType, enumValue);
        }
    }
}
