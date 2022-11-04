// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal struct Utilities
    {
        static internal void AddToList<T>(ref List<T> list, T value)
        {
            if (list == null)
                list = new List<T>();
            list.Add(value);
        }
    }
}
