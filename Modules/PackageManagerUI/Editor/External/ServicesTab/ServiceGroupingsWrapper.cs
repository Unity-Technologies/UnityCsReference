// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    class ServiceGroupingsWrapper
    {
        public string name;
        public int index;
        public List<string> packages;
    }
}
