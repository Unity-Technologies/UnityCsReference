// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.DataContract
{
    public enum UpdateMode
    {
        Automatic,
        Periodic,
        Manual
    }

    public interface IEditorModule : IDisposable
    {
        PackageInfo moduleInfo { get; set; }
        void Initialize();
        void Shutdown(bool wait);
    }

    public interface IPackageManagerModule : IEditorModule
    {
        string editorInstallPath { get; set; }
        string unityVersion { get; set; }
        UpdateMode updateMode { get; set; }
        IEnumerable<PackageInfo> playbackEngines { get; }
        IEnumerable<PackageInfo> unityExtensions { get; }

        void CheckForUpdates();
        void LoadPackage(PackageInfo package);
        void SelectPackage(PackageInfo package);
    }
}
