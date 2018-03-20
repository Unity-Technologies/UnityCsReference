// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.XR
{
    internal static class DotNetHelper
    {
        public static bool TryCopyFixedArrayToList<T>(T[] fixedArrayIn, List<T> listOut)
        {
            if (fixedArrayIn == null)
                return false;

            var count = fixedArrayIn.Length;

            listOut.Clear();

            if (listOut.Capacity < count)
                listOut.Capacity = count;

            listOut.AddRange(fixedArrayIn);
            return true;
        }
    }
}
