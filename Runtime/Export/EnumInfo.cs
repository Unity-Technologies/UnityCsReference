// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEngine
{
    internal class EnumInfo
    {
        public string[] names;
        public int[] values;
        public string[] annotations;
        public bool isFlags;

        [UsedByNativeCode]
        internal static EnumInfo CreateEnumInfoFromNativeEnum(string[] names, int[] values, string[] annotations, bool isFlags)
        {
            EnumInfo result = new EnumInfo();

            result.names = names;
            result.values = values;
            result.annotations = annotations;
            result.isFlags = isFlags;

            return result;
        }
    }
}
