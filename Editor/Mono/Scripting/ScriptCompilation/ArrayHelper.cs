// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal class ArrayHelper
    {
        public static T[] Merge<T>(params T[][] arrays)
        {
            int arraySize = 0;
            foreach (var array in arrays)
            {
                if (array == null)
                {
                    continue;
                }

                arraySize += array.Length;
            }

            var result = new T[arraySize];
            int index = 0;
            foreach (var array in arrays)
            {
                if (array == null)
                {
                    continue;
                }

                Array.Copy(array, 0, result, index, array.Length);
                index += array.Length;
            }

            return result;
        }
    }
}
