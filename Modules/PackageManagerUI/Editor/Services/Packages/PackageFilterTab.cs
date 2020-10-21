// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal enum PackageFilterTab
    {
        Unity = 0,
        Local = 1,
        Modules = 2,
        AssetStore = 3,
        InDevelopment = Custom, // Used by UPM develop package
        Custom = 4,
        Other = 5
    }
}
