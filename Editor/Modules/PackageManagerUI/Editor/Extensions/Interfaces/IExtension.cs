// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal interface IExtension
    {
        int priority { get; set; }
        bool visible { get; set; }
        bool enabled { get; set; }
    }
}
