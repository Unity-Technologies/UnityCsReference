// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Internal
{
    /// <summary>
    /// Adds default value information for optional parameters
    /// </summary>
    [Serializable]
    public class ExcludeFromDocsAttribute : Attribute
    {
        public ExcludeFromDocsAttribute()
        {
        }
    }
}
